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
    /// Move o <b>painel de escolha</b> (diálogo ramificado) para o HUD persistente.
    ///
    /// <para><b>O que isto conserta (2026-09-02).</b> O <c>PainelDeEscolha</c> vivia em <b>duas
    /// cenas das seis</b> do build. Quando ele falta, o <c>CassildaNPC.cs:295</c> pula a
    /// ramificação <b>em silêncio</b> — a conversa acontece pela metade e nada reclama. Qualquer
    /// NPC de escolha posto no Deserto, nos Portões ou no Castelo perderia a conversa.</para>
    ///
    /// <para>Mesmo caminho do <c>PromptDeInteracao</c>, do <c>PainelDeFicha</c> e da caixa de
    /// diálogo antes deles: mora no <c>HUD_Gameplay.prefab</c> e recebe as referências de cena
    /// por <c>Bind()</c>, do <c>GameLoopBootstrap</c>.</para>
    ///
    /// <para><b>Remove os das cenas</b> depois de criar o do HUD: dois painéis, dois
    /// <c>Instancia</c> disputando, e o jogador vendo a escolha duas vezes.</para>
    /// </summary>
    public static class MigrarEscolhaParaOHud
    {
        private const string Marcador = "[EscolhaNoHud]";
        private const string Hud = "Assets/FavelaAmarela/Resources/HUD_Gameplay.prefab";
        private const string Folha =
            "Assets/ThirdParty/DarkAgesUI/DarkAgesUi_v1.0/32x32-Tilesheet.png";

        [MenuItem("Tools/FavelaAmarela/UI: migrar o painel de escolha para o HUD")]
        public static void Executar()
        {
            var resumo = new List<string> { CriarNoHud() };
            resumo.AddRange(RetirarDasCenas());

            AssetDatabase.SaveAssets();

            string quebra = System.Environment.NewLine + "  ";
            Debug.Log($"{Marcador} Concluído:" + quebra + string.Join(quebra, resumo));
        }

        private static string CriarNoHud()
        {
            var raiz = PrefabUtility.LoadPrefabContents(Hud);

            try
            {
                // CRIAR e PADRONIZAR sao dois trabalhos. Sair aqui com "ja existe" foi
                // exatamente o que deixou o LigarBotaoDeOpcoes incapaz de curar o proprio
                // estrago -- e na primeira execucao desta ferramenta eu repeti o padrao: o
                // texto nasceu com fonte fixa e o TipografiaDeDialogoTests pegou.
                var jaExiste = raiz.GetComponentInChildren<PainelDeEscolha>(true);

                if (jaExiste != null)
                {
                    var alvo = new SerializedObject(jaExiste)
                        .FindProperty("texto").objectReferenceValue as Text;

                    if (alvo == null) return "HUD: painel existe, mas sem Text ligado";

                    PadronizarTexto(alvo);
                    PrefabUtility.SaveAsPrefabAsset(raiz, Hud, out _);

                    return "HUD: painel já existia — texto repadronizado";
                }

                var fonte = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

                var go = new GameObject("PainelDeEscolha", typeof(RectTransform));
                go.transform.SetParent(raiz.transform, worldPositionStays: false);

                var rt = (RectTransform)go.transform;
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;

                // A RAIZ é filha, nunca este objeto: o componente a liga e desliga, e desativar
                // a si mesmo derrubaria o próprio Update que lê a navegação. Mesmo cuidado do
                // PromptDeInteracao.
                var painel = new GameObject("Painel", typeof(RectTransform), typeof(Image));
                painel.transform.SetParent(go.transform, worldPositionStays: false);

                var rtp = (RectTransform)painel.transform;
                rtp.anchorMin = new Vector2(0.22f, 0.20f);
                rtp.anchorMax = new Vector2(0.78f, 0.50f);
                rtp.offsetMin = Vector2.zero;
                rtp.offsetMax = Vector2.zero;

                var img = painel.GetComponent<Image>();
                var sprite = AssetDatabase.LoadAllAssetsAtPath(Folha).OfType<Sprite>()
                    .FirstOrDefault(s => s.name == "painel_ornado");

                if (sprite != null)
                {
                    img.sprite = sprite;
                    img.type = Image.Type.Sliced;

                    // O painel é alto (30% da tela), então a moldura cabe grossa. 1,5 dá uma
                    // borda de ~48 unidades: ornamentada sem comer o texto.
                    img.pixelsPerUnitMultiplier = 1.5f;
                }

                img.color = new Color(0.05f, 0.04f, 0.02f, 0.93f);

                var texto = new GameObject("Texto", typeof(RectTransform), typeof(Text));
                texto.transform.SetParent(painel.transform, worldPositionStays: false);

                var rtt = (RectTransform)texto.transform;
                rtt.anchorMin = Vector2.zero;
                rtt.anchorMax = Vector2.one;
                rtt.offsetMin = new Vector2(64f, 48f);
                rtt.offsetMax = new Vector2(-64f, -48f);

                var t = texto.GetComponent<Text>();
                t.font = fonte;
                t.text = "";
                t.alignment = TextAnchor.UpperLeft;
                t.color = new Color(0.93f, 0.89f, 0.72f, 1f);
                t.raycastTarget = false;

                PadronizarTexto(t);

                var escolha = go.AddComponent<PainelDeEscolha>();

                var so = new SerializedObject(escolha);
                so.FindProperty("raiz").objectReferenceValue = painel;
                so.FindProperty("texto").objectReferenceValue = t;
                so.ApplyModifiedPropertiesWithoutUndo();

                painel.SetActive(false);

                PrefabUtility.SaveAsPrefabAsset(raiz, Hud, out bool gravou);

                return gravou
                    ? "HUD: painel de escolha CRIADO, escondido até haver escolha. As " +
                      "referências do jogador vêm do GameLoopBootstrap, por Bind()"
                    : "HUD: SaveAsPrefabAsset RECUSOU";
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(raiz);
            }
        }

        /// <summary>
        /// O padrão de texto de diálogo do projeto: <b>BestFit</b> entre o mínimo legível e o
        /// máximo calibrado, com quebra de linha e transbordo vertical.
        ///
        /// <para>Fonte FIXA é o que o <c>TipografiaDeDialogoTests</c> proíbe, e com razão: as
        /// opções vêm do roteiro e variam de "Onde estou?" a "Você está presa aqui?" — número
        /// fixo corta a longa ou deixa a curta minúscula. Os valores saem do
        /// <c>PadraoDeTextoDeDialogo</c>, que é a fonte única deles.</para>
        /// </summary>
        private static void PadronizarTexto(Text t)
        {
            t.resizeTextForBestFit = true;
            t.resizeTextMinSize = PadraoDeTextoDeDialogo.TamanhoMinimo;
            t.resizeTextMaxSize = PadraoDeTextoDeDialogo.TamanhoMaximo;
            t.fontSize = PadraoDeTextoDeDialogo.TamanhoMaximo;

            t.horizontalOverflow = HorizontalWrapMode.Wrap;

            // TRUNCATE, e não Overflow (2026-09-02). Com Overflow a Unity não encolhe por
            // altura e o BestFit acima nunca é acionado -- o texto vaza por cima do resto da
            // tela. O que torna Truncate seguro é a garantia de capacidade do
            // LayoutDaUiTests.ACaixaDeDialogoComportaAFalaMaisLonga: se a caixa comporta a fala
            // mais longa no piso do BestFit, o corte nunca dispara.
            t.verticalOverflow = VerticalWrapMode.Truncate;

            EditorUtility.SetDirty(t);
        }

        private static IEnumerable<string> RetirarDasCenas()
        {
            foreach (var caminho in AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Scenes" })
                         .Select(AssetDatabase.GUIDToAssetPath)
                         .OrderBy(c => c))
            {
                var cena = EditorSceneManager.OpenScene(caminho, OpenSceneMode.Single);

                var achados = cena.GetRootGameObjects()
                    .SelectMany(r => r.GetComponentsInChildren<PainelDeEscolha>(true))
                    .ToArray();

                if (achados.Length == 0) continue;

                foreach (var p in achados) Object.DestroyImmediate(p.gameObject);

                EditorSceneManager.MarkSceneDirty(cena);
                EditorSceneManager.SaveScene(cena);

                yield return $"{System.IO.Path.GetFileName(caminho)}: {achados.Length} painel(is) " +
                             "de cena removido(s) — quem mostra agora é o HUD persistente";
            }
        }
    }
}
