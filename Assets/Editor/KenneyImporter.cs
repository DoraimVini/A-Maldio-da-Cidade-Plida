using UnityEditor;
using UnityEngine;

public class KenneyImporter : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        if (assetPath.Contains("ThirdParty/Kenney"))
        {
            TextureImporter importer = (TextureImporter)assetImporter;

            // Se ainda não for Sprite, podemos forçar (opcional, dependendo do conteúdo, mas útil para isométricos)
            if (importer.textureType == TextureImporterType.Default)
            {
                importer.textureType = TextureImporterType.Sprite;
            }

            // Aplicar padronização de pixel art de Favela Amarela
            importer.spritePixelsPerUnit = 32;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            
            // Garantir que não gere mipmaps para manter a imagem limpa
            importer.mipmapEnabled = false;
        }
    }
}
