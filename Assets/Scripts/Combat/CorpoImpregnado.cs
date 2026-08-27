using UnityEngine;

namespace FavelaAmarela.Runtime.Combat
{
    /// <summary>
    /// Quanto de Carcosa já entrou neste corpo — e, por consequência, o quanto ele ainda
    /// obedece à física.
    ///
    /// <para><b>A regra da realidade deste jogo</b> (decidida em 2026-08-27, a partir da
    /// pergunta do Vini "por que as coisas funcionam nessa realidade?"):</para>
    ///
    /// <para><b>Em Carcosa, quanto mais uma coisa está impregnada, menos ela se comporta como
    /// matéria.</b></para>
    ///
    /// <para>Isso não é sabor: é <b>legibilidade</b>. O jogador descobre o que uma coisa é pela
    /// forma como ela reage ao golpe. Um Cultista voa para trás porque ainda é gente. Um Eco de
    /// Carcosa não se move porque nunca foi corpo. O Rei em Amarelo não está <i>aqui</i> para
    /// ser empurrado. Nenhuma linha de diálogo precisa explicar isso — o primeiro golpe explica.</para>
    ///
    /// <para><b>Ausente = corpo comum.</b> Um ator sem este componente é totalmente empurrável.
    /// É o padrão certo: a maioria do elenco é gente, e exigir o componente em todo mundo faria
    /// dele mais uma lista à mão para envelhecer.</para>
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CorpoImpregnado : MonoBehaviour
    {
        /// <summary>
        /// 0 = matéria comum, obedece ao impulso por inteiro. 1 = não se move nunca.
        ///
        /// <para>Valores da tabela diegética: Cultista 0,15 · Cortesão Pálido 0,45 ·
        /// Byakhee 0,75 · Eco de Carcosa 1,00 · Rei em Amarelo 1,00.</para>
        /// </summary>
        [Range(0f, 1f)]
        [Tooltip("0 = corpo comum, voa para trás. 1 = imóvel, não é mais matéria.")]
        [SerializeField] private float resistenciaAImpulso;

        /// <summary>Fração do impulso que este corpo absorve, de 0 a 1.</summary>
        public float ResistenciaAImpulso => Mathf.Clamp01(resistenciaAImpulso);

        /// <summary>
        /// Lê a resistência de um alvo qualquer. Sem o componente, devolve <c>0</c> — corpo
        /// comum. Busca no pai porque o golpe costuma acertar um colisor filho (a
        /// <see cref="Hurtbox"/>), não a raiz do ator.
        /// </summary>
        public static float De(Component alvo)
        {
            if (alvo == null) return 0f;
            var corpo = alvo.GetComponentInParent<CorpoImpregnado>();
            return corpo != null ? corpo.ResistenciaAImpulso : 0f;
        }
    }
}
