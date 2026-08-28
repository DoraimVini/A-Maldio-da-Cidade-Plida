namespace FavelaAmarela.Runtime.UI
{
    /// <summary>
    /// A tipografia de <b>todo texto de diálogo</b> do jogo — fala e escolha — num lugar só.
    ///
    /// <para><b>Por que isto existe (playtest de 2026-08-28).</b> O Vini relatou: <i>"a do Abdul
    /// está pequena demais e mal dá para ler o texto, já a da Cassilda as letras estão grandes
    /// demais e não cabem na caixa"</i>. Medido, era o <b>mesmo componente</b>
    /// (<see cref="PainelDeEscolha"/>) autorado à mão em cada cena: fonte <b>16</b> na Tumba,
    /// fonte <b>60</b> no Santuário, e <b>60</b> na caixa de fala da HUD. Quatro vezes de
    /// diferença no mesmo widget — a mesma família do zoom de câmera, que tinha sete valores em
    /// sete ferramentas.</para>
    ///
    /// <para><b>Por que Best Fit, e não um número melhor.</b> Tamanho fixo é uma aposta sobre o
    /// comprimento do texto, e o texto deste jogo varia <b>4×</b>: a fala mais curta do Abdul
    /// tem 72 caracteres; a reação mais longa da Cassilda tem <b>278</b>. Nenhum número serve
    /// aos dois, e foi tentar servir que produziu 16 de um lado e 60 do outro. O Best Fit
    /// responde essa pergunta por caixa e por fala, que é justamente o que estava sendo
    /// respondido à mão.</para>
    ///
    /// <para><b>E o transbordo vertical é Overflow, nunca Truncate.</b> Com Truncate, a fala que
    /// não cabe é <b>cortada sem aviso</b>: o jogador perde o fim da frase e nada denuncia.
    /// Transbordar é feio e <i>visível</i>; cortar é limpo e é mentira. Este projeto já pagou
    /// caro demais pelo defeito que não se vê.</para>
    /// </summary>
    public static class PadraoDeTextoDeDialogo
    {
        /// <summary>
        /// Piso do Best Fit. Abaixo disto o texto deixa de ser legível a 1080p — e é melhor a
        /// caixa transbordar (visível) do que a letra encolher até sumir (silencioso).
        /// </summary>
        public const int TamanhoMinimo = 24;

        /// <summary>
        /// Teto do Best Fit, calibrado pelo pior caso real: a reação mais longa da Cassilda
        /// (278 caracteres) na caixa de diálogo da HUD (1613 × 259 px na referência 1920×1080)
        /// ocupa cerca de 4 linhas a 44. Acima disso, uma fala longa não teria como caber.
        /// </summary>
        public const int TamanhoMaximo = 44;
    }
}
