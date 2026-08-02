using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using FavelaAmarela.Level.Core;
using FavelaAmarela.Runtime.Combat;
using FavelaAmarela.Runtime.Enemies;
using FavelaAmarela.Runtime.GameLoop;

namespace FavelaAmarela.EditorTools
{
    /// <summary>
    /// Utilitário de Editor: povoa o clímax da Tumba de Alhazred na cena aberta —
    /// **Abdul Alhazred** e suas **Pedras de Poder** na arena (Zona 9), e o **Baú da
    /// Tumba** na Câmara do Baú (Zona 6b, logo após a entrada da dungeon).
    ///
    /// <para>As posições vêm do <see cref="LevelBlockoutPlanner"/>, não de coordenadas
    /// chutadas: se o layout mudar, rodar de novo recoloca tudo no lugar certo. Idempotente
    /// — remove o que criou antes de recriar.</para>
    /// </summary>
    public static class SetupArenaDoAbdul
    {
        private const string NomeRaiz = "TumbaDeAbdul_Conteudo";
        private const string CaminhoSpritesheet =
            "Assets/Sprites/Bosses/Alhazred/abdul_alhazred_spritesheet.png";
        private const string SpriteIdleDoAbdul = "abdul_transe_0";
        private const string CaminhoFichaAbdul = "Assets/FavelaAmarela/Config/Ficha_Abdul.asset";

        /// <summary>
        /// Onde a luta acontece. É o ponto onde ficava a Coisa do Cemitério — escolha do
        /// Vini (2026-07-29): a arena do boss é ali, não no centro geométrico da Zona 9.
        /// A Coisa foi removida da cena porque é imortal e mata no toque, o que tornaria
        /// a luta invencível.
        /// </summary>
        private static readonly Vector2 AncoraDaArena = new Vector2(39.41f, -16.2f);

        [MenuItem("Tools/FavelaAmarela/Setup Arena do Abdul (cena aberta)")]
        public static void Setup()
        {
            var cfg = new LevelBlockoutConfig();
            var layout = LevelBlockoutPlanner.BuildSPathLayout(cfg);

            var camara = layout.Rooms.FirstOrDefault(r => r.Name == "Zona6b_CamaraDoBau");

            if (camara.Name == null)
            {
                Debug.LogError("[SetupArena] Zona6b_CamaraDoBau não encontrada no layout. " +
                               "O planner mudou? Abortado.");
                return;
            }

            // ATENÇÃO: este tool NÃO recria mais tudo do zero. Antes ele destruía a raiz
            // inteira a cada execução, o que hoje apagaria o Abdul (que virou instância de
            // prefab) e todo o wiring de cena feito nele — inclusive as referências ao
            // Painel de Escolha, à caixa de diálogo e ao Yug-Neth. Agora só cria o que
            // ainda não existe.
            var raiz = GameObject.Find(NomeRaiz);
            if (raiz == null) raiz = new GameObject(NomeRaiz);

            var abdulExistente = Object.FindAnyObjectByType<AbdulAlhazredAI>(FindObjectsInactive.Include);
            if (abdulExistente == null)
                CriarAbdul(raiz.transform);

            // Pedras de Poder NÃO são mais plantadas aqui: elas nascem em runtime, quando
            // Abdul entra na Fase 1 (decisão do Vini, 2026-07-30 — são âncoras do ritual
            // dele, não cenário permanente da cripta). Ver 'Montar Pedras de Poder'.

            var bauExistente = Object.FindAnyObjectByType<BauDaTumba>(FindObjectsInactive.Include);
            if (bauExistente == null)
                CriarBau(raiz.transform, camara);

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Selection.activeGameObject = raiz;

            Debug.Log($"[SetupArena] Conteúdo garantido na arena {AncoraDaArena} " +
                      $"(Abdul {(abdulExistente != null ? "já existia" : "criado")}, " +
                      $"Baú {(bauExistente != null ? "já existia" : "criado")}). " +
                      "Pedras de Poder não são plantadas — nascem na Fase 1 da luta.");
        }

        // ── Abdul ────────────────────────────────────────────────────────────

        private static AbdulAlhazredAI CriarAbdul(Transform pai)
        {
            var go = new GameObject("Abdul_Alhazred");
            go.transform.SetParent(pai, false);
            go.transform.position = new Vector3(AncoraDaArena.x, AncoraDaArena.y, 0f);
            go.layer = LayerMask.NameToLayer("Enemy");

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = CarregarSprite(SpriteIdleDoAbdul);
            if (sr.sprite == null)
                Debug.LogWarning($"[SetupArena] Sprite '{SpriteIdleDoAbdul}' não encontrado — " +
                                 "o Abdul fica invisível até a folha ser fatiada.");

            var col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(1.2f, 1.2f);

            var ai = go.AddComponent<AbdulAlhazredAI>();
            AtribuirCampo(ai, "ficha", AssetDatabase.LoadAssetAtPath<FichaAtributosConfig>(CaminhoFichaAbdul));

            // Y-sorting dinâmico: sem isto o boss não é ocultado por paredes à frente.
            AdicionarYSortSeExistir(go);

            return ai;
        }

        // ── Baú ──────────────────────────────────────────────────────────────

        private static void CriarBau(Transform pai, RoomSpec camara)
        {
            var go = new GameObject("Bau_DaTumba");
            go.transform.SetParent(pai, false);
            go.transform.position = new Vector3(camara.Center.x, camara.Center.y, 0f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = SpriteQuadradoPlaceholder();
            sr.color = new Color(0.65f, 0.50f, 0.20f); // madeira, placeholder até a arte
            go.transform.localScale = new Vector3(0.9f, 0.7f, 1f);

            // Trigger: o baú abre ao Damião entrar (mesmo padrão do PatuaPickup).
            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(1.6f, 1.6f);

            var bau = go.AddComponent<BauDaTumba>();
            AtribuirCampo(bau, "spriteDoBau", sr);

            AdicionarYSortSeExistir(go);
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private static Sprite CarregarSprite(string nome)
            => AssetDatabase.LoadAllAssetsAtPath(CaminhoSpritesheet)
                .OfType<Sprite>()
                .FirstOrDefault(s => s.name == nome);

        /// <summary>
        /// Sprite branco 1×1 embutido da Unity, usado como placeholder até a arte real
        /// existir — evita objetos invisíveis na cena.
        /// </summary>
        private static Sprite SpriteQuadradoPlaceholder()
        {
            var sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            if (sprite == null)
                Debug.LogWarning("[SetupArena] Sprite placeholder built-in não encontrado.");
            return sprite;
        }

        /// <summary>
        /// Adiciona o <c>DynamicYSort</c> por reflexão de nome (o tipo vive noutro
        /// assembly; resolver por nome é o padrão dos outros builders desta pasta).
        /// </summary>
        private static void AdicionarYSortSeExistir(GameObject go)
        {
            var tipo = System.AppDomain.CurrentDomain.GetAssemblies()
                .Select(a => a.GetType("FavelaAmarela.Runtime.Rendering.DynamicYSort"))
                .FirstOrDefault(t => t != null);

            if (tipo != null) go.AddComponent(tipo);
        }

        private static void AtribuirCampo(Component alvo, string campo, Object valor)
        {
            var so = new SerializedObject(alvo);
            var prop = so.FindProperty(campo);
            if (prop == null)
            {
                Debug.LogWarning($"[SetupArena] Campo '{campo}' não existe em {alvo.GetType().Name}.");
                return;
            }
            prop.objectReferenceValue = valor;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
