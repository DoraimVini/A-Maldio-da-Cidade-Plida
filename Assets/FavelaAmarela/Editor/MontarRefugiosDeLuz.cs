using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using FavelaAmarela.Runtime.GameLoop;

namespace FavelaAmarela.EditorTools
{
    /// <summary>
    /// Ferramenta de Editor. Coloca os <b>Postes de Luz (Refúgios)</b> do Deserto de Hali
    /// nos três pontos que o design especifica (<c>level_design_deserto_hali.md</c> §4).
    ///
    /// <para>Até aqui o <see cref="RefugioDeLuz"/> existia só em código — nenhuma cena tinha
    /// um. Como ele é o <b>único ponto do jogo que grava o save</b>, sem isto a partida
    /// nunca era escrita em disco.</para>
    ///
    /// <para>Idempotente: reaproveita pelo nome e só reposiciona.</para>
    /// </summary>
    public static class MontarRefugiosDeLuz
    {
        private const string CenaDeserto = "Assets/Scenes/Deserto_Hali.unity";
        private const string NomeRaiz = "Refugios";
        private const string TagPontoDeLuz = "PontoDeLuz";

        /// <summary>Raio do volume de luz, em unidades. Generoso para não exigir mira.</summary>
        private const float RaioDaLuz = 1.8f;

        private static readonly (string nome, Vector2 pos, string porque)[] Refugios =
        {
            ("Refugio_Entrada", new Vector2(-12f, -11f),
                "Primeiro Refúgio — perto do ponto de chegada, ensina a mecânica de Resiliência"),

            ("Refugio_SantuarioDeYhtill", new Vector2(-13f, 9f),
                "Refúgio e ponto de save do Santuário (zona de calmaria sobrenatural)"),

            ("Refugio_PortoesDasRuinas", new Vector2(-2f, 13f),
                "Último Refúgio da Fase 1 — preparação para o Byakhee"),
        };

        [MenuItem("Tools/FavelaAmarela/Montar Refúgios de Luz do Deserto")]
        public static void Executar()
        {
            // Salva sem perguntar. `SaveCurrentModifiedScenesIfUserWantsTo` abre um
            // diálogo MODAL, e uma ferramenta disparada pela ponte MCP trava a Unity
            // inteira esperando um clique que ninguém vê.
            var cenaAtiva = EditorSceneManager.GetActiveScene();
            if (cenaAtiva.isDirty && !string.IsNullOrEmpty(cenaAtiva.path))
                EditorSceneManager.SaveScene(cenaAtiva);

            string cenaOriginal = cenaAtiva.path;
            var cena = EditorSceneManager.OpenScene(CenaDeserto, OpenSceneMode.Single);

            var raiz = GameObject.Find(NomeRaiz);
            if (raiz == null)
            {
                raiz = new GameObject(NomeRaiz);
                Undo.RegisterCreatedObjectUndo(raiz, "Criar raiz dos Refúgios");
            }

            bool temTag = TagExiste(TagPontoDeLuz);
            if (!temTag)
            {
                Debug.LogWarning($"[Refúgios] A tag '{TagPontoDeLuz}' não existe no projeto — " +
                                 "os Refúgios ficam sem tag. Funciona, mas quem quiser achá-los " +
                                 "por tag no futuro não vai conseguir.");
            }

            foreach (var (nome, pos, porque) in Refugios)
                Montar(nome, pos, porque, raiz.transform, temTag);

            EditorSceneManager.MarkSceneDirty(cena);
            EditorSceneManager.SaveScene(cena);

            if (!string.IsNullOrEmpty(cenaOriginal) && cenaOriginal != CenaDeserto)
                EditorSceneManager.OpenScene(cenaOriginal, OpenSceneMode.Single);

            Debug.Log($"[Refúgios] {Refugios.Length} Postes de Luz montados no Deserto — " +
                      "são também os pontos de save da Fase 1.");
        }

        private static void Montar(string nome, Vector2 pos, string porque, Transform raiz, bool temTag)
        {
            var t = raiz.Find(nome);
            GameObject go;
            if (t != null)
            {
                go = t.gameObject;
            }
            else
            {
                go = new GameObject(nome);
                Undo.RegisterCreatedObjectUndo(go, "Criar Refúgio de Luz");
                go.transform.SetParent(raiz, false);
            }

            go.transform.position = pos;
            if (temTag) go.tag = TagPontoDeLuz;

            // Círculo, não retângulo: a luz de um poste é radial, e o volume deve casar com
            // o que o jogador enxerga para não parecer que "não pegou" ao entrar de canto.
            var col = go.GetComponent<CircleCollider2D>();
            if (col == null) col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = RaioDaLuz;

            if (go.GetComponent<RefugioDeLuz>() == null) go.AddComponent<RefugioDeLuz>();

            EditorUtility.SetDirty(go);
            Debug.Log($"[Refúgios] {nome} em {pos} — {porque}", go);
        }

        private static bool TagExiste(string tag)
        {
            foreach (var t in UnityEditorInternal.InternalEditorUtility.tags)
                if (t == tag) return true;

            return false;
        }
    }
}
