using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using FavelaAmarela.Inventario;
using FavelaAmarela.Runtime.UI;

namespace FavelaAmarela.EditorTools
{
    /// <summary>
    /// Ferramenta de Editor. Monta a <b>tela de inventário</b> em três colunas — Mochila em
    /// grade, Corpo em lista, e o <see cref="PainelDoItem"/> (o item escolhido, ou o Damião) — e
    /// a liga ao <see cref="PainelDeInventario"/>.
    ///
    /// <para><b>O plano B do inventário (2026-09-28).</b> A versão anterior posicionava cada casa
    /// por âncora calculada à mão e pendurava a ficha de atributos fora da borda direita da
    /// janela (âncora x = 1): ela quebrava linha a cada palavra, cobria as linhas do Corpo e a
    /// dica de fechar. Agora a grade é um <c>GridLayoutGroup</c>, o Corpo um
    /// <c>VerticalLayoutGroup</c>, e o texto do detalhe se empilha sozinho — mudar um tamanho
    /// deixou de exigir refazer contas de âncora.</para>
    ///
    /// <para><b>Onde roda.</b> O HUD é o <c>Resources/HUD_Gameplay.prefab</c>, persistente
    /// desde 2026-08-22; <see cref="MontarNoHud"/> reconstrói só o interior do painel dentro
    /// dele, preservando a raiz (que o <c>HUDController</c> referencia) e as artes de moldura.
    /// <see cref="MontarNaCenaAberta"/> continua servindo ao <c>BuildHUDCompleto</c>.</para>
    ///
    /// <para>Idempotente: refaz a <c>Janela</c> do zero a cada execução. Os caminhos
    /// <c>Janela/Mochila/Slot_N</c> e <c>Janela/Corpo/Corpo_N</c> são os que as regras do
    /// <c>AplicarUiDarkAges</c> esperam — não os renomeie sem elas.</para>
    /// </summary>
    public static class MontarPainelDeInventario
    {
        private const string Hud = "Assets/FavelaAmarela/Resources/HUD_Gameplay.prefab";

        private const int SlotsDaMochila = MainInventory.DefaultCapacidadeSurvivalHorror; // 12
        private const int SlotsDoCorpo = 7;                                               // com a Mão Secundária
        private const int ColunasDaMochila = 4;

        private const float LadoDaCasa = 150f;
        private const float EspacoEntreCasas = 16f;
        private const float AlturaDaLinhaDoCorpo = 90f;

        private static readonly Color CorDoTitulo = new Color(0.92f, 0.86f, 0.55f, 0.95f);
        private static readonly Color CorDoRotulo = new Color(0.85f, 0.80f, 0.60f, 0.75f);
        private static readonly Color CorDoTexto = new Color(0.93f, 0.89f, 0.78f, 1f);
        private static readonly Color CorFraca = new Color(0.72f, 0.66f, 0.52f, 0.9f);
        private static readonly Color CorDoAviso = new Color(0.98f, 0.62f, 0.38f, 1f);

        [MenuItem("Tools/FavelaAmarela/Inventário: montar a tela no HUD")]
        public static void MontarNoHud()
        {
            var raiz = PrefabUtility.LoadPrefabContents(Hud);
            try
            {
                var comp = raiz.GetComponentInChildren<PainelDeInventario>(true);
                if (comp == null)
                {
                    Debug.LogError("[PainelDeInventario] O HUD_Gameplay.prefab não tem PainelDeInventario.");
                    return;
                }

                MontarLayout(comp);
                PrefabUtility.SaveAsPrefabAsset(raiz, Hud);
                Debug.Log("[PainelDeInventario] Tela montada no HUD_Gameplay.prefab (Mochila, Corpo, Detalhe).");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(raiz);
            }
        }

