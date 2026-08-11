using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using FavelaAmarela.Core.Persistencia;
using FavelaAmarela.Inventario;
using FavelaAmarela.Runtime.Enemies;
using FavelaAmarela.Runtime.GameLoop;
using FavelaAmarela.Runtime.Itens;
using FavelaAmarela.Runtime.UI;

namespace FavelaAmarela.EditorTools
{
    /// <summary>
    /// Utilitário de Editor: cria os prefabs que faltavam para a luta do Abdul rodar de
    /// ponta a ponta — <b>Pedra de Poder</b>, <b>Esqueleto Invocado</b>, <b>Cone de Gelo</b>,
    /// <b>Necronomicon</b> e o <b>visual do Escudo Mágico</b> — e liga todos nos campos do
    /// <see cref="AbdulAlhazredAI"/> da cena aberta.
    ///
    /// <para>Todos usam <b>placeholder colorido</b> até a arte real existir: o adaptador do
    /// Abdul tolera prefabs ausentes (a luta roda sem summons), mas com eles a luta fica
    /// completa e testável em Play.</para>
    ///
    /// <para>Idempotente: reaproveita prefabs já existentes e não sobrescreve campos que já
    /// estiverem preenchidos.</para>
    /// </summary>
    public static class MontarPrefabsDaLutaDoAbdul
    {
        private const string Pasta = "Assets/FavelaAmarela/Art/Enemies/";
        private const string CaminhoPedra = Pasta + "PedraDePoder.prefab";
        private const string CaminhoEsqueleto = Pasta + "EsqueletoInvocado.prefab";
        private const string CaminhoCone = Pasta + "ConeDeGelo.prefab";
        private const string CaminhoNecronomicon = "Assets/FavelaAmarela/Art/Items/Necronomicon.prefab";
        private const string CaminhoItemNecronomicon =
            "Assets/FavelaAmarela/Config/Resources/Itens/Item_Necronomicon.asset";

        [MenuItem("Tools/FavelaAmarela/Montar Prefabs da Luta do Abdul")]
        public static void Montar()
        {
            var abdul = Object.FindAnyObjectByType<AbdulAlhazredAI>(FindObjectsInactive.Include);
            if (abdul == null)
            {
                Debug.LogError("[PrefabsLuta] Nenhum AbdulAlhazredAI na cena aberta — abortado.");
                return;
            }

            var pedra = CriarPedraDePoder();
            var esqueleto = CriarEsqueleto();
            var cone = CriarConeDeGelo();
            var necronomicon = CriarNecronomicon();
            var escudo = CriarVisualDoEscudo(abdul);

            int ligados = 0;
            ligados += AtribuirSeVazio(abdul, "prefabPedraDePoder", pedra);
            ligados += AtribuirSeVazio(abdul, "prefabEsqueleto", esqueleto);
            ligados += AtribuirSeVazio(abdul, "prefabConeDeGelo", cone);
            ligados += AtribuirSeVazio(abdul, "prefabNecronomicon", necronomicon);
            ligados += AtribuirSeVazio(abdul, "visualDoEscudo", escudo);

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

            Debug.Log($"[PrefabsLuta] Prefabs criados/reaproveitados e {ligados} campo(s) do Abdul " +
                      "ligados (Pedra, Esqueleto, Cone de Gelo, Necronomicon, Escudo). " +
                      "Todos com arte placeholder. Cena NÃO foi salva — confira antes.");
        }

        // ── Prefabs ─────────────────────────────────────────────────────────

        private static GameObject CriarPedraDePoder()
        {
            var existente = AssetDatabase.LoadAssetAtPath<GameObject>(CaminhoPedra);
            if (existente != null) return existente;

            var go = new GameObject("PedraDePoder",
                typeof(SpriteRenderer), typeof(BoxCollider2D), typeof(PedraDePoder));
            go.layer = LayerMask.NameToLayer("Enemy"); // alvo das armas do jogador

            var sr = go.GetComponent<SpriteRenderer>();
            sr.sprite = Placeholder();
            sr.color = new Color(0.45f, 0.35f, 0.75f); // roxo-anômalo
            go.transform.localScale = new Vector3(0.7f, 0.9f, 1f);

            go.GetComponent<BoxCollider2D>().size = Vector2.one;
            AdicionarYSort(go);

            return Salvar(go, CaminhoPedra);
        }

        private static GameObject CriarEsqueleto()
        {
            var existente = AssetDatabase.LoadAssetAtPath<GameObject>(CaminhoEsqueleto);
            if (existente != null) return existente;

            var go = new GameObject("EsqueletoInvocado",
                typeof(SpriteRenderer), typeof(Rigidbody2D), typeof(BoxCollider2D),
                typeof(EsqueletoInvocado));
            go.layer = LayerMask.NameToLayer("Enemy");

            var rb = go.GetComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.freezeRotation = true;

            var sr = go.GetComponent<SpriteRenderer>();
            sr.sprite = Placeholder();
            sr.color = new Color(0.88f, 0.87f, 0.8f); // osso
            go.transform.localScale = new Vector3(0.5f, 0.8f, 1f);

            go.GetComponent<BoxCollider2D>().size = Vector2.one;
            AdicionarYSort(go);

            return Salvar(go, CaminhoEsqueleto);
        }

