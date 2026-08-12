using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using FavelaAmarela.Core.Combat;
using FavelaAmarela.Runtime.Enemies;
using FavelaAmarela.Runtime.Rendering;

namespace FavelaAmarela.EditorTools
{
    /// <summary>
    /// Ferramenta de Editor. Cria/atualiza o <b>prefab do Byakhee</b> — visual real (frame
    /// "espreita" da folha fatiada por <see cref="SliceSpritesheetByakhee"/>) + a ficha +
    /// <see cref="ByakheeAI"/>, substituindo o corpo construído em runtime que o
    /// <c>CarcosaDebuggerWindow</c> usava até aqui.
    ///
    /// <para><b>Pré-requisito:</b> rodar antes 'Tools/FavelaAmarela/Slice Spritesheet do
    /// Byakhee' — sem os frames fatiados, o prefab fica sem sprite (aviso no console, não
    /// trava).</para>
    ///
    /// <para>Diferente da Cassilda/Rei em Amarelo: o Byakhee tem <c>EnemyBase</c> de verdade
    /// (barra de vida real, drena por combate) — <c>ByakheeAI</c> exige
    /// <c>[RequireComponent(EnemyBase, SpriteRenderer, Rigidbody2D)]</c>. Fora do Play Mode o
    /// `Awake` não dispara (só roda em cena/Play), então preencher `ficha` por
    /// `SerializedObject` **depois** de adicionar o componente é seguro aqui — diferente do
    /// `CarcosaDebuggerWindow`, que precisa da dança de objeto inativo por rodar em Play Mode.
    /// </para>
    ///
    /// <para>Números de combate (dano, alcance, velocidades) ficam nos defaults já calibrados
    /// por simulação em 2026-08-11 — esta ferramenta não os toca.</para>
    ///
    /// <para>Idempotente: reaproveita o asset se já existir e só atualiza os campos.</para>
    /// </summary>
    public static class MontarPrefabDoByakhee
    {
        private const string PastaPrefab = "Assets/FavelaAmarela/Art/Enemies";
        private const string CaminhoPrefab = PastaPrefab + "/Byakhee.prefab";
        private const string CaminhoSpritesheet = PastaPrefab + "/Byakhee_Spritesheet.png";
        private const string SpriteIdle = "byakhee_espreita_0";
        private const string CaminhoFicha = "Assets/FavelaAmarela/Config/Ficha_Byakhee.asset";

        [MenuItem("Tools/FavelaAmarela/Montar Prefab do Byakhee")]
        public static void Executar()
        {
            var existente = AssetDatabase.LoadAssetAtPath<GameObject>(CaminhoPrefab);
            var go = existente != null ? Object.Instantiate(existente) : new GameObject("Byakhee");
            go.name = "Byakhee";

            GarantirVisual(go);
            GarantirFicha(go);
            go.layer = LayerMask.NameToLayer("Enemy");

            if (!AssetDatabase.IsValidFolder(PastaPrefab))
                Directory.CreateDirectory(PastaPrefab);

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, CaminhoPrefab);
            Object.DestroyImmediate(go);

            AssetDatabase.SaveAssets();
            Debug.Log($"[Byakhee] Prefab pronto em '{CaminhoPrefab}'.", prefab);
        }

        private static void GarantirVisual(GameObject go)
        {
            var sr = go.GetComponent<SpriteRenderer>();
            if (sr == null) sr = go.AddComponent<SpriteRenderer>();

            var sprite = CarregarSprite(SpriteIdle);
            if (sprite != null)
            {
                sr.sprite = sprite;
                sr.color = Color.white; // arte real: sem tingimento por cima
            }
            else
            {
                Debug.LogWarning($"[Byakhee] Sprite '{SpriteIdle}' não encontrado — rode " +
                                  "'Tools/FavelaAmarela/Slice Spritesheet do Byakhee' antes.");
            }

            var rb = go.GetComponent<Rigidbody2D>();
            if (rb == null) rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            var col = go.GetComponent<BoxCollider2D>();
            if (col == null) col = go.AddComponent<BoxCollider2D>();
            // 60% do frame inteiro: a folha desenha as asas abertas dentro do quadro de
            // 140×140, e a asa não deveria contar como corpo físico/hitbox.
            col.size = sprite != null ? (Vector2)sprite.bounds.size * 0.6f : new Vector2(2f, 2f);

            if (go.GetComponent<DynamicYSort>() == null) go.AddComponent<DynamicYSort>();
        }

        private static void GarantirFicha(GameObject go)
        {
            // ByakheeAI exige EnemyBase (RequireComponent) — adicioná-lo primeiro deixa o
            // AddComponent<ByakheeAI> só completar o resto.
            var enemyBase = go.GetComponent<EnemyBase>();
            if (enemyBase == null) enemyBase = go.AddComponent<EnemyBase>();

            var ficha = AssetDatabase.LoadAssetAtPath<FichaAtributosConfig>(CaminhoFicha);
            if (ficha == null)
                Debug.LogError($"[Byakhee] Ficha não encontrada em '{CaminhoFicha}'.");

            var so = new SerializedObject(enemyBase);
            so.FindProperty("ficha").objectReferenceValue = ficha;
            so.ApplyModifiedPropertiesWithoutUndo();

            if (go.GetComponent<ByakheeAI>() == null)
                go.AddComponent<ByakheeAI>();
        }

        private static Sprite CarregarSprite(string nome)
            => AssetDatabase.LoadAllAssetsAtPath(CaminhoSpritesheet)
                .OfType<Sprite>()
                .FirstOrDefault(s => s.name == nome);
    }
}
