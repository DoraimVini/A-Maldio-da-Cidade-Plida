using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using FavelaAmarela.Core.GameLoop;
using FavelaAmarela.Runtime.GameLoop;

namespace FavelaAmarela.Runtime.UI
{
    /// <summary>
    /// Camada Runtime (MonoBehaviour). Tira o jogador da tela de Colapso e o devolve ao Menu.
    ///
    /// <para><b>Buraco que motivou (auditoria 2026-08-11):</b> a máquina de estados permitia
    /// <c>Colapso → Menu</c>, mas <b>ninguém no projeto inteiro fazia essa transição</b>. Na
    /// prática, morrer era um beco sem saída: a sequência de morte tocava e o jogo ficava
    /// parado ali para sempre.</para>
    ///
    /// <para>Espera a sequência de morte terminar antes de aceitar input — deixar o jogador
    /// pular a própria morte no primeiro frame apagaria a única punição diegética do jogo.</para>
    /// </summary>
    [AddComponentMenu("FavelaAmarela/UI/Retorno do Colapso")]
    public sealed class RetornoDoColapso : MonoBehaviour
    {
        [Tooltip("Segundos de espera antes de aceitar input, para a sequência de morte ser vista.")]
        [Min(0f)]
        [SerializeField] private float atrasoAntesDeAceitar = 3f;

        [Tooltip("Texto do aviso, mostrado só quando a tecla já é aceita. [ASSET]")]
        [SerializeField] private Text aviso;

        [Tooltip("Mensagem mostrada ao jogador.")]
        [SerializeField] private string mensagem = "Pressione qualquer tecla";

        private float _tempoNoColapso;
        private bool _aceitando;
        private bool _estavaEmColapso;

        private void Update()
        {
            var gm = GameManager.Instance;
            if (gm?.StateMachine == null) return;

            bool emColapso = gm.StateMachine.CurrentState == GameState.Colapso;

            // Detecta a ENTRADA no Colapso aqui, e não em OnEnable: esta tela fica sempre
            // ativa (quem some é o CanvasGroup, por fade da sequência de morte), então
            // OnEnable não dispararia na morte — e a segunda morte já nasceria "aceitando".
            if (emColapso && !_estavaEmColapso) Reiniciar();
            _estavaEmColapso = emColapso;

            if (!emColapso) return;

            if (!_aceitando)
            {
                // Tempo real: se o Colapso vier de um estado congelado, deltaTime seria 0
                // e o jogador ficaria presoesperando um relógio que não anda.
                _tempoNoColapso += Time.unscaledDeltaTime;
                if (_tempoNoColapso < atrasoAntesDeAceitar) return;

                _aceitando = true;
                if (aviso != null) aviso.enabled = true;
            }

            if (AlgumaTeclaOuBotao())
                gm.StateMachine.TryTransition(GameState.Menu);
        }

        /// <summary>Zera o relógio e esconde o aviso — chamado a cada nova morte.</summary>
        private void Reiniciar()
        {
            _tempoNoColapso = 0f;
            _aceitando = false;

            if (aviso != null)
            {
                aviso.text = mensagem;
                aviso.enabled = false;
            }
        }

        private static bool AlgumaTeclaOuBotao()
        {
            if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame) return true;
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) return true;
            if (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame) return true;

            return false;
        }
    }
}