        /// <summary>
        /// Monta o painel na <b>cena já aberta</b> (usado pelo <c>BuildHUDCompleto</c>). Cria a
        /// raiz se faltar.
        /// </summary>
        public static bool MontarNaCenaAberta()
        {
            var comp = Object.FindAnyObjectByType<PainelDeInventario>(FindObjectsInactive.Include);
            if (comp == null)
            {
                var canvas = Object.FindAnyObjectByType<Canvas>(FindObjectsInactive.Include);
                if (canvas == null)
                {
                    Debug.LogWarning("[PainelDeInventario] Sem Canvas nesta cena — pulada.");
                    return false;
                }

                // RectTransform EXPLÍCITO: um `new GameObject(nome)` nasce com Transform comum, e
                // uma Janela esticada 0..1 dentro dele resolvia para 0×0 — o inventário não
                // aparecia e TAB parecia "só um pause" (bug de 2026-08).
                var go = new GameObject("PainelDeInventario", typeof(RectTransform));
                go.transform.SetParent(canvas.transform, false);
                Esticar(go.GetComponent<RectTransform>());
                comp = go.AddComponent<PainelDeInventario>();
            }

            MontarLayout(comp);
            EditorSceneManager.MarkSceneDirty(comp.gameObject.scene);
            return true;
        }

        /// <summary>Refaz a Janela inteira sob <paramref name="comp"/> e liga tudo.</summary>
        public static void MontarLayout(PainelDeInventario comp)
        {
            var modeloCasa = CopiarBotao(comp.transform, "Janela/Mochila/Slot_0");
            var modeloCorpo = CopiarBotao(comp.transform, "Janela/Corpo/Corpo_0");

            var antiga = comp.transform.Find("Janela");
            if (antiga != null) Object.DestroyImmediate(antiga.gameObject);

            var janela = Novo("Janela", comp.transform, typeof(Image));
            Esticar(janela);
            PaletaDaInterface.AplicarPainel(janela.GetComponent<Image>());

            Texto(janela, "Titulo", "INVENTÁRIO", 64, CorDoTitulo, TextAnchor.MiddleLeft,
                new Vector2(0.05f, 0.885f), new Vector2(0.6f, 0.955f));
            Texto(janela, "Dica", "Tab / I para fechar", 32, CorFraca, TextAnchor.MiddleRight,
                new Vector2(0.05f, 0.035f), new Vector2(0.95f, 0.085f));

            Texto(janela, "Rotulo_Mochila", "MOCHILA", 40, CorDoRotulo, TextAnchor.LowerLeft,
                new Vector2(0.05f, 0.80f), new Vector2(0.40f, 0.85f));
            var mochila = MontarMochila(janela, modeloCasa);

            Texto(janela, "Rotulo_Corpo", "CORPO", 40, CorDoRotulo, TextAnchor.LowerLeft,
                new Vector2(0.42f, 0.80f), new Vector2(0.66f, 0.85f));
            var corpo = MontarCorpo(janela, modeloCorpo);

            var detalhe = MontarDetalhe(janela);

            janela.gameObject.SetActive(false);
            Ligar(comp, janela.gameObject, mochila, corpo, detalhe);
        }

        // ── Mochila ──────────────────────────────────────────────────────────

        private static SlotRefs[] MontarMochila(RectTransform janela, ModeloDeBotao? modelo)
        {
            var area = Novo("Mochila", janela, typeof(GridLayoutGroup));
            Ancorar(area, new Vector2(0.05f, 0.14f), new Vector2(0.40f, 0.80f));

            var grade = area.GetComponent<GridLayoutGroup>();
            grade.cellSize = new Vector2(LadoDaCasa, LadoDaCasa);
            grade.spacing = new Vector2(EspacoEntreCasas, EspacoEntreCasas);
            grade.startCorner = GridLayoutGroup.Corner.UpperLeft;
            grade.startAxis = GridLayoutGroup.Axis.Horizontal;
            grade.childAlignment = TextAnchor.UpperLeft;
            grade.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grade.constraintCount = ColunasDaMochila;

            var casas = new SlotRefs[SlotsDaMochila];
            for (int i = 0; i < SlotsDaMochila; i++)
            {
                var casa = Casa(area, $"Slot_{i}", MolduraDeSlot("slot_vazio"), modelo);

                var icone = Novo("Icone", casa, typeof(Image));
                Ancorar(icone, new Vector2(0.16f, 0.16f), new Vector2(0.84f, 0.84f));
                casas[i] = Refs(casa, icone.GetComponent<Image>());
                casas[i].Quantidade = Texto(casa, "Quantidade", "", 33, CorDoTexto, TextAnchor.LowerRight,
                    new Vector2(0.45f, 0.04f), new Vector2(0.92f, 0.4f));
            }
            return casas;
        }

