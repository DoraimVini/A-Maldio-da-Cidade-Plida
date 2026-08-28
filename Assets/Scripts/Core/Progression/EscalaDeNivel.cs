namespace FavelaAmarela.Core.Progression
{
    /// <summary>
    /// A <b>lei de escala</b> do jogo: como um número cresce com o nível. Uma só, usada pela
    /// arma e pela ficha, para que "estar no nível 3" queira dizer a mesma coisa nos dois lados.
    ///
    /// <para><b>Por que existe (2026-08-28).</b> O Vini pediu que a escala <i>cresça com o jogo
    /// e com o personagem</i> — "saber que ele no nível 2 está mais forte e com mais defesa".
    /// Sem uma lei única, cada sistema inventaria a sua e as duas divergiriam em silêncio; é o
    /// modo de falha que este projeto já pagou sete vezes (sete ferramentas, sete zooms de
    /// câmera).</para>
    ///
    /// <para><b>Crescimento linear, não exponencial.</b> A curva de Exposição fecha em
    /// <b>12 níveis</b> (<see cref="Progressao"/>) — é campanha de ~4h, não ARPG de temporada.
    /// Exponencial num teto de 12 produziria ou um começo irrelevante ou um fim absurdo; linear
    /// dá um passo legível a cada nível e mantém o número na cabeça do jogador.</para>
    ///
    /// <para><b>O nível 1 é sempre o valor autorado.</b> Nenhum asset precisa ser reescrito para
    /// entrar na escala: quem está no nível 1 vale exatamente o que o Inspector mostra. Foi essa
    /// a condição para migrar sem rebalancear tudo de uma vez.</para>
    /// </summary>
    public static class EscalaDeNivel
    {
        /// <summary>
        /// Quanto cada nível acrescenta, em fração do valor base. 0,25 = +25% do valor de nível
        /// 1 por nível, então no teto de 12 uma arma bate <b>3,75×</b> o que batia no começo.
        ///
        /// <para>É o botão que define a inclinação da curva de poder inteira. Mexer aqui muda
        /// todo encontro do jogo de uma vez — de propósito: é para isso que a lei é única.</para>
        /// </summary>
        public const float GanhoPorNivel = 0.25f;

        /// <summary>
        /// Multiplicador de dano de uma arma no nível pedido. Nível 1 devolve exatamente 1.
        /// </summary>
        public static float FatorDeDano(int nivel) => Fator(nivel, GanhoPorNivel);

        /// <summary>
        /// Valor escalado de um atributo autorado. <paramref name="ganhoPorNivel"/> é a fração
        /// por nível daquele atributo — Vitalidade e Defesa crescem em ritmos diferentes, e
        /// forçá-los ao mesmo passo é como um jogo fica ou trivial ou impossível no meio.
        /// </summary>
        public static float Valor(float baseNivel1, float ganhoPorNivel, int nivel)
            => baseNivel1 * Fator(nivel, ganhoPorNivel);

        /// <summary>
        /// O fator bruto. Nível abaixo de 1 é tratado como 1 — nível zero é sempre erro de
        /// autoria ou dado não serializado, nunca uma unidade "mais fraca que o começo".
        /// </summary>
        public static float Fator(int nivel, float ganhoPorNivel)
        {
            if (nivel < 1) nivel = 1;
            if (ganhoPorNivel < 0f) ganhoPorNivel = 0f;

            return 1f + ganhoPorNivel * (nivel - 1);
        }
    }
}
