namespace FavelaAmarela.Core.Loot
{
    /// <summary>
    /// Sorteia o <b>grau de impregnação</b> de um item que cai, com os pesos deslizando conforme
    /// o jogador sobe de nível.
    ///
    /// <para><b>O pedido do Vini (2026-08-28):</b> <i>"Nível 1: maioria dos itens de mais baixo
    /// tier, e construir uma escala de RNG onde seja possível o drop de uma arma ou armadura
    /// lendária na primeira fase, mas ter um drop realmente baixo. E ir escalonando conforme a
    /// progressão, onde no endgame você ignore totalmente os itens de T1."</i></para>
    ///
    /// <para><b>Nenhum peso é zero em nível nenhum</b>, e essa é a diferença entre uma curva e um
    /// portão. Portão de nível ("Impregnado só a partir do 5") produz um jogo em que o loot da
    /// primeira fase é sempre igual e ninguém tem motivo para abrir o próximo baú. Peso baixo
    /// produz a história que o jogador conta depois — <i>"caiu no primeiro Cultista"</i>.</para>
    ///
    /// <para><b>O Inerte some por PESO, não por bloqueio.</b> No teto ele vale 1,7% em vez de
    /// 80%: continua possível, e é isso que faz um drop ruim no endgame ser azar em vez de bug.</para>
    ///
    /// <para><b>Relíquia fica de fora do sorteio.</b> <see cref="RegrasDeGrau.PodeSerGerado"/> a
    /// recusa: relíquia é peça única e nomeada, autorada à mão, com história própria — o
    /// Necronomicon e o Anel do Sinal Amarelo caem porque a tabela do chefe os declara, não
    /// porque a sorte os inventou. O grau mais alto que este sorteio alcança é o
    /// <b>Impregnado</b>, e é ele o "lendário rolável" da curva.</para>
    /// </summary>
    public static class CurvaDeGrau
    {
        /// <summary>
        /// Peso de cada grau no nível 1, e quanto ele é multiplicado a cada nível.
        ///
        /// <para>Os números produzem, no nível 1: <b>Inerte 80,6%</b>, Marcado 16,1%,
        /// <b>Impregnado 3,2%</b>. E no teto de 12: Inerte <b>1,7%</b>, Marcado 44,3%,
        /// Impregnado <b>54,0%</b>. É a inversão que o pedido descreve.</para>
        /// </summary>
        private static readonly (GrauDeImpregnacao Grau, float Peso, float PorNivel)[] Pesos =
        {
            (GrauDeImpregnacao.Inerte,     100f, 0.75f),   // encolhe: some por peso
            (GrauDeImpregnacao.Marcado,     20f, 1.15f),
            (GrauDeImpregnacao.Impregnado,   4f, 1.35f),   // possível já no nível 1
        };

        /// <summary>
        /// Se um grau pode sair de sorteio, ou se é autorado à mão.
        ///
        /// <para><b>Relíquia é peça única e nomeada</b> — o Necronomicon, o Anel do Sinal
        /// Amarelo —, com história própria. Ela cai porque a tabela do chefe a declara, nunca
        /// porque a sorte a inventou.</para>
        ///
        /// <para>A regra mora <b>aqui</b>, no Core, junto do enum que ela julga:
        /// <c>RegrasDeGrau.PodeSerGerado</c> (camada de inventário) delega para cá. Duas cópias
        /// da mesma regra em camadas diferentes divergiriam em silêncio — e o sintoma seria uma
        /// relíquia aleatória, que quebra a promessa de cada uma ter história.</para>
        /// </summary>
        public static bool EhSorteavel(GrauDeImpregnacao grau)
            => grau != GrauDeImpregnacao.Reliquia;

        /// <summary>
        /// Sorteia um grau para o nível de jogador dado.
        /// </summary>
        /// <param name="nivelDoJogador">Nível de Exposição. Abaixo de 1 é tratado como 1.</param>
        /// <param name="minimo">
        /// Grau mínimo garantido pela fonte — o que a entrada da tabela autorou. Um chefe que
        /// declara Impregnado nunca larga Inerte por azar; a curva só pode <b>subir</b> a partir
        /// daí. É o que mantém "chefe dá recompensa" como promessa, e não como aposta.
        /// </param>
        /// <param name="fonte">Aleatoriedade injetada, para o sorteio ser afirmável em teste.</param>
        public static GrauDeImpregnacao Sortear(int nivelDoJogador, GrauDeImpregnacao minimo,
                                                IFonteDeAleatoriedade fonte)
        {
            // Relíquia autorada atravessa intacta: ela não é sorteável, e rebaixá-la aqui
            // transformaria o drop garantido do chefe num item comum.
            if (!EhSorteavel(minimo)) return minimo;

            if (nivelDoJogador < 1) nivelDoJogador = 1;

            float total = 0f;
            for (int i = 0; i < Pesos.Length; i++)
            {
                if (Pesos[i].Grau < minimo) continue;
                total += PesoNoNivel(i, nivelDoJogador);
            }

            // Sem fonte, ou com todos os pesos zerados, entrega o mínimo. Nunca devolve algo
            // pior do que a fonte prometeu.
            if (fonte == null || total <= 0f) return minimo;

            float sorteio = Limitar(fonte.ProximoValor()) * total;
            float acumulado = 0f;

            for (int i = 0; i < Pesos.Length; i++)
            {
                if (Pesos[i].Grau < minimo) continue;

                acumulado += PesoNoNivel(i, nivelDoJogador);
                if (sorteio < acumulado) return Pesos[i].Grau;
            }

            // Só se chega aqui por erro de ponto flutuante na última fatia.
            return Pesos[Pesos.Length - 1].Grau;
        }

        /// <summary>
        /// A chance de um grau no nível dado, em fração. Existe para a Forja do Debugger poder
        /// <b>mostrar a tabela</b> em vez de o autor ter de sortear mil vezes para descobri-la.
        /// </summary>
        public static float Chance(GrauDeImpregnacao grau, int nivelDoJogador)
        {
            if (nivelDoJogador < 1) nivelDoJogador = 1;

            float total = 0f, doGrau = 0f;

            for (int i = 0; i < Pesos.Length; i++)
            {
                float peso = PesoNoNivel(i, nivelDoJogador);
                total += peso;
                if (Pesos[i].Grau == grau) doGrau = peso;
            }

            return total <= 0f ? 0f : doGrau / total;
        }

        private static float PesoNoNivel(int indice, int nivel)
        {
            float peso = Pesos[indice].Peso;
            float porNivel = Pesos[indice].PorNivel;

            for (int n = 1; n < nivel; n++) peso *= porNivel;

            return peso < 0f ? 0f : peso;
        }

        private static float Limitar(float v) => v < 0f ? 0f : (v > 0.999999f ? 0.999999f : v);
    }
}
