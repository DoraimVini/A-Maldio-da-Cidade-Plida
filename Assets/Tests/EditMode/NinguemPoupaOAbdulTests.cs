using System.IO;
using System.Linq;
using FavelaAmarela.Runtime.Enemies;
using NUnit.Framework;

namespace FavelaAmarela.Tests.EditMode
{
    /// <summary>
    /// A trégua com o Abdul não volta sem ninguém perceber.
    ///
    /// <para>Desde 2026-09-28 a luta é obrigatória: poupar o Abdul deixava o jogador sem o
    /// Necronomicon e sem como selar o Rei (ver <c>ALutaDoAbdulEhObrigatoriaTests</c>). Este
    /// guarda lê o código e o prefab — é barato e pega o caminho mais provável de regressão:
    /// alguém religar a escolha ou restaurar o prefab antigo.</para>
    /// </summary>
    public sealed class NinguemPoupaOAbdulTests
    {
        private const string Scripts = "Assets/Scripts";
        private const string Prefab = "Assets/FavelaAmarela/Art/Enemies/Abdul_Alhazred.prefab";

        [Test]
        public void NenhumCodigo_GravaQueOAbdulFoiPoupado()
        {
            var infratores = Directory.GetFiles(Scripts, "*.cs", SearchOption.AllDirectories)
                .Where(f => !f.EndsWith("ChavesDeSave.cs"))
                .Where(f => File.ReadAllText(f).Contains("ValorAbdulPoupado"))
                .Select(Path.GetFileName)
                .ToList();

            Assert.IsEmpty(infratores,
                "Código voltou a usar ValorAbdulPoupado: " + string.Join(", ", infratores) +
                ". A trégua saiu em 2026-09-28 — quem poupa o Abdul chega ao Rei sem o Necronomicon.");
        }

        [Test]
        public void OPrefab_TemAConversaQueTerminaEmLuta()
        {
            string txt = File.ReadAllText(Prefab);

            Assert.IsFalse(txt.Contains("textoOpcaoConcordar"),
                "O prefab do Abdul ainda serializa a opção de poupar.");

            // A última fala do padrão é a reação ofendida; basta conferir um trecho dela, já que o
            // YAML guarda acentos escapados.
            Assert.IsTrue(txt.Contains("Vais ler a primeira p"),
                "O prefab do Abdul não tem a conversa nova — a lista serializada manda sobre o default " +
                "do C#. Grave AbdulAlhazredAI.FalasPadrao no prefab.");

            Assert.GreaterOrEqual(AbdulAlhazredAI.FalasPadrao.Length, 2);
        }
    }
}
