using System.Collections.Generic;
using FavelaAmarela.Core.Loot;

namespace FavelaAmarela.Inventario
{
    /// <summary>
    /// Monta um exemplar de item: base autorada + grau + afixos rolados.
    ///
    /// <para><b>POCO puro</b> — sem <c>MonoBehaviour</c>, sem cena, sem <c>Random</c> estático.
    /// Toda aleatoriedade passa por <see cref="IFonteDeAleatoriedade"/>, que já existe neste
    /// projeto e já é fakeada em teste (<c>SorteioDeDropTests</c>). Vive no assembly Runtime, e
    /// não em <c>Core</c>, porque depende de <c>StatType</c> e <c>ItemDef</c>, que são de lá —
    /// a exigência da Regra de Ouro 6 (testável sem a Unity rodando) continua atendida.</para>
    ///
    /// <para><b>A invariante:</b> o gerador <b>nunca inventa um afixo</b>. Ele escolhe de um
    /// pool autorado e rola dentro de uma faixa autorada. Isso é o que separa este sistema de
    /// um gerador procedural de verdade — e é o teto que o <c>CLAUDE.md</c> §1 mantém depois de
    /// revogar a invariante anterior.</para>
    /// </summary>
    public sealed class GeradorDeItem
    {
        /// <summary>
        /// Gera um exemplar.
        /// </summary>
        /// <param name="baseDoItem">O <c>ItemDef</c> autorado que serve de base.</param>
        /// <param name="grau">Quanto de Carcosa entrou neste exemplar.</param>
        /// <param name="nivelDoItem">
        /// Nível <b>do item</b>, derivado da fonte do drop — <b>não</b> o nível do jogador.
        /// Comparar com o nível do jogador é o bug clássico do gênero: uma zona inicial passa a
        /// dropar tier máximo assim que o jogador sobe.
        /// </param>
        /// <param name="pool">Todos os afixos do catálogo. A filtragem é feita aqui.</param>
        /// <param name="fonte">Fonte de aleatoriedade injetada.</param>
        /// <returns>
        /// O exemplar, ou <c>null</c> se a base for nula. Relíquia devolve o item <b>sem</b>
        /// afixos: ela é autorada à mão e nunca sorteada.
        /// </returns>
        public ItemInstance Gerar(ItemDef baseDoItem, GrauDeImpregnacao grau, int nivelDoItem,
                                  IReadOnlyList<AfixoDef> pool, IFonteDeAleatoriedade fonte)
        {
            if (baseDoItem == null) return null;

            var item = new ItemInstance(baseDoItem.Id)
            {
                Grau = grau,
                NivelDoItem = nivelDoItem < 1 ? 1 : nivelDoItem,
            };

            if (!RegrasDeGrau.PodeSerGerado(grau)) return item;
            if (pool == null || pool.Count == 0 || fonte == null) return item;

            // Grupos já usados: é o que impede "+5 Vitalidade" e "+8 Vitalidade" na mesma peça.
            var gruposUsados = new HashSet<string>();

            Sortear(item, TipoDeAfixo.Prefixo, RegrasDeGrau.Prefixos(grau),
                    baseDoItem, pool, fonte, gruposUsados);

            Sortear(item, TipoDeAfixo.Sufixo, RegrasDeGrau.Sufixos(grau),
                    baseDoItem, pool, fonte, gruposUsados);

            return item;
        }

        private static void Sortear(ItemInstance item, TipoDeAfixo tipo, int quantos,
                                    ItemDef baseDoItem, IReadOnlyList<AfixoDef> pool,
                                    IFonteDeAleatoriedade fonte, HashSet<string> gruposUsados)
        {
            for (int n = 0; n < quantos; n++)
            {
                var escolhido = Escolher(tipo, baseDoItem, item.NivelDoItem, pool, fonte,
                                         gruposUsados);

                // Pool esgotado (todo afixo legal já foi usado, ou não há nenhum para este
                // slot/nível): o item sai com menos afixos do que o grau promete. É degradação
                // correta -- melhor que repetir um afixo ou inventar um.
                if (escolhido == null) return;

                gruposUsados.Add(escolhido.Grupo);
                item.Afixos.Add(new AfixoRolado(escolhido.Id, escolhido.Stat,
                                                escolhido.Rolar(fonte, item.NivelDoItem)));
            }
        }

        /// <summary>
        /// Roleta ponderada sobre os afixos <b>legais</b>.
        ///
        /// <para><b>O peso é renormalizado depois do filtro</b>, e isso não é detalhe: somar os
        /// pesos do pool inteiro e sortear dentro dele faria os afixos barrados pelo gate de
        /// nível "roubarem" fatias do sorteio, enviesando a distribuição em silêncio — um afixo
        /// de peso 1 num pool onde metade está barrada não sai com o dobro da chance, sai com
        /// menos. É um dos erros mais difíceis de perceber num sistema de loot, porque o jogo
        /// continua funcionando e só as proporções ficam erradas.</para>
        /// </summary>
        private static AfixoDef Escolher(TipoDeAfixo tipo, ItemDef baseDoItem, int nivelDoItem,
                                         IReadOnlyList<AfixoDef> pool, IFonteDeAleatoriedade fonte,
                                         HashSet<string> gruposUsados)
        {
            float pesoTotal = 0f;

            for (int i = 0; i < pool.Count; i++)
            {
                var a = pool[i];
                if (!EhElegivel(a, tipo, baseDoItem, nivelDoItem, gruposUsados)) continue;
                pesoTotal += a.Peso;
            }

            if (pesoTotal <= 0f) return null;

            float alvo = fonte.ProximoValor() * pesoTotal;
            float acumulado = 0f;

            for (int i = 0; i < pool.Count; i++)
            {
                var a = pool[i];
                if (!EhElegivel(a, tipo, baseDoItem, nivelDoItem, gruposUsados)) continue;

                acumulado += a.Peso;
                if (alvo <= acumulado) return a;
            }

            // Só chega aqui por arredondamento de ponto flutuante quando o alvo cai exatamente
            // no fim: devolve o último elegível em vez de nada.
            for (int i = pool.Count - 1; i >= 0; i--)
            {
                var a = pool[i];
                if (EhElegivel(a, tipo, baseDoItem, nivelDoItem, gruposUsados)) return a;
            }

            return null;
        }

        private static bool EhElegivel(AfixoDef a, TipoDeAfixo tipo, ItemDef baseDoItem,
                                       int nivelDoItem, HashSet<string> gruposUsados)
        {
            if (a == null) return false;
            if (a.Tipo != tipo) return false;
            if (a.Peso <= 0f) return false;
            if (gruposUsados.Contains(a.Grupo)) return false;

            return a.EhLegalPara(baseDoItem.SlotEquipamento, nivelDoItem);
        }
    }
}
