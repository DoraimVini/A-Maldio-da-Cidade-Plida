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

        [Tooltip("Golpe do jogador: nunca acerta quem carrega o marcador Aliado. " +
                 "Golpe de inimigo: DESLIGADO — o Byakhee pode e deve derrubar o companheiro.")]
        [SerializeField] private bool pouparAliados;

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

        private void Awake() => ReconstruirFiltro();

        private void ReconstruirFiltro()
        {
            _filtro = new ContactFilter2D();
            // Hurtboxes são triggers, então sem isto a consulta não acharia nenhuma.
            _filtro.useTriggers = true;
            _filtro.SetLayerMask(camadasAlvo);
        }

        /// <summary>
        /// Define a geometria do golpe a partir de dado — o alcance, o raio e a camada que
        /// esta hitbox pode acertar.
        ///
        /// <para>Existe porque a geometria deixou de ser propriedade do <b>ator</b> e passou a
        /// ser propriedade da <b>arma</b> (ver <c>BaseDeArma</c>). Antes de 2026-08-27 havia um
        /// <c>alcance = 1.2f</c> no <c>MaoFisicaBridge</c> valendo para todas as armas, e por
        /// isso um estilete e um alfanje tinham a mesma pegada.</para>
        /// </summary>
        /// <param name="raioDoGolpe">Raio da área atingida.</param>
        /// <param name="distanciaAFrente">Do corpo até o centro da área.</param>
        /// <param name="camadas">Camadas de hurtbox que este golpe alcança.</param>
        public void Configurar(float raioDoGolpe, float distanciaAFrente, LayerMask camadas)
        {
            raio = Mathf.Max(0.05f, raioDoGolpe);

            // Preserva a direção corrente e troca só a distância: `Armar` gira o deslocamento
            // mantendo a magnitude, então uma magnitude zero aqui faria a direção do golpe ser
            // ignorada para sempre — e o golpe sairia sempre em cima do próprio ator.
            Vector2 direcao = deslocamento.sqrMagnitude > 0.0001f
                ? deslocamento.normalized
                : Vector2.right;

            deslocamento = direcao * Mathf.Max(0.01f, distanciaAFrente);

            camadasAlvo = camadas;
            ReconstruirFiltro();
        }

        /// <summary>
        /// Acha (ou cria) a hitbox de um ator, já configurada.
        ///
        /// <para>Nasce <b>inativa</b> e só é ligada depois de configurada — mesmo padrão de
        /// <c>CarcosaDebuggerWindow.CriarCorpoDoChefe</c> —, para nenhum <c>Awake</c> rodar
        /// antes de a camada alvo existir.</para>
        /// </summary>
        public static Hitbox GarantirPara(GameObject dono, string nome, LayerMask camadas,
                                          float raioDoGolpe, float distanciaAFrente,
                                          bool pouparAliados = false)
        {
            if (dono == null) return null;

            foreach (var existente in dono.GetComponentsInChildren<Hitbox>(true))
            {
                if (existente.gameObject.name != nome) continue;
                existente.pouparAliados = pouparAliados;
                existente.Configurar(raioDoGolpe, distanciaAFrente, camadas);
                return existente;
            }

            var go = new GameObject(nome);
            go.SetActive(false);
            go.transform.SetParent(dono.transform, false);

            var hitbox = go.AddComponent<Hitbox>();
            hitbox.pouparAliados = pouparAliados;
            hitbox.Configurar(raioDoGolpe, distanciaAFrente, camadas);

            go.SetActive(true);
            return hitbox;
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
            // A checagem mora AQUI, e não no Awake, desde 2026-08-27: uma hitbox construída em
            // runtime é configurada depois de existir, então reclamar no Awake acusaria toda
            // hitbox nova por um estado que dura microssegundos. Aqui a reclamação é verdadeira:
            // é o instante em que um golpe sai sem poder acertar nada.
            if (camadasAlvo.value == 0)
                Debug.LogError($"[Hitbox] '{name}' foi armada sem camada alvo — este golpe não " +
                               "pode acertar nada.", this);

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

                // Aliados (Yug-Neth e companheiros futuros) nunca são atingidos pelo golpe do
                // jogador -- nem por acidente no meio de uma luta. A taxonomia de layers do
                // projeto é um conjunto fechado, então quem protege é este MARCADOR, não uma
                // camada própria (ver YugNethAI). A regra é do golpe e não da hitbox: o Byakhee
                // pode e deve derrubar o companheiro.
                if (pouparAliados && hurtbox.GetComponentInParent<Aliado>() != null) continue;

                if (!_jaAtingidos.Add(hurtbox)) continue;

                hurtbox.Receber(_resultado);

                // Corpo, do lado de quem APANHA. Este é o caminho pelo qual o inimigo acerta
                // o jogador (hoje só as garras do Byakhee); o caminho oposto -- o golpe do
                // Damião -- é resolvido em MaoFisicaBridge.ResolverGolpe. São os dois únicos
                // pontos onde um golpe aterrissa, e por isso os dois únicos que precisam
                // conhecer a repulsão: nenhum dos 9 arquivos de IA foi tocado.
                //
                // A direção sai do CENTRO da hitbox para o alvo, e não da direção do golpe:
                // quem está na borda do círculo tem de ser jogado para fora, não para o lado.
                var empurrao = RepulsaoDeImpacto.GarantirPara(hurtbox);
                if (empurrao != null)
                    empurrao.Empurrar((Vector2)hurtbox.transform.position - Centro,
                                      _resultado.ForcaRepulsao);

                HitStop.Bater(_resultado.Dano);
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
