using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace FavelaAmarela.Tests.EditMode
{
    /// <summary>
    /// Guarda a cadeia que torna o Rei em Amarelo <b>derrotável</b>: relíquia exigida →
    /// <c>ArtefatoDef</c> → <c>ItemDef</c> → fonte no mundo.
    ///
    /// <para><b>O que motivou:</b> o Anel do Sinal Amarelo é uma das três relíquias do rito, e a
    /// descrição dele diz de onde vem — "gravação sacra arrancada do Byakhee". A tabela
    /// <c>Drop_Byakhee</c> estava autorada e correta, e <b>nenhuma cena ou prefab a
    /// referenciava</b>: o <c>DropAoAbater</c> só existia no Cultista. O Byakhee morria sem
    /// largar nada e o rito era impossível de fechar. Nenhum teste percebia, porque todos
    /// verificavam que os assets <i>existem</i> — e existir não é estar ligado.</para>
    ///
    /// <para><b>Buraco conhecido que estes testes NÃO cobrem de propósito:</b> o Byakhee não
    /// está em cena nenhuma (falta a arena dos Portões, roadmap item 9). Um teste exigindo isso
    /// nasceria vermelho e seria desligado na primeira semana. O que se guarda aqui é a ligação
    /// que já deve valer — no dia em que a arena existir, o Anel cai sozinho.</para>
    /// </summary>
    public sealed class ReliquiasDoRitoTests
    {
        private const string PrefabRei = "Assets/FavelaAmarela/Art/Enemies/ReiEmAmarelo.prefab";
        private const string PrefabByakhee = "Assets/FavelaAmarela/Art/Enemies/Byakhee.prefab";
        private const string TabelaByakhee = "Assets/FavelaAmarela/Config/Drops/Drop_Byakhee.asset";
        private const string PastaArtefatos = "Assets/FavelaAmarela/Config/Resources/Artefatos";

        /// <summary>Slots de porte de <c>InventarioDeArtefatos</c>.</summary>
        private const int TotalDeSlots = 4;

        [Test]
        public void CadaReliquiaExigida_TemArtefatoDefLigadoAUmItemDef()
        {
            var falhas = new List<string>();

            foreach (string id in IdsExigidosPeloRei())
            {
                string asset = Directory
                    .EnumerateFiles(PastaArtefatos, "*.asset")
                    .FirstOrDefault(a => Regex.IsMatch(File.ReadAllText(a), $@"(?m)^\s*Id:\s*{Regex.Escape(id)}\s*$"));

                if (asset == null) { falhas.Add($"{id}: nenhum ArtefatoDef com esse Id"); continue; }

                // Sem 'Item' preenchido, ArtefatosBridge.ArtefatoDoItem nunca casa e coletar o
                // item no chão não concede a relíquia — ela vira exclusiva do Carcosa Debugger.
                var item = Regex.Match(File.ReadAllText(asset), @"(?m)^\s*Item:\s*\{fileID:\s*(-?\d+)");
                if (!item.Success || item.Groups[1].Value == "0")
                    falhas.Add($"{id}: ArtefatoDef sem 'Item' — impossível de obter coletando");
            }

            Assert.IsEmpty(falhas,
                "Relíquia exigida pelo rito sem caminho de aquisição:\n  " + string.Join("\n  ", falhas));
        }

        /// <summary>
        /// O set exigido tem que <b>caber nos slots de porte</b>. O ponto focal só aceita
        /// relíquia <c>Contem</c> (portada), não <c>Possui</c> (dormente) — um set maior que os
        /// slots deixaria a última relíquia adormecida e o rito travaria em silêncio, sem erro
        /// nem mensagem ao jogador.
        /// </summary>
        [Test]
        public void OSetExigido_CabeNosSlotsDePorte()
        {
            int exigidas = IdsExigidosPeloRei().Count;

            Assert.LessOrEqual(exigidas, TotalDeSlots,
                $"O Rei exige {exigidas} relíquias e há {TotalDeSlots} slots de porte. O ponto " +
                "focal recusa relíquia dormente, então o rito ficaria impossível de completar.");
        }

        /// <summary>
        /// O Byakhee tem que carregar o <c>DropAoAbater</c> apontado para a tabela que contém o
        /// Anel — é a fonte diegética da relíquia, dita pela descrição dela.
        /// </summary>
        [Test]
        public void OByakhee_LargaOAnelAoSerAbatido()
        {
            string guidDrop = GuidDoScript("DropAoAbater");
            Assert.IsNotNull(guidDrop, "Script DropAoAbater não encontrado.");

            string prefab = File.ReadAllText(PrefabByakhee);
            Assert.IsTrue(prefab.Contains(guidDrop),
                "Byakhee.prefab não tem DropAoAbater. O chefe morre sem largar o Anel do Sinal " +
                "Amarelo, e o rito do Rei fica impossível de completar em jogo. Rode " +
                "'Tools/FavelaAmarela/Ligar espólio do Byakhee'.");

            // A referência tem que ser a tabela CERTA: um DropAoAbater apontando para a tabela
            // do Cultista passaria no teste acima e não largaria relíquia nenhuma.
            string guidTabela = GuidDoAsset(TabelaByakhee);
            Assert.IsNotNull(guidTabela, $"Meta ausente para {TabelaByakhee}.");

            var campo = Regex.Match(prefab,
                $@"{Regex.Escape(guidDrop)}[\s\S]{{0,400}}?tabela:\s*\{{fileID:\s*-?\d+,\s*guid:\s*([0-9a-f]{{32}})");

            Assert.IsTrue(campo.Success, "DropAoAbater do Byakhee sem campo 'tabela' legível.");
            Assert.AreEqual(guidTabela, campo.Groups[1].Value,
                "O DropAoAbater do Byakhee aponta para outra tabela que não Drop_Byakhee.");

            // E a tabela precisa mesmo conter o Anel, garantido e em nível alcançável: o nível
            // do jogador está travado em 1 no Vertical Slice (CLAUDE.md §1.1), então uma
            // entrada com NivelMinimo > 1 nunca cairia.
            string tabela = File.ReadAllText(TabelaByakhee);
            string guidAnel = GuidDoAsset("Assets/FavelaAmarela/Config/Resources/Itens/Item_AnelDoSinalAmarelo.asset");

            Assert.IsTrue(tabela.Contains(guidAnel), "Drop_Byakhee não contém o Anel do Sinal Amarelo.");

            var entrada = Regex.Match(tabela,
                $@"{Regex.Escape(guidAnel)}[\s\S]{{0,300}}?NivelMinimo:\s*(\d+)");

            Assert.IsTrue(entrada.Success, "Entrada do Anel sem NivelMinimo legível.");
            Assert.AreEqual(1, int.Parse(entrada.Groups[1].Value),
                "O Anel exige nível acima de 1, mas a progressão está travada no nível 1 no " +
                "Vertical Slice — ele nunca cairia.");
        }

        // ── Auxiliares ────────────────────────────────────────────────────────

        private static List<string> IdsExigidosPeloRei()
        {
            Assert.IsTrue(File.Exists(PrefabRei), $"Prefab ausente: {PrefabRei}");

            var bloco = Regex.Match(File.ReadAllText(PrefabRei),
                                    @"(?ms)idsDasReliquiasExigidas:\s*(.*?)(?=^\s{2}\w)");
            Assert.IsTrue(bloco.Success, "O prefab do Rei não serializa idsDasReliquiasExigidas.");

            var ids = Regex.Matches(bloco.Groups[1].Value, @"(?m)^\s*-\s*(\S+)\s*$")
                           .Cast<Match>()
                           .Select(m => m.Groups[1].Value)
                           .ToList();

            Assert.IsNotEmpty(ids, "O Rei não exige relíquia nenhuma — o rito não teria como começar.");
            return ids;
        }

        private static string GuidDoAsset(string caminho)
        {
            if (!File.Exists(caminho + ".meta")) return null;
            var m = Regex.Match(File.ReadAllText(caminho + ".meta"), @"(?m)^guid:\s*([0-9a-f]{32})");
            return m.Success ? m.Groups[1].Value : null;
        }

        private static string GuidDoScript(string nome)
        {
            var arquivo = Directory
                .EnumerateFiles("Assets/Scripts", nome + ".cs", SearchOption.AllDirectories)
                .FirstOrDefault();

            return arquivo == null ? null : GuidDoAsset(arquivo);
        }
    }
}
