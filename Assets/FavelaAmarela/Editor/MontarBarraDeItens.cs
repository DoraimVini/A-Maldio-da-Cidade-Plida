using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using FavelaAmarela.Runtime.UI;

namespace FavelaAmarela.EditorTools
{
    /// <summary>
    /// Ferramenta de Editor. Monta a <b>barra de itens</b> — faixa retangular de 8 slots no
    /// rodapé, acionada pelas teclas 1–8 — e liga no <c>HUDController</c>.
    ///
    /// <para><b>Pedido do Vini:</b> retangular, mais transparente, ocupando menos tela, e
    /// com utilidade de verdade (as teclas). A barra antiga era um painel informativo que
    /// só mostrava a arma e a habilidade.</para>
    ///
    /// <para>Idempotente: refaz a faixa do zero a cada execução, para os ajustes de tamanho
    /// e opacidade valerem sem sobrar slot velho.</para>
    /// </summary>
    public static class MontarBarraDeItens
    {
        private static readonly string[] Cenas =
        {
            "Assets/Scenes/Deserto_Hali.unity",
            "Assets/Scenes/Playtest_RuinasPalidas.unity",
            "Assets/Scenes/Santuario_Yhtill.unity",
        };

        private const int Slots = 8;

        // Faixa baixa e estreita: ~7% da altura da tela, centrada. O jogo é de tensão
        // visual — HUD grande rouba o pouco que a tempestade deixa ver.
        private const float AlturaDaFaixa = 0.075f;
        private const float LarguraDaFaixa = 0.42f;
        private const float MargemInferior = 0.02f;

        private const float OpacidadeDoFundo = 0.35f;   // bem translúcido, como pedido

