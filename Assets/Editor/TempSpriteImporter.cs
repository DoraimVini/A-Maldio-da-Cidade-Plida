using UnityEditor;
using UnityEngine;

public class TempSpriteImporter : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        if (assetPath.Contains("Cassilda_Sprite.png"))
        {
            TextureImporter importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = 32;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
        }
    }
}