        private static GameObject CriarConeDeGelo()
        {
            var existente = AssetDatabase.LoadAssetAtPath<GameObject>(CaminhoCone);
            if (existente != null) return existente;

            var go = new GameObject("ConeDeGelo",
                typeof(SpriteRenderer), typeof(Rigidbody2D), typeof(BoxCollider2D),
                typeof(ConeDeGelo));

            var rb = go.GetComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            var col = go.GetComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = Vector2.one;

            var sr = go.GetComponent<SpriteRenderer>();
            sr.sprite = Placeholder();
            sr.color = new Color(0.6f, 0.85f, 1f); // gelo
            go.transform.localScale = new Vector3(0.6f, 0.3f, 1f);

            return Salvar(go, CaminhoCone);
        }

        private static GameObject CriarNecronomicon()
        {
            var existente = AssetDatabase.LoadAssetAtPath<GameObject>(CaminhoNecronomicon);
            if (existente != null) return existente;

            var go = new GameObject("Necronomicon",
                typeof(SpriteRenderer), typeof(BoxCollider2D), typeof(ColetavelDeItem));

            var col = go.GetComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(1.4f, 1.4f);

            var sr = go.GetComponent<SpriteRenderer>();
            sr.sprite = Placeholder();
            sr.color = new Color(0.35f, 0.25f, 0.15f); // couro escuro
            go.transform.localScale = new Vector3(0.6f, 0.75f, 1f);

            // Reaproveita a caixa de texto do tutorial, como os outros pickups.
            var caixa = Object.FindAnyObjectByType<TutorialHintUI>(FindObjectsInactive.Include);
            if (caixa != null)
                AtribuirCampo(go.GetComponent<ColetavelDeItem>(), "caixaDeTexto", caixa);

            var coletavel = go.GetComponent<ColetavelDeItem>();
            AtribuirCampoString(coletavel, "chaveDeSave", ChavesDeSave.NecronomiconColetado);
            var def = AssetDatabase.LoadAssetAtPath<ItemDef>(CaminhoItemNecronomicon);
            if (def != null)
                AtribuirCampo(coletavel, "item", def);
            else
                Debug.LogError($"[Abdul] ItemDef do Necronomicon não encontrado em " +
                               $"'{CaminhoItemNecronomicon}' — o drop não entregará nada.");

            AdicionarYSort(go);

            Directory.CreateDirectory(Path.GetDirectoryName(CaminhoNecronomicon)!);
            return Salvar(go, CaminhoNecronomicon);
        }

        /// <summary>
        /// Cria o visual do Escudo Mágico como <b>filho do Abdul</b> (não é prefab: é um
        /// objeto de cena que a FSM liga/desliga). Um círculo azulado semitransparente
        /// atrás dele, para o jogador ler de longe quando o dano entra ou não.
        /// </summary>
        private static GameObject CriarVisualDoEscudo(AbdulAlhazredAI abdul)
        {
            var existente = abdul.transform.Find("VisualDoEscudo");
            if (existente != null) return existente.gameObject;

            var go = new GameObject("VisualDoEscudo", typeof(SpriteRenderer));
            go.transform.SetParent(abdul.transform, false);
            go.transform.localPosition = new Vector3(0f, 0.6f, 0f);
            go.transform.localScale = new Vector3(2.4f, 2.4f, 1f);

            var sr = go.GetComponent<SpriteRenderer>();
            sr.sprite = Placeholder();
            sr.color = new Color(0.55f, 0.75f, 1f, 0.35f);
            sr.sortingOrder = -1; // atrás do Abdul

            return go;
        }

        // ── Helpers ─────────────────────────────────────────────────────────

        private static GameObject Salvar(GameObject go, string caminho)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(caminho)!);
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, caminho);
            Object.DestroyImmediate(go);
            return prefab;
        }

        private static Sprite Placeholder()
            => AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

        private static void AdicionarYSort(GameObject go)
        {
            var tipo = System.AppDomain.CurrentDomain.GetAssemblies()
                .Select(a => a.GetType("FavelaAmarela.Runtime.Rendering.DynamicYSort"))
                .FirstOrDefault(t => t != null);
            if (tipo != null) go.AddComponent(tipo);
        }

        private static int AtribuirSeVazio(Component alvo, string campo, Object valor)
        {
            var so = new SerializedObject(alvo);
            var prop = so.FindProperty(campo);
            if (prop == null)
            {
                Debug.LogWarning($"[PrefabsLuta] Campo '{campo}' não encontrado em {alvo.GetType().Name}.");
                return 0;
            }
            if (prop.objectReferenceValue != null) return 0;

            prop.objectReferenceValue = valor;
            so.ApplyModifiedPropertiesWithoutUndo();
            return 1;
        }

        private static void AtribuirCampo(Component alvo, string campo, Object valor)
        {
            var so = new SerializedObject(alvo);
            var prop = so.FindProperty(campo);
            if (prop == null) return;
            prop.objectReferenceValue = valor;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AtribuirCampoString(Component alvo, string campo, string valor)
        {
            var so = new SerializedObject(alvo);
            var prop = so.FindProperty(campo);
            if (prop == null) return;
            prop.stringValue = valor;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
