using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using FavelaAmarela.Level.Core;
using FavelaAmarela.Runtime.Combat;
using FavelaAmarela.Core.Combat;
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
        private const string CaminhoPrefabDoAbdul =
            "Assets/FavelaAmarela/Art/Enemies/Abdul_Alhazred.prefab";
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

        /// <summary>
        /// Instancia o <b>prefab</b> do Abdul na âncora da arena.
        ///
        /// <para><b>Ela montava um Abdul à mão até 2026-08-28</b> — <c>new GameObject</c> +
        /// <c>SpriteRenderer</c> + <c>BoxCollider2D</c> 1,2×1,2 + <c>AbdulAlhazredAI</c> —, com o
        /// sprite vindo de <c>abdul_alhazred_spritesheet.png</c>. Era uma <b>segunda fonte de
        /// verdade</b> para o boss, e ficou pior que a primeira a cada coisa que o prefab
        /// ganhou: Hurtbox de 2,54×1,29 (sem ela o golpe do jogador não o alcança),
        /// <c>CorpoImpregnado</c>, o animator do Mage, a ficha.</para>
        ///
        /// <para>E a folha que ela lia era a arte de IA <b>opaca</b>, substituída em
        /// <c>LigarAnimacaoDoAbdul</c> e apagada a pedido do Vini. Rodar esta ferramenta
        /// restauraria o boss quadrado e claro.</para>
        /// </summary>
        private static AbdulAlhazredAI CriarAbdul(Transform pai)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CaminhoPrefabDoAbdul);
            if (prefab == null)
            {
                Debug.LogError($"[SetupArena] Prefab do Abdul não encontrado em " +
                               $"'{CaminhoPrefabDoAbdul}'. A arena fica sem boss — montar um à " +
                               "mão aqui seria criar uma segunda versão dele, que é o que esta " +
                               "ferramenta parou de fazer.");
                return null;
            }

            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, pai);
            go.name = "Abdul_Alhazred";
            go.transform.position = new Vector3(AncoraDaArena.x, AncoraDaArena.y, 0f);

            var ai = go.GetComponent<AbdulAlhazredAI>();
            if (ai == null)
                Debug.LogError("[SetupArena] O prefab do Abdul não tem AbdulAlhazredAI.");

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

            // Trigger: o baú abre ao Damião entrar (mesmo padrão do ColetavelDeItem).
            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(1.6f, 1.6f);

            var bau = go.AddComponent<BauDaTumba>();
            AtribuirCampo(bau, "spriteDoBau", sr);

            AdicionarYSortSeExistir(go);
        }

        // ── Helpers ──────────────────────────────────────────────────────────

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
