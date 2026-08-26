using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using FavelaAmarela.Runtime.UI;

namespace FavelaAmarela.EditorTools
{
    /// <summary>
    /// Gera <c>Resources/HUD_Gameplay.prefab</c> — o HUD que passa a nascer <b>uma vez</b> e
    /// sobreviver às trocas de cena, em vez de ser remontado em cada uma.
    ///
    /// <para><b>Por que (Bloco 6 do plano da build).</b> O modo de falha dominante deste projeto
    /// é "N lugares saíram de sincronia": <b>oito</b> listas de cenas escritas à mão já
    /// envelheceram aqui, três delas descobertas só nesta semana. Enquanto o HUD for montado por
    /// cena, ele é mais uma dessas listas — e instância por cena ainda aceita <i>override</i>,
    /// então um ajuste feito numa cena diverge das outras quatro em silêncio.</para>
    ///
    /// <para><b>O padrão não é novo.</b> <c>InventoryManager</c>, <c>GerenciadorDeSave</c> e
    /// <c>ProgressionBridge</c> já fazem exatamente isto neste projeto:
    /// <c>[RuntimeInitializeOnLoadMethod(BeforeSceneLoad)]</c> → <c>Resources.Load</c> →
    /// <c>DontDestroyOnLoad</c> + guarda de singleton. O <c>CLAUDE.md</c> §2 manda seguir
    /// exemplo canônico em vez de inventar padrão; o HUD passa a ser o quarto.</para>
    ///
    /// <para><b>Como o prefab é feito:</b> numa cena vazia, roda o <c>BuildHUDCompleto</c> que
    /// já existe e salva o Canvas resultante. Construir de novo à mão duplicaria a lógica de
    /// layout — e seria uma segunda fonte da verdade, exatamente o que este Bloco combate.</para>
    ///
    /// <para><b>Inclui as telas de fluxo</b> (pause e Colapso). Elas eram montadas por cena e
    /// ficavam penduradas no objeto do HUD — com o HUD virando persistente, a cópia da cena se
    /// autodestruiria pela guarda de singleton e levaria as duas junto, no meio do jogo. A
    /// ligação delas com <c>GameStatePresenter</c> e <c>PlayerDeathController</c> deixou de ser
    /// serializada e passou a ser feita em runtime pelo <c>GameLoopBootstrap</c>.</para>
    ///
    /// <para><b>Fica de fora, de propósito:</b> <c>Veu_Tempestade</c> — é o véu da tempestade do
    /// Deserto, cenário e não HUD.</para>
    /// </summary>
    public static class ExtrairHudPersistente
    {
        /// <summary>
        /// Precisa estar sob <c>Resources/</c>: é de lá que o bootstrap carrega, pelo mesmo
        /// caminho que o <c>InventoryManager</c> usa.
        /// </summary>
        public const string CaminhoDoPrefab =
            "Assets/FavelaAmarela/Resources/HUD_Gameplay.prefab";

        /// <summary>Nome sem extensão, como <c>Resources.Load</c> espera.</summary>
        public const string NomeEmResources = "HUD_Gameplay";

        [MenuItem("Tools/FavelaAmarela/HUD: extrair para prefab persistente")]
        public static void Executar()
        {
            string cenaOriginal = EditorSceneManager.GetActiveScene().path;

            var cena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Reusa o montador existente: ele cria o Canvas, as sete views, o painel de
            // inventário, a ficha e a caixa de diálogo, já ligados no HUDController.
            BuildHUDCompleto.Build();

            var hud = Object.FindAnyObjectByType<HUDController>(FindObjectsInactive.Include);

            if (hud == null)
            {
                Debug.LogError("[HudPersistente] BuildHUDCompleto não deixou HUDController na " +
                               "cena — nada a extrair.");
                return;
            }

            var raiz = hud.gameObject;

            // As telas de fluxo entram no MESMO prefab. Antes elas eram montadas por cena e
            // ficavam penduradas no objeto do HUD -- o que significava que, com o HUD virando
            // persistente, a copia da cena se autodestruiria pela guarda de singleton e levaria
            // pause e Colapso junto, no meio do jogo, sem nada no console.
            //
            // A ligacao com GameStatePresenter e PlayerDeathController NAO e serializada aqui:
            // ela e feita em runtime pelo GameLoopBootstrap, porque uma referencia gravada
            // apontaria para um objeto que vive fora da cena.
            var (pause, colapso) = MontarTelasDeFluxo.MontarNoCanvas(raiz.transform);
            hud.DefinirTelasDeFluxo(pause, colapso);
            EditorUtility.SetDirty(hud);

            if (raiz.GetComponent<Canvas>() == null)
            {
                Debug.LogError($"[HudPersistente] '{raiz.name}' tem HUDController mas não tem " +
                               "Canvas. O prefab sairia sem raiz de UI e não renderizaria nada.");
                return;
            }

            var pasta = Path.GetDirectoryName(CaminhoDoPrefab);
            if (!Directory.Exists(pasta)) Directory.CreateDirectory(pasta);

            // O montador pode ter deixado a raiz como instância do HUD_ResilienciaBar; sem
            // desempacotar, o prefab novo nasceria como variante e continuaria acorrentado ao
            // antigo.
            if (PrefabUtility.IsPartOfAnyPrefab(raiz))
                PrefabUtility.UnpackPrefabInstance(raiz, PrefabUnpackMode.Completely,
                                                   InteractionMode.AutomatedAction);

            raiz.name = NomeEmResources;

            var salvo = PrefabUtility.SaveAsPrefabAsset(raiz, CaminhoDoPrefab, out bool ok);

            if (!ok || salvo == null)
            {
                Debug.LogError($"[HudPersistente] SaveAsPrefabAsset recusou '{CaminhoDoPrefab}'.");
                return;
            }

            AssetDatabase.Refresh();

            // Confere no DISCO, não no retorno da API.
            if (!File.Exists(CaminhoDoPrefab))
            {
                Debug.LogError($"[HudPersistente] '{CaminhoDoPrefab}' não existe depois de salvar.");
                return;
            }

            int views = ContarViews(salvo);

            var hudSalvo = salvo.GetComponent<HUDController>();
            bool comTelas = hudSalvo != null && hudSalvo.TelaPause != null
                                             && hudSalvo.SequenciaColapso != null;

            Debug.Log($"[HudPersistente] '{CaminhoDoPrefab}' criado com {views} view(s) " +
                      $"ligadas e telas de fluxo {(comTelas ? "incluídas" : "AUSENTES")}. " +
                      "A partir daqui o HUD nasce uma vez e sobrevive à troca de cena.");

            if (!string.IsNullOrEmpty(cenaOriginal))
                EditorSceneManager.OpenScene(cenaOriginal, OpenSceneMode.Single);
        }

        /// <summary>Quantas das sete views do <c>HUDController</c> ficaram ligadas no prefab.</summary>
        private static int ContarViews(GameObject prefab)
        {
            var hud = prefab.GetComponent<HUDController>();
            if (hud == null) return 0;

            var so = new SerializedObject(hud);
            string[] campos =
            {
                "resilienciaBar", "vitalidadeBar", "vigorBar",
                "barraDeAcoes", "barraDeItens", "barraDeArtefatos", "companheiroBar",
            };

            int ligadas = 0;
            foreach (var campo in campos)
            {
                var p = so.FindProperty(campo);
                if (p != null && p.objectReferenceValue != null) ligadas++;
                else Debug.LogWarning($"[HudPersistente] View '{campo}' não ficou ligada no prefab.");
            }

            return ligadas;
        }
    }
}
