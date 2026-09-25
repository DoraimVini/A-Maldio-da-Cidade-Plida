using UnityEngine;

namespace FavelaAmarela.Runtime.Interaction
{
    /// <summary>
    /// Contrato de qualquer coisa do mundo que o Damião possa usar apertando o botão de
    /// interação (E / botão Norte do gamepad): baú, colecionável, porta, alavanca, ou um
    /// gatilho de diálogo.
    ///
    /// <para>Vive no Runtime (e não no Core) de propósito: implementações são
    /// <c>MonoBehaviour</c> presas a objetos de cena. A regra de <b>qual</b> alvo vence
    /// quando há vários por perto é pura e mora no Core
    /// (<see cref="FavelaAmarela.Core.Interaction.SeletorDeInteracao"/>).</para>
    ///
    /// <para>Substitui o padrão antigo de <c>OnTriggerEnter2D</c> automático: encostar
    /// não usa mais o objeto, o jogador escolhe. Ver
    /// <c>Docs/KnowledgeBundle/systems/interacao.md</c>.</para>
    /// </summary>
    public interface IInteragivel
    {
        /// <summary>
        /// Texto diegético mostrado no prompt, no infinitivo e já com o verbo da ação
        /// ("Abrir o baú", "Recolher o patuá"). Segue a skill <c>favela-lore-enforcer</c> —
        /// é texto visível ao jogador.
        /// </summary>
        string RotuloDeInteracao { get; }

        /// <summary>
        /// Se aceita interação agora. Um baú já aberto devolve <c>false</c> e some do
        /// prompt, em vez de continuar oferecendo uma ação que não faz nada.
        /// </summary>
        bool PodeInteragir { get; }

        /// <summary>
        /// Desempate quando dois alvos estão praticamente à mesma distância — maior vence.
        /// Deixe 0 para o comportamento normal (o mais perto ganha).
        /// </summary>
        int PrioridadeDeInteracao { get; }

        /// <summary>Posição usada para medir a distância até o Damião.</summary>
        Vector2 PosicaoDeInteracao { get; }

        /// <summary>Executa a interação. Só é chamado quando <see cref="PodeInteragir"/> é true.</summary>
        /// <param name="quemInterage">O objeto do jogador que acionou (para efeitos que precisam dele).</param>
        void Interagir(GameObject quemInterage);
    }
}
