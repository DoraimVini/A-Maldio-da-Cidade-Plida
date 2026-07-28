using System;
using UnityEngine;
using FavelaAmarela.Core.Abilities;
using FavelaAmarela.Core.Player;
using FavelaAmarela.Runtime.Config;
using FavelaAmarela.Runtime.Enemies;

namespace FavelaAmarela.Player
{
    /// <summary>
    /// MonoBehaviour Bridge conectando a arma equipada na Mão Física (hoje fixa em
    /// <see cref="BarraEnferrujada"/>) à Unity. Espelha <see cref="EsquivaBridge"/>
    /// e <see cref="AnomalyPowerBridge"/>: instancia o POCO em Awake, expõe
    /// TryAtacar() pro <see cref="PlayerMovement"/> chamar, e resolve o próprio
    /// golpe (quem foi atingido) via <c>Physics2D.OverlapCircleAll</c> — arma
    /// física não tem custo de Resiliência Mental nem atravessa paredes.
    /// </summary>
    [AddComponentMenu("Favela Amarela/Mao Fisica Bridge")]
    public class MaoFisicaBridge : MonoBehaviour
    {
        [Header("Configuração da Arma")]
        [Tooltip("Asset de tunagem da Barra Enferrujada. Se vazio, usa os defaults do POCO.")]
        [SerializeField] private BarraEnferrujadaConfig config;

        [Header("Alcance do Golpe")]
        [SerializeField] private float alcance = 1.2f;
        [SerializeField] private LayerMask camadaInimigos;

        [Header("Progressão")]
        [Tooltip("Ligar só para testar o combate isolado. No jogo real, Damião começa DESARMADO — a arma é adquirida junto do patuá na Zona 5 (ver DesbloquearArma).")]
        [SerializeField] private bool desbloqueadaNoInicio = false;

        private IArma armaEquipada;
        private float lastUseTime = -999f;
        private bool _armaDesbloqueada;
        private PlayerStateMachine _fsm;

        // Buffer pré-alocado + filtro para resolver o golpe sem alocar lixo por ataque
        // (Regra de Ouro 1). 8 slots cobrem o alcance melee — se mais de 8 inimigos se
        // sobrepuserem no raio, o excedente é ignorado, o que é aceitável para um golpe corpo-a-corpo.
        private readonly Collider2D[] _hitBuffer = new Collider2D[8];
        private ContactFilter2D _filtroInimigos;

        /// <summary>Direção e duração do golpe ativado.</summary>
        public event Action<Vector2, float> OnAtaqueExecutado;

        /// <summary>true enquanto a FSM do jogador estiver no estado Atacando (fonte única de verdade).</summary>
        public bool IsAtacando => _fsm != null && _fsm.CurrentState == PlayerState.Atacando;

        /// <summary>Injeta a FSM de estado do jogador (chamado por <see cref="PlayerMovement"/> no Awake).</summary>
        public void BindStateMachine(PlayerStateMachine fsm) => _fsm = fsm;

        /// <summary>Se a arma da Mão Física já foi adquirida (ver <see cref="DesbloquearArma"/>).</summary>
        public bool ArmaDesbloqueada => _armaDesbloqueada;

        /// <summary>
        /// Equipa uma arma na Mão Física permanentemente. Chamado pelo pickup da
        /// arma inicial na Zona 5 — Damião não nasce armado; toda a primeira metade
        /// do jogo é desarmada, só furtividade.
        /// </summary>
        public void DesbloquearArma() => _armaDesbloqueada = true;

        private void Awake()
        {
            if (config != null)
            {
                armaEquipada = new BarraEnferrujada(config.Duration, config.Cooldown, config.ProbabilidadeAtordoar, config.DuracaoAtordoamento);
            }
            else
            {
                Debug.LogWarning("[MaoFisicaBridge] BarraEnferrujadaConfig não atribuído; usando defaults do POCO.", this);
                armaEquipada = new BarraEnferrujada();
            }

            _armaDesbloqueada = desbloqueadaNoInicio;

            // Fallback seguro: se "Camada Inimigos" ficou sem valor no Inspector
            // (LayerMask 0 = nenhuma camada), usa a layer "Enemy" pelo nome.
            if (camadaInimigos.value == 0)
            {
                camadaInimigos = LayerMask.GetMask("Enemy");
            }

            // Filtro montado uma vez. useTriggers = true preserva o comportamento
            // anterior de OverlapCircleAll, que respeitava Physics2D.queriesHitTriggers
            // (padrão true) — o alvo real (CultistaAI) é filtrado depois por GetComponent.
            _filtroInimigos = new ContactFilter2D();
            _filtroInimigos.useTriggers = true;
            _filtroInimigos.SetLayerMask(camadaInimigos);
        }

        public void TryAtacar(Vector2 direcao)
        {
            if (!_armaDesbloqueada) return;
            if (direcao == Vector2.zero) return;
            if (_fsm == null) return; // fallback seguro: sem FSM injetada, a ação não dispara
            if (!_fsm.EstaLivre) return; // portão barato antes do Execute (que avança o RNG do atordoamento)
            if (!armaEquipada.CanActivate(Time.time - lastUseTime)) return;

            var resultado = armaEquipada.Execute();

            // Commit da exclusão mútua (revalida; em thread única o estado não mudou desde EstaLivre).
            if (!_fsm.TryEntrarAcao(PlayerState.Atacando, resultado.DurationSeconds)) return;

            lastUseTime = Time.time;
            ResolverGolpe(direcao, resultado);
            OnAtaqueExecutado?.Invoke(direcao, resultado.DurationSeconds);
        }

        private void ResolverGolpe(Vector2 direcao, ArmaResult resultado)
        {
            Vector2 centro = (Vector2)transform.position + direcao.normalized * (alcance * 0.5f);
            int total = Physics2D.OverlapCircle(centro, alcance * 0.5f, _filtroInimigos, _hitBuffer);

            for (int i = 0; i < total; i++)
            {
                var cultista = _hitBuffer[i].GetComponent<CultistaAI>();
                if (cultista != null) cultista.ReceberGolpeFisico(resultado);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.8f, 0.2f, 0.2f, 0.4f);
            Gizmos.DrawWireSphere(transform.position, alcance);
        }
    }
}
