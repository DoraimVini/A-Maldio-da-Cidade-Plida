using UnityEngine;
using FavelaAmarela.Core.Abilities;
using FavelaAmarela.Core.Combat;

namespace FavelaAmarela.Runtime.Enemies
{
    public class EnemyCombat : MonoBehaviour
    {
        [Header("Ataque")]
        [SerializeField] private float alcanceDeGolpe = 1.2f;
        [SerializeField] private float cadenciaDeAtaque = 1.2f;
        [Tooltip("Dano do golpe. Fallback: se houver EnemyBase com ficha no mesmo objeto, " +
                 "quem manda é o Ataque DA FICHA -- porque é ele que escala com o nível da " +
                 "unidade. Este campo passa a valer só para inimigo sem ficha.")]
        [SerializeField] private float danoDoGolpe = 20f;
        [Tooltip("Quem este inimigo aceita como alvo. Vazio = Jogador + Aliados.")]
        [SerializeField] private LayerMask camadasAlvo;

        /// <summary>
        /// Alvos legítimos de um inimigo: o jogador e quem luta do lado dele.
        ///
        /// <para>A layer <b>Aliados</b> nasceu em 2026-08-11: até então Yug-Neth estava na
        /// layer <c>Enemy</c> e os inimigos só procuravam <c>Player</c> — ou seja, o
        /// companheiro era <b>invisível para eles</b> e toda a mecânica de incapacitação
        /// (cair, bloquear os Portões, ser reanimado num Refúgio) nunca disparava.</para>
        ///
        /// <para>É layer própria, e não "põe o aliado em Player", porque o jogo vai ganhar
        /// outros aliados e NPCs ao longo da campanha — e porque coisas que miram só em
        /// Damião (câmera, gatilhos de quest) não podem passar a mirar neles junto.</para>
        /// </summary>
        private static readonly string[] LayersDeAlvo = { "Player", "Aliados" };

        private float _attackCooldown;
        private EnemyBase _corpo;

        /// <summary>
        /// O dano que este inimigo causa: o <b>Ataque da ficha</b> quando existe, e só então o
        /// campo local.
        ///
        /// <para><b>O defeito que isto fecha (2026-08-28).</b> A ficha do Cultista autora
        /// <c>Ataque 14</c> e este componente batia com <c>20</c>. Eram <b>dois números
        /// independentes mantidos à mão</b>, e o da ficha era dado morto: rebalancear pela ficha
        /// não mudava nada em jogo, e a <c>ficha_de_atributos.md</c> documentava contas
        /// baseadas num número que ninguém usava.</para>
        ///
        /// <para>E é o que faz o <c>nivelDaUnidade</c> valer: o Ataque escala com o nível pela
        /// <c>EscalaDeNivel</c>, o campo local não escala com nada.</para>
        /// </summary>
        private float DanoEfetivo =>
            _corpo != null && _corpo.Atributos != null && _corpo.Atributos.Ataque > 0f
                ? _corpo.Atributos.Ataque
                : danoDoGolpe;
        private readonly Collider2D[] _bufferAlvo = new Collider2D[4];
        private ContactFilter2D _filtroAlvo;
        private IDanificavel _alvoCache;

        public event System.Action OnAtaqueDesferido;
        public bool EstaPronto => _attackCooldown <= 0f;

        private void Awake()
        {
            // O corpo carrega a ficha -- e com ela o Ataque autorado, que é a fonte da verdade
            // do dano deste componente (ver DanoEfetivo).
            _corpo = GetComponent<EnemyBase>();

            int padrao = LayerMask.GetMask(LayersDeAlvo);

            // Prefabs salvos antes da layer Aliados existir guardam uma máscara que só tem
            // Player. Reaproveitá-la deixaria o companheiro invisível de novo — e em silêncio,
            // que é o pior modo de um alvo não ser alvo. Só respeitamos a máscara autorada se
            // ela já enxergar algum aliado.
            bool autoradaEnxergaAliados = (camadasAlvo.value & LayerMask.GetMask("Aliados")) != 0;
            if (camadasAlvo.value == 0 || !autoradaEnxergaAliados) camadasAlvo = padrao;

            _filtroAlvo = new ContactFilter2D { useTriggers = true };
            _filtroAlvo.SetLayerMask(camadasAlvo);
        }

        private void Update()
        {
            if (_attackCooldown > 0f) _attackCooldown -= Time.deltaTime;
        }

