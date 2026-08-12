using System.IO;
using UnityEditor;
using UnityEngine;
using FavelaAmarela.Runtime.Enemies;
using FavelaAmarela.Runtime.Rendering;

namespace FavelaAmarela.EditorTools
{
    /// <summary>
    /// Ferramenta de Editor. Cria/atualiza o <b>prefab do Rei em Amarelo</b> — visual +
    /// <see cref="ReiEmAmareloAI"/>, como um asset reutilizável em vez do corpo montado em
    /// runtime que o <c>CarcosaDebuggerWindow</c> usava até aqui.
    ///
    /// <para><b>Sprite:</b> <c>ReiEmAmarelo_Placeholder.png</c> — um frame isolado (recorte +
    /// canal alfa preservado, sem redesenho) do spritesheet "Necromancer" que o Vini trouxe
    /// para a Inbox (<c>Assets/Arte/Inbox/Necromancer_creativekind-Sheet.png</c>). Não é a
    /// arte final do Rei (cores erradas, mascote sem a Máscara Pálida), mas é o mesmo
    /// arquétipo visual — figura encapuzada e sinistra com cajado — e resolve o mesmo
    /// problema que o placeholder da Cassilda resolvia: ter <b>algo</b> em cena em vez de um
    /// quadrado colorido, sem travar esperando arte definitiva.
    ///
    /// <para>Mesmo padrão do <c>MontarPrefabDaCassilda</c>: sem <c>EnemyBase</c> (o Rei não
    /// tem barra de vida, de propósito — ver <see cref="ReiEmAmareloAI"/>), <c>Color.white</c>
    /// porque é arte real (não tingir por cima), <c>DynamicYSort</c> para entrar no
    /// Y-sorting isométrico do resto do elenco.</para>
    ///
    /// <para>Idempotente: reaproveita o asset se já existir e só atualiza os campos.</para>
    /// </summary>
    public static class MontarPrefabDoReiEmAmarelo
    {
        private const string PastaPrefab = "Assets/FavelaAmarela/Art/Enemies";
        private const string CaminhoPrefab = PastaPrefab + "/ReiEmAmarelo.prefab";
        private const string CaminhoSprite = PastaPrefab + "/ReiEmAmarelo_Placeholder.png";

        [MenuItem("Tools/FavelaAmarela/Montar Prefab do Rei em Amarelo")]
        public static void Executar()
        {
            var existente = AssetDatabase.LoadAssetAtPath<GameObject>(CaminhoPrefab);
            var go = existente != null ? Object.Instantiate(existente) : new GameObject("ReiEmAmarelo");
            go.name = "ReiEmAmarelo";

            GarantirVisual(go);
            GarantirColisor(go);
            GarantirComponente(go);
            go.layer = LayerMask.NameToLayer("Enemy");

            if (!AssetDatabase.IsValidFolder(PastaPrefab))
                Directory.CreateDirectory(PastaPrefab);

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, CaminhoPrefab);
            Object.DestroyImmediate(go);

            AssetDatabase.SaveAssets();
            Debug.Log($"[ReiEmAmarelo] Prefab pronto em '{CaminhoPrefab}'.", prefab);
        }

        private static void GarantirVisual(GameObject go)
        {
            var sr = go.GetComponent<SpriteRenderer>();
            if (sr == null) sr = go.AddComponent<SpriteRenderer>();

            CorrigirImportacaoDoSprite();
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(CaminhoSprite);

            if (sprite != null)
            {
                sr.sprite = sprite;
                sr.color = Color.white; // arte real (mesmo que emprestada): sem tingimento por cima
            }
            else
            {
                Debug.LogWarning($"[ReiEmAmarelo] Sprite não encontrado em '{CaminhoSprite}' — " +
                                  "prefab ficará sem visual até o arquivo existir.");
            }

            if (go.GetComponent<DynamicYSort>() == null) go.AddComponent<DynamicYSort>();
        }

        /// <summary>
        /// PPU 32 e filtro Point (favela-pixelart-standards) + pivô <c>BottomCenter</c> — o
        /// mesmo ajuste que a Cassilda precisou: sem ele o import padrão fica em
        /// <c>BottomLeft</c> e o Y-sort (que assume pé no centro-base) desalinha.
        /// </summary>
        private static void CorrigirImportacaoDoSprite()
        {
            if (!(AssetImporter.GetAtPath(CaminhoSprite) is TextureImporter importer)) return;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 32f;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.BottomCenter;
            settings.spritePivot = new Vector2(0.5f, 0f);
            importer.SetTextureSettings(settings);

            importer.SaveAndReimport();
        }

        private static void GarantirColisor(GameObject go)
        {
            var col = go.GetComponent<CircleCollider2D>();
            if (col == null) col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = false;
            col.radius = 0.5f;
        }

        private static void GarantirComponente(GameObject go)
        {
            if (go.GetComponent<ReiEmAmareloAI>() == null)
                go.AddComponent<ReiEmAmareloAI>();
        }
    }
}
