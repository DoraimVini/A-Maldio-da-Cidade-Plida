using UnityEditor;
using UnityEngine;
using FavelaAmarela.Inventario;
using System.IO;

namespace FavelaAmarela.Editor
{
    [InitializeOnLoad]
    public static class InventoryPrefabBuilder
    {
        static InventoryPrefabBuilder()
        {
            EditorApplication.delayCall += BuildPrefab;
        }

        private static void BuildPrefab()
        {
            string resourcesPath = "Assets/FavelaAmarela/Resources";
            if (!AssetDatabase.IsValidFolder(resourcesPath))
            {
                Directory.CreateDirectory(resourcesPath);
                AssetDatabase.Refresh();
            }

            string prefabPath = resourcesPath + "/InventoryManager.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) == null)
            {
                GameObject go = new GameObject("InventoryManager");
                go.AddComponent<InventoryManager>();
                
                PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
                GameObject.DestroyImmediate(go);
                
                Debug.Log("[InventoryPrefabBuilder] Prefab do InventoryManager criado com sucesso em " + prefabPath);
            }
        }
    }
}
