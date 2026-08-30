using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using FavelaAmarela.Runtime.UI;

namespace FavelaAmarela.EditorTools
{
    /// <summary>
    /// Monta o prefab da tela de <b>Opções</b> em <c>Resources/Painel_Opcoes</c>.
    ///
    /// <para><b>Construído por código, e não à mão no Editor</b>, pelo mesmo motivo dos outros
    /// builders de UI deste projeto: um prefab montado à mão é irreproduzível — quando algo se
    /// desfaz, não há a que voltar, e o conserto vira arqueologia de Inspector. Aqui o layout é
    /// legível, versionado e re-executável.</para>
    ///
    /// <para><b>Idempotente:</b> rodar de novo reconstrói do zero, sobrescrevendo. Nenhum ajuste
    /// manual sobrevive — o que é a intenção, não um efeito colateral.</para>
    /// </summary>
    public static class MontarPainelDeOpcoes
    {
        private const string Marcador = "[PainelDeOpcoes]";
        private const string Destino = "Assets/FavelaAmarela/Resources/Painel_Opcoes.prefab";

        private const string SpriteDoTrilho = "Assets/FavelaAmarela/Art/UI/Sprites/bar_background.png";
        private const string SpriteDoFill = "Assets/FavelaAmarela/Art/UI/Sprites/bar_fill.png";

        private static readonly Color Fundo = new Color(0.06f, 0.06f, 0.05f, 0.94f);
        private static readonly Color Painel = new Color(0.11f, 0.11f, 0.09f, 1f);
        private static readonly Color Tinta = new Color(0.90f, 0.88f, 0.80f, 1f);
        private static readonly Color Sinal = new Color(0.83f, 0.70f, 0.24f, 1f);   // amarelo de Carcosa

        [MenuItem("Tools/FavelaAmarela/UI: montar o painel de opções")]
        public static void Executar()
        {
            // Resources.GetBuiltinResource, NÃO AssetDatabase.GetBuiltinExtraResource: só o
            // primeiro serve para fontes. E sem fallback para "Arial.ttf" -- na Unity 6 aquele
            // nome LANÇA ArgumentException em vez de devolver null, então um `??` não protege
            // nada: ele avalia o lado direito e a exceção sobe. FonteBuiltinTests guarda isso.
            var fonte = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            if (fonte == null)
            {
                Debug.LogError($"{Marcador} Fonte embutida não encontrada — o painel sairia " +
                               "sem texto nenhum.");
                return;
            }

            var raiz = new GameObject("Painel_Opcoes",
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));

            var canvas = raiz.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            // Acima do HUD (0) e do menu de pausa: opções abertas ficam por cima de tudo.
            canvas.sortingOrder = 200;

            var escala = raiz.GetComponent<CanvasScaler>();
            escala.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            escala.referenceResolution = new Vector2(1920f, 1080f);
            escala.matchWidthOrHeight = 0.5f;

            // ── Conteúdo: o que liga e desliga ────────────────────────────────
            var conteudo = Filho(raiz, "Conteudo");
            Esticar(conteudo);
            PintarFundo(conteudo, Fundo);

            var janela = Filho(conteudo, "Janela");
            var rt = janela.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(680f, 520f);
            PintarFundo(janela, Painel);

            var coluna = janela.AddComponent<VerticalLayoutGroup>();
            coluna.padding = new RectOffset(40, 40, 36, 36);
            coluna.spacing = 20;
            coluna.childControlWidth = true;
            coluna.childControlHeight = false;
            coluna.childForceExpandWidth = true;
            coluna.childForceExpandHeight = false;

            Titulo(janela, "OPÇÕES", fonte);

            var rotuloVolume = Rotulo(janela, "Volume: 80%", fonte);
            var barra = Barra(janela);

            var telaCheia = Alternador(janela, "Tela cheia", fonte);
            var vsync = Alternador(janela, "Sincronização vertical", fonte);

