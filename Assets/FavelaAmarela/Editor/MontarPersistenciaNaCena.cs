using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using FavelaAmarela.Runtime.Persistencia;

namespace FavelaAmarela.EditorTools
{
    /// <summary>
    /// Ferramenta de Editor. Instala a persistência na cena aberta:
    /// <list type="number">
    ///   <item>Um <see cref="GerenciadorDeSave"/> num objeto próprio (<c>DontDestroyOnLoad</c>,
    ///   sobrevive à troca de cena).</item>
    ///   <item>Um <see cref="EstadoPersistenteDoJogador"/> em Damião, que é o que faz a arma
    ///   do baú e a Vitalidade atravessarem a porta da dungeon.</item>
    /// </list>
    ///
    /// <para><b>Sem isto o sistema não faz nada:</b> as classes existem, mas
    /// <c>CapturarTudo()</c> não tem quem chamar se não houver um gerenciador vivo na cena.
    /// Rode em <b>toda</b> cena jogável (Tumba e Deserto).</para>
    ///
    /// <para>Idempotente: reaproveita o que já existir.</para>
    /// </summary>
    public static class MontarPersistenciaNaCena
    {
        private const string NomeDoObjeto = "GerenciadorDeSave";

        private static readonly string[] CenasJogaveis =
        {
            "Assets/Scenes/Tumba_De_Alhazred.unity",
            "Assets/Scenes/Deserto_Hali.unity",
        };

        /// <summary>
        /// Instala a persistência em <b>todas</b> as cenas jogáveis. Necessário porque a
        /// ida e a volta usam cenas diferentes: capturar o estado na saída da Tumba não
        /// serve de nada se o Damião do Deserto não tiver quem reaplique na chegada.
        /// </summary>
        [MenuItem("Tools/FavelaAmarela/Montar persistência em TODAS as cenas jogáveis")]
        public static void ExecutarEmTodasAsCenas()
        {
            // Salva sem perguntar. `SaveCurrentModifiedScenesIfUserWantsTo` abre um
            // diálogo MODAL, e uma ferramenta disparada pela ponte MCP trava a Unity
            // inteira esperando um clique que ninguém vê.
            var cenaAtiva = EditorSceneManager.GetActiveScene();
            if (cenaAtiva.isDirty && !string.IsNullOrEmpty(cenaAtiva.path))
                EditorSceneManager.SaveScene(cenaAtiva);

            string cenaOriginal = cenaAtiva.path;

            foreach (var caminho in CenasJogaveis)
            {
                EditorSceneManager.OpenScene(caminho, OpenSceneMode.Single);
                Executar();
            }

            if (!string.IsNullOrEmpty(cenaOriginal))
                EditorSceneManager.OpenScene(cenaOriginal, OpenSceneMode.Single);

            Debug.Log("[MontarPersistencia] Todas as cenas jogáveis processadas.");
        }

        [MenuItem("Tools/FavelaAmarela/Montar persistência na cena")]
        public static void Executar()
        {
            int mudancas = 0;
            mudancas += InstalarGerenciador() ? 1 : 0;
            mudancas += InstalarEstadoDoJogador() ? 1 : 0;

            if (mudancas == 0)
            {
                Debug.Log("[MontarPersistencia] Cena já estava completa — nada a fazer.");
                return;
            }

            var cena = EditorSceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(cena);
            EditorSceneManager.SaveScene(cena);
            Debug.Log($"[MontarPersistencia] Pronto ({mudancas} adição/ões) e cena '{cena.name}' salva.");
        }

        private static bool InstalarGerenciador()
        {
            if (Object.FindAnyObjectByType<GerenciadorDeSave>(FindObjectsInactive.Include) != null)
                return false;

            var go = new GameObject(NomeDoObjeto);
            Undo.RegisterCreatedObjectUndo(go, "Criar GerenciadorDeSave");
            go.AddComponent<GerenciadorDeSave>();

            Debug.Log("[MontarPersistencia] GerenciadorDeSave criado.", go);
            return true;
        }

        private static bool InstalarEstadoDoJogador()
        {
            var jogador = GameObject.FindGameObjectWithTag("Player");
            if (jogador == null)
            {
                Debug.LogWarning("[MontarPersistencia] Nenhum objeto com a tag Player nesta cena — " +
                                 "a arma e a Vitalidade não serão persistidas aqui.");
                return false;
            }

            if (jogador.GetComponent<EstadoPersistenteDoJogador>() != null) return false;

            Undo.AddComponent<EstadoPersistenteDoJogador>(jogador);
            EditorUtility.SetDirty(jogador);

            Debug.Log($"[MontarPersistencia] EstadoPersistenteDoJogador adicionado a '{jogador.name}'.", jogador);
            return true;
        }
    }
}
