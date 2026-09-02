using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using FavelaAmarela.Runtime.UI;

namespace FavelaAmarela.Tests.EditMode
{
    /// <summary>
    /// Guarda a legibilidade do diálogo — a coisa que o jogador passa mais tempo lendo.
    ///
    /// <para><b>O defeito, do playtest de 2026-08-28.</b> O Vini: <i>"a do Abdul está pequena
    /// demais e mal dá para ler o texto, já a da Cassilda as letras estão grandes demais e não
    /// cabem na caixa"</i>. Medido, era o <b>mesmo componente</b> autorado à mão em cada
    /// lugar:</para>
    ///
    /// <list type="bullet">
    ///   <item><c>Playtest_RuinasPalidas</c> · <c>PainelDeEscolha</c> — fonte <b>16</b></item>
    ///   <item><c>Santuario_Yhtill</c> · <c>PainelDeEscolha</c> — fonte <b>60</b></item>
    ///   <item><c>HUD_Gameplay</c> · <c>CaixaDeDialogo</c> — fonte <b>60</b></item>
    /// </list>
    ///
    /// <para><b>Quatro vezes de diferença.</b> É a mesma família do zoom de câmera, que tinha
    /// sete valores em sete ferramentas: peça autorada em vários lugares, sem nada mantendo os
    /// lugares em acordo. E ninguém vigiava — é um campo de Inspector, e o efeito só aparece
    /// jogando aquela cena específica com aquela fala específica.</para>
    ///
    /// <para><b>Por que a regra é Best Fit e não um número.</b> O texto do jogo varia <b>4×</b>:
    /// a fala mais curta do Abdul tem 72 caracteres, a reação mais longa da Cassilda tem
    /// <b>278</b>. Nenhum número fixo serve aos dois — foi tentar servir que produziu 16 de um
    /// lado e 60 do outro.</para>
    /// </summary>
    public sealed class TipografiaDeDialogoTests
    {
        /// <summary><c>VerticalWrapMode.Overflow</c> na serialização (Truncate é 0).</summary>
        private const int Overflow = 1;

        /// <summary><c>HorizontalWrapMode.Wrap</c> na serialização.</summary>
        private const int Wrap = 0;

        [Test]
        public void TodoTextoDeDialogo_SeAjustaAoTamanhoDaFala()
        {
            var fora = new List<string>();
            var vistos = 0;

            foreach (var t in TextosDeDialogo())
            {
                vistos++;

                if (t.BestFit != 1)
                    fora.Add($"{t.Onde} · {t.Dono}: fonte FIXA em {t.FonteFixa} — texto longo é " +
                             "cortado e texto curto fica minúsculo");

                if (t.Minimo != PadraoDeTextoDeDialogo.TamanhoMinimo)
                    fora.Add($"{t.Onde} · {t.Dono}: mínimo {t.Minimo} " +
                             $"(padrão {PadraoDeTextoDeDialogo.TamanhoMinimo})");

                if (t.Maximo != PadraoDeTextoDeDialogo.TamanhoMaximo)
                    fora.Add($"{t.Onde} · {t.Dono}: máximo {t.Maximo} " +
                             $"(padrão {PadraoDeTextoDeDialogo.TamanhoMaximo})");
            }

            Assert.Greater(vistos, 0,
                "Nenhum texto de diálogo encontrado. Este guarda parou de olhar para o jogo — " +
                "o campo 'texto' do TutorialHintUI ou do PainelDeEscolha mudou de nome?");

            Assert.IsEmpty(fora,
                "Texto(s) de diálogo fora do padrão:" + Environment.NewLine + "  " +
                string.Join(Environment.NewLine + "  ", fora) + Environment.NewLine +
                "Conserto: 'Tools/FavelaAmarela/UI: padronizar o texto de diálogo'.");
        }

