using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using FavelaAmarela.Runtime.Enemies;

namespace FavelaAmarela.EditorTools
{
    /// <summary>
    /// Utilitário de Editor de execução única: converte o <c>Abdul_Alhazred</c> já
    /// existente na cena aberta (montado à mão por <see cref="SetupArenaDoAbdul"/> como
    /// um <c>GameObject</c> solto) num <b>Prefab Asset</b> de verdade — mesmo padrão do
    /// Cultista, Espectro e Coisa do Cemitério.
    ///
    /// <para>Usa <c>SaveAsPrefabAssetAndConnect</c> em vez de <c>SaveAsPrefabAsset</c>:
    /// preserva TODOS os valores já configurados na instância (ficha, falas, cooldowns) e
    /// deixa o objeto da cena conectado ao asset novo — não descarta nada do trabalho já
    /// feito. Referências de cena (<c>painelDeEscolha</c>, <c>caixaDeDialogo</c>,
    /// <c>yugNethNaArena</c>) não sobrevivem no ASSET (um prefab não pode apontar pra um
    /// objeto específico de uma cena), mas ficam preservadas como <b>override da
    /// instância</b> — confira depois de rodar.</para>
    /// </summary>
    public static class ConverterAbdulEmPrefab
    {
        private const string CaminhoPrefab = "Assets/FavelaAmarela/Art/Enemies/Abdul_Alhazred.prefab";

        [MenuItem("Tools/FavelaAmarela/Converter Abdul em Prefab (execução única)")]
        public static void Converter()
        {
            var abdul = Object.FindAnyObjectByType<AbdulAlhazredAI>(FindObjectsInactive.Include);
            if (abdul == null)
            {
                Debug.LogError("[ConverterAbdul] Nenhum AbdulAlhazredAI na cena aberta — abortado.");
                return;
            }

            if (PrefabUtility.IsPartOfAnyPrefab(abdul.gameObject))
            {
                Debug.LogWarning("[ConverterAbdul] O Abdul da cena já é uma instância de prefab — nada a fazer.");
                return;
            }

            var asset = PrefabUtility.SaveAsPrefabAssetAndConnect(
                abdul.gameObject, CaminhoPrefab, InteractionMode.AutomatedAction);

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

            Debug.Log($"[ConverterAbdul] Abdul convertido em prefab: '{CaminhoPrefab}'. " +
                      "A instância na cena agora é um Prefab Instance conectado ao asset. " +
                      "Referências de cena (painelDeEscolha/caixaDeDialogo/yugNethNaArena) " +
                      "ficam como override desta instância — confira no Inspector antes de salvar.");

            Selection.activeObject = asset;
        }
    }
}
