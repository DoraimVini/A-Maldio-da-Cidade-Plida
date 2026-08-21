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
        /// Disparado quando o companheiro passa a valer para a run.
        ///
        /// <para>Existe porque o registro acontece <b>no meio do jogo</b>, e não no bootstrap:
        /// quem quiser reagir a ele — a barra do HUD é o caso — não pode simplesmente perguntar
        /// no arranque, porque no arranque a resposta é <c>null</c>. E consultar todo frame para
        /// descobrir quando deixou de ser null violaria a Regra de Ouro 8 (eventos, não
        /// polling).</para>
        /// </summary>
        public event System.Action<YugNethAI> OnCompanheiroRegistrado;

        /// <summary>
        /// Registra o companheiro assim que ele é libertado. Idempotente e defensivo: passar
        /// <c>null</c> ou o mesmo já registrado não faz nada — quem chama nem sempre sabe se já
        /// registrou.
        /// </summary>
        public void RegistrarYugNeth(YugNethAI yugNeth)
        {
            if (yugNeth == null || _yugNeth == yugNeth) return;
            _yugNeth = yugNeth;

            // Depois de gravar o campo: quem escuta pode consultar YugNeth no callback e tem
            // que encontrar o valor novo, não o anterior.
            OnCompanheiroRegistrado?.Invoke(yugNeth);
        }

        /// <summary>
        /// Disparado quando Yug-Neth deixa de ser companheiro da run.
        /// </summary>
        public event System.Action OnCompanheiroAposentado;

        /// <summary>
        /// <b>Aposenta</b> o companheiro: ele continua existindo no mundo, mas pára de contar
        /// como companheiro da run.
        ///
        /// <para><b>Por que existe:</b> ao entrar no Castelo, Yug-Neth vira o NPC que ensina o
        /// artesanato (decisão do Vini, 2026-08-20). Como o artesanato é conteúdo pós-Vertical
        /// Slice, ele não pode seguir acumulando as responsabilidades de companheiro até lá —
        /// a barra de RC no HUD, a reanimação no Refúgio, o bloqueio de progresso quando cai.
        /// Aposentar é o oposto de registrar, não uma morte: o objeto segue em cena.</para>
        ///
        /// <para>Idempotente: aposentar duas vezes não dispara o evento duas vezes.</para>
        /// </summary>
        public void Aposentar()
        {
            if (_yugNeth == null) return;

            _yugNeth = null;
            OnCompanheiroAposentado?.Invoke();
        }
    }
}
