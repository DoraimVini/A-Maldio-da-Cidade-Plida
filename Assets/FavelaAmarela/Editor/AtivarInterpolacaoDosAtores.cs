using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace FavelaAmarela.EditorTools
{
    /// <summary>
    /// Liga <c>Rigidbody2D.interpolation = Interpolate</c> em todo ator dinâmico.
    ///
    /// <para><b>Por que agora (2026-08-21):</b> o projeto voltou <c>Physics2D.simulationMode</c>
    /// para <c>FixedUpdate</c>, alinhando com o jeito que <c>PlayerMovement</c> e toda IA de
    /// inimigo já escrevem <c>linearVelocity</c>. A documentação da Unity é explícita sobre a
    /// contrapartida: em modo <c>FixedUpdate</c>, "<i>Unity may render multiple frames between
    /// simulation updates</i>" — ou seja, o corpo físico só avança na cadência do tick fixo, mas
    /// a tela redesenha mais vezes que isso. Sem interpolação, o movimento visualmente
    /// engasga entre um tick físico e o próximo. A própria doc do modo recomenda:
    /// "<i>Rigidbody2D interpolation should be used to provide smoother movement per-frame
    /// where appropriate</i>".</para>
    /// </summary>
    public static class AtivarInterpolacaoDosAtores
    {
        private const string PastaDeArte = "Assets/FavelaAmarela/Art";

        private static readonly string[] Cenas =
        {
            "Assets/Scenes/Deserto_Hali.unity",
            "Assets/Scenes/Playtest_RuinasPalidas.unity",
            "Assets/Scenes/Santuario_Yhtill.unity",
            "Assets/Scenes/Portoes_Das_Ruinas.unity",
            "Assets/Scenes/Castelo_Carcosa.unity",
        };

        [MenuItem("Tools/FavelaAmarela/Física: ativar interpolação nos atores")]
        public static void Executar()
        {
            int prefabsAlterados = 0;
            int prefabsJaOk = 0;

            foreach (var caminho in System.IO.Directory.GetFiles(
                         PastaDeArte, "*.prefab", System.IO.SearchOption.AllDirectories))
            {
                var raiz = PrefabUtility.LoadPrefabContents(caminho);
                if (raiz == null) continue;

                try
                {
                    bool algumaMudanca = false;

                    foreach (var rb in raiz.GetComponentsInChildren<Rigidbody2D>(true))
                    {
                        if (rb.bodyType != RigidbodyType2D.Dynamic) continue;
                        if (rb.interpolation == RigidbodyInterpolation2D.Interpolate) continue;

                        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
                        algumaMudanca = true;
                    }

                    if (algumaMudanca)
                    {
                        PrefabUtility.SaveAsPrefabAsset(raiz, caminho, out bool salvou);
                        if (salvou) prefabsAlterados++;
                        else Debug.LogError($"[Interpolacao] SaveAsPrefabAsset recusou {caminho}.");
                    }
                    else
                    {
                        prefabsJaOk++;
                    }
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(raiz);
                }
            }

            int cenasAlteradas = 0;

            foreach (var caminho in Cenas)
            {
                if (!System.IO.File.Exists(caminho)) continue;

                var cena = EditorSceneManager.OpenScene(caminho, OpenSceneMode.Single);
                bool algumaMudanca = false;

                foreach (var rb in Object.FindObjectsByType<Rigidbody2D>(
                             FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    if (rb.bodyType != RigidbodyType2D.Dynamic) continue;
                    if (rb.interpolation == RigidbodyInterpolation2D.Interpolate) continue;

                    rb.interpolation = RigidbodyInterpolation2D.Interpolate;
                    algumaMudanca = true;
                }

                if (algumaMudanca)
                {
                    EditorSceneManager.MarkSceneDirty(cena);
                    if (EditorSceneManager.SaveScene(cena)) cenasAlteradas++;
                }
            }

            Debug.Log($"[Interpolacao] {prefabsAlterados} prefab(s) alterado(s), " +
                      $"{prefabsJaOk} já em Interpolate, {cenasAlteradas} cena(s) alterada(s).");
        }
    }
}