        /// <summary>
        /// Texto de diálogo usa <b>Truncate</b>, não Overflow.
        ///
        /// <para><b>Esta regra era o INVERSO até 2026-09-02</b>, e o raciocínio de então estava
        /// certo <i>para o que existia</i>: <i>"a fala que não cabe é cortada sem aviso;
        /// transbordar é feio e visível, cortar é limpo e é mentira"</i>. Sem garantia de caber,
        /// o corte silencioso é mesmo o pior dos dois.</para>
        ///
        /// <para><b>O que mudou.</b> Primeiro, o preço real do Overflow apareceu: o Vini mandou
        /// um print com o poema da Cassilda <b>atravessando a tela inteira</b>, por cima do HUD e
        /// do cenário. Não era "feio e visível" — era ilegível. E a causa é técnica: com
        /// <c>Overflow</c> a Unity <b>não encolhe por altura</b>, então o <c>BestFit</c>, mesmo
        /// ligado, nunca era acionado. A caixa jamais tentou caber.</para>
        ///
        /// <para>Segundo, e é o que torna Truncate seguro: passou a existir a
        /// <b>garantia de capacidade</b>. O
        /// <c>LayoutDaUiTests.ACaixaDeDialogoComportaAFalaMaisLonga</c> (PlayMode) afirma que a
        /// caixa comporta 12 linhas <b>no piso do BestFit</b> — mais do que a fala mais longa do
        /// jogo. Com essa garantia o Truncate <b>nunca dispara</b>, e o que ele passa a fazer é
        /// permitir que o BestFit funcione.</para>
        ///
        /// <para><b>Os dois testes são um par:</b> este exige Truncate, aquele garante que
        /// Truncate não corta. Enfraquecer um sem o outro devolve o defeito.</para>
        /// </summary>
        [Test]
        public void TodoTextoDeDialogo_UsaTruncateParaOBestFitFuncionar()
        {
            var vazam = TextosDeDialogo()
                .Where(t => t.VerticalOverflow == Overflow)
                .Select(t => $"{t.Onde} · {t.Dono}: vertical Overflow")
                .ToList();

            Assert.IsEmpty(vazam,
                "Texto(s) de diálogo em Overflow:" + Environment.NewLine + "  " +
                string.Join(Environment.NewLine + "  ", vazam) + Environment.NewLine +
                "Com Overflow a Unity NÃO encolhe por altura: o BestFit fica ligado e nunca é " +
                "acionado, e a fala atravessa a tela por cima do HUD e do cenário — foi o print " +
                "do Vini em 2026-09-02." + Environment.NewLine +
                "Truncate só é seguro junto com a garantia de capacidade: ver " +
                "LayoutDaUiTests.ACaixaDeDialogoComportaAFalaMaisLonga. Os dois andam em par.");
        }

        [Test]
        public void TodoTextoDeDialogo_QuebraLinhaEmVezDeVazarNaHorizontal()
        {
            var vazam = TextosDeDialogo()
                .Where(t => t.HorizontalOverflow != Wrap)
                .Select(t => $"{t.Onde} · {t.Dono}")
                .ToList();

            Assert.IsEmpty(vazam,
                "Texto(s) de diálogo sem quebra de linha:" + Environment.NewLine + "  " +
                string.Join(Environment.NewLine + "  ", vazam) + Environment.NewLine +
                "Sem Wrap, a fala vira uma linha só e sai pelas laterais da tela.");
        }

        // ── Leitura do YAML ───────────────────────────────────────────────────

        private sealed class TextoDeDialogo
        {
            public string Onde;
            public string Dono;
            public int BestFit;
            public int Minimo;
            public int Maximo;
            public int HorizontalOverflow;
            public int VerticalOverflow;
            public int FonteFixa;
        }

