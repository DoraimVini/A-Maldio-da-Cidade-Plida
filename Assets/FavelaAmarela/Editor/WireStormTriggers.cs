using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace FavelaAmarela.EditorTools
{
    /// <summary>
    /// Utilitário de Editor (cena da Tempestade): atribui a referência
    /// <c>TempestadeAmbiente</c> aos 4 GameObjects de trigger de zona na cena
    /// ativa via <see cref="SerializedObject"/>. Existe pela mesma razão do
    /// <see cref="WireConfigAssets"/>: o MCP <c>update_component</c> não resolve
    /// referências de objeto (nem asset, nem instância de cena) para campos
    /// tipados — só o tipo exato <c>UnityEngine.Object</c>.
    /// </summary>
    public static class WireStormTriggers
    {
        private static readonly (string triggerName, string componentTypeName)[] Triggers =
        {
            ("TempestadeTrigger_Z1_Spawn", "TempestadeZonaTrigger"),
            ("TempestadeTrigger_Z2_Rajadas", "TempestadeRajadaAleatoria"),
            ("TempestadeTrigger_Z3Z4_Forte", "TempestadeZonaTrigger"),
            ("TempestadeTrigger_Z5_Nula", "TempestadeZonaTrigger"),
        };

        [MenuItem("Tools/FavelaAmarela/Wire Storm Triggers")]
        public static void Wire()
        {
            var tempestadeAmbienteGO = GameObject.Find("TempestadeAmbiente");
            if (tempestadeAmbienteGO == null)
            {
                Debug.LogError("[WireStormTriggers] 'TempestadeAmbiente' não encontrado na cena ativa.");
                return;
            }

            var tempestadeAmbiente = EncontrarComponente(tempestadeAmbienteGO, "TempestadeAmbiente");
            if (tempestadeAmbiente == null)
            {
                Debug.LogError("[WireStormTriggers] GameObject 'TempestadeAmbiente' não possui o componente TempestadeAmbiente.");
                return;
            }

            var ok = true;
            foreach (var (triggerName, componentTypeName) in Triggers)
            {
                ok &= AssignReference(triggerName, componentTypeName, tempestadeAmbiente);
            }

            if (!ok)
            {
                Debug.LogError("[WireStormTriggers] Uma ou mais atribuições falharam; cena NÃO foi salva.");
                return;
            }

            var scene = tempestadeAmbienteGO.scene;
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[WireStormTriggers] 4 triggers de tempestade fiados e cena salva com sucesso.");
        }

        private static bool AssignReference(string triggerName, string componentTypeName, Component tempestadeAmbiente)
        {
            var trigger = GameObject.Find(triggerName);
            if (trigger == null)
            {
                Debug.LogError($"[WireStormTriggers] GameObject '{triggerName}' não encontrado.");
                return false;
            }

            var comp = EncontrarComponente(trigger, componentTypeName);
            if (comp == null)
            {
                Debug.LogError($"[WireStormTriggers] Componente '{componentTypeName}' não encontrado em '{triggerName}'.");
                return false;
            }

            var so = new SerializedObject(comp);
            var prop = so.FindProperty("tempestadeAmbiente");
            if (prop == null)
            {
                Debug.LogError($"[WireStormTriggers] Campo 'tempestadeAmbiente' não existe em '{componentTypeName}'.");
                return false;
            }

            prop.objectReferenceValue = tempestadeAmbiente;
            so.ApplyModifiedPropertiesWithoutUndo();
            Debug.Log($"[WireStormTriggers] {triggerName} ({componentTypeName}).tempestadeAmbiente ← TempestadeAmbiente");
            return true;
        }

        private static Component EncontrarComponente(GameObject go, string tipoNome)
        {
            foreach (var c in go.GetComponents<Component>())
            {
                if (c != null && c.GetType().Name == tipoNome)
                    return c;
            }
            return null;
        }
    }
}
