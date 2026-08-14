using UnityEngine;
using FavelaAmarela.Runtime.Enemies;

namespace FavelaAmarela.Player
{
    /// <summary>
    /// Camada Runtime. Guarda quem é o companheiro <b>Yug-Neth</b> da run corrente.
    ///
    /// <para><b>Por que registro sob demanda, e não bootstrap:</b> Yug-Neth já existe na cena
    /// desde o começo, mas <b>cativo</b> — ele só passa a valer para a run quando o jogador o
    /// liberta do Abdul, no meio do jogo. Procurá-lo no bootstrap registraria um companheiro que
    /// ainda não é companheiro.</para>
    ///
    /// <para>Extraído do <c>GameManager</c> em 2026-08-13. Chamadores hoje:
    /// <c>AbdulAlhazredAI</c> e <c>TravessiaDoCompanheiro</c> registram;
    /// <c>RefugioDeLuz</c> consulta para reanimá-lo.</para>
    /// </summary>
    [AddComponentMenu("Favela Amarela/Player/Companion Manager")]
    public sealed class CompanionManager : MonoBehaviour
    {
        private YugNethAI _yugNeth;

        /// <summary>
        /// O companheiro, ou <c>null</c> antes de ser libertado. Quem consulta hoje é o gatilho
        /// dos Portões de Carcosa (que não abrem com ele caído) e o <c>RefugioDeLuz</c>.
        ///
        /// <para><b>Nota histórica (2026-07-31):</b> a morte de Yug-Neth já foi fim de run
        /// imediato, estilo escolta. Revogado — hoje ele fica <b>incapacitado</b> e é reanimado
        /// num Refúgio: bloqueia o progresso, não a run inteira.</para>
        /// </summary>
        public YugNethAI YugNeth => _yugNeth;

        /// <summary>
        /// Registra o companheiro assim que ele é libertado. Idempotente e defensivo: passar
        /// <c>null</c> ou o mesmo já registrado não faz nada — quem chama nem sempre sabe se já
        /// registrou.
        /// </summary>
        public void RegistrarYugNeth(YugNethAI yugNeth)
        {
            if (yugNeth == null || _yugNeth == yugNeth) return;
            _yugNeth = yugNeth;
        }
    }
}
