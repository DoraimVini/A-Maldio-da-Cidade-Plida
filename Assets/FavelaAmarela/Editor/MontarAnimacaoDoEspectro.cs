using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using FavelaAmarela.Runtime.Enemies;

namespace FavelaAmarela.EditorTools
{
    /// <summary>
    /// Ferramenta de Editor. Liga a animação do <b>Espectro de Hali</b>: acrescenta o
    /// <see cref="AnimadorDoEspectro"/> ao prefab e preenche <c>idle</c>/<c>mover</c> com os
    /// quadros fatiados do spritesheet. Só dois ciclos — ver o porquê no XML doc do componente.
    ///
    /// <para><b>Corrige o import antes de ler:</b> <c>EspectroHali_Spritesheet_24x48.png</c>
    /// chegou a PPU 16 com pivô misto (algumas fatias <c>Center</c>, outras já
    /// <c>BottomCenter</c>) — o sprite único que o prefab usava antes
    /// (<c>EspectroHali_Idle.png</c>) está a PPU 32 com pivô no rodapé.</para>
    /// </summary>
    public static class MontarAnimacaoDoEspectro
    {
        private const string CaminhoPrefab = "Assets/FavelaAmarela/Art/Enemies/EspectroHali.prefab";
        private const string CaminhoFolha =
            "Assets/FavelaAmarela/Art/Enemies/EspectroHali_Spritesheet_24x48.png";

        private static readonly (string Campo, string Prefixo)[] Ciclos =
        {
            ("idle",  "espectro_idle_"),
            ("mover", "espectro_move_"),
        };

        [MenuItem("Tools/FavelaAmarela/Montar Animação do Espectro")]
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
                var animador = raiz.GetComponent<AnimadorDoEspectro>()
                               ?? raiz.AddComponent<AnimadorDoEspectro>();

                var so = new SerializedObject(animador);
                int total = 0;

                foreach (var (campo, prefixo) in Ciclos)
                {
                    if (!quadros.TryGetValue(prefixo, out var lista) || lista.Count == 0)
                    {
                        Debug.LogWarning($"[AnimacaoEspectro] Nenhum quadro com prefixo '{prefixo}'.");
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
                if (sr != null && quadros.TryGetValue("espectro_idle_", out var idle) && idle.Count > 0)
                    sr.sprite = idle[0];

                PrefabUtility.SaveAsPrefabAsset(raiz, CaminhoPrefab);
                Debug.Log($"[AnimacaoEspectro] Pronto — {total} quadro(s) ligados no prefab.");
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
                Debug.LogError($"[AnimacaoEspectro] Nenhum sprite fatiado em '{CaminhoFolha}'.");
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
