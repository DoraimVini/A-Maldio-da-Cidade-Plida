using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using FavelaAmarela.Player;

namespace FavelaAmarela.EditorTools
{
    /// <summary>
    /// Utilitário de Editor (uso único): põe o <see cref="GerenciadorDeVigor"/> no
    /// <c>Player_Damiao.prefab</c> e remove o override redundante da Arena.
    ///
    /// <para><b>O bug (achado em 2026-08-18):</b> o componente <b>não estava no prefab</b>. Ele
    /// tinha sido adicionado como override de instância <b>só na <c>Cena_ArenaDeTestes</c></b>.
    /// Nas três cenas jogáveis, <c>GetComponent&lt;GerenciadorDeVigor&gt;()</c> devolvia null — e
    /// o código degrada em silêncio:</para>
    ///
    /// <code>if (_vigor != null &amp;&amp; !_vigor.TentarConsumirEsquiva()) return;</code>
    ///
    /// <para>Sem o componente, a condição curto-circuita e a ação passa. Resultado: <b>esquiva
    /// grátis e corrida infinita</b> fora da Arena, sem um aviso sequer. Também explica por que
    /// os 4 <c>StatType</c> de Vigor pareciam vivos na auditoria: estavam, mas só na Arena — um
    /// item com <c>+VigorMaximo</c> não fazia nada no Deserto.</para>
    ///
    /// <para><b>Por que remover o override da Arena:</b> com o componente no prefab, a instância
    /// da Arena ficaria com <b>dois</b> <c>GerenciadorDeVigor</c> — o herdado e o adicionado. Os
    /// valores do override são idênticos aos defaults da classe (100 / 12 / 25 / 25 / 15 / 30),
    /// então nada de balanceamento se perde na remoção.</para>
    /// </summary>
    public static class LigarVigorNoPrefab
    {
        private const string CaminhoDoPrefab =
            "Assets/FavelaAmarela/Art/Characters/Damiao/Player_Damiao.prefab";

        private const string CenaDaArena = "Assets/Scenes/Cena_ArenaDeTestes.unity";

        [MenuItem("Tools/FavelaAmarela/Ligar Vigor no prefab do Damião")]
        public static void Ligar()
        {
            if (!AcrescentarAoPrefab()) return;
            LimparOverrideDaArena();
        }

        private static bool AcrescentarAoPrefab()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CaminhoDoPrefab);
            if (prefab == null)
            {
                Debug.LogError($"[LigarVigorNoPrefab] Prefab não encontrado: {CaminhoDoPrefab}");
                return false;
            }

            var raiz = PrefabUtility.LoadPrefabContents(CaminhoDoPrefab);
            try
            {
                if (raiz.GetComponent<GerenciadorDeVigor>() != null)
                {
                    Debug.Log("[LigarVigorNoPrefab] O prefab já tem GerenciadorDeVigor — intocado.");
                }
                else
                {
                    raiz.AddComponent<GerenciadorDeVigor>();
                    PrefabUtility.SaveAsPrefabAsset(raiz, CaminhoDoPrefab);
                    Debug.Log("[LigarVigorNoPrefab] GerenciadorDeVigor acrescentado ao " +
                              "Player_Damiao.prefab (valores default: 100 / 12 / 25 / 25 / 15 / 30).");
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(raiz);
            }

            return true;
        }

        /// <summary>
        /// Remove o componente que a Arena adicionava por cima da instância. Sem isso, a cena
        /// passaria a ter dois <c>GerenciadorDeVigor</c> no mesmo GameObject.
        /// </summary>
        private static void LimparOverrideDaArena()
        {
            var cena = EditorSceneManager.OpenScene(CenaDaArena, OpenSceneMode.Single);

            var todos = Object.FindObjectsByType<GerenciadorDeVigor>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);

            int removidos = 0;
            foreach (var vigor in todos)
            {
                // Só interessa o que foi ADICIONADO à instância; o que vem do prefab fica.
                if (!PrefabUtility.IsAddedComponentOverride(vigor)) continue;

                Object.DestroyImmediate(vigor, true);
                removidos++;
            }

            if (removidos > 0)
            {
                EditorSceneManager.MarkSceneDirty(cena);
                EditorSceneManager.SaveScene(cena);
                Debug.Log($"[LigarVigorNoPrefab] {removidos} override(s) removido(s) da Arena; " +
                          "o componente agora vem do prefab. Cena salva.");
            }
            else
            {
                Debug.Log("[LigarVigorNoPrefab] Nenhum override de Vigor na Arena — nada a limpar.");
            }
        }
    }
}
