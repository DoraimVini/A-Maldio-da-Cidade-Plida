using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class FixCriticoPrefabs
{
    private static readonly string[] PastasAlvo = {
        "Assets/FavelaAmarela/Art/Characters",
        "Assets/FavelaAmarela/Art/Enemies"
    };

    // Nome totalmente qualificado da classe (namespace + classe)
    private const string NomeClasseYSort = "FavelaAmarela.Runtime.Rendering.DynamicYSort";
    private const string NomeAssembly = "FavelaAmarela.Runtime";

    [MenuItem("Favela Amarela/Fix Críticos (Auditoria)")]
    public static void FixCriticos()
    {
        try
        {
            // Verifica a existência do tipo (com namespace completo)
            System.Type tipoYSort = System.Type.GetType($"{NomeClasseYSort}, {NomeAssembly}");

            // Fallback: se não encontrou, tenta sem namespace (escopo global)
            if (tipoYSort == null)
            {
                string nomeCurto = "DynamicYSort";
                tipoYSort = System.Type.GetType($"{nomeCurto}, {NomeAssembly}");
            }

            if (tipoYSort == null)
            {
                Debug.LogError($"<b><color=red>[Auditoria Crítica]</color></b> Tipo '{NomeClasseYSort}' não encontrado. " +
                               "Verifique o nome, o namespace e o assembly definition. Nenhuma alteração foi feita.");
                return;
            }

            CorrigirY_SortingDinamico(tipoYSort);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("<b><color=green>[Auditoria Crítica]</color></b> Componentes 'DynamicYSort' injetados com sucesso.");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"<b><color=red>[Auditoria Erro]</color></b> Falha: {e.Message}");
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }

    private static void CorrigirY_SortingDinamico(System.Type tipoYSort)
    {
        var guids = new List<string>();
        foreach (string pasta in PastasAlvo)
        {
            if (AssetDatabase.IsValidFolder(pasta))
                guids.AddRange(AssetDatabase.FindAssets("t:Prefab", new[] { pasta }));
        }

        int adicionados = 0;
        int ignoradosProjeteis = 0;
        int jaPossui = 0;
        int total = guids.Count;

        // Batching de importações (protege contra callbacks durante o processo)
        AssetDatabase.StartAssetEditing();

        try
        {
            for (int i = 0; i < total; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);

                bool cancelado = EditorUtility.DisplayCancelableProgressBar(
                    "Injetando DynamicYSort",
                    $"Verificando: {System.IO.Path.GetFileName(path)}",
                    (float)i / total
                );

                if (cancelado)
                {
                    Debug.LogWarning("[Auditoria Crítica] Processo cancelado pelo usuário.");
                    return;
                }

                GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefabAsset == null) continue;

                bool isProjetil = prefabAsset.name.Contains("Projetil", System.StringComparison.OrdinalIgnoreCase);
                if (isProjetil)
                {
                    ignoradosProjeteis++;
                    continue;
                }

                if (prefabAsset.GetComponent<SpriteRenderer>() == null)
                    continue;

                if (prefabAsset.GetComponent(tipoYSort) != null)
                {
                    jaPossui++;
                    continue;
                }

                // Edição segura do prefab
                var conteudo = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    conteudo.AddComponent(tipoYSort);
                    PrefabUtility.SaveAsPrefabAsset(conteudo, path);
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(conteudo);
                }
                adicionados++;
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
        }

        Debug.Log($"[Auditoria Crítica] Resultados: {adicionados} Y-Sorts adicionados, " +
                  $"{jaPossui} já possuíam, {ignoradosProjeteis} projéteis ignorados, " +
                  $"{total - adicionados - jaPossui - ignoradosProjeteis} sem SpriteRenderer.");
    }
}
