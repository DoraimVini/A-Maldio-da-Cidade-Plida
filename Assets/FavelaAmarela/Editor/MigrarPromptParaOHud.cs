using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using FavelaAmarela.Runtime.UI;

namespace FavelaAmarela.EditorTools
{
    /// <summary>
    /// Move o <b>prompt de interação</b> ("E — Abrir o baú") para o HUD persistente.
    ///
    /// <para><b>O que isto conserta (2026-09-02).</b> O <c>PromptDeInteracao</c> existia em
    /// <b>uma cena das seis</b> do build — só no <c>Playtest_RuinasPalidas</c>. Nas outras cinco
    /// o jogador <b>nunca via "E — ..."</b> em nada: nem no baú da Tumba, nem nos consumíveis do
    /// Deserto, nem na Cassilda, nem no Baú de Yhtill que eu mesmo pus no Santuário na sessão
    /// passada. O objeto era interagível e não anunciava isso.</para>
    ///
    /// <para><b>O caminho é o mesmo que a caixa de diálogo já percorreu</b> em 2026-08-22, e o
    /// mesmo que a ficha percorreu depois: viver no <c>HUD_Gameplay.prefab</c> e receber a
    /// referência de cena por <c>Bind()</c>, do <c>GameLoopBootstrap</c> — porque um
    /// prefab-asset não pode referenciar objeto de cena.</para>
    ///
    /// <para><b>Remove o da cena</b> depois de criar o do HUD: dois prompts inscritos no mesmo
    /// detector desenhariam a mesma frase duas vezes.</para>
    /// </summary>
    public static class MigrarPromptParaOHud
    {
        private const string Marcador = "[PromptNoHud]";
        private const string Hud = "Assets/FavelaAmarela/Resources/HUD_Gameplay.prefab";
        private const string Folha =
            "Assets/ThirdParty/DarkAgesUI/DarkAgesUi_v1.0/32x32-Tilesheet.png";

        private const string NomeDoPrompt = "PromptDeInteracao";

        [MenuItem("Tools/FavelaAmarela/UI: migrar o prompt de interação para o HUD")]
        public static void Executar()
        {
            var resumo = new List<string> { CriarNoHud() };
            resumo.AddRange(RetirarDasCenas());

            AssetDatabase.SaveAssets();

            string quebra = System.Environment.NewLine + "  ";
            Debug.Log($"{Marcador} Concluído:" + quebra + string.Join(quebra, resumo));
        }

        // ── No HUD ────────────────────────────────────────────────────────────

        private static string CriarNoHud()
        {
            var raiz = PrefabUtility.LoadPrefabContents(Hud);

            try
            {
                var existente = raiz.GetComponentInChildren<PromptDeInteracao>(true);
                if (existente != null) return "HUD: já tem prompt";

                var fonte = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

                // Logo ACIMA da barra de itens (que vai de 0,02 a 0,095): o prompt fala do que
                // está sob a mira, e o olho do jogador já mora nessa faixa de baixo.
                var go = new GameObject(NomeDoPrompt, typeof(RectTransform));
                go.transform.SetParent(raiz.transform, worldPositionStays: false);

                var rt = (RectTransform)go.transform;
                rt.anchorMin = new Vector2(0.28f, 0.115f);
                rt.anchorMax = new Vector2(0.72f, 0.185f);
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;

                // O PAINEL é filho, nunca este objeto: o componente desativa a raiz quando não
                // há alvo, e desativar a si mesmo derrubaria o OnDisable, desinscreveria do
                // evento e o prompt nunca mais voltaria. O próprio script guarda contra isso.
                var painel = new GameObject("Painel", typeof(RectTransform), typeof(Image));
                painel.transform.SetParent(go.transform, worldPositionStays: false);

                var rtp = (RectTransform)painel.transform;
                rtp.anchorMin = Vector2.zero;
                rtp.anchorMax = Vector2.one;
                rtp.offsetMin = Vector2.zero;
                rtp.offsetMax = Vector2.zero;

                var img = painel.GetComponent<Image>();
                var sprite = AssetDatabase.LoadAllAssetsAtPath(Folha).OfType<Sprite>()
                    .FirstOrDefault(s => s.name == "painel_ornado");

                if (sprite != null)
                {
                    img.sprite = sprite;
                    img.type = Image.Type.Sliced;

                    // 3,125 = densidade 1:1. A caixa é baixa, e com multiplicador 1 as bordas
                    // de 71,9 unidades se atravessariam -- foi o que amassou a barra de itens.
                    img.pixelsPerUnitMultiplier = 3.125f;
                }

                img.color = new Color(0.05f, 0.04f, 0.02f, 0.85f);
                img.raycastTarget = false;

                var texto = new GameObject("Texto", typeof(RectTransform), typeof(Text));
                texto.transform.SetParent(painel.transform, worldPositionStays: false);

                var rtt = (RectTransform)texto.transform;
                rtt.anchorMin = Vector2.zero;
                rtt.anchorMax = Vector2.one;
                rtt.offsetMin = new Vector2(24f, 8f);
                rtt.offsetMax = new Vector2(-24f, -8f);

                var t = texto.GetComponent<Text>();
                t.font = fonte;
                t.text = "E — ";
                t.fontSize = 32;
                t.alignment = TextAnchor.MiddleCenter;
                t.color = new Color(0.93f, 0.89f, 0.72f, 1f);
                t.raycastTarget = false;

                // BestFit: "E — Falar com a rainha de Yhtill" é bem mais largo que "E — Abrir o
                // baú", e o rótulo vem do IInteragivel, não daqui. Piso no mínimo legível.
                t.resizeTextForBestFit = true;
                t.resizeTextMinSize = 24;
                t.resizeTextMaxSize = 32;

                var prompt = go.AddComponent<PromptDeInteracao>();

                var so = new SerializedObject(prompt);
                so.FindProperty("raiz").objectReferenceValue = painel;
                so.FindProperty("label").objectReferenceValue = t;
                so.FindProperty("tecla").stringValue = "E";
                so.ApplyModifiedPropertiesWithoutUndo();

                // Nasce escondido: sem alvo sob a mira não há o que anunciar.
                painel.SetActive(false);

                PrefabUtility.SaveAsPrefabAsset(raiz, Hud, out bool gravou);

                return gravou
                    ? "HUD: prompt CRIADO — painel + texto, escondido até haver alvo. O detector " +
                      "vem do GameLoopBootstrap, por Bind()"
                    : "HUD: SaveAsPrefabAsset RECUSOU";
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(raiz);
            }
        }

        // ── Fora das cenas ────────────────────────────────────────────────────

        private static IEnumerable<string> RetirarDasCenas()
        {
            foreach (var caminho in AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Scenes" })
                         .Select(AssetDatabase.GUIDToAssetPath)
                         .OrderBy(c => c))
            {
                var cena = EditorSceneManager.OpenScene(caminho, OpenSceneMode.Single);

                var achados = cena.GetRootGameObjects()
                    .SelectMany(r => r.GetComponentsInChildren<PromptDeInteracao>(true))
                    .ToArray();

                if (achados.Length == 0) continue;

                foreach (var p in achados) Object.DestroyImmediate(p.gameObject);

                EditorSceneManager.MarkSceneDirty(cena);
                EditorSceneManager.SaveScene(cena);

                yield return $"{System.IO.Path.GetFileName(caminho)}: {achados.Length} prompt(s) " +
                             "de cena removido(s) — quem anuncia agora é o HUD persistente, e " +
                             "dois inscritos no mesmo detector escreveriam a frase duas vezes";
            }
        }
    }
}
