using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using FavelaAmarela.Runtime.GameLoop;

namespace FavelaAmarela.EditorTools
{
    /// <summary>
    /// Ferramenta de Editor. Coloca <see cref="TravessiaDoCompanheiro"/> em toda cena que o
    /// jogador possa alcançar depois da Tumba — sem isto, sair da Tumba de Alhazred deixava
    /// Yug-Neth para trás (bug relatado em playtest, 2026-08-02): ele é um GameObject só da
    /// cena de origem, e a troca de cena (não-aditiva) o destrói junto com o resto.
    ///
    /// <para>Roda em todas as cenas da lista, não só uma: o Deserto e o Santuário são os
    /// dois destinos alcançáveis a partir da Tumba hoje, e o companheiro precisa poder
    /// reaparecer em qualquer um deles.</para>
    ///
    /// <para>Idempotente: reaproveita a instância se já existir em cada cena.</para>
    /// </summary>
    public static class MontarTravessiaDoCompanheiro
    {
        private const string CaminhoPrefabYugNeth = "Assets/FavelaAmarela/Art/Characters/MiGo/YugNeth.prefab";

        private static readonly string[] CenasDestino =
        {
            "Assets/Scenes/Deserto_Hali.unity",
            "Assets/Scenes/Santuario_Yhtill.unity",
        };

        [MenuItem("Tools/FavelaAmarela/Montar travessia de cena do Yug-Neth")]
        public static void Executar()
        {
            var cenaAtiva = EditorSceneManager.GetActiveScene();
            if (cenaAtiva.isDirty && !string.IsNullOrEmpty(cenaAtiva.path))
                EditorSceneManager.SaveScene(cenaAtiva);

            string cenaOriginal = cenaAtiva.path;

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CaminhoPrefabYugNeth);
            if (prefab == null)
                Debug.LogError($"[Travessia] Prefab do Yug-Neth não encontrado em {CaminhoPrefabYugNeth}. " +
                                "As instâncias serão criadas mesmo assim, mas sem o prefab atribuído.");

            foreach (var caminho in CenasDestino)
                MontarNaCena(caminho, prefab);

            if (!string.IsNullOrEmpty(cenaOriginal))
                EditorSceneManager.OpenScene(cenaOriginal, OpenSceneMode.Single);

            Debug.Log("[Travessia] Pronto — Yug-Neth agora atravessa de cena quando já foi libertado.");
        }

        private static void MontarNaCena(string caminhoDaCena, GameObject prefab)
        {
            if (!System.IO.File.Exists(caminhoDaCena))
            {
                Debug.LogWarning($"[Travessia] Cena '{caminhoDaCena}' não existe — pulando.");
                return;
            }

            var cena = EditorSceneManager.OpenScene(caminhoDaCena, OpenSceneMode.Single);

            var travessia = Object.FindAnyObjectByType<TravessiaDoCompanheiro>(FindObjectsInactive.Include);
            GameObject go;
            if (travessia != null)
            {
                go = travessia.gameObject;
            }
            else
            {
                go = new GameObject("Travessia_YugNeth", typeof(TravessiaDoCompanheiro));
                Undo.RegisterCreatedObjectUndo(go, "Criar Travessia do Companheiro");
                travessia = go.GetComponent<TravessiaDoCompanheiro>();
            }

            if (prefab != null)
            {
                var so = new SerializedObject(travessia);
                so.FindProperty("prefabYugNeth").objectReferenceValue = prefab;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            EditorUtility.SetDirty(go);
            EditorSceneManager.MarkSceneDirty(cena);
            EditorSceneManager.SaveScene(cena);

            Debug.Log($"[Travessia] Configurado em '{caminhoDaCena}'.", go);
        }
    }
}
