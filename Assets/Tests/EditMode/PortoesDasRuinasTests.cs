using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace FavelaAmarela.Tests.EditMode
{
    /// <summary>
    /// Guarda a arena dos <b>Portões das Ruínas</b> — a luta que fecha a Fase 1.
    ///
    /// <para><b>O que motivou:</b> o Byakhee estava completo (FSM com 10 testes, ficha calibrada
    /// por simulação, prefab com spritesheet real, tabela de espólio autorada) e <b>em cena
    /// nenhuma</b>. <c>IniciarLuta()</c> só era chamado pelo Carcosa Debugger. Como o Anel do
    /// Sinal Amarelo é espólio garantido dele e o rito do Rei exige o Anel, o jogo não era
    /// terminável: dava para chegar ao Rei e não havia como selá-lo.</para>
    ///
    /// <para><b>O que estes testes medem que os outros não mediam:</b> conectividade, não
    /// presença. O Castelo passou em quatro testes de presença enquanto o jogador nascia lacrado
    /// numa sala sem porta. Aqui se verifica que o gatilho de luta <b>atravessa</b> o piso, que o
    /// portão barra de verdade, e que a cadeia abate→destranca→abre→passagem está ligada ponta a
    /// ponta.</para>
    /// </summary>
    public sealed class PortoesDasRuinasTests
    {
        private const string Cena = "Assets/Scenes/Portoes_Das_Ruinas.unity";
        private const string CenaDeserto = "Assets/Scenes/Deserto_Hali.unity";
        private const string BuildSettings = "ProjectSettings/EditorBuildSettings.asset";
        private const string PrefabByakhee = "Assets/FavelaAmarela/Art/Enemies/Byakhee.prefab";

        [Test]
        public void ACena_ExisteEstaNaBuildEAlcancavelPeloDeserto()
        {
            Assert.IsTrue(File.Exists(Cena),
                "Portoes_Das_Ruinas.unity não existe. Rode 'Tools/FavelaAmarela/Montar Portões " +
                "das Ruínas' — e confira o disco, não o log.");

            Assert.IsTrue(File.ReadAllText(BuildSettings).Contains("Portoes_Das_Ruinas.unity"),
                "Os Portões não estão no Build Settings — nenhuma build carregaria a cena.");

            // O marco 'Portoes_DasRuinas' do Deserto era pura decoração (Transform +
            // SpriteRenderer + DynamicYSort). Sem um portal nele, a arena fica inalcançável.
            var destinos = Regex.Matches(File.ReadAllText(CenaDeserto), @"cenaDestino:\s*(\S+)")
                                .Cast<Match>()
                                .Select(m => m.Groups[1].Value)
                                .ToList();

            CollectionAssert.Contains(destinos, "Portoes_Das_Ruinas",
                "Nenhum PortalDeCena do Deserto leva aos Portões — a arena existiria sem caminho " +
                "até ela.");
        }

        [Test]
        public void OByakheeEAsPecasDaLuta_EstaoNaCena()
        {
            string txt = File.ReadAllText(Cena);
            var falhas = new List<string>();

            foreach (var (script, quantos) in new[]
            {
                ("ByakheeAI", 1), ("ArenaDosPortoes", 1), ("PortaoDosPortoes", 1),
                ("RefugioDeLuz", 1),                       // o poste liberado ao fim da luta
                ("PontoDeChegada", 2),                     // chegada do Deserto + irmao do Refugio
                ("PortalDeCena", 2),                       // passagem + volta ao Deserto
            })
            {
                string guid = GuidDoScript(script);
                if (guid == null) { falhas.Add($"{script}: script não encontrado"); continue; }

                int n = Regex.Matches(txt, Regex.Escape(guid)).Count;
                if (n < quantos) falhas.Add($"{script}: {n} instância(s), esperado {quantos}");
            }

            foreach (var nome in new[] { "Os_Portoes", "Batente", "Gatilho_DaArena",
                                          "Passagem_ParaOCastelo", "Centro_DaArena",
                                          "Refugio_DosPortoes", "CaixaDeDialogo" })
            {
                if (!Regex.IsMatch(txt, $@"(?m)^\s+m_Name:\s*{nome}\s*$"))
                    falhas.Add($"objeto '{nome}' ausente");
            }

            Assert.IsEmpty(falhas,
                "Arena dos Portões incompleta — código escrito e mundo vazio é o modo de falha " +
                "que este guarda existe para pegar:\n  " + string.Join("\n  ", falhas));
        }

        /// <summary>
        /// O gatilho de luta e os Portões precisam <b>atravessar o piso</b> na altura em que
        /// estão.
        ///
        /// <para><b>Por que isto não é paranoia:</b> o piso é um losango isométrico, não um
        /// retângulo — com <c>cellSize (1, 0.5)</c> a célula <c>(gx,gy)</c> cai em
        /// <c>x=(gx-gy)/2</c>, <c>y=(gx+gy)/4</c>, e a largura afina até virar ponta nos extremos
        /// de Y. Uma faixa dimensionada "de olho" deixa passagem pelas beiradas: o jogador
        /// contorna o gatilho e chega ao Byakhee sem despertá-lo, ou contorna os Portões. É a
        /// mesma família do Castelo, que nasceu com salas lacradas por geometria presumida.</para>
        /// </summary>
        [Test]
        public void OGatilhoEOsPortoes_AtravessamOPiso()
        {
            string txt = File.ReadAllText(Cena);
            var falhas = new List<string>();

            // (nome do objeto, y em que está, largura mínima exigida pelo losango ali)
            foreach (var (nome, y, minimo) in new[]
            {
                ("Gatilho_DaArena", -7f, 36f),
                ("Os_Portoes", 11f, 18f),
            })
            {
                float largura = LarguraDoColisor(txt, nome);

                if (float.IsNaN(largura)) { falhas.Add($"{nome}: colisor não encontrado"); continue; }

                if (largura < minimo)
                    falhas.Add($"{nome} (y={y}): colisor de {largura}, mas o piso ali tem " +
                               $"{minimo} — dá para contornar pela beirada");
            }

            Assert.IsEmpty(falhas,
                "Faixa que deveria ser intransponível não atravessa o losango:\n  " +
                string.Join("\n  ", falhas));
        }

        /// <summary>
        /// O Byakhee em cena tem que carregar o <c>DropAoAbater</c> — senão a luta acontece,
        /// o chefe cai, e o Anel do Sinal Amarelo não sai. O rito do Rei continuaria impossível
        /// com a arena inteira construída.
        /// </summary>
        [Test]
        public void OByakheeDaArena_LargaOEspolio()
        {
            string guid = GuidDoScript("DropAoAbater");
            Assert.IsNotNull(guid, "Script DropAoAbater não encontrado.");

            Assert.IsTrue(File.ReadAllText(PrefabByakhee).Contains(guid),
                "O prefab do Byakhee não tem DropAoAbater — vencer a luta não entregaria o Anel " +
                "do Sinal Amarelo, e o rito do Rei seguiria impossível de completar. Rode " +
                "'Tools/FavelaAmarela/Ligar espólio do Byakhee'.");
        }

        /// <summary>
        /// O Poste de Luz liberado ao fim da luta precisa do que o <c>RefugioDeLuz</c> cobra em
        /// execução: um <c>Collider2D</c> de gatilho e um <c>PontoDeChegada</c> <b>irmão</b>.
        ///
        /// <para>Sem o ponto irmão o renascimento cai na posição padrão da cena em vez de sob a
        /// luz — o próprio componente emite um <c>LogWarning</c> sobre isso, que é fácil de
        /// perder no meio de um playtest.</para>
        ///
        /// <para>Este poste é a resposta ao Yug-Neth: em vez de o companheiro ser a chave dos
        /// Portões, o Refúgio pós-luta é onde ele é reanimado.</para>
        /// </summary>
        [Test]
        public void OPosteDeLuz_TemGatilhoEPontoDeChegadaIrmao()
        {
            string txt = File.ReadAllText(Cena);

            var go = Regex.Match(txt,
                @"---\s*!u!1\s*&(\d+)\r?\nGameObject:(?:(?!^---)[\s\S])*?m_Name:\s*Refugio_DosPortoes\s*$",
                RegexOptions.Multiline);

            Assert.IsTrue(go.Success, "Refugio_DosPortoes não está na cena.");

            var ids = Regex.Matches(go.Value, @"component:\s*\{fileID:\s*(-?\d+)\}")
                           .Cast<Match>().Select(m => m.Groups[1].Value).ToList();

            bool temGatilho = false, temPonto = false;
            string guidPonto = GuidDoScript("PontoDeChegada");

            foreach (string id in ids)
            {
                var doc = Regex.Match(txt, $@"---\s*!u!\d+\s*&{id}\r?\n(?:(?!^---)[\s\S])*",
                                      RegexOptions.Multiline);
                if (!doc.Success) continue;

                // A Unity escreve o nome da classe como chave do mapeamento no YAML, então
                // procurar "Collider2D" no texto do documento acha qualquer tipo de colisor —
                // não é preciso casar o número de classe.
                if (doc.Value.Contains("Collider2D") && doc.Value.Contains("m_IsTrigger: 1"))
                    temGatilho = true;

                if (guidPonto != null && doc.Value.Contains(guidPonto)) temPonto = true;
            }

            Assert.IsTrue(temGatilho,
                "O Poste não tem Collider2D marcado como gatilho — o RefugioDeLuz dá LogError e " +
                "nunca ancora.");

            Assert.IsTrue(temPonto,
                "O Poste não tem PontoDeChegada irmão — renascer ali cairia na posição padrão da " +
                "cena, não sob a luz.");
        }

        // ── Auxiliares ────────────────────────────────────────────────────────

        /// <summary>
        /// Largura do <c>BoxCollider2D</c> do GameObject de nome dado. Percorre os componentes
        /// declarados <b>naquele</b> GameObject em vez de pegar o primeiro <c>m_Size</c> depois
        /// do nome: a ordem dos documentos no YAML não é a da hierarquia, e ler o tamanho errado
        /// já fez um teste deste projeto reprovar dado correto.
        /// </summary>
        private static float LarguraDoColisor(string txt, string nome)
        {
            var go = Regex.Match(txt,
                $@"---\s*!u!1\s*&(\d+)\r?\nGameObject:(?:(?!^---)[\s\S])*?m_Name:\s*{Regex.Escape(nome)}\s*$",
                RegexOptions.Multiline);

            if (!go.Success) return float.NaN;

            var componentes = Regex.Matches(go.Value, @"component:\s*\{fileID:\s*(-?\d+)\}")
                                   .Cast<Match>()
                                   .Select(m => m.Groups[1].Value);

            foreach (string id in componentes)
            {
                var doc = Regex.Match(txt, $@"---\s*!u!61\s*&{id}\r?\n(?:(?!^---)[\s\S])*",
                                      RegexOptions.Multiline);
                if (!doc.Success) continue;

                var size = Regex.Match(doc.Value, @"m_Size:\s*\{x:\s*([\d.eE+-]+)");
                if (size.Success) return float.Parse(size.Groups[1].Value,
                    System.Globalization.CultureInfo.InvariantCulture);
            }

            return float.NaN;
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
