using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using FavelaAmarela.Inventario;
using FavelaAmarela.Runtime.UI;

namespace FavelaAmarela.EditorTools
{
    /// <summary>
    /// Ferramenta de Editor. Monta a <b>tela de inventário</b> — mochila em grade + slots do
    /// corpo em coluna — e a liga ao <see cref="PainelDeInventario"/>.
    ///
    /// <para>Até 2026-08-11 o jogo só tinha a barra de 8 posições; os slots de equipamento
    /// não tinham interface nenhuma. Esta tela abre com <b>Tab</b> ou <b>I</b>.</para>
    ///
    /// <para>Idempotente: refaz do zero a cada execução, para ajuste de layout valer sem
    /// sobrar slot velho.</para>
    /// </summary>
    public static class MontarPainelDeInventario
    {
        private static readonly string[] Cenas =
        {
            "Assets/Scenes/Deserto_Hali.unity",
            "Assets/Scenes/Playtest_RuinasPalidas.unity",
            "Assets/Scenes/Santuario_Yhtill.unity",
        };

        private const int SlotsDaMochila = MainInventory.DefaultCapacidadeSurvivalHorror; // 12
        private const int SlotsDoCorpo = 7;                                               // anatomia (com Mão Secundária)
        private const int ColunasDaMochila = 4;

        /// <summary>
        /// Proporção largura/altura de um slot de CORPO, medida na cena: 600 × 81 px num canvas
        /// de 1920 × 1080 (área de corpo 614 × 778, sete linhas). É o que converte "quero uma
        /// miniatura quadrada" em âncoras, que são relativas ao slot e portanto anisotrópicas.
        /// </summary>
        private const float ProporcaoDaLinhaDeCorpo = 7.37f;

        [MenuItem("Tools/FavelaAmarela/Montar painel de inventário (Tab)")]
        public static void Executar()
        {
            var cenaAtiva = EditorSceneManager.GetActiveScene();
            if (cenaAtiva.isDirty && !string.IsNullOrEmpty(cenaAtiva.path))
                EditorSceneManager.SaveScene(cenaAtiva);

            string cenaOriginal = cenaAtiva.path;
            int feitas = 0;

            foreach (var caminho in Cenas)
            {
                if (!System.IO.File.Exists(caminho)) continue;

                var cena = EditorSceneManager.OpenScene(caminho, OpenSceneMode.Single);
                if (MontarNaCenaAberta())
                {
                    EditorSceneManager.MarkSceneDirty(cena);
                    EditorSceneManager.SaveScene(cena);
                    feitas++;
                    Debug.Log($"[PainelDeInventario] Montado em '{cena.name}'.");
                }
            }

            if (!string.IsNullOrEmpty(cenaOriginal))
                EditorSceneManager.OpenScene(cenaOriginal, OpenSceneMode.Single);

            Debug.Log($"[PainelDeInventario] Pronto — {feitas} cena(s). Abre com Tab ou I.");
        }

        /// <summary>
        /// Monta o painel na <b>cena já aberta</b>. Público pelo mesmo motivo da
        /// <c>MontarBarraDeItens.MontarNaCenaAberta</c>: cenas montadas por outras ferramentas
        /// precisam de HUD completo, não de metade dele.
        /// </summary>
        public static bool MontarNaCenaAberta()
        {
            var canvas = Object.FindAnyObjectByType<Canvas>(FindObjectsInactive.Include);
            if (canvas == null)
            {
                Debug.LogWarning("[PainelDeInventario] Sem Canvas nesta cena — pulada.");
                return false;
            }

            var antigo = GameObject.Find("PainelDeInventario");
            if (antigo != null) Object.DestroyImmediate(antigo);

            // Raiz do controlador: fica SEMPRE ativa, senão o Update que lê a tecla não roda.
            // Quem liga/desliga é o filho "Janela".
            //
            // RectTransform EXPLÍCITO, e esta linha é o conserto de um bug que custou caro: um
            // `new GameObject(nome)` nasce com Transform comum, e um RectTransform filho de
            // Transform comum não tem retângulo de pai onde ancorar — com âncoras 0..1 a Janela
            // resolvia para 0×0 e o inventário simplesmente não aparecia. Apertar TAB parecia
            // "só um pause". Os outros montadores de UI escapam por acidente: eles criam a raiz
            // com `Image`, que exige RectTransform e faz a Unity adicioná-lo. Este era o único
            // sem Graphic, e por isso o único quebrado.
            var raiz = new GameObject("PainelDeInventario", typeof(RectTransform));
            raiz.transform.SetParent(canvas.transform, false);

            var rtRaiz = raiz.GetComponent<RectTransform>();
            rtRaiz.anchorMin = Vector2.zero;
            rtRaiz.anchorMax = Vector2.one;
            rtRaiz.offsetMin = Vector2.zero;
            rtRaiz.offsetMax = Vector2.zero;
            var comp = raiz.AddComponent<PainelDeInventario>();

            var janela = MontarJanela(raiz.transform);

            var mochila = new SlotRefs[SlotsDaMochila];
            var corpo = new SlotRefs[SlotsDoCorpo];

            var areaMochila = MontarArea(janela.transform, "Mochila", "MOCHILA",
                xMin: 0.06f, xMax: 0.56f);
            for (int i = 0; i < SlotsDaMochila; i++)
                mochila[i] = MontarSlot(areaMochila, $"Slot_{i}", i, ColunasDaMochila, SlotsDaMochila, comRotulo: false);

            var areaCorpo = MontarArea(janela.transform, "Corpo", "CORPO",
                xMin: 0.62f, xMax: 0.94f);
            for (int i = 0; i < SlotsDoCorpo; i++)
                corpo[i] = MontarSlot(areaCorpo, $"Corpo_{i}", i, 1, SlotsDoCorpo, comRotulo: true);

            Ligar(comp, janela, mochila, corpo);
            return true;
        }

