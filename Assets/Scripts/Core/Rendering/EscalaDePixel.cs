namespace FavelaAmarela.Core.Rendering
{
    /// <summary>
    /// A aritmética que faz um pixel de arte ocupar sempre o mesmo número de pixels de tela.
    ///
    /// <para><b>Por que isto virou uma classe (2026-08-27).</b> O número <c>4,21875</c> estava
    /// escrito à mão numa constante de ferramenta, com a conta explicada só em comentário — e
    /// <b>sete ferramentas de Editor</b> montavam câmera cada uma com o seu valor:
    /// <c>PadronizarCanvasDasCenas</c> usava 4,21875 (e 5,625 nas arenas),
    /// <c>MontarPortoesDasRuinas</c> 7, <c>MontarCasteloCarcosa</c> /
    /// <c>MontarCenaDoSantuario</c> / <c>BuildDesertOverworld</c> 6, e
    /// <c>PrefabMigrationTool</c> 8. A cena ficava com o valor de quem rodou por último: os
    /// Portões estavam em <b>7</b> quando o padrão dizia 5,625.</para>
    ///
    /// <para><b>A conta.</b> A altura visível em unidades de mundo é <c>tamanhoOrtográfico × 2</c>.
    /// Multiplicada pela PPU, dá a altura da arte em pixels. Para a ampliação ser inteira, essa
    /// altura tem de caber um número exato de vezes na altura da tela alvo:</para>
    ///
    /// <code>
    ///   alturaDeReferência = alturaDaTela / ampliação
    ///   tamanhoOrtográfico = alturaDeReferência / (2 × PPU)
    /// </code>
    ///
    /// <para>A 1080 de tela, PPU 32 e ampliação 4: referência 270 px, ortográfico
    /// <b>4,21875</b>. Ampliação 3: referência 360 px, ortográfico <b>5,625</b>. Qualquer valor
    /// fora dessa família mistura pixels de tamanhos diferentes, e a arte <i>cintila</i> ao
    /// mover — foi um dos "tudo parece meio fora" do playtest.</para>
    ///
    /// <para><b>Isto continua importando com o <c>PixelPerfectCamera</c> ligado.</b> O componente
    /// não usa o tamanho ortográfico: ele usa a <b>resolução de referência</b> e recalcula o
    /// tamanho a cada quadro, para qualquer tela. É esta classe que diz qual é essa resolução —
    /// e o tamanho ortográfico continua sendo escrito na cena para que o Scene View e o Game
    /// View fora de Play mostrem o mesmo enquadramento do jogo rodando.</para>
    /// </summary>
    public static class EscalaDePixel
    {
        /// <summary>
        /// Pixels por unidade de mundo. É o padrão único do projeto desde 2026-07-28 e a skill
        /// <c>favela-pixelart-standards</c> o exige em todo sprite. Mudar aqui sem reimportar a
        /// arte quebra a correspondência.
        /// </summary>
        public const int PixelsPorUnidade = 32;

        /// <summary>
        /// Altura da tela para a qual a ampliação é calculada: 1080p. É a resolução alvo do
        /// jogo, e a única em que a ampliação sai exata sem o <c>PixelPerfectCamera</c> — com
        /// ele, este número só define a <i>família</i> de resoluções de referência.
        /// </summary>
        public const int AlturaDaTelaAlvo = 1080;

        /// <summary>Ampliação padrão do jogo: 4× (referência 480 × 270).</summary>
        public const int AmpliacaoPadrao = 4;

        /// <summary>
        /// Ampliação das arenas de chefe: 3× (referência 640 × 360). Mais aberto de propósito —
        /// a luta do Byakhee é aérea, com rasantes que atravessam a arena, e a 4× o jogador
        /// perde o chefe de vista no meio do mergulho.
        /// </summary>
        public const int AmpliacaoDeArena = 3;

        /// <summary>Proporção da tela alvo (16:9), para derivar a largura da referência.</summary>
        private const float ProporcaoDaTela = 16f / 9f;

        /// <summary>
        /// Altura da resolução de referência, em pixels, para uma dada ampliação inteira.
        /// </summary>
        /// <param name="ampliacao">Quantas vezes cada pixel de arte é ampliado na tela alvo.</param>
        public static int AlturaDeReferencia(int ampliacao)
        {
            if (ampliacao < 1) ampliacao = 1;
            return AlturaDaTelaAlvo / ampliacao;
        }

        /// <summary>
        /// Largura da resolução de referência, em pixels, mantendo 16:9.
        /// </summary>
        public static int LarguraDeReferencia(int ampliacao)
            => (int)(AlturaDeReferencia(ampliacao) * ProporcaoDaTela + 0.5f);

        /// <summary>
        /// O <c>orthographicSize</c> que corresponde a uma ampliação inteira. É metade da altura
        /// visível em unidades de mundo.
        /// </summary>
        public static float TamanhoOrtografico(int ampliacao)
            => AlturaDeReferencia(ampliacao) / (2f * PixelsPorUnidade);

        /// <summary>
        /// Se um <c>orthographicSize</c> qualquer corresponde a alguma ampliação inteira na tela
        /// alvo — ou seja, se ele preserva o tamanho do pixel.
        /// </summary>
        /// <param name="tamanhoOrtografico">O valor autorado na câmera.</param>
        /// <param name="tolerancia">Folga de ponto flutuante.</param>
        public static bool PreservaOPixel(float tamanhoOrtografico, float tolerancia = 0.0001f)
        {
            if (tamanhoOrtografico <= 0f) return false;

            float alturaEmPixels = tamanhoOrtografico * 2f * PixelsPorUnidade;
            float ampliacao = AlturaDaTelaAlvo / alturaEmPixels;

            float inteiro = (int)(ampliacao + 0.5f);
            return inteiro >= 1f && System.Math.Abs(ampliacao - inteiro) <= tolerancia;
        }
    }
}
