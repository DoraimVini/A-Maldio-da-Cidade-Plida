using System.Collections.Generic;
using UnityEngine;
using FavelaAmarela.Core.Abilities;

namespace FavelaAmarela.Runtime.Combat
{
    /// <summary>
    /// A área que <b>causa</b> dano, ligada só durante os <b>quadros ativos</b> do golpe.
    ///
    /// <para><b>O defeito que isto conserta (2026-08-21).</b> O Vini relatou que a luta contra o
    /// Byakhee "não tem feel bom". Achados os motivos um a um — o chefe sem colisor (era
    /// inacertável), o golpe sem som, a física girando — sobrou o mais importante: <b>o dano dos
    /// inimigos era um teste de distância instantâneo</b>. O <c>ByakheeAI.GolpearComGarras</c>
    /// rodava <b>uma vez</b>, na entrada do estado <c>Pousado</c>, e fazia
    /// <c>Vector2.Distance &lt;= alcance</c>.</para>
    ///
    /// <para>Isso quebra o combate de ARPG em dois pontos:</para>
    /// <list type="number">
    ///   <item><b>Não há janela.</b> Sendo um teste de um quadro só, não existe "esquivar no
    ///   tempo certo" — só "estar longe naquele instante exato". O mergulho telegrafado vira
    ///   decoração: o jogador não tem o que ler nem quando reagir.</item>
    ///   <item><b>Não há direção.</b> Distância é radial. Estar <b>atrás</b> do Byakhee, a 1,4
    ///   unidade, levava garrada igual. Lê como injustiça, porque contradiz o que se vê.</item>
    /// </list>
    ///
    /// <para><b>Como esta classe resolve:</b> <see cref="Armar"/> abre uma janela de
    /// <c>duracaoAtiva</c> segundos. Enquanto ela dura, a área é consultada a cada
    /// <c>FixedUpdate</c> — então atravessar a área <i>durante</i> o golpe acerta, e sair antes
    /// da janela abrir (ou entrar depois de fechar) não acerta. É isso que torna a esquiva uma
    /// decisão de tempo em vez de um teste de posição.</para>
    ///
    /// <para><b>Por que consulta e não colisor com trigger:</b> um trigger só dispara
    /// <c>OnTriggerEnter2D</c> em quem <b>entra</b>. Se a janela abre com o alvo <b>já</b>
    /// sobreposto — o caso mais comum, porque o inimigo mira em quem está perto — o evento nunca
    /// vem e o golpe passa branco. A consulta explícita pega ambos os casos. Usa
    /// <c>Physics2D.OverlapCircle</c> com <c>ContactFilter2D</c>, o mesmo padrão que
    /// <c>MaoFisicaBridge.ResolverGolpe</c> já usa neste projeto.</para>
    ///
    /// <para><b>Uma vez por ativação:</b> cada alvo atingido entra num conjunto que só é limpo
    /// na próxima <see cref="Armar"/>. Sem isso, uma janela de 0,2 s a 50 fps aplicaria o dano
    /// dez vezes.</para>
    /// </summary>
    [AddComponentMenu("Favela Amarela/Combate/Hitbox (área que causa dano)")]
    public sealed class Hitbox : MonoBehaviour
    {
        [Header("Forma")]
        [Tooltip("Raio da área de acerto, em unidades de mundo.")]
        [Min(0.05f)]
        [SerializeField] private float raio = 1f;

        [Tooltip("Deslocamento a partir deste objeto. Aponta para a frente do golpe.")]
        [SerializeField] private Vector2 deslocamento = Vector2.zero;

        [Header("Alvo")]
        [Tooltip("Camadas de hurtbox que este golpe pode atingir " +
                 "(PlayerHurtbox para inimigos, EnemyHurtbox para o jogador).")]
        [SerializeField] private LayerMask camadasAlvo;

        // Buffer reusado: alocar por golpe seria lixo em hot path (Regra de Ouro 1).
        private readonly Collider2D[] _buffer = new Collider2D[16];
        private readonly HashSet<Hurtbox> _jaAtingidos = new HashSet<Hurtbox>();

        private ContactFilter2D _filtro;
        private ArmaResult _resultado;
        private float _fimDaJanela;
        private bool _ativa;

        /// <summary>Se a janela de acerto está aberta agora.</summary>
        public bool Ativa => _ativa;

        /// <summary>Centro da área em coordenadas de mundo.</summary>
        public Vector2 Centro => (Vector2)transform.position + deslocamento;

        private void Awake()
        {
            _filtro = new ContactFilter2D();
            // Hurtboxes são triggers, então sem isto a consulta não acharia nenhuma.
            _filtro.useTriggers = true;
            _filtro.SetLayerMask(camadasAlvo);

            if (camadasAlvo.value == 0)
                Debug.LogError($"[Hitbox] '{name}' está sem camada alvo — este golpe não pode " +
                               "acertar nada.", this);
        }

        /// <summary>
        /// Abre a janela de acerto por <paramref name="duracaoAtiva"/> segundos, carregando o
        /// <paramref name="resultado"/> que será aplicado em quem for atingido.
        /// </summary>
        /// <param name="direcao">
        /// Para onde o golpe aponta. Gira o <see cref="deslocamento"/> para essa direção, que é
        /// o que dá <b>frente</b> ao ataque — atrás do atacante deixa de ser zona de dano.
        /// Passe <c>Vector2.zero</c> para manter o deslocamento como está (golpe radial, ex.: um
        /// baque de pouso).
        /// </param>
        public void Armar(ArmaResult resultado, float duracaoAtiva, Vector2 direcao = default)
        {
            _resultado = resultado;
            _fimDaJanela = Time.time + Mathf.Max(0f, duracaoAtiva);
            _ativa = true;
            _jaAtingidos.Clear();

            if (direcao.sqrMagnitude > 0.0001f)
            {
                float distancia = deslocamento.magnitude;
                if (distancia > 0.0001f) deslocamento = direcao.normalized * distancia;
            }

            // Consulta já neste quadro: uma janela curta poderia fechar antes do próximo
            // FixedUpdate e o golpe sairia sem nunca ter sido testado.
            Consultar();
        }

        /// <summary>Fecha a janela antes do tempo (interrupção, atordoamento, morte).</summary>
        public void Desarmar() => _ativa = false;

        private void FixedUpdate()
        {
            if (!_ativa) return;

            if (Time.time >= _fimDaJanela)
            {
                _ativa = false;
                return;
            }

            Consultar();
        }

        private void Consultar()
        {
            int total = Physics2D.OverlapCircle(Centro, raio, _filtro, _buffer);

            for (int i = 0; i < total; i++)
            {
                var hurtbox = _buffer[i].GetComponent<Hurtbox>();
                if (hurtbox == null) hurtbox = _buffer[i].GetComponentInParent<Hurtbox>();
                if (hurtbox == null) continue;

                // Não se fere a si mesmo: a hurtbox do próprio dono está na mesma hierarquia.
                if (hurtbox.transform.IsChildOf(transform.root)) continue;

                if (!_jaAtingidos.Add(hurtbox)) continue;

                hurtbox.Receber(_resultado);
            }
        }

        /// <summary>
        /// Desenha a área no Scene view: vermelha enquanto ativa, cinza em repouso. Uma hitbox
        /// invisível é impossível de calibrar — e calibrar é justamente o trabalho de dar feel.
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Application.isPlaying && _ativa
                ? new Color(1f, 0.2f, 0.2f, 0.9f)
                : new Color(0.6f, 0.6f, 0.6f, 0.4f);

            Gizmos.DrawWireSphere(Centro, raio);
        }
    }
}
