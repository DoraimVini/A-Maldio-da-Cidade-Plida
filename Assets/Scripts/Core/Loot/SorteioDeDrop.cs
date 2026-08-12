using System.Collections.Generic;

namespace FavelaAmarela.Core.Loot
{
    /// <summary>
    /// A regra do sorteio de espólio, isolada do mundo: recebe candidatos, o nível de
    /// Exposição de Damião e uma fonte de aleatoriedade injetada, e devolve o que caiu.
    ///
    /// <para>Escolhe <b>qual</b> item cai, <b>nunca</b> gera atributos — cada item segue
    /// autorado à mão e determinístico. É essa invariante que segura o escopo.</para>
    ///
    /// <para>Dois modos: <see cref="Sortear"/> (várias entradas independentes — inimigo
    /// abatido) e <see cref="SortearUm"/> (exatamente uma, ponderada — baú selado).</para>
    /// </summary>
    public sealed class SorteioDeDrop
    {
        private static readonly IReadOnlyList<ItemSorteado> Vazio = new List<ItemSorteado>();

        /// <summary>
        /// Resolve a tabela inteira. Ordem: garantidos primeiro (sem chance, sem gate de
        /// nível), depois cada entrada por chance independente, respeitando o teto e sem
        /// repetir o mesmo item na mesma resolução.
        /// </summary>
        /// <param name="candidatos">Linhas da tabela. Nulo ou vazio devolve lista vazia.</param>
        /// <param name="nivelDoJogador">Nível de Exposição atual, que libera entradas mais impregnadas.</param>
        /// <param name="fonte">Fonte de aleatoriedade. Nula devolve só os garantidos.</param>
        /// <param name="tetoDeItens">Máximo de itens por resolução; &lt;= 0 significa sem teto.</param>
        public IReadOnlyList<ItemSorteado> Sortear(IReadOnlyList<CandidatoDeDrop> candidatos,
            int nivelDoJogador, IFonteDeAleatoriedade fonte, int tetoDeItens)
        {
            if (candidatos == null || candidatos.Count == 0) return Vazio;

            var resultado = new List<ItemSorteado>();
            var jaCaiu = new HashSet<string>();

            for (int i = 0; i < candidatos.Count; i++)
            {
                var c = candidatos[i];
                if (!c.Garantido) continue;
                AdicionarSePossivel(c, resultado, jaCaiu, fonte, tetoDeItens);
            }

            if (fonte == null) return resultado;

            for (int i = 0; i < candidatos.Count; i++)
            {
                var c = candidatos[i];
                if (c.Garantido) continue;
                if (c.NivelMinimo > nivelDoJogador) continue;
                if (AtingiuTeto(resultado.Count, tetoDeItens)) break;

                if (fonte.ProximoValor() < c.Chance)
                    AdicionarSePossivel(c, resultado, jaCaiu, fonte, tetoDeItens);
            }

            return resultado;
        }

        /// <summary>
        /// Escolhe <b>exatamente um</b> item entre os elegíveis, ponderado pela chance
        /// (pesos iguais = uniforme). É a semântica do baú, que sempre entrega uma peça —
        /// diferente das chances independentes de um inimigo abatido.
        /// </summary>
        /// <returns>O item sorteado, ou <c>null</c> se nada for elegível.</returns>
        public ItemSorteado? SortearUm(IReadOnlyList<CandidatoDeDrop> candidatos,
            int nivelDoJogador, IFonteDeAleatoriedade fonte)
        {
            if (candidatos == null || candidatos.Count == 0 || fonte == null) return null;

            var elegiveis = new List<CandidatoDeDrop>();
            float pesoTotal = 0f;

            for (int i = 0; i < candidatos.Count; i++)
            {
                var c = candidatos[i];
                if (string.IsNullOrEmpty(c.ItemDefId)) continue;
                if (!c.Garantido && c.NivelMinimo > nivelDoJogador) continue;
                if (c.Chance <= 0f) continue;

                elegiveis.Add(c);
                pesoTotal += c.Chance;
            }

            if (elegiveis.Count == 0 || pesoTotal <= 0f) return null;

            float alvo = fonte.ProximoValor() * pesoTotal;
            float acumulado = 0f;

            for (int i = 0; i < elegiveis.Count; i++)
            {
                acumulado += elegiveis[i].Chance;
                if (alvo < acumulado)
                    return new ItemSorteado(elegiveis[i].ItemDefId, Quantidade(elegiveis[i], fonte));
            }

            // Só alcançável por imprecisão de ponto flutuante no limite superior.
            var ultimo = elegiveis[elegiveis.Count - 1];
            return new ItemSorteado(ultimo.ItemDefId, Quantidade(ultimo, fonte));
        }

        private static void AdicionarSePossivel(CandidatoDeDrop c, List<ItemSorteado> resultado,
            HashSet<string> jaCaiu, IFonteDeAleatoriedade fonte, int tetoDeItens)
        {
            if (string.IsNullOrEmpty(c.ItemDefId)) return;
            if (AtingiuTeto(resultado.Count, tetoDeItens)) return;
            if (!jaCaiu.Add(c.ItemDefId)) return;

            resultado.Add(new ItemSorteado(c.ItemDefId, Quantidade(c, fonte)));
        }

        private static bool AtingiuTeto(int quantosJaCairam, int tetoDeItens)
            => tetoDeItens > 0 && quantosJaCairam >= tetoDeItens;

        private static int Quantidade(CandidatoDeDrop c, IFonteDeAleatoriedade fonte)
        {
            if (c.QuantidadeMax <= c.QuantidadeMin || fonte == null) return c.QuantidadeMin;
            return fonte.ProximoInteiro(c.QuantidadeMin, c.QuantidadeMax + 1);
        }
    }
}
