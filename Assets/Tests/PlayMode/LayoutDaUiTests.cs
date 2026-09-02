using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace FavelaAmarela.Tests.PlayMode
{
    /// <summary>
    /// A <b>régua</b> da interface: carrega o HUD de verdade, deixa a Unity rodar o passo de
    /// layout, e mede os retângulos <b>resultantes</b>.
    ///
    /// <para><b>Por que este arquivo existe (2026-09-02).</b> O projeto tinha 129 arquivos de
    /// teste EditMode — <b>73 deles apenas leem YAML como texto</b> — e <b>um</b> de PlayMode.
    /// Sobreposição, texto que não cabe e clique que não chega <b>não existem no YAML</b>: eles
    /// nascem quando a Unity calcula o layout. O resultado foi eu relatar "conferido no disco"
    /// 17 vezes enquanto o menu de pause desenhava "Opções" em cima de "Sair do jogo".</para>
    ///
    /// <para><b>A regra dura deste arquivo:</b> se ele <b>não conseguir medir</b>, ele
    /// <b>falha</b> — não passa verde em silêncio. Um teste que não mede e diz "ok" é pior que
    /// teste nenhum, porque compra confiança que não tem lastro. Ver
    /// <see cref="AsMedidasFuncionam"/>.</para>
    ///
    /// <para><b>Ruído esperado:</b> o HUD é instanciado sem as dependências que o
    /// <c>GameLoopBootstrap</c> normalmente injeta, então vários <c>Awake</c> reclamam de
    /// referência nula. Isso é legítimo aqui — estamos medindo geometria, não comportamento —
    /// e por isso os erros de log são ignorados no <see cref="SetUp"/>.</para>
    /// </summary>
    public sealed class LayoutDaUiTests
    {
        private const string CaminhoDoHud = "HUD_Gameplay";

        /// <summary>Folga em pixels. Abaixo disto é arredondamento, não sobreposição.</summary>
        private const float Folga = 0.5f;

        private GameObject _hud;

        [SetUp]
        public void SetUp()
        {
            // O HUD sem o Bootstrap reclama de injeção ausente. Não é o que este arquivo mede.
            LogAssert.ignoreFailingMessages = true;
        }

        [TearDown]
        public void TearDown()
        {
            if (_hud != null) Object.DestroyImmediate(_hud);
            LogAssert.ignoreFailingMessages = false;
        }

        // ── Montagem ──────────────────────────────────────────────────────────

        /// <summary>
        /// Pega o HUD <b>que o jogo usa</b> e <b>liga tudo</b>.
        ///
        /// <para><b>Não instancia o prefab à mão</b>, e isso não é detalhe: o
        /// <c>HUDController.Awake</c> (`:135-137`) destrói duplicatas, então uma cópia minha
        /// seria destruída no mesmo frame e o teste mediria um objeto morto — foi o que
        /// aconteceu na primeira execução. Medir a instância real é mais honesto de qualquer
        /// forma: é ela que aparece na tela do Vini.</para>
        ///
        /// <para>Painéis como a tela de pause nascem <b>inativos</b>, e objeto inativo não tem
        /// retângulo calculado — mediria zero e passaria por acaso. Por isso tudo é ligado.</para>
        /// </summary>
        private IEnumerator MontarOHud()
        {
            FavelaAmarela.Runtime.UI.HUDController.GarantirInstancia();
            yield return null;

            var controlador = FavelaAmarela.Runtime.UI.HUDController.Instancia;
            Assert.IsNotNull(controlador,
                "HUDController.GarantirInstancia() não produziu instância — sem HUD não há o " +
                "que medir.");

            _hud = controlador.gameObject;

            foreach (var t in _hud.GetComponentsInChildren<Transform>(includeInactive: true))
                if (t != null && !t.gameObject.activeSelf) t.gameObject.SetActive(true);

            Canvas.ForceUpdateCanvases();
            yield return null;
            Canvas.ForceUpdateCanvases();
            yield return null;

            Assert.IsTrue(_hud != null, "O HUD foi destruído durante a montagem.");
        }

        /// <summary>O retângulo do elemento em pixels de tela, já com o layout resolvido.</summary>
        private static Rect Retangulo(RectTransform rt)
        {
            var cantos = new Vector3[4];
            rt.GetWorldCorners(cantos);

            return Rect.MinMaxRect(cantos[0].x, cantos[0].y, cantos[2].x, cantos[2].y);
        }

        private static string Caminho(Transform t)
        {
            var partes = new List<string>();
            for (var a = t; a != null; a = a.parent) partes.Add(a.name);
            partes.Reverse();
            return string.Join("/", partes);
        }

        // ── 1. A régua sabe medir? ────────────────────────────────────────────

        /// <summary>
        /// <b>Roda primeiro, e os outros não valem nada sem ele.</b> Em <c>-nographics</c> as
        /// métricas de fonte podem não existir, e aí <c>preferredWidth</c> devolve 0 para todo
        /// texto — o que faria os testes de "cabe na caixa" passarem verdes sem medir nada.
        ///
        /// <para>Se este teste falhar, rode com <c>-ComGraficos</c>. O que <b>não</b> se faz é
        /// ignorar: foi exatamente esse tipo de silêncio que produziu o print quebrado.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator AsMedidasFuncionam()
        {
            yield return MontarOHud();

            var comTexto = _hud.GetComponentsInChildren<Text>(true)
                .Where(t => !string.IsNullOrWhiteSpace(t.text))
                .ToArray();

            Assert.IsNotEmpty(comTexto, "Nenhum Text com conteúdo no HUD — nada a medir.");

            var mudos = comTexto.Where(t => t.preferredWidth <= 0f)
                                .Select(t => Caminho(t.transform))
                                .ToList();

            Assert.IsEmpty(mudos,
                $"{mudos.Count} de {comTexto.Length} Text não têm métrica de fonte — " +
                "'preferredWidth' devolveu 0 num texto não-vazio. **NÃO CONSEGUI MEDIR**, e por " +
                "isso este arquivo inteiro está cego. Rode a suíte PlayMode com -ComGraficos. " +
                "Primeiros: " + string.Join(", ", mudos.Take(5)));

            var comArea = _hud.GetComponentsInChildren<RectTransform>(true)
                              .Count(rt => Retangulo(rt).width > 1f);

            Assert.Greater(comArea, 10,
                $"Só {comArea} elementos têm área — o Canvas não resolveu o layout, e medir " +
                "retângulo de largura zero passaria em qualquer asserção.");
        }

        // ── 2. Nada de irmão em cima de irmão ─────────────────────────────────

        /// <summary>
        /// Dois <c>Selectable</c> irmãos no mesmo retângulo: o de baixo fica invisível e
        /// inclicável, sem erro nenhum.
        ///
        /// <para>Este é o defeito que o Vini viu no print — "Opções" por cima de "Sair do jogo"
        /// na tela de pause. Já existia um teste assim (<c>MenuSemBotaoEscondidoTests</c>), e
        /// ele olhava <b>só</b> <c>Cena_Menu.unity</c>: o gêmeo no <c>HUD_Gameplay.prefab</c>
        /// ficou quebrado o tempo todo.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator NenhumBotaoIrmaoOcupaOMesmoLugar()
        {
            yield return MontarOHud();

            var problemas = new List<string>();

            foreach (var grupo in _hud.GetComponentsInChildren<Selectable>(true)
                         .Where(s => s.transform.parent != null)
                         .GroupBy(s => s.transform.parent))
            {
                var irmaos = grupo.ToArray();

                for (int i = 0; i < irmaos.Length; i++)
                for (int j = i + 1; j < irmaos.Length; j++)
                {
                    var a = Retangulo((RectTransform)irmaos[i].transform);
                    var b = Retangulo((RectTransform)irmaos[j].transform);

                    var corte = Rect.MinMaxRect(
                        Mathf.Max(a.xMin, b.xMin), Mathf.Max(a.yMin, b.yMin),
                        Mathf.Min(a.xMax, b.xMax), Mathf.Min(a.yMax, b.yMax));

                    if (corte.width <= Folga || corte.height <= Folga) continue;

                    float menor = Mathf.Min(a.width * a.height, b.width * b.height);
                    float fracao = menor <= 0f ? 1f : (corte.width * corte.height) / menor;

                    problemas.Add(
                        $"{irmaos[i].name} × {irmaos[j].name} em '{Caminho(grupo.Key)}': " +
                        $"{fracao:P0} de sobreposição (a={a}, b={b})");
                }
            }

            Assert.IsEmpty(problemas,
                "Botão(ões) desenhando em cima de irmão:" + System.Environment.NewLine + "  " +
                string.Join(System.Environment.NewLine + "  ", problemas) +
                System.Environment.NewLine +
                "O de baixo fica invisível e inclicável, e nada no projeto reclama.");
        }

        // ── 3. Todo texto cabe na própria caixa ───────────────────────────────

        /// <summary>
        /// Com <c>verticalOverflow = Truncate</c> e <c>BestFit</c> desligado — o estado de 66
        /// dos 67 <c>Text</c> do HUD — texto que passa da altura da caixa é simplesmente
        /// <b>cortado</b>. É o "Vitalidade Corpórea: 100" virando pedaços de três letras.
        /// </summary>
        [UnityTest]
        public IEnumerator TodoTextoCabeNaPropriaCaixa()
        {
            yield return MontarOHud();

            var estourando = new List<string>();

            foreach (var txt in _hud.GetComponentsInChildren<Text>(true))
            {
                if (string.IsNullOrWhiteSpace(txt.text)) continue;
                if (txt.resizeTextForBestFit) continue;              // o BestFit já encolhe
                if (txt.verticalOverflow == VerticalWrapMode.Overflow) continue;  // pode vazar

                // LOCAL contra LOCAL. 'preferredHeight' vem em unidades do próprio
                // RectTransform; 'GetWorldCorners' vem em pixels de tela, já multiplicados pelo
                // scaleFactor do Canvas. Comparar os dois acusou dois rótulos inocentes na
                // primeira execução -- o teste é que estava errado, não o HUD.
                float disponivel = txt.rectTransform.rect.height;
                if (disponivel <= Folga) continue;                    // sem área, sem asserção

                float precisa = txt.preferredHeight;
                if (precisa <= disponivel + Folga) continue;

                estourando.Add(
                    $"{Caminho(txt.transform)}: precisa de {precisa:0} un de altura e tem " +
                    $"{disponivel:0} (fonte {txt.fontSize}, texto \"{Encurtar(txt.text)}\")");
            }

            Assert.IsEmpty(estourando,
                "Texto(s) cortados pela própria caixa:" + System.Environment.NewLine + "  " +
                string.Join(System.Environment.NewLine + "  ", estourando) +
                System.Environment.NewLine +
                "Conserto: caixa maior, fonte menor, ou BestFit ligado — nesta ordem.");
        }

        private static string Encurtar(string s)
        {
            s = s.Replace("\n", " ").Trim();
            return s.Length <= 32 ? s : s.Substring(0, 29) + "...";
        }
    }
}
