using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using FavelaAmarela.Runtime.UI;
using static FavelaAmarela.EditorTools.MontarPainelDeOpcoes;

namespace FavelaAmarela.EditorTools
{
    /// <summary>
    /// Monta o prefab <c>Resources/Painel_Creditos</c>: uma janela com o texto de
    /// <c>Resources/CREDITOS.txt</c> numa rolagem, e um botão Fechar.
    ///
    /// <para><b>Por que a tela existe</b> está no <see cref="PainelDeCreditos"/> — é condição de
    /// licença, não enfeite. <b>Por que uma ferramenta</b>: o mesmo motivo do painel de opções —
    /// o prefab nasce de código que se lê e se roda de novo, não de cliques que ninguém
    /// registrou. Reaproveita os helpers e a paleta do <see cref="MontarPainelDeOpcoes"/>, para
    /// as duas telas terem a mesma cara.</para>
    ///
    /// <para><b>Layout que cabe por construção</b> (a lição do painel de opções, 2026-09-10): a
    /// janela é uma coluna com <c>childControlHeight</c> ligado, o corpo é uma <c>ScrollRect</c>
    /// que ocupa o que sobra (<c>flexibleHeight</c>), e o texto dentro dela cresce pelo
    /// <c>ContentSizeFitter</c>. Texto longo rola; nada sai da janela. Guarda:
    /// <c>OPainelDeCreditosTests</c>.</para>
    /// </summary>
    public static class MontarPainelDeCreditos
    {
        private const string Marcador = "[PainelDeCreditos]";
        private const string Destino = "Assets/FavelaAmarela/Resources/Painel_Creditos.prefab";

        /// <summary>Janela mais alta que a de opções: créditos são texto corrido.</summary>
        private const float AlturaDaJanela = 820f;
        private const float LarguraDaJanela = 900f;

        [MenuItem("Tools/FavelaAmarela/UI: montar o painel de créditos")]
        public static void Executar()
        {
            var fonte = PaletaDaInterface.Fonte;
            if (fonte == null)
            {
                Debug.LogError($"{Marcador} Fonte do jogo não encontrada (PaletaDaInterface.Fonte).");
                fonte = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (fonte == null) return;
            }

            var textoDosCreditos = AssetDatabase.LoadAssetAtPath<TextAsset>(
                $"Assets/FavelaAmarela/Resources/{PainelDeCreditos.NomeDoTexto}.txt");
            if (textoDosCreditos == null)
                Debug.LogWarning($"{Marcador} Resources/{PainelDeCreditos.NomeDoTexto}.txt não existe — o " +
                                 "painel vai nascer e carregar o texto em runtime, se ele aparecer.");

            var raiz = new GameObject("Painel_Creditos",
                typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));

            var canvas = raiz.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 200;   // por cima do HUD e do menu, como as opções

            var escala = raiz.GetComponent<CanvasScaler>();
            escala.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            escala.referenceResolution = new Vector2(1920f, 1080f);
            escala.matchWidthOrHeight = 0.5f;

            var conteudo = Filho(raiz, "Conteudo");
            Esticar(conteudo);
            PintarFundo(conteudo, Fundo);

            var janela = Filho(conteudo, "Janela");
            var rt = janela.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(LarguraDaJanela, AlturaDaJanela);
            PintarFundo(janela, Painel);

            var coluna = janela.AddComponent<VerticalLayoutGroup>();
            coluna.padding = new RectOffset(40, 40, 36, 36);
            coluna.spacing = 20;
            coluna.childControlWidth = true;
            coluna.childControlHeight = true;    // as alturas preferidas VALEM (ver a lição das opções)
            coluna.childForceExpandWidth = true;
            coluna.childForceExpandHeight = false;

            Titulo(janela, "CRÉDITOS", fonte);

            var (rolagem, corpo) = Rolagem(janela, fonte, textoDosCreditos != null ? textoDosCreditos.text : "");

            var linhaDeBotoes = Filho(janela, "Botoes");
            Altura(linhaDeBotoes, 64f);
            var linha = linhaDeBotoes.AddComponent<HorizontalLayoutGroup>();
            linha.spacing = 16;
            linha.childControlWidth = true;
            linha.childForceExpandWidth = true;
            linha.childAlignment = TextAnchor.MiddleRight;

            // childForceExpandHeight nasce TRUE num HorizontalLayoutGroup novo, e isso faz a linha
            // inteira reportar flexibleHeight = 1 para a coluna de cima — que então dividia o
            // espaço livre entre a rolagem e a linha dos botões (medido: botões com 255 px de
            // altura e a rolagem espremida em 399). A linha tem a altura dos botões, e só.
            linha.childForceExpandHeight = false;
            linha.childControlHeight = true;

            var fechar = Botao(linhaDeBotoes, "Fechar", fonte, Sinal);

            var painel = raiz.AddComponent<PainelDeCreditos>();
            var so = new SerializedObject(painel);
            Ligar(so, "conteudo", conteudo);
            Ligar(so, "corpo", corpo);
            Ligar(so, "rolagem", rolagem);
            Ligar(so, "botaoDeFechar", fechar.GetComponent<Button>());
            Ligar(so, "texto", textoDosCreditos);
            so.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(raiz, Destino, out bool gravou);
            Object.DestroyImmediate(raiz);

            Debug.Log(gravou
                ? $"{Marcador} Prefab gravado em {Destino}."
                : $"{Marcador} FALHA ao gravar {Destino}.");
        }

        /// <summary>
        /// A rolagem: <c>ScrollRect</c> ocupando o que sobra da coluna, com um <c>Viewport</c>
        /// mascarado e um <c>Text</c> que cresce em altura pelo <c>ContentSizeFitter</c>.
        /// </summary>
        private static (ScrollRect, Text) Rolagem(GameObject pai, Font fonte, string texto)
        {
            var area = Filho(pai, "Rolagem");
            var le = area.AddComponent<LayoutElement>();
            le.flexibleHeight = 1f;     // o que sobrar da janela é do texto
            le.minHeight = 200f;

            var rolagem = area.AddComponent<ScrollRect>();
            rolagem.horizontal = false;
            rolagem.vertical = true;
            rolagem.movementType = ScrollRect.MovementType.Clamped;
            rolagem.scrollSensitivity = 40f;

            var viewport = Filho(area, "Viewport");
            Esticar(viewport);
            var mascara = viewport.AddComponent<Image>();
            mascara.color = new Color(1f, 1f, 1f, 0.02f);   // quase invisível; a Mask precisa de uma Image
            mascara.raycastTarget = true;                    // é o que recebe a roda do mouse
            viewport.AddComponent<Mask>().showMaskGraphic = false;

            var conteudo = Filho(viewport, "Conteudo");
            var crt = conteudo.GetComponent<RectTransform>();
            crt.anchorMin = new Vector2(0f, 1f);
            crt.anchorMax = new Vector2(1f, 1f);
            crt.pivot = new Vector2(0.5f, 1f);
            crt.anchoredPosition = Vector2.zero;
            crt.sizeDelta = new Vector2(0f, 0f);

            var t = conteudo.AddComponent<Text>();
            t.font = fonte;
            t.fontSize = 26;
            t.lineSpacing = 1.15f;
            t.color = Tinta;
            t.alignment = TextAnchor.UpperLeft;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;   // a altura vem do fitter, não do corte
            t.supportRichText = false;
            t.raycastTarget = false;
            t.text = texto;

            var fitter = conteudo.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            rolagem.viewport = viewport.GetComponent<RectTransform>();
            rolagem.content = crt;

            return (rolagem, t);
        }
    }
}
