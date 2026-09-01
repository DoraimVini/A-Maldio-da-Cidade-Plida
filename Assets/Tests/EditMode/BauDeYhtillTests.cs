using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using FavelaAmarela.Core.Persistencia;
using FavelaAmarela.Inventario;

namespace FavelaAmarela.Tests.EditMode
{
    /// <summary>
    /// Guarda o <b>Baú de Yhtill</b> — a recompensa material da quest da Cassilda.
    ///
    /// <para><b>Por que existe (2026-09-01).</b> "A Canção Incompleta" entregava só o Patuá das
    /// Luas Gêmeas, que é uma <i>relíquia de rito</i> cobrada lá no Rei em Amarelo. Do ponto de
    /// vista de quem está jogando a Fase 1, a maior quest do Santuário devolvia um item que não
    /// muda nada no minuto seguinte — a recompensa existia e não era <b>sentida</b>.</para>
    ///
    /// <para>Este teste guarda as três coisas que fazem o baú funcionar e que quebram caladas:
    /// ele está <b>na cena</b>, o portão aponta para a chave <b>certa</b>, e há um
    /// <c>DropAoAbater</c> no mesmo objeto — sem ele o baú abre <b>vazio</b>, porque quem
    /// materializa o espólio é ele.</para>
    /// </summary>
    public sealed class BauDeYhtillTests
    {
        private const string CenaDoSantuario = "Assets/Scenes/Santuario_Yhtill.unity";
        private const string CaminhoDaTabela =
            "Assets/FavelaAmarela/Config/Drops/Drop_BauDeYhtill.asset";

        private const string NomeDoBau = "Bau_DeYhtill";

        private static string Yaml()
        {
            Assert.IsTrue(File.Exists(CenaDoSantuario), $"Cena ausente: {CenaDoSantuario}");
            return File.ReadAllText(CenaDoSantuario);
        }

        /// <summary>A âncora do GameObject de nome dado, ou null.</summary>
        private static string AncoraDoBau(string yaml)
        {
            var docs = Regex.Split(yaml, @"^--- !u!\d+ &(\d+)", RegexOptions.Multiline);
            var pares = docs.Skip(1).Where((_, i) => i % 2 == 0)
                            .Zip(docs.Skip(2).Where((_, i) => i % 2 == 0), (a, d) => (a, d));

            foreach (var (ancora, corpo) in pares)
                if (Regex.IsMatch(corpo, $@"^  m_Name: {NomeDoBau}\s*$", RegexOptions.Multiline))
                    return ancora;

            return null;
        }

        [Test]
        public void OBauEstaNaCenaDoSantuario()
        {
            Assert.IsNotNull(AncoraDoBau(Yaml()),
                $"'{NomeDoBau}' não está em {CenaDoSantuario}. A quest da Cassilda volta a " +
                "terminar entregando só o Patuá — uma relíquia que só é cobrada no Castelo.");
        }

        [Test]
        public void OPortaoApontaParaAChaveDaCassilda()
        {
            string yaml = Yaml();

            StringAssert.Contains($"chaveDeSaveExigida: {ChavesDeSave.CassildaConcluida}", yaml,
                "O portão do baú não aponta para a chave de conclusão da quest. Ou o baú abre " +
                "de graça (e a quest deixa de valer), ou aponta para uma chave que ninguém " +
                "marca (e o baú nunca abre). As duas falham em silêncio.");
        }

        [Test]
        public void OBauTemQuemMaterializeOEspolio()
        {
            string yaml = Yaml();
            string ancora = AncoraDoBau(yaml);

            Assert.IsNotNull(ancora, $"'{NomeDoBau}' não está na cena.");

            // Quantos MonoBehaviour apontam para este GameObject. O baú precisa de dois: ele
            // mesmo e o DropAoAbater -- ele NÃO materializa nada por conta própria, de
            // propósito, para não duplicar o que o DropAoAbater já sabe fazer.
            int componentes = Regex.Matches(yaml, $@"m_GameObject: \{{fileID: {ancora}\}}").Count;

            Assert.GreaterOrEqual(componentes, 4,
                $"'{NomeDoBau}' tem só {componentes} componente(s) ligados. Esperados ao menos " +
                "4: Transform, SpriteRenderer, BoxCollider2D e os dois scripts. Faltando o " +
                "DropAoAbater, o baú abre e não entrega nada.");
        }

        [Test]
        public void OBroquelEGarantido()
        {
            var tabela = AssetDatabase.LoadAssetAtPath<TabelaDeDrop>(CaminhoDaTabela);
            Assert.IsNotNull(tabela, $"Tabela ausente: {CaminhoDaTabela}");

            var candidatos = tabela.ProjetarCandidatos();
            Assert.IsNotEmpty(candidatos, "A tabela do baú está vazia.");

            // O Id do broquel é 'broquel_couro_ressecado', em minúsculas -- os itens antigos
            // usam esse estilo, e só as armas novas têm Id igual ao nome do arquivo. Comparar
            // sem ignorar caixa foi o que fez este teste falhar na primeira execução.
            var broquel = candidatos.FirstOrDefault(
                c => c.ItemDefId.IndexOf("broquel", System.StringComparison.OrdinalIgnoreCase) >= 0);

            Assert.IsNotNull(broquel.ItemDefId,
                "O Broquel de Couro saiu da recompensa da quest. Ids presentes: " +
                string.Join(", ", candidatos.Select(c => c.ItemDefId)));

            Assert.IsTrue(broquel.Garantido,
                "O broquel deixou de ser garantido. Ele é a razão de a quest existir do ponto " +
                "de vista mecânico: a mitigação é subtrativa e o Byakhee bate 26 contra a " +
                "Defesa 6 do Damião — cinco golpes até o Colapso. Depender de sorte para isso " +
                "transforma a recompensa da exploração num sorteio que pode sair vazio.");
        }

        [Test]
        public void ATabelaNaoEntregaMaisDoQueOTeto()
        {
            var tabela = AssetDatabase.LoadAssetAtPath<TabelaDeDrop>(CaminhoDaTabela);
            Assert.IsNotNull(tabela, $"Tabela ausente: {CaminhoDaTabela}");

            Assert.Greater(tabela.TetoDeItens, 0,
                "Teto zerado significa SEM teto: o baú entregaria as três armas T2 de uma vez, " +
                "e o degrau de arma da Fase 1 perderia a graça na hora.");

            Assert.LessOrEqual(tabela.TetoDeItens, 4,
                $"Teto de {tabela.TetoDeItens} é generoso demais para uma quest opcional de " +
                "Fase 1 — o Byakhee, que fecha a fase, entrega 3.");
        }
    }
}
