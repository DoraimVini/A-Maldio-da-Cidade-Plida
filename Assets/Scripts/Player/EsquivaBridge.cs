using System;
using System.Collections;
using UnityEngine;
using FavelaAmarela.Core.Abilities;
using FavelaAmarela.Core.Player;
using FavelaAmarela.Runtime.Config;

namespace FavelaAmarela.Player
{
    /// <summary>
    /// MonoBehaviour Bridge conectando o POCO <see cref="Esquiva"/> à Unity.
    /// Espelha o papel dos demais bridges de ação do jogador, mas para um movimento
    /// físico comum — sem custo de Resiliência Mental e sem tornar Damião intangível
    /// (a Esquiva colide com paredes normalmente; só o Salto Dimensional atravessa
    /// barreiras anômalas).
    /// </summary>
    [AddComponentMenu("Favela Amarela/Esquiva Bridge")]
    public class EsquivaBridge : MonoBehaviour
    {
        [Header("Configuração")]
        [Tooltip("Asset de tunagem da Esquiva. Se vazio, usa os defaults do POCO.")]
        [SerializeField] private EsquivaConfig config;

        private Esquiva esquiva;
        private float lastUseTime = -999f;
        private PlayerStateMachine _fsm;
        private GerenciadorDeVigor _vigor;

        /// <summary>Direção, duração e multiplicador de velocidade da esquiva ativada.</summary>
        public event Action<Vector2, float, float> OnEsquivaActivada;

        /// <summary>true enquanto a FSM do jogador estiver no estado Esquivando (fonte única de verdade).</summary>
        public bool IsEsquivando => _fsm != null && _fsm.CurrentState == PlayerState.Esquivando;

        /// <summary>Injeta a FSM de estado do jogador (chamado por <see cref="PlayerMovement"/> no Awake).</summary>
        public void BindStateMachine(PlayerStateMachine fsm) => _fsm = fsm;

        private void Awake()
        {
            if (config != null)
            {
                esquiva = new Esquiva(config.Duration, config.Cooldown, config.SpeedMultiplier);
            }
            else
            {
                Debug.LogWarning("[EsquivaBridge] EsquivaConfig não atribuído; usando defaults do POCO.", this);
                esquiva = new Esquiva();
            }
            _vigor = GetComponent<GerenciadorDeVigor>();
        }

        public void TryActivateEsquiva(Vector2 direction)
        {
            if (direction == Vector2.zero) return;
            if (_fsm == null) return; // fallback seguro: sem FSM injetada, a ação não dispara
            if (!_fsm.EstaLivre) return; // portão barato antes do Execute (que é irreversível)
            if (!esquiva.CanActivate(Time.time - lastUseTime)) return;

            if (_vigor != null && !_vigor.TentarConsumirEsquiva()) return;

            var result = esquiva.Execute();

            // Commit da exclusão mútua (revalida; em thread única o estado não mudou desde EstaLivre).
            if (!_fsm.TryEntrarAcao(PlayerState.Esquivando, result.DurationSeconds)) return;

            lastUseTime = Time.time;
            OnEsquivaActivada?.Invoke(direction, result.DurationSeconds, result.SpeedMultiplier);

            StartCoroutine(EsquivaIFramesCoroutine(result.DurationSeconds));
        }

        /// <summary>
        /// Quadros de invencibilidade da Esquiva: a <b>hurtbox</b> do Damião sai do ar pela
        /// duração do movimento.
        ///
        /// <para><b>Dois defeitos consertados aqui em 2026-08-27, e o segundo é o grave.</b></para>
        ///
        /// <para><b>1. Não havia invencibilidade nenhuma.</b> A versão anterior desligava a
        /// colisão entre as camadas <c>Player</c> e <c>Enemy</c>. Mas <b>o jogador não leva
        /// dano por colisão</b> — o projeto inteiro tem <b>zero</b> <c>OnCollisionEnter2D</c>.
        /// O dano chega por <c>Hitbox</c> → <c>Hurtbox</c>, que é uma <b>query</b>
        /// (<c>Physics2D.OverlapCircle</c>), e query não olha a matriz de colisão. A corrotina
        /// prometia i-frames no nome e não entregava nada.</para>
        ///
        /// <para><b>2. Ela CORROMPIA a matriz global.</b> O Project Settings já tem
        /// <c>Player × Enemy</c> <b>desligado</b>. Então o <c>IgnoreLayerCollision(..., true)</c>
        /// era no-op, e o <c>false</c> do fim <b>LIGAVA</b> uma colisão que o asset mandava
        /// desligar — permanentemente, em todas as cenas, até o próximo carregamento de domínio.
        /// Depois da primeira esquiva da partida, inimigos passavam a empurrar o Damião para
        /// sempre.</para>
        ///
        /// <para><b>Por que desligar o colisor e não usar <c>excludeLayers</c>:</b> a doc da
        /// 6.4 é explícita que <c>excludeLayers</c> decide <i>"if a <b>contact</b> ... should
        /// happen"</i>. Contato não é query. Para sumir de um <c>OverlapCircle</c>, o colisor
        /// precisa estar desligado — e é isso que dá i-frame de verdade contra o caminho de
        /// dano que este jogo realmente usa.</para>
        ///
        /// <para>O <c>try/finally</c> não é zelo excessivo: sem ele, uma troca de cena ou uma
        /// morte no meio da esquiva deixaria o Damião <b>permanentemente invulnerável</b> — a
        /// versão espelhada do bug que acabou de ser consertado.</para>
        /// </summary>
        private IEnumerator EsquivaIFramesCoroutine(float duration)
        {
            var hurtbox = GetComponentInChildren<FavelaAmarela.Runtime.Combat.Hurtbox>(true);
            var colisor = hurtbox != null ? hurtbox.GetComponent<Collider2D>() : null;

            if (colisor == null)
            {
                Debug.LogError("[EsquivaBridge] Damião não tem Hurtbox com colisor — a Esquiva " +
                               "não concede invencibilidade nenhuma.", this);
                yield break;
            }

            colisor.enabled = false;

            try
            {
                yield return new WaitForSeconds(duration);
            }
            finally
            {
                if (colisor != null) colisor.enabled = true;
            }
        }
    }
}
