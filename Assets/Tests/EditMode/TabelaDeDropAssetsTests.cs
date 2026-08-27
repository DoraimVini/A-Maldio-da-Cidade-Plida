using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using FavelaAmarela.Inventario;

namespace FavelaAmarela.Tests.EditMode
{
    /// <summary>
    /// Guarda os assets de tabela autorados à mão: verifica que as referências de
    /// <c>ItemDef</c> realmente resolvem e que a projeção para o Core não perde linha.
    /// Uma tabela que importa mas não vincula nada falharia silenciosamente em jogo —
    /// o inimigo simplesmente não largaria nada, sem erro no console.
    ///
    /// <para><b>Derivado em 2026-08-27.</b> Este guarda tinha as contagens escritas à mão
    /// (<c>Drop_BauDaTumba = 3</c>, <c>Drop_Cultista = 5</c>) e a lista de tabelas também. Com o
    /// arsenal indo crescer, as duas ficariam para trás — foi o modo de falha dominante deste
    /// projeto <b>nove</b> vezes. Agora a lista vem da pasta e a contagem vem do próprio asset,
    /// então tabela nova entra sozinha e linha nova não exige editar teste.</para>
    /// </summary>
    public sealed class TabelaDeDropAssetsTests
    {
        private const string PastaDasTabelas = "Assets/FavelaAmarela/Config/Drops";
        private const string CaminhoBau = PastaDasTabelas + "/Drop_BauDaTumba.asset";

        /// <summary>Toda tabela da pasta — varrida, não listada.</summary>
        private static string[] TodasAsTabelas() =>
            Directory.Exists(PastaDasTabelas)
                ? Directory.GetFiles(PastaDasTabelas, "*.asset", SearchOption.AllDirectories)
                           .Select(c => c.Replace(Path.DirectorySeparatorChar, '/'))
                           .OrderBy(c => c)
                           .ToArray()
                : new string[0];

        [Test]
        public void ExisteAlgumaTabela()
        {
            Assert.IsNotEmpty(TodasAsTabelas(),
                $"Nenhuma tabela em '{PastaDasTabelas}'. Se a pasta mudou de lugar, este guarda " +
                "parou de olhar para o jogo — e passaria verde sem verificar nada.");
        }

        /// <summary>
        /// A contagem esperada vem do <b>próprio asset</b> (<c>entradas.arraySize</c>), não de um
        /// número escrito aqui. O que está sendo testado é que
        /// <c>ProjetarCandidatos</c> não descarta nenhuma linha — e ele descarta, em silêncio
        /// (só um <c>LogWarning</c>), quando a referência de <c>ItemDef</c> está quebrada.
        /// </summary>
        [Test]
        [TestCaseSource(nameof(TodasAsTabelas))]
        public void Tabela_ProjetaTodasAsLinhasComIdValido(string caminho)
        {
            var tabela = AssetDatabase.LoadAssetAtPath<TabelaDeDrop>(caminho);
            Assert.IsNotNull(tabela, $"Asset não encontrado ou não é TabelaDeDrop: {caminho}");

            var so = new SerializedObject(tabela);
            var entradas = so.FindProperty("entradas");
            Assert.IsNotNull(entradas,
                "O campo 'entradas' sumiu da TabelaDeDrop — este guarda precisa ser reescrito.");

            int autoradas = entradas.arraySize;
            var candidatos = tabela.ProjetarCandidatos();

            Assert.AreEqual(autoradas, candidatos.Count,
                $"'{Path.GetFileName(caminho)}': {autoradas} linha(s) autorada(s) mas " +
                $"{candidatos.Count} projetada(s). Uma referência de ItemDef está quebrada — em " +
                "jogo isso é o inimigo não largar o item, sem erro nenhum no console.");

            foreach (var c in candidatos)
                Assert.IsFalse(string.IsNullOrEmpty(c.ItemDefId),
                    $"'{Path.GetFileName(caminho)}' projetou um candidato sem ItemDefId.");
        }

        /// <summary>
        /// Esta continua sendo uma afirmação de <b>design</b>, não uma contagem que envelhece: o
        /// baú da Tumba entrega uma arma, qualquer que seja o tamanho do catálogo.
        /// </summary>
        [Test]
        public void BauDaTumba_EntregaExatamenteUmaArma()
        {
            var tabela = AssetDatabase.LoadAssetAtPath<TabelaDeDrop>(CaminhoBau);
            Assert.IsNotNull(tabela, $"Asset não encontrado: {CaminhoBau}");

            Assert.AreEqual(1, tabela.TetoDeItens,
                "O baú entrega uma arma só — teto diferente de 1 quebra a premissa da Tumba.");
        }
    }
}
