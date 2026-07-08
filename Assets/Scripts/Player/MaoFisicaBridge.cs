using System;
using UnityEngine;
using FavelaAmarela.Core.Abilities;
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
        [Header("Arma Equipada")]
        [SerializeField] private float duration = 0.3f;
        [SerializeField] private float cooldown = 0.6f;
        [SerializeField] private float probabilidadeAtordoar = 0.35f;
        [SerializeField] private float duracaoAtordoamento = 2f;

        [Header("Alcance do Golpe")]
        [SerializeField] private float alcance = 1.2f;
        [SerializeField] private LayerMask camadaInimigos;

        [Header("Progressão")]
        [Tooltip("Ligar só para testar o combate isolado. No jogo real, Damião começa DESARMADO — a arma é adquirida junto do patuá na Zona 5 (ver DesbloquearArma).")]
        [SerializeField] private bool desbloqueadaNoInicio = false;

        private IArma armaEquipada;
        private float lastUseTime = -999f;
        private bool _armaDesbloqueada;

        /// <summary>Direção e duração do golpe ativado.</summary>
        public event Action<Vector2, float> OnAtaqueExecutado;

        public bool IsAtacando { get; private set; }

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
            armaEquipada = new BarraEnferrujada(duration, cooldown, probabilidadeAtordoar, duracaoAtordoamento);
            _armaDesbloqueada = desbloqueadaNoInicio;

            // Fallback seguro: se "Camada Inimigos" ficou sem valor no Inspector
            // (LayerMask 0 = nenhuma camada), usa a layer "Enemy" pelo nome.
            if (camadaInimigos.value == 0)
            {
                camadaInimigos = LayerMask.GetMask("Enemy");
            }
        }

        public void TryAtacar(Vector2 direcao)
        {
            if (!_armaDesbloqueada) return;
            if (IsAtacando) return;
            if (direcao == Vector2.zero) return;
            if (!armaEquipada.CanActivate(Time.time - lastUseTime)) return;

            var resultado = armaEquipada.Execute();
            lastUseTime = Time.time;
            IsAtacando = true;

            ResolverGolpe(direcao, resultado);

            OnAtaqueExecutado?.Invoke(direcao, resultado.DurationSeconds);
            Invoke(nameof(EndAtaque), resultado.DurationSeconds);
        }

        private void ResolverGolpe(Vector2 direcao, ArmaResult resultado)
        {
            Vector2 centro = (Vector2)transform.position + direcao.normalized * (alcance * 0.5f);
            var atingidos = Physics2D.OverlapCircleAll(centro, alcance * 0.5f, camadaInimigos);

            foreach (var colisor in atingidos)
            {
                var cultista = colisor.GetComponent<CultistaAI>();
                if (cultista != null) cultista.ReceberGolpeFisico(resultado);
            }
        }

        private void EndAtaque()
        {
            IsAtacando = false;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.8f, 0.2f, 0.2f, 0.4f);
            Gizmos.DrawWireSphere(transform.position, alcance);
        }
    }
}
