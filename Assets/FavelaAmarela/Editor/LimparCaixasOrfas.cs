using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using FavelaAmarela.Runtime.UI;

namespace FavelaAmarela.EditorTools
{
    /// <summary>
    /// Ferramenta de Editor. Remove <b>caixas de diálogo órfãs</b> — painéis pretos sem o
    /// componente <see cref="TutorialHintUI"/>.
    ///
    /// <para><b>Como surgiram:</b> a ferramenta que monta a caixa criava o painel e só
    /// depois adicionava o componente. Em 2026-08-01 ela quebrou no meio, entre um passo e
    /// outro, por causa de <c>Arial.ttf</c> (que <b>lança exceção</b> na Unity 6). O painel
    /// ficava; o componente não. Na execução seguinte ela procurava um <c>TutorialHintUI</c>,
    /// não achava, e criava <b>outro</b> painel. Quatro tentativas, quatro órfãs.</para>
    ///
    /// <para>Sem o componente ninguém zera o alpha delas, então ficavam pretas e permanentes
    /// na tela — a "mancha preta" relatada pelo Vini, que só sumia ao trocar de mapa (porque
    /// pertenciam à cena).</para>
    ///
    /// <para><b>Lição embutida:</b> ferramenta de Editor que monta objeto em várias etapas
    /// precisa ser idempotente <i>por etapa</i> — reconhecer trabalho parcial e completá-lo,
    /// em vez de só perguntar "já existe o produto final?".</para>
    /// </summary>
    public static class LimparCaixasOrfas
    {
        private const string NomeDaCaixa = "CaixaDeDialogo";

        private static readonly string[] Cenas =
        {
            "Assets/Scenes/Deserto_Hali.unity",
            "Assets/Scenes/Playtest_RuinasPalidas.unity",
            "Assets/Scenes/Santuario_Yhtill.unity",
        };

        [MenuItem("Tools/FavelaAmarela/Limpar caixas de dialogo orfas")]
        public static void Executar()
        {
            var cenaAtiva = EditorSceneManager.GetActiveScene();
            if (cenaAtiva.isDirty && !string.IsNullOrEmpty(cenaAtiva.path))
                EditorSceneManager.SaveScene(cenaAtiva);

            string cenaOriginal = cenaAtiva.path;
            int totalRemovidas = 0;

            foreach (var caminho in Cenas)
            {
                if (!System.IO.File.Exists(caminho)) continue;

                var cena = EditorSceneManager.OpenScene(caminho, OpenSceneMode.Single);
                int removidas = LimparCena();

                if (removidas > 0)
                {
                    EditorSceneManager.MarkSceneDirty(cena);
                    EditorSceneManager.SaveScene(cena);
                    totalRemovidas += removidas;
                    Debug.Log($"[Órfãs] '{cena.name}': {removidas} painel(éis) preto(s) removido(s).");
                }
            }

            if (!string.IsNullOrEmpty(cenaOriginal))
                EditorSceneManager.OpenScene(cenaOriginal, OpenSceneMode.Single);

            Debug.Log($"[Órfãs] Pronto — {totalRemovidas} caixa(s) órfã(s) removida(s) ao todo.");
        }

        private static int LimparCena()
        {
            // Include inativos: uma órfã desativada continuaria no arquivo e voltaria a
            // confundir a próxima investigação.
            var todas = Object.FindObjectsByType<RectTransform>(
                FindObjectsInactive.Include);

            int removidas = 0;
            foreach (var rt in todas)
            {
                if (rt == null || rt.name != NomeDaCaixa) continue;

                // A boa é a que tem o componente. As outras são restos de execução parcial.
                if (rt.GetComponent<TutorialHintUI>() != null) continue;

                Debug.Log($"[Órfãs] Removendo '{rt.name}' (sem TutorialHintUI).", rt.gameObject);
                Object.DestroyImmediate(rt.gameObject);
                removidas++;
            }

            return removidas;
        }
    }
}

