using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace FavelaAmarela.EditorTools
{
    /// <summary>
    /// Preenche o chão do Deserto <b>varrendo o MUNDO</b>, e não o espaço de célula.
    ///
    /// <para><b>O erro que isto conserta, e que eu mesmo cometi (2026-09-01).</b> Ao dobrar o
    /// mapa, o repintor derivou um retângulo em espaço de <b>célula</b> a partir dos cantos do
    /// mundo e iterou por ele. Num grid <c>CellLayout.Isometric</c>, um retângulo de células
    /// desenha um <b>losango</b> no mundo — então os extremos leste e oeste, na faixa vertical
    /// do meio, ficaram <b>sem chão</b>.</para>
    ///
    /// <para>A cobertura medida ficou em <b>44,7%</b>. E a ferramenta relatou <i>"5.683 células
    /// pintadas"</i>, que era verdade e enganosa — contar o que se pintou não responde se o
    /// jogador tem onde pisar. É o Corolário 4 do <c>CLAUDE.md</c> na sua forma mais literal:
    /// o log de uma ferramenta não é evidência.</para>
    ///
    /// <para><b>A varredura correta parte do mundo:</b> amostra a área jogável a passos menores
    /// que a célula e pinta a célula de cada amostra. Assim a cobertura é garantida por
    /// construção — não há geometria intermediária onde a conversão possa perder cantos.</para>
    /// </summary>
    public static class RepintarOChaoDoDeserto
    {
        private const string Marcador = "[ChaoDoDeserto]";
        private const string Cena = "Assets/Scenes/Deserto_Hali.unity";
        private const string Pincel = "Assets/FavelaAmarela/Art/Tiles/Regras/RuleTile_Areia.asset";

        /// <summary>
        /// Margem para FORA dos limites. As paredes têm espessura, e o jogador vê um pouco além
        /// delas — chão terminando exatamente na parede mostraria o vazio na borda da tela.
        /// </summary>
        private const float MargemDeFora = 4f;

        [MenuItem("Tools/FavelaAmarela/Deserto: preencher o chão até as bordas")]
        public static void Executar()
        {
            var cena = EditorSceneManager.OpenScene(Cena, OpenSceneMode.Single);
            var raizes = cena.GetRootGameObjects();

            var chao = raizes.SelectMany(r => r.GetComponentsInChildren<Tilemap>(true))
                             .FirstOrDefault(t => t.name.Contains("Floor"));

            if (chao == null) { Debug.LogError($"{Marcador} Nenhum Tilemap de chão."); return; }

            var pincel = AssetDatabase.LoadAssetAtPath<TileBase>(Pincel);
            if (pincel == null)
            {
                Debug.LogError($"{Marcador} {Pincel} ausente — rode 'Arte: montar os Rule Tiles'.");
                return;
            }

            var limites = raizes.SelectMany(r => r.GetComponentsInChildren<Transform>(true))
                                .Where(t => t.name.StartsWith("Limite_"))
                                .ToArray();

            if (limites.Length == 0) { Debug.LogError($"{Marcador} Sem Limite_*."); return; }

            float maxX = limites.Max(t => Mathf.Abs(t.position.x)) + MargemDeFora;
            float maxY = limites.Max(t => Mathf.Abs(t.position.y)) + MargemDeFora;

            var grade = chao.layoutGrid;

            // Passo MENOR que a menor dimensão da célula. Com célula 1 × 0,5, um passo de 0,25
            // garante que nenhuma célula seja pulada entre duas amostras.
            float passo = Mathf.Min(grade.cellSize.x, grade.cellSize.y) * 0.5f;
            if (passo <= 0.01f) passo = 0.25f;

            var aPintar = new HashSet<Vector3Int>();

            for (float x = -maxX; x <= maxX; x += passo)
            for (float y = -maxY; y <= maxY; y += passo)
                aPintar.Add(grade.WorldToCell(new Vector3(x, y, 0f)));

            int pintadas = 0;

            foreach (var celula in aPintar)
            {
                // Só onde falta: repintar o existente trocaria chão autorado à mão pela regra.
                if (chao.HasTile(celula)) continue;

                chao.SetTile(celula, pincel);
                pintadas++;
            }

            chao.CompressBounds();

            EditorSceneManager.MarkSceneDirty(cena);
            EditorSceneManager.SaveScene(cena);

            string quebra = System.Environment.NewLine + "  ";
            Debug.Log($"{Marcador} Concluído:" + quebra +
                      $"área varrida: {maxX * 2:0} × {maxY * 2:0} (inclui {MargemDeFora} de " +
                      $"margem para fora das paredes)" + quebra +
                      $"passo de amostragem: {passo} un, célula {grade.cellSize.x} × " +
                      $"{grade.cellSize.y}" + quebra +
                      $"células distintas alcançadas: {aPintar.Count}" + quebra +
                      $"pintadas agora: {pintadas}" + quebra +
                      "CONFIRA com 'Deserto: conferir a cobertura do chão' — contar o que se " +
                      "pintou não é a mesma coisa que o jogador ter onde pisar.");
        }
    }
}
