using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using FavelaAmarela.Runtime.Enemies;

namespace FavelaAmarela.EditorTools
{
    /// <summary>
    /// Ferramenta de Editor. Liga a animação do <b>Byakhee</b>: acrescenta o
    /// <see cref="AnimadorDoByakhee"/> ao prefab e preenche os seis ciclos com os quadros
    /// fatiados do spritesheet.
    ///
    /// <para><b>Por que os quadros vêm por nome:</b> o <c>.aseprite</c> foi fatiado com tags,
    /// então cada sprite já se chama <c>byakhee_&lt;estado&gt;_&lt;n&gt;</c>. Agrupar por
    /// prefixo e ordenar pelo índice reconstrói cada ciclo sem ninguém arrastar sprite à mão no
    /// Inspector — e sobrevive a um refatiamento, desde que os nomes se mantenham.</para>
    ///
    /// <para>Idempotente: reaproveita o componente se já existir.</para>
    /// </summary>
    public static class MontarAnimacaoDoByakhee
    {
        private const string CaminhoPrefab = "Assets/FavelaAmarela/Art/Enemies/Byakhee.prefab";
        private const string CaminhoFolha = "Assets/FavelaAmarela/Art/Enemies/Byakhee_Spritesheet.png";

        private static readonly (string Campo, string Prefixo)[] Ciclos =
        {
            ("espreita", "byakhee_espreita_"),
            ("rasante",  "byakhee_rasante_"),
            ("garras",   "byakhee_garras_"),
            ("grito",    "byakhee_grito_"),
            ("dano",     "byakhee_dano_"),
            ("derrota",  "byakhee_derrota_"),
        };

        [MenuItem("Tools/FavelaAmarela/Montar Animação do Byakhee")]
        public static void Executar()
        {
            var quadros = CarregarQuadros();
            if (quadros == null) return;

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CaminhoPrefab);
            if (prefab == null)
            {
                Debug.LogError($"[AnimacaoByakhee] Prefab não encontrado em '{CaminhoPrefab}'.");
                return;
            }

            var raiz = PrefabUtility.LoadPrefabContents(CaminhoPrefab);
            try
            {
                var animador = raiz.GetComponent<AnimadorDoByakhee>()
                               ?? raiz.AddComponent<AnimadorDoByakhee>();

                var so = new SerializedObject(animador);
                int total = 0;

                foreach (var (campo, prefixo) in Ciclos)
                {
                    if (!quadros.TryGetValue(prefixo, out var lista) || lista.Count == 0)
                    {
                        Debug.LogWarning($"[AnimacaoByakhee] Nenhum quadro com prefixo '{prefixo}'.");
                        continue;
                    }

                    var prop = so.FindProperty(campo);
                    prop.arraySize = lista.Count;
                    for (int i = 0; i < lista.Count; i++)
                        prop.GetArrayElementAtIndex(i).objectReferenceValue = lista[i];

                    total += lista.Count;
                    Debug.Log($"[AnimacaoByakhee] '{campo}': {lista.Count} quadro(s).");
                }

                so.ApplyModifiedPropertiesWithoutUndo();

                // Arte real não se tinge — o ByakheeAI pintava o sprite por estado enquanto era
                // placeholder. Deixa o primeiro quadro visível no Inspector.
                var sr = raiz.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    sr.color = Color.white;
                    if (quadros.TryGetValue("byakhee_espreita_", out var idle) && idle.Count > 0)
                        sr.sprite = idle[0];
                }

                PrefabUtility.SaveAsPrefabAsset(raiz, CaminhoPrefab);
                Debug.Log($"[AnimacaoByakhee] Pronto — {total} quadro(s) ligados no prefab.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(raiz);
            }
        }

        /// <summary>
        /// Sprites da folha agrupados por prefixo de ciclo e ordenados pelo índice numérico do
        /// nome — ordenação por string colocaria <c>_10</c> antes de <c>_2</c>.
        /// </summary>
        private static Dictionary<string, List<Sprite>> CarregarQuadros()
        {
            var todos = AssetDatabase.LoadAllAssetsAtPath(CaminhoFolha).OfType<Sprite>().ToList();
            if (todos.Count == 0)
            {
                Debug.LogError($"[AnimacaoByakhee] Nenhum sprite fatiado em '{CaminhoFolha}'. " +
                               "Rode 'Slice Spritesheet Byakhee' antes.");
                return null;
            }

            var porCiclo = new Dictionary<string, List<Sprite>>();
            foreach (var (_, prefixo) in Ciclos)
            {
                var lista = todos
                    .Where(s => s.name.StartsWith(prefixo))
                    .OrderBy(s => IndiceDe(s.name, prefixo))
                    .ToList();

                porCiclo[prefixo] = lista;
            }

            return porCiclo;
        }

        private static int IndiceDe(string nome, string prefixo)
            => int.TryParse(nome.Substring(prefixo.Length), out int n) ? n : 0;
    }
}
