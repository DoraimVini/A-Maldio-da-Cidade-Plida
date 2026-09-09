using System;

namespace FavelaAmarela.Core.Camera
{
    /// <summary>
    /// O canal por onde o <b>mundo</b> pede um tremor de tela — o que não é golpe.
    ///
    /// <para><b>Por que existe (2026-09-09).</b> Golpe já sacode a tela sozinho: a
    /// <c>Hurtbox</c> resolve o dano, o <c>HitStop</c> anuncia por <c>OnImpacto</c>, e a câmera
    /// assina. Mas o mundo tem impactos que não são dano — um chefe pousando, um portão
    /// batendo, chão desmoronando. O <c>AcrescentarTrauma</c> da câmera existia público para
    /// isso e <b>não tinha um chamador sequer</b>.</para>
    ///
    /// <para><b>Por que um evento e não uma referência.</b> As duas alternativas óbvias falham
    /// neste projeto:</para>
    ///
    /// <list type="bullet">
    ///   <item><b>Campo serializado no inimigo</b> — o Byakhee é um <i>prefab</i> e a câmera é
    ///   um objeto de <i>cena</i>. Prefab não referencia objeto de cena: o campo nasceria vazio
    ///   e precisaria de fiação manual em cada cena, que é o modo de falha dominante daqui.</item>
    ///   <item><b>Buscar a câmera em runtime</b> — o <c>CLAUDE.md</c> proíbe
    ///   <c>FindObjectOfType</c> em produção, e com razão.</item>
    /// </list>
    ///
    /// <para><b>E por que em <c>Core</c>.</b> Aqui é o único lugar que Inimigos e Câmera já
    /// referenciam. Pôr o evento na camada da câmera criaria uma aresta Inimigos → Câmera que
    /// não existe hoje e que só cresceria.</para>
    ///
    /// <para><b>Quem assina precisa se desinscrever</b> no <c>OnDestroy</c>: evento estático
    /// segura o assinante vivo entre trocas de cena. Mesma disciplina do
    /// <c>HitStop.OnImpacto</c>.</para>
    /// </summary>
    public static class TremorDoMundo
    {
        /// <summary>
        /// Disparado quando algo no mundo sacode a tela. O argumento é <b>trauma</b>, de 0 a 1 —
        /// não deslocamento: quanto a tela treme é decisão da câmera, que conhece o zoom da cena
        /// e a amplitude configurada. Quem sacode diz <i>o quanto foi forte</i>, não <i>quantas
        /// unidades mexer</i>.
        /// </summary>
        public static event Action<float> OnTremor;

        /// <summary>
        /// Pede um tremor. Sem assinante, não acontece nada — e isso é de propósito: uma cena
        /// sem câmera de gameplay (o menu) não deveria quebrar por causa disto.
        /// </summary>
        /// <param name="trauma">De 0 a 1. Valores fora da faixa são responsabilidade de quem recebe.</param>
        public static void Sacudir(float trauma)
        {
            if (trauma <= 0f) return;

            OnTremor?.Invoke(trauma);
        }
    }
}
