using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using FavelaAmarela.Runtime.UI;

namespace FavelaAmarela.EditorTools
{
    /// <summary>
    /// Ferramenta de Editor. Instancia o prefab do HUD no Deserto de Hali.
    ///
    /// <para><b>Problema que resolve:</b> o Deserto <b>não tinha HUD nenhum</b> — nem
    /// <c>HUDController</c>, nem barras. O <c>GameManager</c> reclamava no bootstrap
    /// ("Nenhum HUDController na cena") e o jogador atravessava o overworld sem ver
    /// Resiliência nem Vitalidade. A Tumba já tinha; o Deserto ficou para trás.</para>
    ///
    /// <para>Usa o mesmo prefab da Tumba (<c>HUD_ResilienciaBar</c>) em vez de montar um
    /// HUD novo: dois HUDs montados à mão divergiriam com o tempo.</para>
    /// </summary>
    public static class MontarHUDNoDeserto
    {
        private const string CenaDeserto = "Assets/Scenes/Deserto_Hali.unity";
        private const string PrefabHUD = "Assets/FavelaAmarela/Art/UI/HUD_ResilienciaBar.prefab";

        [MenuItem("Tools/FavelaAmarela/Montar HUD no Deserto")]
        public static void Executar()
        {
            // Salva sem perguntar. `SaveCurrentModifiedScenesIfUserWantsTo` abre um
            // diálogo MODAL, e uma ferramenta disparada pela ponte MCP trava a Unity
            // inteira esperando um clique que ninguém vê.
            var cenaAtiva = EditorSceneManager.GetActiveScene();
            if (cenaAtiva.isDirty && !string.IsNullOrEmpty(cenaAtiva.path))
                EditorSceneManager.SaveScene(cenaAtiva);

            string cenaOriginal = cenaAtiva.path;
            var cena = EditorSceneManager.OpenScene(CenaDeserto, OpenSceneMode.Single);

            if (Object.FindAnyObjectByType<HUDController>(FindObjectsInactive.Include) != null)
            {
                Debug.Log("[HUD Deserto] Já existe um HUDController — nada a fazer.");
                RestaurarCena(cenaOriginal);
                return;
            }

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabHUD);
            if (prefab == null)
            {
                Debug.LogError($"[HUD Deserto] Prefab não encontrado em '{PrefabHUD}'.");
                RestaurarCena(cenaOriginal);
                return;
            }

            // InstantiatePrefab (não Instantiate): mantém o vínculo, então melhorias no HUD
            // chegam às duas cenas sozinhas.
            var instancia = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            Undo.RegisterCreatedObjectUndo(instancia, "Instanciar HUD no Deserto");
            instancia.name = "HUD";

            EditorSceneManager.MarkSceneDirty(cena);
            EditorSceneManager.SaveScene(cena);

            Debug.Log("[HUD Deserto] HUD instanciado — o GameManager para de reclamar e as " +
                      "barras passam a funcionar no overworld.", instancia);

            RestaurarCena(cenaOriginal);
        }

        private static void RestaurarCena(string caminho)
        {
            if (!string.IsNullOrEmpty(caminho) && caminho != CenaDeserto)
                EditorSceneManager.OpenScene(caminho, OpenSceneMode.Single);
        }
    }
}
