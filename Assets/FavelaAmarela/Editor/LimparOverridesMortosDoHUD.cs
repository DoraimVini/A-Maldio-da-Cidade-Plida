using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using FavelaAmarela.Runtime.UI;

namespace FavelaAmarela.EditorTools
{
    /// <summary>
    /// Ferramenta de Editor. Remove <b>overrides mortos</b> de campos do
    /// <see cref="HUDController"/> nas instâncias de prefab.
    ///
    /// <para><b>Por que existe:</b> a barra de Vitalidade era um objeto solto na cena da
    /// Tumba, referenciado por um override da instância do prefab. Ao mover a barra <i>para
    /// dentro</i> do prefab e apagar a cópia solta, o override ficou apontando para nada
    /// (<c>fileID: 0</c>) — e <b>override vence o prefab</b>. Resultado: a Tumba ficaria sem
    /// barra de vida justamente depois da correção que a levou para todas as cenas.</para>
    ///
    /// <para>Remover o override faz a instância voltar a herdar o valor do prefab.</para>
    /// </summary>
    public static class LimparOverridesMortosDoHUD
    {
        private static readonly string[] Cenas =
        {
            "Assets/Scenes/Deserto_Hali.unity",
            "Assets/Scenes/Tumba_De_Alhazred.unity",
            "Assets/Scenes/Santuario_Yhtill.unity",
        };

        /// <summary>Campos do HUDController que apontam para views e podem ter ficado órfãos.</summary>
        private static readonly string[] Campos =
        {
            "vitalidadeBar", "resilienciaBar", "barraDeAcoes", "barraDeItens",
        };

        [MenuItem("Tools/FavelaAmarela/Limpar overrides mortos do HUD")]
        public static void Executar()
        {
            var cenaAtiva = EditorSceneManager.GetActiveScene();
            if (cenaAtiva.isDirty && !string.IsNullOrEmpty(cenaAtiva.path))
                EditorSceneManager.SaveScene(cenaAtiva);

            string cenaOriginal = cenaAtiva.path;
            int total = 0;

            foreach (var caminho in Cenas)
            {
                if (!System.IO.File.Exists(caminho)) continue;

                var cena = EditorSceneManager.OpenScene(caminho, OpenSceneMode.Single);
                int limpos = LimparCena();

                if (limpos > 0)
                {
                    EditorSceneManager.MarkSceneDirty(cena);
                    EditorSceneManager.SaveScene(cena);
                    total += limpos;
                    Debug.Log($"[Overrides] '{cena.name}': {limpos} override(s) morto(s) removido(s).");
                }
            }

            if (!string.IsNullOrEmpty(cenaOriginal))
                EditorSceneManager.OpenScene(cenaOriginal, OpenSceneMode.Single);

            Debug.Log($"[Overrides] Pronto — {total} no total. As instâncias voltam a herdar o prefab.");
        }

        private static int LimparCena()
        {
            int limpos = 0;

            foreach (var hud in Object.FindObjectsByType<HUDController>(FindObjectsInactive.Include))
            {
                if (!PrefabUtility.IsPartOfPrefabInstance(hud)) continue;

                var so = new SerializedObject(hud);
                foreach (var campo in Campos)
                {
                    var prop = so.FindProperty(campo);
                    if (prop == null) continue;

                    // Só o override que aponta para NADA é morto. Um override apontando para
                    // um objeto de cena legítimo (a barra de itens, por exemplo) deve ficar.
                    if (prop.objectReferenceValue != null) continue;
                    if (!prop.prefabOverride) continue;

                    PrefabUtility.RevertPropertyOverride(prop, InteractionMode.AutomatedAction);
                    limpos++;
                    Debug.Log($"[Overrides] '{campo}' revertido para o valor do prefab.", hud);
                }
            }

            return limpos;
        }
    }
}