        // ── Corpo ────────────────────────────────────────────────────────────

        private static SlotRefs[] MontarCorpo(RectTransform janela, ModeloDeBotao? modelo)
        {
            var area = Novo("Corpo", janela, typeof(VerticalLayoutGroup));
            Ancorar(area, new Vector2(0.42f, 0.14f), new Vector2(0.66f, 0.80f));

            var lista = area.GetComponent<VerticalLayoutGroup>();
            lista.spacing = 10f;
            lista.childAlignment = TextAnchor.UpperLeft;
            lista.childControlWidth = true;
            lista.childControlHeight = true;
            lista.childForceExpandWidth = true;
            lista.childForceExpandHeight = false;

            var linhas = new SlotRefs[SlotsDoCorpo];
            for (int i = 0; i < SlotsDoCorpo; i++)
            {
                var linha = Casa(area, $"Corpo_{i}", PaletaDaInterface.Slot, modelo);
                var le = linha.gameObject.AddComponent<LayoutElement>();
                le.preferredHeight = AlturaDaLinhaDoCorpo;
                le.minHeight = AlturaDaLinhaDoCorpo;

                // Miniatura QUADRADA na ponta esquerda (70 × 70 numa linha de 90): o ícone
                // esticado na linha inteira foi o "distorce o desenho dos itens" de 2026-09.
                var icone = Novo("Icone", linha, typeof(Image));
                icone.anchorMin = new Vector2(0f, 0f);
                icone.anchorMax = new Vector2(0f, 1f);
                icone.offsetMin = new Vector2(12f, 10f);
                icone.offsetMax = new Vector2(82f, -10f);

                linhas[i] = Refs(linha, icone.GetComponent<Image>());
                linhas[i].Quantidade = Texto(linha, "Quantidade", "", 24, CorDoTexto, TextAnchor.LowerRight,
                    new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(12f, 8f), new Vector2(82f, -8f));

                var rotulo = Texto(linha, "Rotulo", "", 26, CorDoRotulo, TextAnchor.MiddleLeft,
                    Vector2.zero, Vector2.one, new Vector2(96f, 8f), new Vector2(-14f, -8f));
                rotulo.resizeTextForBestFit = true;
                rotulo.resizeTextMinSize = 16;
                rotulo.resizeTextMaxSize = 26;
                linhas[i].Rotulo = rotulo;
            }
            return linhas;
        }

        // ── Detalhe ──────────────────────────────────────────────────────────

        private static PainelDoItem MontarDetalhe(RectTransform janela)
        {
            var painel = Novo("Detalhe", janela, typeof(Image), typeof(VerticalLayoutGroup));
            Ancorar(painel, new Vector2(0.68f, 0.11f), new Vector2(0.95f, 0.85f));
            PaletaDaInterface.AplicarPainel(painel.GetComponent<Image>());
            painel.GetComponent<Image>().raycastTarget = false;

            // Padding medido na tela, não na borda 9-slice: com 32 px o título entrava debaixo
            // do canto ornado e a dica do rodapé também (captura de 2026-09-28). Os cantos do
            // painel_ornado ocupam ~52 px a 1920 × 1080; a trama lateral, ~22.
            var vl = painel.GetComponent<VerticalLayoutGroup>();
            vl.padding = new RectOffset(44, 44, 58, 56);
            vl.spacing = 12f;
            vl.childAlignment = TextAnchor.UpperLeft;
            vl.childControlWidth = true;
            vl.childControlHeight = true;
            vl.childForceExpandWidth = true;
            vl.childForceExpandHeight = false;

            var titulo = Linha(painel, "Titulo", 40, CorDoTexto);
            var subtitulo = Linha(painel, "Subtitulo", 24, CorFraca);
            var descricao = Linha(painel, "Descricao", 24, CorFraca);
            var atributos = Linha(painel, "Atributos", 28, CorDoTexto);
            var comparacao = Linha(painel, "Comparacao", 26, CorDoTexto);

            var espaco = Novo("Espaco", painel);
            espaco.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1f;

            var aviso = Linha(painel, "Aviso", 26, CorDoAviso);
            var dica = Linha(painel, "DicaDoItem", 22, CorFraca);

            var comp = painel.gameObject.AddComponent<PainelDoItem>();
            var so = new SerializedObject(comp);
            so.FindProperty("titulo").objectReferenceValue = titulo;
            so.FindProperty("subtitulo").objectReferenceValue = subtitulo;
            so.FindProperty("descricao").objectReferenceValue = descricao;
            so.FindProperty("atributos").objectReferenceValue = atributos;
            so.FindProperty("comparacao").objectReferenceValue = comparacao;
            so.FindProperty("aviso").objectReferenceValue = aviso;
            so.FindProperty("dica").objectReferenceValue = dica;
            so.ApplyModifiedPropertiesWithoutUndo();
            return comp;
        }

