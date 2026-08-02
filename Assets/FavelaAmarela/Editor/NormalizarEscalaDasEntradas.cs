using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace FavelaAmarela.EditorTools
{
    /// <summary>
    /// Ferramenta de Editor. Zera a <b>escala residual</b> dos marcos de entrada do Deserto.
    ///
    /// <para><b>O bug (achado pelo Vini em playtest):</b> uma "bolha preta" gigante cobrindo
    /// meio mapa — era o sprite do Lago Negro em escala <c>5,2 × 6,2</c>, ou seja ~21 × 18
    /// unidades. Os marcos tinham sido escalados enquanto eram quadrados coloridos de
    /// placeholder, cada um representando a área que ocupava. Ao trocar pelos sprites de
    /// verdade eu <b>resetei o tint mas esqueci a escala</b>, e a arte foi esticada junto.</para>
    ///
    /// <para>Com escala 1, cada entrada volta a medir os <b>4 unidades</b> em que a arte foi
    /// autorada — o tamanho que o Vini aprovou. Qualquer ajuste de tamanho daqui em diante
    /// deve ser feito <b>regerando o sprite</b>, não escalando o transform: escalar um sprite
    /// de pixel art quebra a proporção de pixel com o resto do jogo.</para>
    /// </summary>
    public static class NormalizarEscalaDasEntradas
    {
        private const string CenaDeserto = "Assets/Scenes/Deserto_Hali.unity";

        private static readonly string[] Marcos =
        {
            "Entrada_TumbaAlhazred",
            "Santuario_Yhtill",
            "Lago_De_Hali",
            "Entrada_TemploSerpente",
            "Portoes_DasRuinas",
        };

        [MenuItem("Tools/FavelaAmarela/Normalizar escala das entradas")]
        public static void Executar()
        {
            var cenaAtiva = EditorSceneManager.GetActiveScene();
            if (cenaAtiva.isDirty && !string.IsNullOrEmpty(cenaAtiva.path))
                EditorSceneManager.SaveScene(cenaAtiva);

            string cenaOriginal = cenaAtiva.path;
            var cena = EditorSceneManager.OpenScene(CenaDeserto, OpenSceneMode.Single);

            int corrigidos = 0;
            foreach (var nome in Marcos)
            {
                var go = GameObject.Find(nome);
                if (go == null)
                {
                    Debug.LogWarning($"[Escala] '{nome}' não achado na cena.");
                    continue;
                }

                var antes = go.transform.localScale;
                if (Mathf.Approximately(antes.x, 1f) && Mathf.Approximately(antes.y, 1f)) continue;

                Undo.RecordObject(go.transform, "Normalizar escala da entrada");
                go.transform.localScale = Vector3.one;
                EditorUtility.SetDirty(go);
                corrigidos++;

                var sr = go.GetComponent<SpriteRenderer>();
                var tam = sr != null && sr.sprite != null ? sr.sprite.bounds.size : Vector3.zero;
                Debug.Log($"[Escala] {nome}: {antes.x:0.##}×{antes.y:0.##} → 1×1 " +
                          $"(agora {tam.x:0.#}×{tam.y:0.#} unidades)", go);
            }

            if (corrigidos > 0)
            {
                EditorSceneManager.MarkSceneDirty(cena);
                EditorSceneManager.SaveScene(cena);
            }

            if (!string.IsNullOrEmpty(cenaOriginal) && cenaOriginal != CenaDeserto)
                EditorSceneManager.OpenScene(cenaOriginal, OpenSceneMode.Single);

            Debug.Log($"[Escala] {corrigidos} marco(s) normalizados.");
        }
    }
}
