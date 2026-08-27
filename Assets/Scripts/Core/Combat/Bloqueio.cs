using FavelaAmarela.Core.Loot;

namespace FavelaAmarela.Core.Combat
{
    /// <summary>O que aconteceu quando um golpe encontrou um escudo.</summary>
    public readonly struct ResultadoDeBloqueio
    {
        /// <summary>Se o escudo aparou o golpe.</summary>
        public readonly bool Bloqueou;

        /// <summary>O dano que efetivamente passou.</summary>
        public readonly float DanoFinal;

        /// <summary>Cria o resultado.</summary>
        public ResultadoDeBloqueio(bool bloqueou, float danoFinal)
        {
            Bloqueou = bloqueou;
            DanoFinal = danoFinal;
        }
    }

    /// <summary>
    /// Bloqueio por escudo na Mão Secundária.
    ///
    /// <para><b>É chance, não botão — e a escolha é deliberada.</b> Num isométrico com câmera
    /// afastada e vários inimigos em volta, segurar um botão para aparar exige ler direção e
    /// tempo de um alvo que o jogador mal distingue no meio da tela. É a mecânica de um jogo de
    /// câmera baixa atrás do ombro, não deste. O D2 resolveu isso do mesmo jeito: o escudo é um
    /// <b>atributo</b>, e o bloqueio acontece sozinho.</para>
    ///
    /// <para>Ganho prático: a Mão Secundária vira uma decisão de <i>build</i> — sobrevivência
    /// contra poder — em vez de mais uma tecla para segurar. E não exige uma ação nova no
    /// Input System.</para>
    ///
    /// <para>POCO puro, com a aleatoriedade injetada por <see cref="IFonteDeAleatoriedade"/>:
    /// bloqueio testável é bloqueio que se pode balancear.</para>
    /// </summary>
    public static class Bloqueio
    {
        /// <summary>
        /// Teto da chance de bloqueio. Sem ele, empilhar escudo e afixos levaria a 100% —
        /// imunidade, que nenhum item deveria conceder.
        /// </summary>
        public const float ChanceMaxima = 0.6f;

        /// <summary>
        /// Tenta aparar um golpe.
        /// </summary>
        /// <param name="danoBruto">Dano que chegaria sem escudo.</param>
        /// <param name="chance">Probabilidade de bloquear, de 0 a 1. Limitada por
        /// <see cref="ChanceMaxima"/>.</param>
        /// <param name="reducao">Fração do dano aparada quando bloqueia, de 0 a 1.</param>
        /// <param name="fonte">Fonte de aleatoriedade injetada.</param>
        public static ResultadoDeBloqueio Tentar(float danoBruto, float chance, float reducao,
                                                 IFonteDeAleatoriedade fonte)
        {
            if (danoBruto <= 0f) return new ResultadoDeBloqueio(false, 0f);
            if (chance <= 0f || fonte == null) return new ResultadoDeBloqueio(false, danoBruto);

            float chanceReal = chance > ChanceMaxima ? ChanceMaxima : chance;

            if (fonte.ProximoValor() >= chanceReal)
                return new ResultadoDeBloqueio(false, danoBruto);

            float reducaoReal = reducao < 0f ? 0f : (reducao > 1f ? 1f : reducao);

            return new ResultadoDeBloqueio(true, danoBruto * (1f - reducaoReal));
        }
    }
}