        [MenuItem("Tools/FavelaAmarela/Montar barra de itens 1-8")]
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
                    Debug.Log($"[BarraDeItens] Montada em '{cena.name}'.");
                }
            }

            if (!string.IsNullOrEmpty(cenaOriginal))
                EditorSceneManager.OpenScene(cenaOriginal, OpenSceneMode.Single);

            Debug.Log($"[BarraDeItens] Pronto — {feitas} cena(s).");
        }

        /// <summary>
        /// Monta a barra na <b>cena já aberta</b>. Público para que outras ferramentas de
        /// montagem de cena (como a <c>MontarArenaDeTestes</c>) produzam um HUD completo em vez
        /// de meio HUD — até 2026-08-13 a Arena não tinha barra de itens nenhuma, porque esta
        /// ferramenta só percorria a lista fixa de cenas de jogo.
        /// </summary>
        public static bool MontarNaCenaAberta()
        {
            var hud = Object.FindAnyObjectByType<HUDController>(FindObjectsInactive.Include);
            if (hud == null)
            {
                Debug.LogWarning("[BarraDeItens] Sem HUDController nesta cena — pulada.");
                return false;
            }

            var canvas = hud.GetComponentInParent<Canvas>()
                         ?? Object.FindAnyObjectByType<Canvas>(FindObjectsInactive.Include);
            if (canvas == null)
            {
                Debug.LogWarning("[BarraDeItens] Sem Canvas nesta cena — pulada.");
                return false;
            }

            // Refaz do zero: garante que mudanças de tamanho/opacidade valham de verdade.
            var antiga = GameObject.Find("BarraDeItens");
            if (antiga != null) Object.DestroyImmediate(antiga);

            var faixa = new GameObject("BarraDeItens", typeof(CanvasGroup), typeof(Image));
            faixa.transform.SetParent(canvas.transform, false);

            var rt = faixa.GetComponent<RectTransform>();
            float meiaLargura = LarguraDaFaixa / 2f;
            rt.anchorMin = new Vector2(0.5f - meiaLargura, MargemInferior);
            rt.anchorMax = new Vector2(0.5f + meiaLargura, MargemInferior + AlturaDaFaixa);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var fundo = faixa.GetComponent<Image>();
            fundo.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            fundo.type = Image.Type.Sliced;              // retangular, sem cantos redondos falsos
            fundo.color = new Color(0.04f, 0.03f, 0.02f, OpacidadeDoFundo);
            fundo.raycastTarget = false;                 // HUD não intercepta clique

            var comp = faixa.AddComponent<BarraDeItens>();
            var slots = new (CanvasGroup grupo, Image icone, Text numero, Text qtd)[Slots];

            for (int i = 0; i < Slots; i++)
                slots[i] = MontarSlot(faixa.transform, i);

            // Preenche o array serializado de slots.
            var so = new SerializedObject(comp);
            var arr = so.FindProperty("slots");
            arr.arraySize = Slots;
            for (int i = 0; i < Slots; i++)
            {
                var el = arr.GetArrayElementAtIndex(i);
                el.FindPropertyRelative("grupo").objectReferenceValue = slots[i].grupo;
                el.FindPropertyRelative("icone").objectReferenceValue = slots[i].icone;
                el.FindPropertyRelative("quantidade").objectReferenceValue = slots[i].qtd;
            }
            so.ApplyModifiedPropertiesWithoutUndo();

            // Liga no HUDController.
            var soHud = new SerializedObject(hud);
            soHud.FindProperty("barraDeItens").objectReferenceValue = comp;
            soHud.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(hud);

            return true;
        }

        private static (CanvasGroup, Image, Text, Text) MontarSlot(Transform pai, int indice)
        {
            var go = new GameObject($"Slot_{indice + 1}", typeof(CanvasGroup), typeof(Image));
            go.transform.SetParent(pai, false);

            // Divide a faixa em 8 colunas iguais, com respiro entre elas.
            const float folga = 0.008f;
            float largura = 1f / 8f;
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(indice * largura + folga, 0.12f);
            rt.anchorMax = new Vector2((indice + 1) * largura - folga, 0.88f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var moldura = go.GetComponent<Image>();
            moldura.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            moldura.type = Image.Type.Sliced;
            moldura.color = new Color(0.85f, 0.80f, 0.60f, 0.18f);   // moldura discreta
            moldura.raycastTarget = false;

            var fonte = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // Número da tecla, no canto — é o que ensina o atalho sem tutorial.
            var goNum = new GameObject("Tecla", typeof(Text));
            goNum.transform.SetParent(go.transform, false);
            var rtNum = goNum.GetComponent<RectTransform>();
            rtNum.anchorMin = new Vector2(0.04f, 0.02f);
            rtNum.anchorMax = new Vector2(0.5f, 0.45f);
            rtNum.offsetMin = Vector2.zero;
            rtNum.offsetMax = Vector2.zero;
            var num = goNum.GetComponent<Text>();
            num.font = fonte;
            num.text = (indice + 1).ToString();
            num.fontSize = 12;
            num.alignment = TextAnchor.LowerLeft;
            num.color = new Color(0.9f, 0.86f, 0.65f, 0.55f);
            num.raycastTarget = false;

            // Ícone do item.
            var goIcone = new GameObject("Icone", typeof(Image));
            goIcone.transform.SetParent(go.transform, false);
            var rtIcone = goIcone.GetComponent<RectTransform>();
            rtIcone.anchorMin = new Vector2(0.15f, 0.15f);
            rtIcone.anchorMax = new Vector2(0.85f, 0.85f);
            rtIcone.offsetMin = Vector2.zero;
            rtIcone.offsetMax = Vector2.zero;
            var icone = goIcone.GetComponent<Image>();
            icone.preserveAspect = true;
            icone.raycastTarget = false;
            icone.enabled = false;   // ligado só quando o slot tem item

            // Quantidade, no canto oposto ao número da tecla.
            var goQtd = new GameObject("Quantidade", typeof(Text));
            goQtd.transform.SetParent(go.transform, false);
            var rtQtd = goQtd.GetComponent<RectTransform>();
            rtQtd.anchorMin = new Vector2(0.5f, 0.02f);
            rtQtd.anchorMax = new Vector2(0.96f, 0.45f);
            rtQtd.offsetMin = Vector2.zero;
            rtQtd.offsetMax = Vector2.zero;
            var qtd = goQtd.GetComponent<Text>();
            qtd.font = fonte;
            qtd.fontSize = 13;
            qtd.alignment = TextAnchor.LowerRight;
            qtd.color = new Color(0.95f, 0.92f, 0.78f, 0.95f);
            qtd.raycastTarget = false;
            qtd.enabled = false;

            return (go.GetComponent<CanvasGroup>(), icone, num, qtd);
        }
    }
}