        /// <summary>
        /// Carrega uma moldura fatiada da folha do Dark Ages UI.
        ///
        /// <para>Devolve <c>null</c> — e avisa — se a fatia não existir, em vez de estourar: a
        /// folha precisa ter passado por
        /// <c>Tools/FavelaAmarela/Fatiar molduras de slot (Dark Ages UI)</c> antes. O painel cai
        /// no sprite embutido e continua utilizável, só sem a arte.</para>
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

        private struct SlotRefs
        {
            public CanvasGroup Grupo;
            public Image Moldura;
            public Image Icone;
            public Text Quantidade;
            public Text Rotulo;
        }

        private static GameObject MontarJanela(Transform pai)
        {
            var janela = new GameObject("Janela", typeof(Image));
            janela.transform.SetParent(pai, false);

            var rt = janela.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            // Véu escuro sobre o mundo: a tela pausa o jogo, e o escurecido comunica isso.
            var fundo = janela.GetComponent<Image>();
            fundo.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            fundo.type = Image.Type.Sliced;
            fundo.color = new Color(0.02f, 0.02f, 0.015f, 0.88f);

            MontarTexto(janela.transform, "Titulo", "INVENTÁRIO",
                new Vector2(0.06f, 0.88f), new Vector2(0.94f, 0.96f), 60, TextAnchor.MiddleLeft,
                new Color(0.92f, 0.86f, 0.55f, 0.9f));

            MontarTexto(janela.transform, "Dica", "Tab / I para fechar",
                new Vector2(0.06f, 0.03f), new Vector2(0.94f, 0.09f), 36, TextAnchor.MiddleRight,
                new Color(0.85f, 0.82f, 0.65f, 0.45f));

            janela.SetActive(false);
            return janela;
        }

