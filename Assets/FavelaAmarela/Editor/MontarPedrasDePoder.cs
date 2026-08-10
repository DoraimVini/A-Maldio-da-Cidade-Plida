using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using FavelaAmarela.Runtime.Enemies;

namespace FavelaAmarela.EditorTools
{
    /// <summary>
    /// Utilitário de Editor: cria o <b>prefab da Pedra de Poder</b> e o liga ao Abdul da
    /// cena aberta.
    ///
    /// <para>As Pedras <b>não ficam pré-plantadas na dungeon</b> (decisão do Vini,
    /// 2026-07-30): são âncoras do ritual de Abdul, então nascem quando ele desperta
    /// (entrada na Fase 1) e somem na virada para a Fase 2 — onde o escudo deixa de
    /// depender delas. Por isso a Pedra precisa existir como <b>prefab</b>, instanciado em
    /// runtime por <c>AbdulAlhazredAI</c>, e não como objetos soltos na cena.</para>
    ///
    /// <para>Também <b>remove</b> quaisquer Pedras soltas que tenham sobrado na cena de
    /// execuções antigas do <c>SetupArenaDoAbdul</c> — elas ficariam visíveis na cripta
    /// antes da luta, que é exatamente o que esta mudança corrige.</para>
    ///
    /// Idempotente: reaproveita o prefab se já existir.
    /// </summary>
    public static class MontarPedrasDePoder
    {
        private const string CaminhoPrefab = "Assets/FavelaAmarela/Art/Enemies/PedraDePoder.prefab";

        [MenuItem("Tools/FavelaAmarela/Montar Pedras de Poder (prefab + wiring)")]
        public static void Montar()
        {
            var abdul = Object.FindAnyObjectByType<AbdulAlhazredAI>(FindObjectsInactive.Include);
            if (abdul == null)
            {
                Debug.LogError("[MontarPedras] Nenhum AbdulAlhazredAI na cena aberta — abortado.");
                return;
            }

            var prefab = ObterOuCriarPrefab();
            int soltasRemovidas = RemoverPedrasSoltasDaCena();

            bool ligou = AtribuirCampo(abdul, "prefabPedraDePoder", prefab);

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

            Debug.Log($"[MontarPedras] Prefab '{CaminhoPrefab}' pronto; " +
                      $"campo do Abdul {(ligou ? "ligado" : "NÃO ligado (confira o nome do campo)")}; " +
                      $"{soltasRemovidas} pedra(s) solta(s) removida(s) da cena. " +
                      "As Pedras agora nascem só na Fase 1. Cena NÃO foi salva — confira antes.");
        }

        private static GameObject ObterOuCriarPrefab()
        {
            var existente = AssetDatabase.LoadAssetAtPath<GameObject>(CaminhoPrefab);
            if (existente != null) return existente;

            var go = new GameObject("PedraDePoder",
                typeof(SpriteRenderer), typeof(BoxCollider2D), typeof(PedraDePoder));

            go.layer = LayerMask.NameToLayer("Enemy"); // alvo das armas do jogador

            var sr = go.GetComponent<SpriteRenderer>();
            sr.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            sr.color = new Color(0.45f, 0.35f, 0.75f); // roxo-anômalo, placeholder até a arte
            go.transform.localScale = new Vector3(0.7f, 0.9f, 1f);

            var col = go.GetComponent<BoxCollider2D>();
            col.size = Vector2.one;

            AdicionarYSortSeExistir(go);

            Directory.CreateDirectory(Path.GetDirectoryName(CaminhoPrefab)!);
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, CaminhoPrefab);
            Object.DestroyImmediate(go);
            return prefab;
        }

        /// <summary>
        /// Apaga Pedras que estejam soltas na cena (resquício do modelo antigo, em que elas
        /// ficavam plantadas na arena desde sempre). Ignora instâncias que sejam parte de um
        /// prefab recém-instanciado em runtime — em Editor não há nenhuma, então é seguro.
        /// </summary>
        private static int RemoverPedrasSoltasDaCena()
        {
            var soltas = Object.FindObjectsByType<PedraDePoder>(
                FindObjectsInactive.Include);

            int total = 0;
            foreach (var pedra in soltas)
            {
                Object.DestroyImmediate(pedra.gameObject);
                total++;
            }
            return total;
        }

        private static void AdicionarYSortSeExistir(GameObject go)
        {
            var tipo = System.AppDomain.CurrentDomain.GetAssemblies()
                .Select(a => a.GetType("FavelaAmarela.Runtime.Rendering.DynamicYSort"))
                .FirstOrDefault(t => t != null);
            if (tipo != null) go.AddComponent(tipo);
        }

        private static bool AtribuirCampo(Component alvo, string campo, Object valor)
        {
            var so = new SerializedObject(alvo);
            var prop = so.FindProperty(campo);
            if (prop == null) return false;

            prop.objectReferenceValue = valor;
            so.ApplyModifiedPropertiesWithoutUndo();
            return true;
        }
    }
}

