using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace FavelaAmarela.Tests.EditMode
{
    /// <summary>
    /// Guarda quem desenha o <b>sprite embutido da Unity</b> — o quadrado branco — nas cenas do
    /// Build Settings.
    ///
    /// <para><b>Por que existe (2026-09-09).</b> Os 6 Nobres Fossilizados e os 3 Espelhos de
    /// Aldebaran do Castelo passaram meses assim, e nenhum teste viu: eles <i>estavam</i> na
    /// cena, com componente e colisor, e <c>CasteloDeCarcosaTests</c> os contava e passava
    /// verde. Contar instância não é perguntar como ela aparece.</para>
    ///
    /// <para><b>E o guarda não pode olhar só o YAML.</b> Foi a armadilha que quase me fez
    /// reportar errado: os 3 Pontos Focais, o Refúgio e os 9 consumíveis do Deserto <b>também</b>
    /// gravam o sprite embutido no arquivo — e estão certos, porque
    /// <c>PontoFocalDeReliquia</c>, <c>RefugioDeLuz</c> e <c>ColetavelDeItem</c> escrevem
    /// <c>SpriteRenderer.sprite</c> no <c>Awake</c>. Só é defeito quando <b>ninguém</b> escreve
    /// por ele.</para>
    ///
    /// <para><b>Este teste não proíbe: ele congela a lista.</b> Os que restam estão em
    /// <see cref="ConhecidosSemArte"/>, com o que cada um significa em jogo. Um objeto novo
    /// nessa condição reprova; e um que <b>ganhou</b> arte também reprova, cobrando a saída da
    /// lista — senão ela vira depósito e para de dizer qualquer coisa.</para>
    /// </summary>
    public sealed class ArteNasCenasTests
    {
        /// <summary>GUID dos recursos internos da Unity. O quadrado branco vem daqui.</summary>
        private const string GuidEmbutido = "0000000000000000f000000000000000";

        /// <summary>
        /// Objetos que hoje desenham o quadrado branco e <b>ninguém</b> veste em runtime, com o
        /// que cada um custa em jogo. A lista existe para <b>encolher</b>.
        /// </summary>
        private static readonly Dictionary<string, string> ConhecidosSemArte =
            new Dictionary<string, string>
            {
                // Os três Fragmentos de Yhtill SAÍRAM daqui em 2026-09-09: ganharam arte
                // própria (cacos de uma mesma tábua quebrada, com glifos), e a lista existe
                // para encolher. Eram o caso mais grave dela — itens que a quest manda o
                // jogador ACHAR, desenhados como quadrado branco.
                // Passagem_ParaOCastelo SAIU em 2026-09-10: o Vini deu arte a ela na cena.

                ["Piso"] = "o piso do Santuário de Yhtill — já registrado no roadmap como " +
                           "pendência de arte da quest da Cassilda",

                ["VisualDoEscudo"] = "o Escudo Mágico do Abdul na Fase 1 da luta. Quebrar as " +
                                     "Pedras de Poder é a única forma de causar dano ali, e o " +
                                     "escudo é o sinal de que ele está de pé",
            };

        [Test]
        public void NenhumObjetoNovo_FicaComOQuadradoBrancoDaUnity()
        {
            var escrevemSprite = ScriptsQueEscrevemSprite();

            var nusAgora = new Dictionary<string, string>();

            foreach (var cena in CenasDoBuild())
            {
                string yaml = File.ReadAllText(cena);
                var blocos = yaml.Split(new[] { "--- " }, System.StringSplitOptions.None);

                var nomes = new Dictionary<string, string>();
                var scriptsDoObjeto = new Dictionary<string, List<string>>();

                foreach (var bloco in blocos)
                {
                    var go = Regex.Match(bloco, @"^!u!1 &(\d+)");
                    if (go.Success)
                    {
                        var nome = Regex.Match(bloco, @"m_Name: (.+)");
                        if (nome.Success) nomes[go.Groups[1].Value] = nome.Groups[1].Value.Trim();
                        continue;
                    }

                    if (!Regex.IsMatch(bloco, @"^!u!114 &\d+")) continue;

                    var dono = Regex.Match(bloco, @"m_GameObject: \{fileID: (\d+)\}");
                    var script = Regex.Match(bloco, @"m_Script: \{fileID: \d+, guid: (\w+)");
                    if (!dono.Success || !script.Success) continue;

                    if (!scriptsDoObjeto.TryGetValue(dono.Groups[1].Value, out var lista))
                        scriptsDoObjeto[dono.Groups[1].Value] = lista = new List<string>();
                    lista.Add(script.Groups[1].Value);
                }

                foreach (var bloco in blocos)
                {
                    if (!Regex.IsMatch(bloco, @"^!u!212 &\d+")) continue;

                    var sprite = Regex.Match(bloco, @"m_Sprite: \{fileID: -?\d+, guid: (\w+)");
                    var dono = Regex.Match(bloco, @"m_GameObject: \{fileID: (\d+)\}");
                    if (!sprite.Success || !dono.Success) continue;
                    if (sprite.Groups[1].Value != GuidEmbutido) continue;

                    // Alguém veste este objeto no Awake? Então o YAML não diz nada.
                    scriptsDoObjeto.TryGetValue(dono.Groups[1].Value, out var scripts);
                    if (scripts != null && scripts.Any(escrevemSprite.Contains)) continue;

                    string nome = nomes.TryGetValue(dono.Groups[1].Value, out var n) ? n : "(?)";
                    nusAgora[nome] = Path.GetFileNameWithoutExtension(cena);
                }
            }

            var novos = nusAgora.Keys.Where(n => !ConhecidosSemArte.ContainsKey(n)).ToList();
            var vestidos = ConhecidosSemArte.Keys.Where(n => !nusAgora.ContainsKey(n)).ToList();

            var falhas = new List<string>();

            foreach (var nome in novos)
                falhas.Add($"NOVO sem arte: {nusAgora[nome]} / {nome} — desenha o quadrado " +
                           "branco da Unity e nenhum componente escreve o sprite dele em runtime");

            foreach (var nome in vestidos)
                falhas.Add($"JÁ TEM ARTE, mas continua na lista: {nome} — tire-o de " +
                           "ConhecidosSemArte, senão a lista vira depósito");

            Assert.IsEmpty(falhas,
                string.Join("\n  ", falhas) +
                "\n\nUm quadrado branco não aparece em erro nenhum do console: ele desenha, " +
                "colide e funciona — só não comunica nada. Foi assim que os 6 Nobres " +
                "Fossilizados e os 3 Espelhos de Aldebaran atravessaram meses de suíte verde.");
        }

        private static IEnumerable<string> CenasDoBuild()
        {
            string build = File.ReadAllText("ProjectSettings/EditorBuildSettings.asset");

            foreach (Match m in Regex.Matches(build, @"path: (Assets/Scenes/[^\s]+\.unity)"))
                if (File.Exists(m.Groups[1].Value))
                    yield return m.Groups[1].Value;
        }

        /// <summary>
        /// GUIDs dos scripts que atribuem <c>SpriteRenderer.sprite</c>. Um objeto com um deles
        /// não é julgado pelo YAML — o valor de lá é substituído em runtime.
        /// </summary>
        private static HashSet<string> ScriptsQueEscrevemSprite()
        {
            var guids = new HashSet<string>();

            foreach (var meta in Directory.EnumerateFiles("Assets/Scripts", "*.cs.meta",
                                                          SearchOption.AllDirectories))
            {
                string fonte = meta.Substring(0, meta.Length - ".meta".Length);
                if (!File.Exists(fonte)) continue;

                if (!Regex.IsMatch(File.ReadAllText(fonte), @"\.sprite\s*=")) continue;

                var g = Regex.Match(File.ReadAllText(meta), @"(?m)^guid: ([0-9a-f]{32})");
                if (g.Success) guids.Add(g.Groups[1].Value);
            }

            Assert.Greater(guids.Count, 0,
                "Nenhum script que escreve SpriteRenderer.sprite foi encontrado. A detecção " +
                "quebrou, e sem ela TODO objeto vestido em runtime seria acusado de estar nu — " +
                "que foi exatamente o erro da primeira versão desta medição.");

            return guids;
        }
    }
}
