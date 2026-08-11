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
    /// </summary>
    public class TabelaDeDropAssetsTests
    {
        private const string CaminhoBau = "Assets/FavelaAmarela/Config/Drops/Drop_BauDaTumba.asset";
        private const string CaminhoCultista = "Assets/FavelaAmarela/Config/Drops/Drop_Cultista.asset";

        [TestCase(CaminhoBau, 3)]
        [TestCase(CaminhoCultista, 3)]
        public void Tabela_ProjetaTodasAsLinhasComIdValido(string caminho, int linhasEsperadas)
        {
            var tabela = AssetDatabase.LoadAssetAtPath<TabelaDeDrop>(caminho);
            Assert.IsNotNull(tabela, $"Asset não encontrado: {caminho}");

            var candidatos = tabela.ProjetarCandidatos();
            Assert.AreEqual(linhasEsperadas, candidatos.Count,
                "Linha perdida na projeção — provável referência de ItemDef quebrada.");

            foreach (var c in candidatos)
                Assert.IsFalse(string.IsNullOrEmpty(c.ItemDefId), "Candidato sem ItemDefId.");
        }

        [Test]
        public void BauDaTumba_EntregaExatamenteUmaArma()
        {
            var tabela = AssetDatabase.LoadAssetAtPath<TabelaDeDrop>(CaminhoBau);

            Assert.AreEqual(1, tabela.TetoDeItens,
                "O baú entrega uma arma só — teto diferente de 1 quebra a premissa da Tumba.");
        }
    }
}
