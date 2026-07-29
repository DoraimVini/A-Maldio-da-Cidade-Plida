using System;
using UnityEngine;
using FavelaAmarela.Core.Abilities;
using FavelaAmarela.Core.Combat;
using FavelaAmarela.Core.Player;

namespace FavelaAmarela.Player
{
    /// <summary>
    /// Bridge da Mão Física: conecta a arma equipada (um <see cref="IArmaComHabilidade"/>)
    /// à Unity. A arma <b>não é mais fixa</b> — Damião começa desarmado e equipa uma
    /// arma da Tumba em runtime (o baú sorteia entre Cravo de Aklo, Estilete de Irem e
    /// Alfanje de Alhazred). Expõe <see cref="TryAtacar"/> (ataque básico) e
    /// <see cref="TryUsarHabilidade"/> (habilidade, botão separado) para o
    /// <c>PlayerMovement</c>, e resolve o golpe contra qualquer <see cref="IDanificavel"/>
    /// (Cultista, Aparição Primordial/boss) — não mais só o Cultista.
    /// </summary>
    [AddComponentMenu("Favela Amarela/Mao Fisica Bridge")]
    public class MaoFisicaBridge : MonoBehaviour
    {
        /// <summary>Armas da Tumba para equipar em teste isolado (no jogo real vem do baú).</summary>
        public enum ArmaDeTeste { Nenhuma, CravoDeAklo, EstileteDeIrem, AlfanjeDeAlhazred }

        [Header("Arma (equipada pelo baú no jogo; aqui só para teste isolado)")]
        [Tooltip("No jogo real Damião começa DESARMADO — a arma vem do baú da Tumba. Escolha uma aqui só para testar o combate.")]
        [SerializeField] private ArmaDeTeste armaInicialParaTeste = ArmaDeTeste.Nenhuma;

        [Header("Alcance do Golpe")]
        [SerializeField] private float alcance = 1.2f;
        [SerializeField] private LayerMask camadaInimigos;

        private IArmaComHabilidade _armaEquipada;
        private float _lastUseTime = -999f;
        private float _lastAbilityUseTime = -999f;
        private PlayerStateMachine _fsm;

        // Buffer pré-alocado + filtro para resolver o golpe sem alocar lixo por golpe
        // (Regra de Ouro 1). 8 slots cobrem o alcance melee.
        private readonly Collider2D[] _hitBuffer = new Collider2D[8];
        private ContactFilter2D _filtroInimigos;

        /// <summary>Direção e duração do ataque básico executado.</summary>
        public event Action<Vector2, float> OnAtaqueExecutado;

        /// <summary>Direção e duração da habilidade da arma executada.</summary>
        public event Action<Vector2, float> OnHabilidadeExecutada;

        /// <summary>true enquanto a FSM do jogador estiver Atacando (fonte única de verdade).</summary>
        public bool IsAtacando => _fsm != null && _fsm.CurrentState == PlayerState.Atacando;

        /// <summary>Injeta a FSM de estado do jogador (chamado por <c>PlayerMovement</c> no Awake).</summary>
        public void BindStateMachine(PlayerStateMachine fsm) => _fsm = fsm;

        /// <summary>Se há uma arma equipada na Mão Física.</summary>
        public bool TemArmaEquipada => _armaEquipada != null;

        /// <summary>Nome diegético da arma equipada, ou vazio se desarmado.</summary>
        public string NomeDaArmaEquipada => _armaEquipada?.NomeDaArma ?? "";

        /// <summary>Nome da habilidade da arma equipada, ou vazio se desarmado.</summary>
        public string NomeDaHabilidade => _armaEquipada?.NomeHabilidade ?? "";

        /// <summary>
        /// Equipa uma arma na Mão Física (chamado pelo baú da Tumba). Substitui a arma
        /// anterior — o slot de Mão Física é único (troca só sob a luz de um Refúgio, no design).
        /// </summary>
        public void EquiparArma(IArmaComHabilidade arma) => _armaEquipada = arma;

        private void Awake()
        {
            // Fallback seguro: se "Camada Inimigos" ficou sem valor no Inspector, usa "Enemy".
            if (camadaInimigos.value == 0)
                camadaInimigos = LayerMask.GetMask("Enemy");

            _filtroInimigos = new ContactFilter2D();
            _filtroInimigos.useTriggers = true;
            _filtroInimigos.SetLayerMask(camadaInimigos);

            var armaTeste = CriarArmaDeTeste(armaInicialParaTeste);
            if (armaTeste != null) EquiparArma(armaTeste);
        }

        private static IArmaComHabilidade CriarArmaDeTeste(ArmaDeTeste escolha) => escolha switch
        {
            ArmaDeTeste.CravoDeAklo => new CravoDeAklo(),
            ArmaDeTeste.EstileteDeIrem => new EstileteDeIrem(),
            ArmaDeTeste.AlfanjeDeAlhazred => new AlfanjeDeAlhazred(),
            _ => null,
        };

        /// <summary>Ataque básico da arma equipada, na direção dada.</summary>
        public void TryAtacar(Vector2 direcao)
        {
            if (_armaEquipada == null) return;                 // desarmado
            if (direcao == Vector2.zero) return;
            if (_fsm == null || !_fsm.EstaLivre) return;
            if (!_armaEquipada.CanActivate(Time.time - _lastUseTime)) return;

            var resultado = _armaEquipada.Execute();
            if (!_fsm.TryEntrarAcao(PlayerState.Atacando, resultado.DurationSeconds)) return;

            _lastUseTime = Time.time;
            ResolverGolpe(direcao, resultado);
            OnAtaqueExecutado?.Invoke(direcao, resultado.DurationSeconds);
        }

        /// <summary>Habilidade da arma equipada (botão separado, cooldown próprio), na direção dada.</summary>
        public void TryUsarHabilidade(Vector2 direcao)
        {
            if (_armaEquipada == null) return;
            if (direcao == Vector2.zero) return;
            if (_fsm == null || !_fsm.EstaLivre) return;
            if (!_armaEquipada.CanActivateHabilidade(Time.time - _lastAbilityUseTime)) return;

            var resultado = _armaEquipada.ExecuteHabilidade();
            if (!_fsm.TryEntrarAcao(PlayerState.Atacando, resultado.DurationSeconds)) return;

            _lastAbilityUseTime = Time.time;
            ResolverGolpe(direcao, resultado);
            OnHabilidadeExecutada?.Invoke(direcao, resultado.DurationSeconds);
        }

        private void ResolverGolpe(Vector2 direcao, ArmaResult resultado)
        {
            Vector2 centro = (Vector2)transform.position + direcao.normalized * (alcance * 0.5f);
            int total = Physics2D.OverlapCircle(centro, alcance * 0.5f, _filtroInimigos, _hitBuffer);

            for (int i = 0; i < total; i++)
            {
                // Mira qualquer IDanificavel (Cultista, Aparição Primordial/boss...), não
                // mais só o CultistaAI — é isto que permite as armas atingirem o Abdul.
                var alvo = _hitBuffer[i].GetComponent<IDanificavel>();
                if (alvo != null) alvo.ReceberGolpe(resultado);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.8f, 0.2f, 0.2f, 0.4f);
            Gizmos.DrawWireSphere(transform.position, alcance);
        }
    }
}
