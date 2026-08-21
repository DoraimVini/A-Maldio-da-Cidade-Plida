using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace FavelaAmarela.Tests.EditMode
{
    /// <summary>
    /// Guarda que a UI das cenas tem <b>como ser desenhada</b> — não só que existe.
    ///
    /// <para><b>O bug que motivou:</b> o <c>PainelDeInventario</c> era criado com
    /// <c>new GameObject(nome)</c>, que nasce com <c>Transform</c> comum. Um
    /// <c>RectTransform</c> filho de <c>Transform</c> comum <b>não tem retângulo de pai onde
    /// ancorar</b>: com âncoras 0..1 a <c>Janela</c> resolvia para 0×0. Apertar TAB abria o
    /// inventário, pausava o jogo, e não mostrava absolutamente nada — relatado pelo Vini como
    /// "parece ser só um pause".</para>
    ///
    /// <para><b>Por que os testes existentes não pegavam:</b> <c>SlotsDoInventarioTests</c>
    /// verifica 12 casas de mochila, 7 de corpo e o <c>raizDoPainel</c> ligado. Tudo isso estava
    /// certo. Estrutura correta, e invisível — a diferença entre "os objetos estão lá" e "os
    /// objetos aparecem" não era medida por nada.</para>
    ///
    /// <para><b>Por que só o inventário quebrou:</b> os outros montadores de UI criam a raiz com
    /// <c>Image</c>, que exige <c>RectTransform</c> e faz a Unity adicioná-lo por tabela. O
    /// painel era o único sem <c>Graphic</c> — escapava do acidente que salvava os demais.</para>
    /// </summary>
    public sealed class UiRenderavelTests
    {
        private static readonly string[] Cenas =
        {
            "Assets/Scenes/Deserto_Hali.unity",
            "Assets/Scenes/Playtest_RuinasPalidas.unity",
            "Assets/Scenes/Santuario_Yhtill.unity",
            "Assets/Scenes/Portoes_Das_Ruinas.unity",
            "Assets/Scenes/Castelo_Carcosa.unity",
        };

        /// <summary>
        /// Objetos que vivem sob o <c>Canvas</c> e portanto <b>precisam</b> de
        /// <c>RectTransform</c>. Lista explícita, e não heurística: nomear o que se espera é o
        /// que faz a falha apontar para a peça certa.
        /// </summary>
        private static readonly string[] RaizesDeUi =
        {
            "PainelDeInventario", "Janela", "Mochila", "Corpo",
            "BarraDeItens", "CaixaDeDialogo",
        };

        /// <summary>Classe 4 = <c>Transform</c>; 224 = <c>RectTransform</c>.</summary>
        private const string ClasseTransform = "4";
        private const string ClasseRectTransform = "224";

        [Test]
        public void TodaRaizDeUi_TemRectTransform()
        {
            var falhas = new List<string>();

            foreach (var caminho in Cenas)
            {
                if (!File.Exists(caminho)) { falhas.Add($"{Nome(caminho)}: cena ausente"); continue; }

                string txt = File.ReadAllText(caminho);
                var porId = IndexarDocumentos(txt);

                foreach (var (fileId, doc) in porId)
                {
                    if (doc.Classe != "1") continue;

                    var nome = Regex.Match(doc.Texto, @"(?m)^\s*m_Name:\s*(\S[^\r\n]*)");
                    if (!nome.Success) continue;

                    string n = nome.Groups[1].Value.Trim();
                    if (System.Array.IndexOf(RaizesDeUi, n) < 0) continue;

                    string classeDoTransform = ClasseDoTransform(doc.Texto, porId);

                    if (classeDoTransform == ClasseTransform)
                        falhas.Add($"{Nome(caminho)}: '{n}' tem Transform comum em vez de " +
                                   "RectTransform — os filhos ancoram em nada e somem da tela");
                }
            }

            Assert.IsEmpty(falhas,
                "UI presente na cena e impossível de desenhar:\n  " + string.Join("\n  ", falhas));
        }

        // ── Leitura do YAML ───────────────────────────────────────────────────

        private readonly struct Documento
        {
            public readonly string Classe;
            public readonly string Texto;
            public Documento(string classe, string texto) { Classe = classe; Texto = texto; }
        }

        private static Dictionary<string, Documento> IndexarDocumentos(string txt)
        {
            var mapa = new Dictionary<string, Documento>();

            foreach (Match m in Regex.Matches(txt,
                         @"---\s*!u!(\d+)\s*&(-?\d+)\r?\n(?:(?!^---)[\s\S])*",
                         RegexOptions.Multiline))
            {
                mapa[m.Groups[2].Value] = new Documento(m.Groups[1].Value, m.Value);
            }

            return mapa;
        }

        /// <summary>
        /// Classe do transform do GameObject, percorrendo os componentes <b>dele</b> — e não
        /// pegando o primeiro que aparece depois do nome no arquivo. A ordem dos documentos no
        /// YAML não é a da hierarquia, e ler o componente do objeto errado já fez um teste deste
        /// projeto reprovar dado correto.
        /// </summary>
        private static string ClasseDoTransform(string docDoGo, Dictionary<string, Documento> porId)
        {
            foreach (Match c in Regex.Matches(docDoGo, @"component:\s*\{fileID:\s*(-?\d+)\}"))
            {
                string id = c.Groups[1].Value;
                if (!porId.TryGetValue(id, out var comp)) continue;

                if (comp.Classe == ClasseTransform || comp.Classe == ClasseRectTransform)
                    return comp.Classe;
            }

            return null;
        }

        private static string Nome(string caminho) => Path.GetFileNameWithoutExtension(caminho);
    }
}