        private static Text Linha(RectTransform pai, string nome, int tamanho, Color cor)
        {
            var t = Texto(pai, nome, "", tamanho, cor, TextAnchor.UpperLeft, Vector2.zero, Vector2.one);
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.supportRichText = true;
            t.lineSpacing = 1.05f;
            return t;
        }

        // ── Casas ────────────────────────────────────────────────────────────

        private struct SlotRefs
        {
            public CanvasGroup Grupo;
            public Image Moldura;
            public Image Icone;
            public Text Quantidade;
            public Text Rotulo;
            public Button Botao;
        }

        private static RectTransform Casa(RectTransform pai, string nome, Sprite moldura, ModeloDeBotao? modelo)
        {
            var casa = Novo(nome, pai, typeof(CanvasGroup), typeof(Image), typeof(Button));
            var img = casa.GetComponent<Image>();
            img.sprite = moldura;
            img.type = Image.Type.Sliced;
            img.color = Color.white;   // a arte tem cor própria; tingir escureceria o ouro

            var botao = casa.GetComponent<Button>();
            botao.targetGraphic = img;
            if (modelo.HasValue)
            {
                botao.transition = modelo.Value.Transicao;
                botao.spriteState = modelo.Value.Sprites;
                botao.colors = modelo.Value.Cores;
            }
            else
            {
                // Sem modelo: troca de arte sob o cursor, que é o que o guarda
                // OsBotoesTrocamDeArteSobOCursor exige (tint escureceria a moldura).
                botao.transition = Selectable.Transition.SpriteSwap;
                botao.spriteState = new SpriteState { highlightedSprite = MolduraDeSlot("slot_cheio") };
            }
            return casa;
        }

        private static SlotRefs Refs(RectTransform casa, Image icone)
        {
            icone.raycastTarget = false;
            icone.preserveAspect = true;   // a arte não é toda quadrada (Água da Cacimba: 11 × 31)
            icone.enabled = false;
            return new SlotRefs
            {
                Grupo = casa.GetComponent<CanvasGroup>(),
                Moldura = casa.GetComponent<Image>(),
                Icone = icone,
                Botao = casa.GetComponent<Button>(),
            };
        }

        /// <summary>A configuração de um botão de casa, copiada antes de a Janela ser refeita.</summary>
        private struct ModeloDeBotao
        {
            public Selectable.Transition Transicao;
            public SpriteState Sprites;
            public ColorBlock Cores;
        }

        /// <summary>
        /// Guarda a configuração do botão de uma casa antiga (transição e sprites de realce), para
        /// a casa nova herdar o que o <c>AplicarUiDarkAges</c> já aplicou.
        /// </summary>
        private static ModeloDeBotao? CopiarBotao(Transform raiz, string caminho)
        {
            var t = raiz.Find(caminho);
            var b = t != null ? t.GetComponent<Button>() : null;
            if (b == null) return null;
            return new ModeloDeBotao { Transicao = b.transition, Sprites = b.spriteState, Cores = b.colors };
        }

        // ── Ligação ──────────────────────────────────────────────────────────

