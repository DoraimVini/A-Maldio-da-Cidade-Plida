using UnityEngine;

namespace FavelaAmarela.Runtime.GameLoop
{
    /// <summary>
    /// Camada Runtime (MonoBehaviour). Fecha as saídas de uma arena enquanto uma luta de
    /// chefe está em curso: <b>nenhum chefe do jogo pode ser abandonado antes do desfecho</b>
    /// (decisão do Vini, 2026-07-31).
    ///
    /// <para><b>Genérico de propósito.</b> Não sabe qual chefe o controla, nem consulta o
    /// save, nem conhece <c>PortalDeCena</c> especificamente — recebe uma lista de
    /// <see cref="Collider2D"/> e liga/desliga. Byakhee e o Rei em Amarelo reaproveitam a
    /// mesma peça só ligando os campos no Inspector, sem código novo.</para>
    ///
    /// <para><b>Por que desligar o <c>Collider2D</c> e não o GameObject inteiro:</b> o objeto
    /// da saída pode carregar outras coisas (visual da porta, luz, som de ambiente) que devem
    /// continuar existindo enquanto a passagem está fechada. Desligar só o colisor fecha a
    /// passagem sem apagar o cenário.</para>
    ///
    /// <para><b>Quem chama:</b> o adaptador do chefe, a partir da FSM dele — trancar ao
    /// entrar em combate, destrancar ao resolver. Ver <c>AbdulAlhazredAI</c>.</para>
    /// </summary>
    [AddComponentMenu("Favela Amarela/GameLoop/Tranca de Arena")]
    public sealed class TrancaDeArena : MonoBehaviour
    {
        [Tooltip("Colisores das saídas a fechar durante a luta (portais, portas, gatilhos de " +
                 "transição). Ficam desligados enquanto a arena estiver trancada.")]
        [SerializeField] private Collider2D[] saidas;

        /// <summary>Se a arena está fechada neste momento.</summary>
        public bool Trancada { get; private set; }

        /// <summary>Fecha as saídas — chamado quando a luta começa de verdade.</summary>
        public void Trancar()
        {
            Trancada = true;
            AplicarEstado();
        }

        /// <summary>
        /// Reabre as saídas. Chamado ao resolver a luta e também ao restaurar uma cena onde
        /// ela já estava resolvida — uma arena nunca pode ficar trancada para sempre.
        /// </summary>
        public void Destrancar()
        {
            Trancada = false;
            AplicarEstado();
        }

        private void Awake()
        {
            if (saidas == null || saidas.Length == 0)
                Debug.LogError($"[TrancaDeArena] '{name}' não tem nenhuma saída atribuída — " +
                               "a arena não vai trancar nada.", this);
        }

        private void AplicarEstado()
        {
            if (saidas == null) return;

            for (int i = 0; i < saidas.Length; i++)
            {
                if (saidas[i] == null) continue;
                saidas[i].enabled = !Trancada;
            }
        }
    }
}