        [Header("Janela do golpe")]
        [Tooltip("Segundos que a área de acerto fica aberta. NÃO é a cadência: é quanto tempo " +
                 "o golpe existe, e portanto quanto tempo há para esquivar dele.")]
        [Min(0.05f)]
        [SerializeField] private float janelaDoGolpe = 0.15f;

        [Tooltip("Raio da área atingida, em unidades de mundo.")]
        [Min(0.1f)]
        [SerializeField] private float raioDoGolpe = 0.6f;

        private FavelaAmarela.Runtime.Combat.Hitbox _hitbox;

        private void GarantirHitbox()
        {
            if (_hitbox != null) return;

            _hitbox = FavelaAmarela.Runtime.Combat.Hitbox.GarantirPara(
                gameObject, "Hitbox_Inimigo", LayerMask.GetMask("PlayerHurtbox"),
                raioDoGolpe, alcanceDeGolpe, pouparAliados: false,
                profundidade: FavelaAmarela.Runtime.Combat.Hitbox.ProfundidadeDeUmaCelula);
        }

        public bool AlvoEstaAoAlcance()
        {
            if (_alvoCache != null)
            {
                if (_alvoCache is MonoBehaviour mb && mb == null) _alvoCache = null;
                else if (_alvoCache is IDanificavel && !((MonoBehaviour)_alvoCache).enabled) _alvoCache = null;
            }

            // Esta consulta e DETECCAO, nao dano: ela responde "tem alguem ao alcance?" para a
            // FSM decidir entre Chase e Attack. Radial esta certo aqui -- proximidade nao tem
            // direcao.
            //
            // Ate 2026-09-04 ela era tambem o GOLPE: TentarAtacar aplicava dano direto no alvo
            // que esta consulta cacheava, sem janela e sem direcao. Consequencia: nao dava para
            // esquivar no tempo (o golpe existia por um quadro so) e estar ATRAS do Cultista
            // nao protegia. O dano passou para uma Hitbox com janela; ver TentarAtacar.
            FavelaAmarela.Runtime.Diagnostico.VisualizadorDeGolpes.RegistrarCirculo(
                transform.position, alcanceDeGolpe,
                FavelaAmarela.Runtime.Diagnostico.VisualizadorDeGolpes.CorDeGolpe);

            int total = Physics2D.OverlapCircle(transform.position, alcanceDeGolpe, _filtroAlvo, _bufferAlvo);
            if (total <= 0) { _alvoCache = null; return false; }

            if (_alvoCache == null)
            {
                for (int i = 0; i < total; i++)
                {
                    _alvoCache = _bufferAlvo[i].GetComponentInParent<IDanificavel>();
                    if (_alvoCache != null) break;
                }
            }
            return _alvoCache != null;
        }

        /// <summary>
        /// Desfere o golpe. <b>Abre uma janela</b> em vez de aplicar dano no mesmo quadro.
        ///
        /// <para>É a diferença entre um golpe que se pode esquivar e um teste de posição: com
        /// janela, atravessar a área <i>durante</i> o golpe acerta, e sair antes de ela abrir
        /// não acerta. E com direção, estar atrás do inimigo protege — o que o golpe radial
        /// anterior não permitia.</para>
        /// </summary>
        public bool TentarAtacar()
        {
            if (!EstaPronto || !AlvoEstaAoAlcance()) return false;

            _attackCooldown = cadenciaDeAtaque;
            GarantirHitbox();

            var golpe = new ArmaResult(true, 0f, 0f, false, 0f, DanoEfetivo);

            if (_hitbox != null)
            {
                Vector2 direcao = DirecaoAteOAlvo();
                _hitbox.Armar(golpe, janelaDoGolpe, direcao);
                OnAtaqueDesferido?.Invoke();
                return true;
            }

            // Reserva: sem hitbox o inimigo ficaria inofensivo, o que e pior que o golpe
            // antigo. Falha alto para nao virar "o Cultista parou de bater" em playtest.
            Debug.LogError($"[EnemyCombat] '{name}' esta sem Hitbox -- o golpe caiu para o " +
                           "acerto instantaneo, que nao e esquivavel no tempo.", this);
            _alvoCache?.ReceberGolpe(golpe);
            OnAtaqueDesferido?.Invoke();
            return true;
        }

        /// <summary>Direção até o alvo cacheado; zero se ele sumiu (a Hitbox trata como radial).</summary>
        private Vector2 DirecaoAteOAlvo()
        {
            if (_alvoCache is MonoBehaviour mb && mb != null)
                return ((Vector2)mb.transform.position - (Vector2)transform.position).normalized;

            return Vector2.zero;
        }
    }
}
