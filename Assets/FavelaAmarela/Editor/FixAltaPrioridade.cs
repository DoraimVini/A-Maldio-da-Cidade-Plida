using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class FixAltaPrioridade
{
    // Constantes isoladas para fácil manutenção futura
    private static readonly string[] PastasAssets = {
        "Assets/FavelaAmarela/Art/Characters",
        "Assets/FavelaAmarela/Art/Enemies"
    };

    [MenuItem("Favela Amarela/Fix Alta Prioridade (Auditoria)")]
    public static void FixAlta()
    {
        try
        {
            AtualizarMatrizColisao();
            CorrigirPivotsPersonagens();
            CorrigirConstraintsPrefabs();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("<b><color=green>[Auditoria]</color></b> Todas as correções de alta prioridade concluídas.");
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

    private static void AtualizarMatrizColisao()
    {
        int enemyLayer = LayerMask.NameToLayer("Enemy");
        if (enemyLayer != -1)
        {
            Physics2D.IgnoreLayerCollision(enemyLayer, enemyLayer, true);
            Debug.Log("[Auditoria] Enemy x Enemy ignorado.");
        }
        else
        {
            Debug.LogWarning("[Auditoria] Camada 'Enemy' não encontrada.");
        }
    }

    private static void CorrigirPivotsPersonagens()
    {
        var guids = new List<string>();
        foreach (string pasta in PastasAssets)
        {
            if (AssetDatabase.IsValidFolder(pasta))
                guids.AddRange(AssetDatabase.FindAssets("t:Texture2D", new[] { pasta }));
        }

        int corrigidos = 0;
        int total = guids.Count;

        // Batching de importações: pausa o AssetDatabase
        AssetDatabase.StartAssetEditing();

        try
        {
            for (int i = 0; i < total; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);

                // Barra de progresso com opção de cancelar
                bool cancelado = EditorUtility.DisplayCancelableProgressBar(
                    "Corrigindo Pivots",
                    $"Processando {System.IO.Path.GetFileName(path)}",
                    (float)i / total
                );

                if (cancelado)
                {
                    Debug.LogWarning("[Auditoria] Correção de pivots cancelada pelo usuário.");
                    return; // Sai do método, mas o finally será chamado!
                }

                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null || importer.textureType != TextureImporterType.Sprite)
                    continue;

                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);

                if (settings.spriteAlignment != (int)SpriteAlignment.BottomCenter)
                {
                    settings.spriteAlignment = (int)SpriteAlignment.BottomCenter;
                    importer.SetTextureSettings(settings);

                    // Salva no .meta e enfileira pro Batch (A Unity não vai travar graças ao StartAssetEditing)
                    importer.SaveAndReimport(); 
                    corrigidos++;
                }
            }
        }
        finally
        {
            // Retoma o AssetDatabase – as importações serão processadas em lote
            AssetDatabase.StopAssetEditing();
        }

        Debug.Log($"[Auditoria] Pivots corrigidos para BottomCenter em {corrigidos} de {total} sprites.");
    }

    private static void CorrigirConstraintsPrefabs()
    {
        var guids = new List<string>();
        foreach (string pasta in PastasAssets)
        {
            if (AssetDatabase.IsValidFolder(pasta))
                guids.AddRange(AssetDatabase.FindAssets("t:Prefab", new[] { pasta }));
        }

        int corrigidos = 0;
        int total = guids.Count;

        for (int i = 0; i < total; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);

            bool cancelado = EditorUtility.DisplayCancelableProgressBar(
                "Corrigindo Prefabs",
                $"Ajustando Rigidbodies: {System.IO.Path.GetFileName(path)}",
                (float)i / total
            );

            if (cancelado)
            {
                Debug.LogWarning("[Auditoria] Correção de prefabs cancelada pelo usuário.");
                return;
            }

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) continue;

            Rigidbody2D rb = prefab.GetComponent<Rigidbody2D>();
            if (rb == null) continue;

            bool modificado = false;

            if (rb.constraints != RigidbodyConstraints2D.FreezeRotation)
            {
                rb.constraints = RigidbodyConstraints2D.FreezeRotation;
                modificado = true;
            }

            if (!prefab.name.Contains("Player", System.StringComparison.OrdinalIgnoreCase) &&
                !prefab.name.Contains("Projetil", System.StringComparison.OrdinalIgnoreCase))
            {
                if (rb.collisionDetectionMode != CollisionDetectionMode2D.Discrete)
                {
                    rb.collisionDetectionMode = CollisionDetectionMode2D.Discrete;
                    modificado = true;
                }
            }

            if (modificado)
            {
                EditorUtility.SetDirty(rb);
                PrefabUtility.SavePrefabAsset(prefab);
                corrigidos++;
            }
        }

        Debug.Log($"[Auditoria] Rigidbodies otimizados em {corrigidos} de {total} prefabs.");
    }
}
