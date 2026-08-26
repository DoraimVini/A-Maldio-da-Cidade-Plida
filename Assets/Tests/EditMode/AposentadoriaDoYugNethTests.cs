using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace FavelaAmarela.Tests.EditMode
{
    /// <summary>
    /// Guarda a virada de papel do <b>Yug-Neth</b>: ele entra no Castelo com Damião e, ao chegar,
    /// <b>deixa de ser companheiro</b> e vira o NPC do artesanato (decisão do Vini, 2026-08-20).
    ///
    /// <para><b>O artesanato é conteúdo pós-Vertical Slice</b> e não está implementado — estes
    /// testes cobrem a virada, não o sistema que virá depois.</para>
    ///
    /// <para><b>Por que precisa de guarda:</b> a cadeia inteira é feita de coisas que somem sem
    /// erro. Um <c>bool</c> serializado que volta a <c>false</c> numa remontagem; um evento C#
    /// que perde o assinante numa refatoração; um <c>Bind</c> que alguém reintroduz "para
    /// consertar" o Yug-Neth parado. Em nenhum desses casos o jogo deixa de compilar — ele só
    /// passa a ter um companheiro onde deveria haver um NPC, ou uma barra de RC no HUD durante a
    /// fase final.</para>
    /// </summary>
    public sealed class AposentadoriaDoYugNethTests
    {
        private const string Castelo = "Assets/Scenes/Castelo_Carcosa.unity";
        private const string CompanionManager = "Assets/Scripts/Player/CompanionManager.cs";
        private const string Travessia = "Assets/Scripts/GameLoop/TravessiaDoCompanheiro.cs";
        private const string Bootstrap = "Assets/Scripts/GameLoop/GameLoopBootstrap.cs";
        private const string HUD = "Assets/Scripts/UI/HUDController.cs";
        private const string Artesao = "Assets/Scripts/Quests/YugNethArtesao.cs";
        private const string YugNethAI = "Assets/Scripts/Enemies/YugNethAI.cs";

        [Test]
        public void ACadeiaDaAposentadoria_EstaInteira()
        {
            var falhas = new List<string>();

            void Exigir(string arquivo, string trecho, string porque)
            {
                if (!File.Exists(arquivo)) { falhas.Add($"{arquivo}: não existe"); return; }
                if (!File.ReadAllText(arquivo).Contains(trecho))
                    falhas.Add($"{Path.GetFileName(arquivo)}: falta '{trecho}' — {porque}");
            }

            Exigir(CompanionManager, "public void Aposentar()",
                   "sem isso não há como deixar de ser companheiro sem morrer");
            Exigir(CompanionManager, "OnCompanheiroAposentado",
                   "sem o evento, o HUD nunca fica sabendo e a barra de RC continua na tela");

            Exigir(YugNethAI, "public void TornarNpc()",
                   "sem isso ele continuaria seguindo o jogador pelo Castelo");

            Exigir(Travessia, "aposentarAoChegar",
                   "sem o modo, a travessia registraria Yug-Neth como companheiro no Castelo");

            Exigir(Bootstrap, "OnCompanheiroAposentado +=",
                   "o bootstrap não assinaria a aposentadoria e a barra ficaria visível");
            Exigir(Bootstrap, "OnCompanheiroAposentado -=",
                   "sem desassinar, o assinante sobrevive à cena e vaza");

            Exigir(HUD, "RetirarCompanheiro",
                   "sem o ponto de retirada, nada esconde a barra");
            Exigir(HUD, "companheiroBar.Unbind()",
                   "esconder sem Unbind deixa a barra assinada em quem não é mais companheiro");

            Exigir(Artesao, "IInteragivel",
                   "sem isso o Yug-Neth do Castelo seria decoração, não NPC");

            Assert.IsEmpty(falhas,
                "Cadeia da aposentadoria rompida — compila, e Yug-Neth chega ao Castelo como " +
                "companheiro:\n  " + string.Join("\n  ", falhas));
        }

        /// <summary>
        /// A cena do Castelo tem que ligar o modo de aposentadoria de fato.
        ///
        /// <para>O código pode estar todo certo e o <c>bool</c> vir <c>false</c> — é uma
        /// referência serializada como qualquer outra, e este projeto já perdeu campos assim mais
        /// de uma vez. Um <c>aposentarAoChegar: 0</c> faz Yug-Neth entrar no Castelo como
        /// companheiro, seguindo Damião até o Trono, sem uma linha no console.</para>
        /// </summary>
        [Test]
        public void OCastelo_LigaOModoDeAposentadoria()
        {
            Assert.IsTrue(File.Exists(Castelo), $"Cena ausente: {Castelo}");

            string guid = GuidDoScript("TravessiaDoCompanheiro");
            Assert.IsNotNull(guid, "Script TravessiaDoCompanheiro não encontrado.");

            string txt = File.ReadAllText(Castelo);

            var doc = Regex.Match(txt,
                $@"---\s*!u!114\s*&-?\d+\r?\n(?:(?!^---)[\s\S])*?{guid}(?:(?!^---)[\s\S])*",
                RegexOptions.Multiline);

            Assert.IsTrue(doc.Success,
                "O Castelo não tem TravessiaDoCompanheiro — Yug-Neth não atravessaria para lá, e " +
                "o NPC do artesanato não existiria. Rode 'Tools/FavelaAmarela/Montar Castelo de " +
                "Carcosa'.");

            var flag = Regex.Match(doc.Value, @"(?m)^\s*aposentarAoChegar:\s*(\d)");
            Assert.IsTrue(flag.Success, "Campo 'aposentarAoChegar' ausente do YAML.");
            Assert.AreEqual("1", flag.Groups[1].Value,
                "aposentarAoChegar está desligado no Castelo — Yug-Neth entraria como " +
                "companheiro e seguiria Damião até o Trono.");

            var falhas = new List<string>();
            // "caixaDeTexto" saiu desta lista em 2026-08-22: a caixa vive no prefab
            // persistente do HUD, e TravessiaDoCompanheiro cai para
            // TutorialHintUI.Instancia quando o campo está vazio.
            foreach (var campo in new[] { "prefabYugNeth", "postoDeArtesao" })
            {
                var m = Regex.Match(doc.Value, $@"(?m)^\s*{campo}:\s*\{{fileID:\s*(-?\d+)");

                if (!m.Success) falhas.Add($"{campo}: ausente");
                else if (m.Groups[1].Value == "0") falhas.Add($"{campo}: nulo");
            }

            Assert.IsEmpty(falhas,
                "Travessia do Castelo com referência solta:\n  " + string.Join("\n  ", falhas));
        }

        private static string GuidDoScript(string nome)
        {
            var arquivo = Directory
                .EnumerateFiles("Assets/Scripts", nome + ".cs", SearchOption.AllDirectories)
                .FirstOrDefault();

            if (arquivo == null || !File.Exists(arquivo + ".meta")) return null;

            var m = Regex.Match(File.ReadAllText(arquivo + ".meta"), @"(?m)^guid:\s*([0-9a-f]{32})");
            return m.Success ? m.Groups[1].Value : null;
        }
    }
}
