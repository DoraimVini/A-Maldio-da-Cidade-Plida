using NUnit.Framework;
using UnityEditor;
using FavelaAmarela.Inventario;

namespace FavelaAmarela.Tests.EditMode
{
    /// <summary>
    /// Guarda os quatro assets de Artefato autorados à mão. Um asset que importa mas não
    /// vincula falharia em silêncio — o Artefato apareceria na barra sem fazer nada, sem um
    /// erro sequer no console.
    /// </summary>
    public class ArtefatoDefAssetsTests
    {
        private const string Pasta = "Assets/FavelaAmarela/Config/Resources/Artefatos/";

        private const string Necronomicon = Pasta + "Artefato_Necronomicon.asset";
        private const string Patua = Pasta + "Artefato_PatuaDasLuasGemeas.asset";
        private const string Anel = Pasta + "Artefato_AnelDoSinalAmarelo.asset";
        private const string Coroa = Pasta + "Artefato_CoroaDeOssos.asset";

        [TestCase(Necronomicon)]
        [TestCase(Patua)]
        [TestCase(Anel)]
        [TestCase(Coroa)]
        public void Artefato_CarregaComIdItemEHabilidade(string caminho)
        {
            var def = AssetDatabase.LoadAssetAtPath<ArtefatoDef>(caminho);
            Assert.IsNotNull(def, $"Asset não encontrado: {caminho}");

            Assert.IsFalse(string.IsNullOrEmpty(def.Id), "Artefato sem Id — o inventário guarda ids.");
            Assert.IsFalse(string.IsNullOrEmpty(def.NomeDaHabilidade), "Artefato sem nome de habilidade.");
            Assert.IsNotNull(def.Item, "Artefato sem ItemDef vinculado — nada o concede ao jogador.");
        }

        [TestCase(Necronomicon)]
        [TestCase(Patua)]
        [TestCase(Anel)]
        [TestCase(Coroa)]
        public void Artefato_ProduzUmAtivoValido(string caminho)
        {
            var def = AssetDatabase.LoadAssetAtPath<ArtefatoDef>(caminho);

            var ativo = def.CriarAtivo();

            Assert.IsNotNull(ativo);
            Assert.AreEqual(def.NomeDaHabilidade, ativo.Nome);
            Assert.IsTrue(ativo.Cooldown > 0f, "Habilidade sem recarga vira spam de botão.");
        }

        [Test]
        public void Necronomicon_EhArtefatoEnaoMaisChave()
        {
            var def = AssetDatabase.LoadAssetAtPath<ArtefatoDef>(Necronomicon);

            Assert.AreEqual(ItemType.Artefato, def.Item.Tipo,
                "O Necronomicon migrou de Chave para Artefato — se voltou a Chave, a passiva " +
                "seria contada duas vezes (mochila + slot de artefato).");
        }

        [Test]
        public void QuatroArtefatos_TemIdsDistintos()
        {
            var ids = new System.Collections.Generic.HashSet<string>();

            foreach (var caminho in new[] { Necronomicon, Patua, Anel, Coroa })
            {
                var def = AssetDatabase.LoadAssetAtPath<ArtefatoDef>(caminho);
                Assert.IsTrue(ids.Add(def.Id), $"Id repetido em {caminho}: '{def.Id}'.");
            }
        }
    }
}