            Rotulo(janela, "Limite de quadros", fonte, 24);
            var quadros = Seletor(janela, fonte);

            var linhaDeBotoes = Filho(janela, "Botoes");
            Altura(linhaDeBotoes, 56f);
            var linha = linhaDeBotoes.AddComponent<HorizontalLayoutGroup>();
            linha.spacing = 16;
            linha.childControlWidth = true;
            linha.childForceExpandWidth = true;

            var restaurar = Botao(linhaDeBotoes, "Restaurar padrões", fonte, Painel);
            var fechar = Botao(linhaDeBotoes, "Fechar", fonte, Sinal);

            // ── O componente, com tudo ligado ─────────────────────────────────
            var painel = raiz.AddComponent<PainelDeOpcoes>();
            var so = new SerializedObject(painel);

            Ligar(so, "conteudo", conteudo);
            Ligar(so, "barraDeVolume", barra.GetComponent<Slider>());
            Ligar(so, "rotuloDoVolume", rotuloVolume.GetComponent<Text>());
            Ligar(so, "alternadorDeTelaCheia", telaCheia.GetComponent<Toggle>());
            Ligar(so, "alternadorDeVSync", vsync.GetComponent<Toggle>());
            Ligar(so, "seletorDeQuadros", quadros.GetComponent<Dropdown>());
            Ligar(so, "botaoDeFechar", fechar.GetComponent<Button>());
            Ligar(so, "botaoDeRestaurar", restaurar.GetComponent<Button>());

            so.ApplyModifiedPropertiesWithoutUndo();

            Directory.CreateDirectory(Path.GetDirectoryName(Destino));
            PrefabUtility.SaveAsPrefabAsset(raiz, Destino, out bool gravou);
            Object.DestroyImmediate(raiz);

            if (!gravou)
            {
                Debug.LogError($"{Marcador} SaveAsPrefabAsset RECUSOU.");
                return;
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"{Marcador} Criado em {Destino} — volume, tela cheia, VSync, limite de " +
                      "quadros, restaurar e fechar, todos ligados.");
        }

        // ── Peças ─────────────────────────────────────────────────────────────

        private static GameObject Filho(GameObject pai, string nome)
        {
            var go = new GameObject(nome, typeof(RectTransform));
            go.transform.SetParent(pai.transform, false);
            return go;
        }

