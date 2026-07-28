using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace FavelaAmarela.EditorTools
{
    /// <summary>
    /// Utilitário de Editor: re-gera SÓ o blockout (paredes/chão) da cena ativa
    /// chamando <c>LevelBlockoutGenerator.GenerateBlockout()</c>. Diferente dos
    /// MenuItems "Montar Cena", NÃO recria player/câmera/HUD/inimigos/triggers —
    /// só reconstrói a geometria sob o Blockout_Root a partir do Planner (agora com
    /// as 9 zonas). Resolve o componente por nome de tipo (padrão dos outros builders).
    /// </summary>
    public static class RegenerateBlockout
    {
        [MenuItem("Tools/FavelaAmarela/Regenerate Blockout (9 zonas)")]
        public static void Regenerate()
        {
            var type = ResolverTipo("FavelaAmarela.Level.Runtime.LevelBlockoutGenerator");
            if (type == null)
            {
                Debug.LogError("[RegenBlockout] Tipo LevelBlockoutGenerator não encontrado (recompile?).");
                return;
            }

            var gen = UnityEngine.Object.FindAnyObjectByType(type);
            if (gen == null)
            {
                Debug.LogError("[RegenBlockout] Nenhum LevelBlockoutGenerator na cena ativa.");
                return;
            }

            var metodo = type.GetMethod("GenerateBlockout");
            if (metodo == null)
            {
                Debug.LogError("[RegenBlockout] Método GenerateBlockout não encontrado.");
                return;
            }

            metodo.Invoke(gen, null);

            var scene = EditorSceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[RegenBlockout] Blockout re-gerado com as 9 zonas e cena salva.");
        }

        private static Type ResolverTipo(string fullName)
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                var t = asm.GetType(fullName);
                if (t != null) return t;
            }
            return null;
        }
    }
}
