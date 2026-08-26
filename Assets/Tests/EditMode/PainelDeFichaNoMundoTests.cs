using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace FavelaAmarela.Tests.EditMode
{
    /// <summary>
    /// Guarda o painel de ficha: presente nas cenas jogáveis e com as <b>duas</b> referências
    /// ligadas.
    ///
    /// <para><b>Por que as duas importam:</b> sem <c>corpo</c> o painel não escreve nada; sem
    /// <c>vitalidadeDoJogador</c> ele escreve "ficha indisponível". Nos dois casos a tela abre
    /// vazia — e um instrumento de diagnóstico em branco é pior que nenhum, porque se lê como
    /// "os atributos estão zerados" em vez de "o painel não está ligado". Este painel existe
    /// justamente para tornar visíveis erros silenciosos de atributo
    /// (<c>systems/inventario_analise.md</c>); ele falhando em silêncio anularia o propósito.</para>
    /// </summary>
    public sealed class PainelDeFichaNoMundoTests
    {
        private const string PastaDeCenas = "Assets/Scenes/";

        private static readonly string[] CenasJogaveis =
        {
            PastaDeCenas + "Deserto_Hali.unity",
            PastaDeCenas + "Playtest_RuinasPalidas.unity",
            PastaDeCenas + "Santuario_Yhtill.unity",
        };

                // MIGRADO EM 2026-08-22 (Bloco 6): o HUD deixou de ser montado por cena e passou
        // a viver em Resources/HUD_Gameplay.prefab, carregado por
        // HUDController.GarantirInstancia com DontDestroyOnLoad. Verificar a presenca
        // dele nas cenas passou a ser verificar o vazio. A cobertura NAO sumiu: mudou de
        // alvo para HudPersistenteTests, que checa o prefab.
        [Ignore("Contrato mudou: ver HudPersistenteTests")]
        [TestCaseSource(nameof(CenasJogaveis))]
        public void CenaJogavel_TemPainelDeFicha(string caminhoDaCena)
        {
            var bloco = BlocoDoScript(caminhoDaCena, "PainelDeFicha.cs");

            Assert.IsNotNull(bloco,
                $"{Path.GetFileName(caminhoDaCena)}: sem PainelDeFicha. Sem ele não há como ver " +
                "o efeito de um item nos atributos — foi a falta dessa tela que deixou " +
                "'ResistenciaAnomala zerada a cada troca de equipamento' passar despercebido. " +
                "Rode 'Tools/FavelaAmarela/Montar Painel de Ficha na cena'.");
        }

                // MIGRADO EM 2026-08-22 (Bloco 6): o HUD deixou de ser montado por cena e passou
        // a viver em Resources/HUD_Gameplay.prefab, carregado por
        // HUDController.GarantirInstancia com DontDestroyOnLoad. Verificar a presenca
        // dele nas cenas passou a ser verificar o vazio. A cobertura NAO sumiu: mudou de
        // alvo para HudPersistenteTests, que checa o prefab.
        [Ignore("Contrato mudou: ver HudPersistenteTests")]
        [TestCaseSource(nameof(CenasJogaveis))]
        public void PainelDeFicha_TemAsDuasReferencias(string caminhoDaCena)
        {
            var bloco = BlocoDoScript(caminhoDaCena, "PainelDeFicha.cs");
            Assert.IsNotNull(bloco, $"{Path.GetFileName(caminhoDaCena)}: sem PainelDeFicha.");

            var faltando = new List<string>();
            if (ReferenciaDe(bloco, "corpo") == "0") faltando.Add("corpo (o Text da ficha)");
            if (ReferenciaDe(bloco, "vitalidadeDoJogador") == "0")
                faltando.Add("vitalidadeDoJogador (a VitalidadeBridge de Damião)");

            Assert.IsEmpty(faltando,
                $"{Path.GetFileName(caminhoDaCena)}: PainelDeFicha com referência solta — " +
                "a tela abriria vazia e pareceria que os atributos estão zerados: " +
                string.Join(", ", faltando));
        }

        // ── Apoio ────────────────────────────────────────────────────────────

        private static List<string> BlocoDoScript(string caminhoDaCena, string nomeDoScript)
        {
            if (!File.Exists(caminhoDaCena)) return null;

            var metas = Directory.GetFiles("Assets/Scripts", nomeDoScript + ".meta",
                SearchOption.AllDirectories);
            if (metas.Length == 0) return null;

            var mg = Regex.Match(File.ReadAllText(metas[0]), @"guid:\s*([0-9a-f]{32})");
            if (!mg.Success) return null;
            string guid = mg.Groups[1].Value;

            var separador = new Regex(@"^--- !u!\d+ &\d+");
            List<string> atual = null;
            var blocos = new List<List<string>>();

            foreach (var linha in File.ReadAllLines(caminhoDaCena))
            {
                if (separador.IsMatch(linha))
                {
                    atual = new List<string>();
                    blocos.Add(atual);
                    continue;
                }
                atual?.Add(linha);
            }

            foreach (var bloco in blocos)
                foreach (var linha in bloco)
                    if (linha.Contains("m_Script:") && linha.Contains(guid))
                        return bloco;

            return null;
        }

        private static string ReferenciaDe(List<string> bloco, string campo)
        {
            var padrao = new Regex($@"^  {Regex.Escape(campo)}: \{{fileID: (-?\d+)");
            foreach (var linha in bloco)
            {
                var m = padrao.Match(linha);
                if (m.Success) return m.Groups[1].Value;
            }
            return "0";
        }
    }
}
