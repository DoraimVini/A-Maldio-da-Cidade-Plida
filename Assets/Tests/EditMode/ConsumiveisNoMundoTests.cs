using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace FavelaAmarela.Tests.EditMode
{
    /// <summary>
    /// Guarda a <b>obtenibilidade</b> dos consumíveis. O <c>ConsumiveisAssetsTests</c> já
    /// verifica que os itens estão bem autorados; este verifica algo diferente e que passou
    /// meses errado: que o jogador <b>consegue pegá-los</b>.
    ///
    /// <para><b>O que motivou (2026-08-12):</b> os três <c>ItemDef</c> existiam, o pipeline de
    /// consumo funcionava ponta a ponta, e o roadmap dizia que faltava "conteúdo". A lacuna real
    /// era mais simples e mais grave: <b>zero instâncias de <c>ColetavelDeItem</c> de consumível
    /// em qualquer cena</b>, e nenhuma <c>TabelaDeDrop</c> os incluía. Os itens existiam e nada
    /// no mundo os entregava — sem erro, sem aviso.</para>
    ///
    /// <para>Lê o YAML da cena em vez de abrir a cena: um teste EditMode que chama
    /// <c>OpenScene</c> mexe no estado do Editor de quem está rodando a suíte. A mesma técnica
    /// do <c>FichaAtributosAssetsTests</c>.</para>
    /// </summary>
    public sealed class ConsumiveisNoMundoTests
    {
        private const string CenaDoDeserto =
            "Assets/Scenes/Deserto_Hali.unity";

        /// <summary>
        /// Quantos de cada consumível a ferramenta <c>MontarConsumiveisDoDeserto</c> espalha.
        /// É o dial de escassez — se mudar lá, mude aqui, de propósito: a quantidade é decisão
        /// de balanceamento e merece falhar visível em vez de derrapar sem ninguém notar.
        /// </summary>
        private static readonly (string Id, int Quantidade)[] Esperado =
        {
            ("consumivel_agua_cacimba", 4),
            ("consumivel_erva_ancoragem", 3),
            ("consumivel_raiz_yhtill", 2),
        };

        private static List<string> ChavesDeConsumivelNaCena()
        {
            Assert.IsTrue(File.Exists(CenaDoDeserto), $"Cena não encontrada: {CenaDoDeserto}");

            var achadas = new List<string>();
            var padrao = new Regex(@"chaveDeSave: (Item\.Deserto\.[a-z_]+\.\d+)");

            foreach (string linha in File.ReadAllLines(CenaDoDeserto))
            {
                var m = padrao.Match(linha);
                if (m.Success) achadas.Add(m.Groups[1].Value);
            }

            return achadas;
        }

        [Test]
        public void CadaConsumivelTemInstanciasNoDeserto()
        {
            var chaves = ChavesDeConsumivelNaCena();

            foreach (var (id, quantidade) in Esperado)
            {
                int encontradas = 0;
                foreach (string chave in chaves)
                    if (chave.StartsWith($"Item.Deserto.{id}.")) encontradas++;

                Assert.AreEqual(quantidade, encontradas,
                    $"'{id}' deveria ter {quantidade} instância(s) no Deserto e tem {encontradas}. " +
                    "Rode 'Tools/FavelaAmarela/Montar consumíveis do Deserto' — sem isto o item " +
                    "existe como asset mas o jogador não tem como obtê-lo.");
            }
        }

        [Test]
        public void ChavesDeSaveSaoDistintas()
        {
            var chaves = ChavesDeConsumivelNaCena();
            var unicas = new HashSet<string>(chaves);

            Assert.AreEqual(chaves.Count, unicas.Count,
                "Chave repetida faz um coletável recolhido sumir com o outro no save.");
        }

        /// <summary>
        /// Trava a lição de 2026-08-12: <c>PovoarODeserto</c> usa
        /// <c>ObjetoPersistente.GarantirChave()</c>, que sorteia GUID novo a cada reconstrução —
        /// rodar a ferramenta de novo troca todas as chaves e o save perde o registro de tudo
        /// que o jogador já pegou. A ferramenta de consumíveis usa chave <b>derivada</b>
        /// (<c>Item.Deserto.&lt;id&gt;.&lt;índice&gt;</c>) para ser reexecutável em segurança.
        /// Este teste falha se alguém trocar por chave aleatória.
        /// </summary>
        [Test]
        public void ChavesSaoDerivadas_NaoGuidAleatorio()
        {
            var chaves = ChavesDeConsumivelNaCena();
            Assert.IsNotEmpty(chaves, "Nenhuma chave de consumível na cena — nada a verificar.");

            var guidCru = new Regex(@"^[0-9a-f]{32}$");
            var derivada = new Regex(@"^Item\.Deserto\.[a-z_]+\.\d+$");

            foreach (string chave in chaves)
            {
                Assert.IsFalse(guidCru.IsMatch(chave),
                    $"'{chave}' parece GUID aleatório: rodar a ferramenta de novo invalidaria o save.");
                Assert.IsTrue(derivada.IsMatch(chave),
                    $"'{chave}' fora do padrão derivado esperado.");
            }
        }

        [Test]
        public void TodoColetavelDeConsumivelTemChave()
        {
            // Chave vazia faz o coletável reaparecer a cada carga de cena — para espólio de
            // inimigo isso é correto, mas para um consumível finito no mapa vira farm infinito
            // e destrói o modelo de escassez inteiro.
            string cena = File.ReadAllText(CenaDoDeserto);

            int coletaveis = Regex.Matches(cena, @"m_Name: Coletavel_consumivel_").Count;
            int chaves = ChavesDeConsumivelNaCena().Count;

            Assert.AreEqual(coletaveis, chaves,
                $"{coletaveis} coletável(is) de consumível na cena mas {chaves} chave(s) de save. " +
                "Coletável sem chave reaparece a cada carga — farm infinito.");
        }
    }
}
