using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using FavelaAmarela.Runtime.Enemies;

namespace FavelaAmarela.EditorTools
{
    /// <summary>
    /// Ferramenta de Editor. Liga a animação do <b>Cultista</b>: acrescenta o
    /// <see cref="AnimadorDoCultista"/> ao prefab e preenche os quatro ciclos com os quadros
    /// fatiados do spritesheet. Mesmo molde de <c>MontarAnimacaoDoByakhee</c>.
    ///
    /// <para><b>Corrige o import antes de ler:</b> <c>Cultista_Spritesheet_16x32.png</c> chegou
    /// fatiada a <b>PPU 16</b> com pivô <c>Center</c> — o sprite único que o prefab usava antes
    /// (<c>Cultista_Idle.png</c>) está a PPU 32 com pivô no rodapé. Sem corrigir, o Cultista
    /// dobraria de tamanho e flutuaria acima do chão.</para>
    ///
    /// <para>Idempotente: reaproveita o componente se já existir.</para>
    /// </summary>
    public static class MontarAnimacaoDoCultista
    {
        private const string CaminhoPrefab = "Assets/FavelaAmarela/Art/Enemies/Cultista.prefab";

        // A folha animada NÃO está em Assets/FavelaAmarela/Art/Enemies/ (onde o resto da arte de
        // inimigo mora) — ficou numa pasta legada, Assets/Sprites/Cultistas/. Conferido no disco
        // depois de a primeira tentativa falhar com "Textura não encontrada" e o marcador de log
        // da ferramenta nunca aparecer — silêncio total, nem warning.
        private const string CaminhoFolha =
            "Assets/Sprites/Cultistas/Cultista_Spritesheet_16x32.png";

        private static readonly (string Campo, string Prefixo)[] Ciclos =
        {
            ("idle",   "cultista_idle_"),
            ("walk",   "cultista_walk_"),
            ("attack", "cultista_attack_"),
            ("death",  "cultista_death_"),
        };

        [MenuItem("Tools/FavelaAmarela/Montar Animação do Cultista")]
        public static void Executar()
        {
            if (!MontadorDeAnimacao.AjustarImportDeFolhaFatiada(CaminhoFolha, 32f,
                                                                 SpriteAlignment.BottomCenter))
                return;

            var quadros = CarregarQuadros();
            if (quadros == null) return;

            var raiz = PrefabUtility.LoadPrefabContents(CaminhoPrefab);
            try
            {
                var animador = raiz.GetComponent<AnimadorDoCultista>()
                               ?? raiz.AddComponent<AnimadorDoCultista>();

                var so = new SerializedObject(animador);
                int total = 0;

                foreach (var (campo, prefixo) in Ciclos)
                {
                    if (!quadros.TryGetValue(prefixo, out var lista) || lista.Count == 0)
                    {
                        Debug.LogWarning($"[AnimacaoCultista] Nenhum quadro com prefixo '{prefixo}'.");
                        continue;
                    }

                    var prop = so.FindProperty(campo);
                    prop.arraySize = lista.Count;
                    for (int i = 0; i < lista.Count; i++)
                        prop.GetArrayElementAtIndex(i).objectReferenceValue = lista[i];

                    total += lista.Count;
                }

                so.ApplyModifiedPropertiesWithoutUndo();

                var sr = raiz.GetComponent<SpriteRenderer>();
                if (sr != null && quadros.TryGetValue("cultista_idle_", out var idle) && idle.Count > 0)
                    sr.sprite = idle[0];

                PrefabUtility.SaveAsPrefabAsset(raiz, CaminhoPrefab);
                Debug.Log($"[AnimacaoCultista] Pronto — {total} quadro(s) ligados no prefab.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(raiz);
            }
        }

        private static Dictionary<string, List<Sprite>> CarregarQuadros()
        {
            var todos = AssetDatabase.LoadAllAssetsAtPath(CaminhoFolha).OfType<Sprite>().ToList();
            if (todos.Count == 0)
            {
                Debug.LogError($"[AnimacaoCultista] Nenhum sprite fatiado em '{CaminhoFolha}'.");
                return null;
            }

            var porCiclo = new Dictionary<string, List<Sprite>>();
            foreach (var (_, prefixo) in Ciclos)
            {
                porCiclo[prefixo] = todos
                    .Where(s => s.name.StartsWith(prefixo))
                    .OrderBy(s => IndiceDe(s.name, prefixo))
                    .ToList();
            }
            return porCiclo;
        }

        private static int IndiceDe(string nome, string prefixo)
            => int.TryParse(nome.Substring(prefixo.Length), out int n) ? n : 0;
    }
}