        private static Transform MontarArea(Transform pai, string nome, string titulo, float xMin, float xMax)
        {
            var area = new GameObject(nome);
            area.transform.SetParent(pai, false);

            var rt = area.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(xMin, 0.12f);
            rt.anchorMax = new Vector2(xMax, 0.84f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            MontarTexto(area.transform, "Rotulo", titulo,
                new Vector2(0f, 0.93f), new Vector2(1f, 1f), 39, TextAnchor.MiddleLeft,
                new Color(0.85f, 0.80f, 0.60f, 0.7f));

            return area.transform;
        }

        private static SlotRefs MontarSlot(Transform pai, string nome, int indice, int colunas,
            int total, bool comRotulo)
        {
            int linhas = Mathf.CeilToInt(total / (float)colunas);
            int coluna = indice % colunas;
            int linha = indice / colunas;

            var go = new GameObject(nome, typeof(CanvasGroup), typeof(Image));
            go.transform.SetParent(pai, false);

            const float folga = 0.012f;
            float larguraCel = 1f / colunas;
            float alturaCel = 0.9f / linhas;

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(coluna * larguraCel + folga,
                                       0.9f - (linha + 1) * alturaCel + folga);
            rt.anchorMax = new Vector2((coluna + 1) * larguraCel - folga,
                                       0.9f - linha * alturaCel - folga);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var moldura = go.GetComponent<Image>();

            // Arte do Dark Ages UI em vez do sprite embutido tingido a 16% de alpha, que era o
            // que fazia a grade parecer "caixas chapadas". Quem troca entre vazio e cheio em
            // runtime é o PainelDeInventario; aqui só entra o estado inicial.
            var vazia = MolduraDeSlot("slot_vazio");
            moldura.sprite = vazia != null
                ? vazia
                : AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

            moldura.type = Image.Type.Sliced;
            moldura.color = Color.white;   // arte com cor própria; tingir escureceria o ouro
            moldura.raycastTarget = false;

            var refs = new SlotRefs
            {
                Grupo = go.GetComponent<CanvasGroup>(),
                Moldura = moldura,
            };

            var goIcone = new GameObject("Icone", typeof(Image));
            goIcone.transform.SetParent(go.transform, false);
            var rtIcone = goIcone.GetComponent<RectTransform>();

            if (comRotulo)
            {
                // Slot de CORPO. Ele é uma linha de lista, não um quadrado: 600 × 81 px, ou
                // 7,37 : 1. Isso está certo para uma linha -- o errado era o ícone preencher a
                // linha inteira. Uma peça de armadura de 32 × 32 acabava esticada 12,7× na
                // horizontal contra 1,7× na vertical, e foi isso que o Vini viu como
                // "distorce o desenho dos itens".
                //
                // Agora o ícone é miniatura QUADRADA na ponta esquerda: 0,8 da altura da linha
                // em cima e embaixo, e a mesma medida em largura, convertida pela proporção da
                // linha. O rótulo ocupa o resto.
                const float alturaDaMiniatura = 0.8f;
                float larguraDaMiniatura = alturaDaMiniatura / ProporcaoDaLinhaDeCorpo;

                rtIcone.anchorMin = new Vector2(0.02f, (1f - alturaDaMiniatura) / 2f);
                rtIcone.anchorMax = new Vector2(0.02f + larguraDaMiniatura,
                                                1f - (1f - alturaDaMiniatura) / 2f);
            }
            else
            {
                // Slot de MOCHILA: 217 × 215 px, praticamente quadrado. Aqui a proporção do
                // slot nunca foi o problema.
                rtIcone.anchorMin = new Vector2(0.16f, 0.16f);
                rtIcone.anchorMax = new Vector2(0.84f, 0.84f);
            }

            rtIcone.offsetMin = Vector2.zero;
            rtIcone.offsetMax = Vector2.zero;
            refs.Icone = goIcone.GetComponent<Image>();
            refs.Icone.raycastTarget = false;
            refs.Icone.enabled = false;

            // Sem isto o ícone deforma para encher o retângulo, mesmo num slot quadrado: a arte
            // não é toda quadrada (a Água da Cacimba tem 11 × 31, a Raiz de Yhtill 51 × 35).
            // A barra de ações já fazia isso desde sempre; o painel é que tinha ficado de fora.
            refs.Icone.preserveAspect = true;

            refs.Quantidade = MontarTexto(go.transform, "Quantidade", "",
                new Vector2(0.45f, 0.02f), new Vector2(0.96f, 0.4f), 33, TextAnchor.LowerRight,
                new Color(0.95f, 0.92f, 0.75f, 0.9f));

            if (comRotulo)
            {
                // Ao lado da miniatura, não por cima dela. Antes o rótulo cobria o terço
                // superior do slot inteiro -- inclusive o ícone.
                const float alturaDaMiniatura = 0.8f;
                float larguraDaMiniatura = alturaDaMiniatura / ProporcaoDaLinhaDeCorpo;

                refs.Rotulo = MontarTexto(go.transform, "Rotulo", "",
                    new Vector2(0.04f + larguraDaMiniatura, 0.1f), new Vector2(0.97f, 0.9f),
                    30, TextAnchor.MiddleLeft,
                    new Color(0.85f, 0.82f, 0.62f, 0.75f));
            }

            return refs;
        }

        private static Text MontarTexto(Transform pai, string nome, string conteudo,
            Vector2 ancoraMin, Vector2 ancoraMax, int tamanho, TextAnchor alinhamento, Color cor)
        {
            var go = new GameObject(nome, typeof(Text));
            go.transform.SetParent(pai, false);

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = ancoraMin;
            rt.anchorMax = ancoraMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var texto = go.GetComponent<Text>();
            texto.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            texto.text = conteudo;
            texto.fontSize = tamanho;
            texto.alignment = alinhamento;
            texto.color = cor;
            texto.raycastTarget = false;

            return texto;
        }

        private static void Ligar(PainelDeInventario comp, GameObject janela,
            SlotRefs[] mochila, SlotRefs[] corpo)
        {
            var so = new SerializedObject(comp);
            so.FindProperty("raizDoPainel").objectReferenceValue = janela;

            so.FindProperty("molduraVazia").objectReferenceValue = MolduraDeSlot("slot_vazio");
            so.FindProperty("molduraCheia").objectReferenceValue = MolduraDeSlot("slot_cheio");

            // 1, e não o 0,25 antigo: com duas molduras distintas quem comunica o estado é a
            // arte. Manter o desbotamento apagaria a própria moldura da casa vazia — que é
            // justamente a que precisa ser vista para a grade fazer sentido.
            so.FindProperty("opacidadeVazio").floatValue = 1f;

            PreencherArray(so.FindProperty("slotsDaMochila"), mochila);
            PreencherArray(so.FindProperty("slotsDoCorpo"), corpo);

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(comp);
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
            }
        }
    }
}