        private static void Esticar(GameObject go)
        {
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        private static void Altura(GameObject go, float h)
        {
            var le = go.AddComponent<LayoutElement>();
            le.minHeight = le.preferredHeight = h;
        }

        private static void PintarFundo(GameObject go, Color cor)
        {
            var img = go.AddComponent<Image>();
            img.color = cor;

            // Sprite obrigatório: uma Image sem sprite ignora o 'type' (Image.cs:883) e o
            // fundo vira um retângulo cru. Aqui é Simple, então o efeito é só de estilo -- mas
            // o hábito é o que impede a próxima barra de nascer sem preenchimento.
            img.sprite = Carregar(SpriteDoTrilho);
            img.type = Image.Type.Sliced;
        }

        private static Sprite Carregar(string caminho)
        {
            var direto = AssetDatabase.LoadAssetAtPath<Sprite>(caminho);
            if (direto != null) return direto;

            // spriteMode Multiple: o Sprite é sub-asset e LoadAssetAtPath devolve null.
            foreach (var o in AssetDatabase.LoadAllAssetsAtPath(caminho))
                if (o is Sprite s) return s;

            return null;
        }

        private static GameObject Titulo(GameObject pai, string texto, Font fonte)
        {
            var go = Filho(pai, "Titulo");
            Altura(go, 54f);

            var t = go.AddComponent<Text>();
            t.text = texto;
            t.font = fonte;
            t.fontSize = 40;
            t.color = Sinal;
            t.alignment = TextAnchor.MiddleCenter;
            return go;
        }

        private static GameObject Rotulo(GameObject pai, string texto, Font fonte, int tamanho = 24)
        {
            var go = Filho(pai, $"Rotulo_{texto}");
            Altura(go, tamanho + 12f);

            var t = go.AddComponent<Text>();
            t.text = texto;
            t.font = fonte;
            t.fontSize = tamanho;
            t.color = Tinta;
            t.alignment = TextAnchor.MiddleLeft;
            return go;
        }

        private static GameObject Barra(GameObject pai)
        {
            var go = Filho(pai, "Barra_Volume");
            Altura(go, 30f);

            var slider = go.AddComponent<Slider>();

            var trilho = Filho(go, "Trilho");
            Esticar(trilho);
            var imgTrilho = trilho.AddComponent<Image>();
            imgTrilho.sprite = Carregar(SpriteDoTrilho);
            imgTrilho.type = Image.Type.Sliced;
            imgTrilho.color = Color.white;

            var area = Filho(go, "AreaDoPreenchimento");
            Esticar(area);

            var fill = Filho(area, "Preenchimento");
            Esticar(fill);
            var imgFill = fill.AddComponent<Image>();
            imgFill.sprite = Carregar(SpriteDoFill);
            imgFill.type = Image.Type.Sliced;
            imgFill.color = Sinal;

            slider.fillRect = fill.GetComponent<RectTransform>();
            slider.targetGraphic = imgTrilho;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 0.8f;

            return go;
        }

        private static GameObject Alternador(GameObject pai, string texto, Font fonte)
        {
            var go = Filho(pai, $"Alternador_{texto}");
            Altura(go, 40f);

            var toggle = go.AddComponent<Toggle>();

            var caixa = Filho(go, "Caixa");
            var rt = caixa.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0.5f);
            rt.anchorMax = new Vector2(0f, 0.5f);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.anchoredPosition = new Vector2(0f, 0f);
            rt.sizeDelta = new Vector2(28f, 28f);

            var fundo = caixa.AddComponent<Image>();
            fundo.sprite = Carregar(SpriteDoTrilho);
            fundo.type = Image.Type.Sliced;
            fundo.color = Color.white;

            var marca = Filho(caixa, "Marca");
            Esticar(marca);
            var imgMarca = marca.AddComponent<Image>();
            imgMarca.sprite = Carregar(SpriteDoFill);
            imgMarca.type = Image.Type.Sliced;
            imgMarca.color = Sinal;

            var rotulo = Filho(go, "Rotulo");
            var rrt = rotulo.GetComponent<RectTransform>();
            rrt.anchorMin = new Vector2(0f, 0f);
            rrt.anchorMax = new Vector2(1f, 1f);
            rrt.offsetMin = new Vector2(40f, 0f);
            rrt.offsetMax = Vector2.zero;

            var t = rotulo.AddComponent<Text>();
            t.text = texto;
            t.font = fonte;
            t.fontSize = 24;
            t.color = Tinta;
            t.alignment = TextAnchor.MiddleLeft;

            toggle.targetGraphic = fundo;
            toggle.graphic = imgMarca;
            toggle.isOn = true;

            return go;
        }