        private static void Ligar(PainelDeInventario comp, GameObject janela,
            SlotRefs[] mochila, SlotRefs[] corpo, PainelDoItem detalhe)
        {
            var so = new SerializedObject(comp);
            so.FindProperty("raizDoPainel").objectReferenceValue = janela;
            so.FindProperty("detalhe").objectReferenceValue = detalhe;

            // As artes de moldura ficam as que já estão (o AplicarUiDarkAges as escolheu); só
            // preenche o que estiver vazio.
            Preencher(so, "molduraVazia", MolduraDeSlot("slot_vazio"));
            Preencher(so, "molduraCheia", MolduraDeSlot("slot_cheio"));
            Preencher(so, "molduraCorpoVazia", PaletaDaInterface.Slot);
            Preencher(so, "molduraCorpoCheia", PaletaDaInterface.Slot);
            so.FindProperty("opacidadeVazio").floatValue = 1f;

            PreencherArray(so.FindProperty("slotsDaMochila"), mochila);
            PreencherArray(so.FindProperty("slotsDoCorpo"), corpo);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(comp);
        }

        private static void Preencher(SerializedObject so, string campo, Object valor)
        {
            var p = so.FindProperty(campo);
            if (p != null && p.objectReferenceValue == null) p.objectReferenceValue = valor;
        }

        private static void PreencherArray(SerializedProperty arr, SlotRefs[] refs)
        {
            arr.arraySize = refs.Length;
            for (int i = 0; i < refs.Length; i++)
            {
                var el = arr.GetArrayElementAtIndex(i);
                el.FindPropertyRelative("grupo").objectReferenceValue = refs[i].Grupo;
                el.FindPropertyRelative("moldura").objectReferenceValue = refs[i].Moldura;
                el.FindPropertyRelative("icone").objectReferenceValue = refs[i].Icone;
                el.FindPropertyRelative("quantidade").objectReferenceValue = refs[i].Quantidade;
                el.FindPropertyRelative("rotulo").objectReferenceValue = refs[i].Rotulo;
                el.FindPropertyRelative("botao").objectReferenceValue = refs[i].Botao;
            }
        }

        // ── Utilidades ───────────────────────────────────────────────────────

        /// <summary>
        /// Moldura fatiada da folha do Dark Ages UI, ou <c>null</c> (e aviso) se a folha não
        /// passou por <c>Tools/FavelaAmarela/Fatiar molduras de slot (Dark Ages UI)</c>.
        /// </summary>
        private static Sprite MolduraDeSlot(string nome)
        {
            const string folha = "Assets/ThirdParty/DarkAgesUI/DarkAgesUi_v1.0/32x32-Tilesheet.png";
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(folha))
                if (asset is Sprite sprite && sprite.name == nome) return sprite;

            Debug.LogWarning($"[PainelDeInventario] Moldura '{nome}' não está fatiada na folha. " +
                             "Rode 'Tools/FavelaAmarela/Fatiar molduras de slot (Dark Ages UI)'.");
            return null;
        }

        private static RectTransform Novo(string nome, Transform pai, params System.Type[] componentes)
        {
            var tipos = new System.Type[componentes.Length + 1];
            tipos[0] = typeof(RectTransform);
            componentes.CopyTo(tipos, 1);
            var go = new GameObject(nome, tipos);
            go.transform.SetParent(pai, false);
            return go.GetComponent<RectTransform>();
        }

        private static void Esticar(RectTransform rt) => Ancorar(rt, Vector2.zero, Vector2.one);

        private static void Ancorar(RectTransform rt, Vector2 min, Vector2 max)
        {
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static Text Texto(RectTransform pai, string nome, string conteudo, int tamanho, Color cor,
            TextAnchor alinhamento, Vector2 min, Vector2 max, Vector2 offsetMin = default, Vector2 offsetMax = default)
        {
            var rt = Novo(nome, pai, typeof(Text));
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;

            var texto = rt.GetComponent<Text>();
            texto.font = PaletaDaInterface.Fonte;
            texto.text = conteudo;
            texto.fontSize = tamanho;
            texto.alignment = alinhamento;
            texto.color = cor;
            texto.raycastTarget = false;
            return texto;
        }
    }
}
