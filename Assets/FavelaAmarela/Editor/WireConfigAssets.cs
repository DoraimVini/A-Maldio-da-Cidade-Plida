using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace FavelaAmarela.EditorTools
{
    /// <summary>
    /// Utilitário de Editor (uso único do Slice 3): atribui os quatro assets de
    /// <c>Config</c> aos bridges do Player na cena ativa via <see cref="SerializedObject"/>.
    /// Existe porque o MCP <c>update_component</c> não resolve referências de asset para
    /// campos tipados de <see cref="ScriptableObject"/> — só o tipo exato <c>UnityEngine.Object</c>.
    /// O casamento é por nome de tipo do componente, então este script não depende da
    /// assembly runtime em tempo de compilação.
    /// </summary>
    public static class WireConfigAssets
    {
        [MenuItem("Tools/FavelaAmarela/Wire Config Assets")]
        public static void Wire()
        {
            var player = GameObject.Find("Player_Damiao");
            if (player == null)
            {
                Debug.LogError("[WireConfigAssets] 'Player_Damiao' não encontrado na cena ativa.");
                return;
            }

            var ok = true;
            ok &= AssignField(player, "PlayerMovement", "locomocaoConfig", "Assets/FavelaAmarela/Config/LocomocaoConfig.asset");
            ok &= AssignField(player, "EsquivaBridge", "config", "Assets/FavelaAmarela/Config/EsquivaConfig.asset");
            // (MaoFisicaBridge não tem mais 'config' — a Barra Enferrujada foi descartada;
            //  a arma é equipada pelo baú da Tumba em runtime, não por um asset de config.)

            if (!ok)
            {
                Debug.LogError("[WireConfigAssets] Uma ou mais atribuições falharam; cena NÃO foi salva.");
                return;
            }

            EditorSceneManager.MarkSceneDirty(player.scene);
            EditorSceneManager.SaveScene(player.scene);
            Debug.Log("[WireConfigAssets] 4 configs atribuídos e cena salva com sucesso.");
        }

        private static bool AssignField(GameObject go, string componentTypeName, string fieldName, string assetPath)
        {
            Component comp = null;
            foreach (var c in go.GetComponents<Component>())
            {
                if (c != null && c.GetType().Name == componentTypeName)
                {
                    comp = c;
                    break;
                }
            }

            if (comp == null)
            {
                Debug.LogError($"[WireConfigAssets] Componente '{componentTypeName}' não encontrado em Player_Damiao.");
                return false;
            }

            var asset = AssetDatabase.LoadAssetAtPath<Object>(assetPath);
            if (asset == null)
            {
                Debug.LogError($"[WireConfigAssets] Asset não encontrado em '{assetPath}'.");
                return false;
            }

            var so = new SerializedObject(comp);
            var prop = so.FindProperty(fieldName);
            if (prop == null)
            {
                Debug.LogError($"[WireConfigAssets] Campo '{fieldName}' não existe em '{componentTypeName}'.");
                return false;
            }

            prop.objectReferenceValue = asset;
            so.ApplyModifiedPropertiesWithoutUndo();
            Debug.Log($"[WireConfigAssets] {componentTypeName}.{fieldName} ← {assetPath}");
            return true;
        }
    }
}
