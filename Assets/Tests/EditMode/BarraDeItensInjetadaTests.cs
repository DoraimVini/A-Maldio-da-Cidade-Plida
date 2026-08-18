using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace FavelaAmarela.Tests.EditMode
{
    /// <summary>
    /// Guarda a Fase 4: a <c>BarraDeItens</c> recebe o inventário por injeção e <b>não</b> volta a
    /// buscá-lo sozinha.
    ///
    /// <para><b>O que motivou (2026-08-18):</b> a barra alcançava <c>InventoryManager.Instance</c>
    /// em cinco pontos, um deles <b>dentro do <c>Update</c></b> — busca de singleton a cada frame,
    /// proibida pela Regra de Ouro 1 do <c>CLAUDE.md</c>. Havia ainda
    /// <c>OnDisable → Unbind</c> sem contrapartida no <c>OnEnable</c>: desativar e reativar o
    /// GameObject deixava a barra morta, desenhada e congelada.</para>
    ///
    /// <para>Este guarda lê o <b>fonte</b>, não a cena: o defeito era de código, e um teste de
    /// wiring de cena não o alcançaria.</para>
    /// </summary>
    public sealed class BarraDeItensInjetadaTests
    {
        private const string Barra = "Assets/Scripts/UI/BarraDeItens.cs";
        private const string Hud = "Assets/Scripts/UI/HUDController.cs";
        private const string Bootstrap = "Assets/Scripts/GameLoop/GameLoopBootstrap.cs";

        [Test]
        public void BarraDeItens_NaoAlcancaOSingletonSozinha()
        {
            Assert.IsTrue(File.Exists(Barra), $"Arquivo ausente: {Barra}");

            string codigo = SemComentarios(File.ReadAllText(Barra));
            var ocorrencias = Regex.Matches(codigo, @"InventoryManager\s*\.\s*Instance");

            Assert.AreEqual(0, ocorrencias.Count,
                $"{ocorrencias.Count} referência(s) a InventoryManager.Instance na BarraDeItens. " +
                "A fonte deve chegar por Bind(InventoryManager), injetada pelo HUDController. " +
                "Buscar o singleton aqui traz de volta a busca por frame no Update.");
        }

        /// <summary>
        /// Remove linhas de comentário antes de procurar padrões proibidos.
        ///
        /// <para>A primeira versão deste guarda falhou contra o próprio código já corrigido: o
        /// XML doc da <c>BarraDeItens</c> <b>explica</b> que ela alcançava
        /// <c>InventoryManager.Instance</c>, e o regex leu a prosa como se fosse código. Um
        /// guarda que proíbe mencionar o defeito na documentação proíbe explicar por que ele
        /// existiu.</para>
        /// </summary>
        private static string SemComentarios(string fonte)
        {
            var linhas = fonte.Split('\n');
            var sb = new System.Text.StringBuilder(fonte.Length);

            foreach (var linha in linhas)
            {
                string t = linha.TrimStart();
                if (t.StartsWith("//") || t.StartsWith("*") || t.StartsWith("/*")) continue;
                sb.Append(linha).Append('\n');
            }

            return sb.ToString();
        }

        /// <summary>
        /// O <c>Update</c> roda todo frame; qualquer resolução de dependência dentro dele viola a
        /// Regra de Ouro 1. Este teste isola o corpo do método e olha só ele.
        /// </summary>
        [Test]
        public void Update_NaoResolveDependencia()
        {
            string fonte = File.ReadAllText(Barra);

            int inicio = fonte.IndexOf("private void Update()");
            Assert.Greater(inicio, -1, "Método Update não encontrado — se foi renomeado, " +
                                       "atualize este guarda junto.");

            // Do início do Update até a próxima declaração de método.
            int fim = fonte.IndexOf("private static KeyControl", inicio);
            if (fim < 0) fim = fonte.Length;

            string corpo = SemComentarios(fonte.Substring(inicio, fim - inicio));

            foreach (var proibido in new[] { "InventoryManager.Instance", "GetComponent",
                                             "FindAnyObjectByType", "FindObjectsByType" })
            {
                StringAssert.DoesNotContain(proibido, corpo,
                    $"'{proibido}' dentro do Update da BarraDeItens — resolução de dependência " +
                    "por frame. Cacheie no Bind.");
            }
        }

        /// <summary>
        /// Sem <c>OnEnable</c>, desativar e reativar o GameObject deixa a barra permanentemente
        /// morta: o <c>OnDisable</c> desinscreve e nada reinscreve.
        /// </summary>
        [Test]
        public void TemOnEnableParaReatarDepoisDeDesativar()
        {
            string fonte = File.ReadAllText(Barra);

            StringAssert.Contains("private void OnEnable()", fonte,
                "BarraDeItens sem OnEnable. Com OnDisable desinscrevendo e nada reinscrevendo, " +
                "desativar e reativar o GameObject deixa a barra congelada para sempre.");
        }

        [Test]
        public void BarraDeItens_ExpoeBindComInventario()
        {
            string fonte = File.ReadAllText(Barra);

            Assert.IsTrue(Regex.IsMatch(fonte, @"public\s+void\s+Bind\s*\(\s*InventoryManager"),
                "BarraDeItens deve expor Bind(InventoryManager) — é por aí que o HUDController " +
                "entrega a fonte.");
        }

        [Test]
        public void HudInjetaInventario_EOBootstrapChama()
        {
            string hud = File.ReadAllText(Hud);
            Assert.IsTrue(Regex.IsMatch(hud, @"public\s+void\s+InjetarInventario\s*\("),
                "HUDController sem InjetarInventario. O campo 'barraDeItens' já existia ligado " +
                "nas 4 cenas e era lido por ninguém — é ele que deve entregar a fonte.");

            string bootstrap = File.ReadAllText(Bootstrap);
            StringAssert.Contains("InjetarInventario", bootstrap,
                "GameLoopBootstrap não chama InjetarInventario — a barra nunca receberia a fonte " +
                "e as teclas 1–8 ficariam inertes.");
        }
    }
}