        /// <summary>
        /// Todo <c>Text</c> que <b>é</b> texto de diálogo. <b>Derivado, não por nome:</b>
        /// segue a referência do campo <c>texto</c> de quem escreve diálogo
        /// (<c>TutorialHintUI</c> e <c>PainelDeEscolha</c>). Uma lista de nomes seria frágil e
        /// ambígua — os objetos se chamam todos "Texto".
        /// </summary>
        private static IEnumerable<TextoDeDialogo> TextosDeDialogo()
        {
            string guidDica = GuidDo("Assets/Scripts/UI/TutorialHintUI.cs");
            string guidEscolha = GuidDo("Assets/Scripts/UI/PainelDeEscolha.cs");

            foreach (var caminho in Arquivos())
            {
                string yaml = File.ReadAllText(caminho);
                if (!yaml.Contains(guidDica) && !yaml.Contains(guidEscolha)) continue;

                var docs = Documentos(yaml);
                var donos = new Dictionary<string, string>();

                foreach (var d in docs)
                {
                    string dono = d.Corpo.Contains(guidDica) ? "TutorialHintUI"
                                : d.Corpo.Contains(guidEscolha) ? "PainelDeEscolha"
                                : null;
                    if (dono == null) continue;

                    var t = Regex.Match(d.Corpo, @"^\s*texto:\s*\{fileID:\s*(-?\d+)\}",
                                        RegexOptions.Multiline);
                    if (t.Success && t.Groups[1].Value != "0") donos[t.Groups[1].Value] = dono;
                }

                foreach (var d in docs)
                {
                    if (!donos.TryGetValue(d.Id, out string dono)) continue;
                    if (!d.Corpo.Contains("UnityEngine.UI.Text")) continue;

                    yield return new TextoDeDialogo
                    {
                        Onde = Path.GetFileName(caminho),
                        Dono = dono,
                        BestFit = Numero(d.Corpo, "m_BestFit"),
                        Minimo = Numero(d.Corpo, "m_MinSize"),
                        Maximo = Numero(d.Corpo, "m_MaxSize"),
                        HorizontalOverflow = Numero(d.Corpo, "m_HorizontalOverflow"),
                        VerticalOverflow = Numero(d.Corpo, "m_VerticalOverflow"),
                        FonteFixa = Numero(d.Corpo, "m_FontSize"),
                    };
                }
            }
        }

        private static IEnumerable<string> Arquivos()
        {
            foreach (var c in Directory.GetFiles("Assets", "*.unity", SearchOption.AllDirectories))
                yield return c;

            foreach (var c in Directory.GetFiles("Assets", "*.prefab", SearchOption.AllDirectories))
                yield return c;
        }

        private static string GuidDo(string script)
        {
            string meta = script + ".meta";
            Assert.IsTrue(File.Exists(meta), $"Meta ausente: {meta}");

            var m = Regex.Match(File.ReadAllText(meta), @"guid:\s*(\w+)");
            Assert.IsTrue(m.Success, $"Sem guid em {meta}");
            return m.Groups[1].Value;
        }

        private sealed class Documento
        {
            public string Id;
            public string Corpo;
        }

        private static List<Documento> Documentos(string yaml)
        {
            var docs = new List<Documento>();
            var marcadores = Regex.Matches(yaml, @"^--- !u!\d+ &(-?\d+).*$", RegexOptions.Multiline);

            for (int i = 0; i < marcadores.Count; i++)
            {
                int inicio = marcadores[i].Index + marcadores[i].Length;
                int fim = i + 1 < marcadores.Count ? marcadores[i + 1].Index : yaml.Length;

                docs.Add(new Documento
                {
                    Id = marcadores[i].Groups[1].Value,
                    Corpo = yaml.Substring(inicio, fim - inicio),
                });
            }

            return docs;
        }

        private static int Numero(string corpo, string campo)
        {
            var m = Regex.Match(corpo, @"^\s*" + Regex.Escape(campo) + @":\s*(-?\d+)\s*$",
                                RegexOptions.Multiline);
            return m.Success && int.TryParse(m.Groups[1].Value, out int v) ? v : -1;
        }
    }
}
