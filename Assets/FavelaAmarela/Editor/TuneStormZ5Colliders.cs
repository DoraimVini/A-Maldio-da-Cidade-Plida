using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace FavelaAmarela.EditorTools
{
    /// <summary>
    /// Utilitário de Editor (fix de bug): ajusta os colliders dos triggers de
    /// tempestade pra que a fronteira Z4/Z5 caia exatamente na barreira de anomalia
    /// (y = -30.25), sem o trigger da tempestade forte vazar pra dentro da Zona 5.
    /// (O MCP update_component dá "success" falso ao setar m_Size, por isso via script.)
    /// </summary>
    public static class TuneStormZ5Colliders
    {
        [MenuItem("Tools/FavelaAmarela/Tune Storm Z5 Colliders")]
        public static void Tune()
        {
            var ok = true;
            ok &= SetSize("TempestadeTrigger_Z5_Nula", new Vector2(10f, 8.5f));   // pos y=-34.5 → topo -30.25
            ok &= SetSize("TempestadeTrigger_Z3Z4_Forte", new Vector2(15f, 14f)); // pos y=-23.25 → base -30.25

            if (!ok) return;

            var scene = EditorSceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[TuneStormZ5] Colliders ajustados: fronteira Z4/Z5 na barreira (-30.25).");
        }

        private static bool SetSize(string nome, Vector2 size)
        {
            var go = GameObject.Find(nome);
            if (go == null) { Debug.LogError($"[TuneStormZ5] '{nome}' não encontrado."); return false; }
            var col = go.GetComponent<BoxCollider2D>();
            if (col == null) { Debug.LogError($"[TuneStormZ5] '{nome}' sem BoxCollider2D."); return false; }

            var so = new SerializedObject(col);
            so.FindProperty("m_Size").vector2Value = size;
            so.ApplyModifiedPropertiesWithoutUndo();
            Debug.Log($"[TuneStormZ5] {nome}.size = {size}");
            return true;
        }
    }
}