        private static GameObject Seletor(GameObject pai, Font fonte)
        {
            var go = Filho(pai, "Seletor_Quadros");
            Altura(go, 40f);

            var img = go.AddComponent<Image>();
            img.sprite = Carregar(SpriteDoTrilho);
            img.type = Image.Type.Sliced;
            img.color = Color.white;

            var drop = go.AddComponent<Dropdown>();
            drop.targetGraphic = img;

            var rotulo = Filho(go, "Rotulo");
            var rrt = rotulo.GetComponent<RectTransform>();
            rrt.anchorMin = Vector2.zero;
            rrt.anchorMax = Vector2.one;
            rrt.offsetMin = new Vector2(14f, 0f);
            rrt.offsetMax = new Vector2(-28f, 0f);

            var t = rotulo.AddComponent<Text>();
            t.font = fonte;
            t.fontSize = 24;
            t.color = Tinta;
            t.alignment = TextAnchor.MiddleLeft;
            drop.captionText = t;

            // Modelo da lista: o Dropdown exige um Template desativado com um item dentro.
            var template = Filho(go, "Template");
            var trt = template.GetComponent<RectTransform>();
            trt.anchorMin = new Vector2(0f, 0f);
            trt.anchorMax = new Vector2(1f, 0f);
            trt.pivot = new Vector2(0.5f, 1f);
            trt.anchoredPosition = new Vector2(0f, 2f);
            trt.sizeDelta = new Vector2(0f, 160f);

            var fundoTemplate = template.AddComponent<Image>();
            fundoTemplate.color = Painel;

            var scroll = template.AddComponent<ScrollRect>();

            var viewport = Filho(template, "Viewport");
            Esticar(viewport);
            viewport.AddComponent<Mask>().showMaskGraphic = false;
            viewport.AddComponent<Image>().color = Color.white;

            var conteudoLista = Filho(viewport, "Content");
            var crt = conteudoLista.GetComponent<RectTransform>();
            crt.anchorMin = new Vector2(0f, 1f);
            crt.anchorMax = new Vector2(1f, 1f);
            crt.pivot = new Vector2(0.5f, 1f);
            crt.sizeDelta = new Vector2(0f, 36f);

            var item = Filho(conteudoLista, "Item");
            var irt = item.GetComponent<RectTransform>();
            irt.anchorMin = new Vector2(0f, 0.5f);
            irt.anchorMax = new Vector2(1f, 0.5f);
            irt.sizeDelta = new Vector2(0f, 36f);

            var itemToggle = item.AddComponent<Toggle>();

            var itemFundo = Filho(item, "ItemFundo");
            Esticar(itemFundo);
            var imgItem = itemFundo.AddComponent<Image>();
            imgItem.color = Sinal;
            itemToggle.targetGraphic = imgItem;
            itemToggle.graphic = imgItem;

            var itemRotulo = Filho(item, "ItemRotulo");
            Esticar(itemRotulo);
            var itemTexto = itemRotulo.AddComponent<Text>();
            itemTexto.font = fonte;
            itemTexto.fontSize = 24;
            itemTexto.color = Tinta;
            itemTexto.alignment = TextAnchor.MiddleLeft;

            scroll.content = crt;
            scroll.viewport = viewport.GetComponent<RectTransform>();
            scroll.horizontal = false;

            drop.template = trt;
            drop.itemText = itemTexto;

            template.SetActive(false);

            return go;
        }

        private static GameObject Botao(GameObject pai, string texto, Font fonte, Color cor)
        {
            var go = Filho(pai, $"Botao_{texto}");
            Altura(go, 48f);

            var img = go.AddComponent<Image>();
            img.sprite = Carregar(SpriteDoTrilho);
            img.type = Image.Type.Sliced;
            img.color = cor;

            var botao = go.AddComponent<Button>();
            botao.targetGraphic = img;

            var rotulo = Filho(go, "Rotulo");
            Esticar(rotulo);

            var t = rotulo.AddComponent<Text>();
            t.text = texto;
            t.font = fonte;
            t.fontSize = 24;
            t.color = cor == Sinal ? new Color(0.08f, 0.08f, 0.06f) : Tinta;
            t.alignment = TextAnchor.MiddleCenter;

            return go;
        }

        private static void Ligar(SerializedObject so, string campo, Object valor)
        {
            var prop = so.FindProperty(campo);

            if (prop == null)
            {
                Debug.LogError($"{Marcador} Campo '{campo}' não existe no PainelDeOpcoes — a " +
                               "referência ficou solta.");
                return;
            }

            prop.objectReferenceValue = valor;
        }
    }
}
