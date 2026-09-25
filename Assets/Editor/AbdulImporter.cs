using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.Collections.Generic;
using System.IO;

public class AbdulImporter : Editor
{
    private const string TexturePath = "Assets/Sprites/Bosses/Alhazred/abdul_alhazred_spritesheet.png";
    private const string TargetFolder = "Assets/Sprites/Bosses/Alhazred";

    [MenuItem("Favela Amarela/Setup/Configurar Animações do Abdul")]
    public static void ConfigurarAbdul()
    {
        // 1. Import and Slice Texture
        TextureImporter importer = AssetImporter.GetAtPath(TexturePath) as TextureImporter;
        if (importer == null)
        {
            Debug.LogError($"[AbdulImporter] Arquivo não encontrado em: {TexturePath}");
            return;
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = 32;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        
        // Define slicing (8 columns, 7 rows, 64x64 each)
        int cols = 8;
        int rows = 7;
        int cellWidth = 64;
        int cellHeight = 64;

        List<SpriteMetaData> metaDataList = new List<SpriteMetaData>();
        
        // Rows names
        string[] animNames = new string[] { "idle", "move", "cast", "summon", "teleport", "hurt", "death" };

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                SpriteMetaData meta = new SpriteMetaData();
                meta.rect = new Rect(c * cellWidth, (rows - 1 - r) * cellHeight, cellWidth, cellHeight);
                meta.pivot = new Vector2(0.5f, 0.5f);
                meta.name = $"abdul_{animNames[r]}_{c}";
                metaDataList.Add(meta);
            }
        }

        importer.spritesheet = metaDataList.ToArray();
        EditorUtility.SetDirty(importer);
        importer.SaveAndReimport();

        // Load all sliced sprites
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(TexturePath);
        List<Sprite> allSprites = new List<Sprite>();
        foreach (var asset in assets)
        {
            if (asset is Sprite s) allSprites.Add(s);
        }

        // 2. Create Animations
        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath($"{TargetFolder}/Abdul_Controller.controller");

        for (int r = 0; r < rows; r++)
        {
            string animName = animNames[r];
            AnimationClip clip = new AnimationClip();
            clip.frameRate = 12; // Standard pixel art framerate

            if (animName == "idle" || animName == "move")
            {
                AnimationClipSettings clipSettings = AnimationUtility.GetAnimationClipSettings(clip);
                clipSettings.loopTime = true;
                AnimationUtility.SetAnimationClipSettings(clip, clipSettings);
            }

            EditorCurveBinding spriteBinding = new EditorCurveBinding();
            spriteBinding.type = typeof(SpriteRenderer);
            spriteBinding.path = "";
            spriteBinding.propertyName = "m_Sprite";

            ObjectReferenceKeyframe[] keyFrames = new ObjectReferenceKeyframe[cols];
            for (int c = 0; c < cols; c++)
            {
                keyFrames[c] = new ObjectReferenceKeyframe();
                keyFrames[c].time = c / clip.frameRate;
                
                Sprite sprite = allSprites.Find(s => s.name == $"abdul_{animName}_{c}");
                keyFrames[c].value = sprite;
            }

            AnimationUtility.SetObjectReferenceCurve(clip, spriteBinding, keyFrames);
            
            // Save clip
            string clipPath = $"{TargetFolder}/Abdul_{animName}.anim";
            AssetDatabase.CreateAsset(clip, clipPath);

            // Add to Controller
            controller.AddMotion(clip);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[AbdulImporter] Importação, fatiamento e criação de Animation Controller concluídos com sucesso!");
    }
}
