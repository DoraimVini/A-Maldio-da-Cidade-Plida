using UnityEngine;
using FavelaAmarela.Core.Combat;
using FavelaAmarela.Core.GameLoop;
using FavelaAmarela.Runtime.Combat;

namespace FavelaAmarela.Runtime.GameLoop
{
    /// <summary>
    /// Camada Runtime. Detecta a derrota de Damião pelos <b>dois vetores</b> e leva o jogo ao
    /// <see cref="GameState.Colapso"/>, guardando qual dos dois foi — a frase final muda se a
    /// mente se dissolveu ou se o corpo cedeu.
    ///
    /// <list type="bullet">
    ///   <item><b>Mental:</b> <see cref="ResilienciaMental"/> a zero
    ///   (<c>ResilienciaChangedArgs.EntrouEmColapso</c>).</item>
    ///   <item><b>Corpórea:</b> <see cref="Vitalidade"/> a zero
    ///   (<c>VitalidadeBridge.OnAbatido</c>).</item>
    /// </list>
    ///
    /// <para>Extraído do <c>GameManager</c> em 2026-08-13, junto com o campo
    /// <c>_tipoDeDerrota</c> e a chamada de <see cref="SequenciaDeColapso"/> — que só faz sentido
    /// aqui, porque é quem conhece a causa da morte.</para>
    /// </summary>
    [AddComponentMenu("Favela Amarela/GameLoop/Controlador de Derrota")]
    public sealed class PlayerDeathController : MonoBehaviour
    {
        [Tooltip("Sequência de morte (dissolução + frase) tocada ao entrar em Colapso. [CENA]")]
        [SerializeField] private SequenciaDeColapso sequenciaColapso;

        private GameLoopStateMachine _maquina;
        private ResilienciaMental _resiliencia;
        private VitalidadeBridge _vitalidade;

        /// <summary>
        /// Como Damião foi derrotado da última vez. Começa em <see cref="TipoDeDerrota.Mental"/>
        /// porque a Resiliência é o vetor de derrota dominante — se algo disparar o Colapso sem
        /// passar por aqui, "a mente cedeu" é o palpite certo.
        /// </summary>
        public TipoDeDerrota TipoDeDerrota { get; private set; } = TipoDeDerrota.Mental;

        /// <summary>
        /// Liga as duas fontes de derrota e a máquina de estados. A <c>VitalidadeBridge</c> pode
        /// vir nula (cena sem Damião corpóreo) — nesse caso só a morte mental funciona, e o aviso
        /// deixa claro qual metade ficou de fora.
        /// </summary>
        public void Bind(GameLoopStateMachine maquina, ResilienciaMental resiliencia,
            VitalidadeBridge vitalidade)
        {
            Desinscrever();

            _maquina = maquina;
            _resiliencia = resiliencia;
            _vitalidade = vitalidade;

            if (_maquina == null)
                Debug.LogError("[PlayerDeathController] Sem máquina de estados — a derrota não " +
                               "levará ao Colapso.", this);

            if (_resiliencia != null)
                _resiliencia.OnChanged += HandleResilienciaChanged;
            else
                Debug.LogError("[PlayerDeathController] Sem Resiliência Mental — a morte por " +
                               "Colapso mental não será detectada.", this);

            if (_vitalidade != null)
                _vitalidade.OnAbatido += HandleDamiaoAbatido;
            else
                Debug.LogWarning("[PlayerDeathController] Nenhuma VitalidadeBridge de Damião; " +
                                 "ele não terá morte física — só a mental.", this);
        }

        private void OnDestroy() => Desinscrever();

        private void Desinscrever()
        {
            if (_resiliencia != null) _resiliencia.OnChanged -= HandleResilienciaChanged;
            if (_vitalidade != null) _vitalidade.OnAbatido -= HandleDamiaoAbatido;

            _resiliencia = null;
            _vitalidade = null;
            _maquina = null;
        }

        private void HandleResilienciaChanged(ResilienciaChangedArgs args)
        {
            if (args.EntrouEmColapso) Derrotar(TipoDeDerrota.Mental);
        }

        private void HandleDamiaoAbatido() => Derrotar(TipoDeDerrota.Corporea);

        /// <summary>
        /// Registra a causa e leva ao Colapso. A sequência de morte só toca se a transição for
        /// aceita — assim uma segunda fonte de derrota chegando logo depois (levar o último
        /// golpe no mesmo frame em que a mente cede) não toca a dissolução duas vezes.
        /// </summary>
        private void Derrotar(TipoDeDerrota tipo)
        {
            TipoDeDerrota = tipo;

            if (_maquina == null) return;
            if (!_maquina.TryTransition(GameState.Colapso)) return;

            if (sequenciaColapso != null) sequenciaColapso.Tocar(tipo);
        }
    }
}
