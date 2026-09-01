using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace FavelaAmarela.EditorTools
{
    /// <summary>
    /// Confere se o chão <b>cobre a área jogável inteira</b>, amostrando o mundo em vez de
    /// contar células.
    ///
    /// <para><b>Por que contar células não responde (2026-09-01).</b> Numa grade
    /// <c>CellLayout.Isometric</c>, um retângulo em espaço de <i>célula</i> vira um
    /// <b>losango</b> no mundo. A ferramenta que dobrou o mapa pintou um retângulo de células
    /// derivado dos cantos do mundo — e isso <b>não garante</b> que os cantos do mundo tenham
    /// chão. Um jogador andando para o canto nordeste pode achar o vazio, e o número "5.683
    /// células pintadas" não teria denunciado nada.</para>
    ///
    /// <para>Amostra numa malha regular sobre a área delimitada pelos <c>Limite_*</c> e relata
    /// quantos pontos caíram fora do chão, com as coordenadas dos piores casos.</para>
    /// </summary>
    public static class ConferirCoberturaDoChao
    {
        private const string Marcador = "[CoberturaDoChao]";
        private const string Cena = "Assets/Scenes/Deserto_Hali.unity";

        /// <summary>Espaçamento da amostragem, em unidades de mundo.</summary>
        private const float Passo = 2f;

        [MenuItem("Tools/FavelaAmarela/Deserto: conferir a cobertura do chão")]
        public static void Executar()
        {
            var cena = EditorSceneManager.OpenScene(Cena, OpenSceneMode.Single);
            var raizes = cena.GetRootGameObjects();

            var chao = raizes.SelectMany(r => r.GetComponentsInChildren<Tilemap>(true))
                             .FirstOrDefault(t => t.name.Contains("Floor"));

            if (chao == null) { Debug.LogError($"{Marcador} Nenhum Tilemap de chão."); return; }

            var limites = raizes.SelectMany(r => r.GetComponentsInChildren<Transform>(true))
                                .Where(t => t.name.StartsWith("Limite_"))
                                .ToArray();

            if (limites.Length == 0) { Debug.LogError($"{Marcador} Sem Limite_*."); return; }

            float maxX = limites.Max(t => Mathf.Abs(t.position.x));
            float maxY = limites.Max(t => Mathf.Abs(t.position.y));

            var grade = chao.layoutGrid;

            int total = 0, vazios = 0;
            var piores = new List<string>();

            // Margem para dentro: o ponto exatamente sobre a parede não precisa de chão.
            for (float x = -maxX + 1f; x <= maxX - 1f; x += Passo)
            for (float y = -maxY + 1f; y <= maxY - 1f; y += Passo)
            {
                total++;

                var celula = grade.WorldToCell(new Vector3(x, y, 0f));
                if (chao.HasTile(celula)) continue;

                vazios++;
                if (piores.Count < 12) piores.Add($"({x:0}, {y:0})");
            }

            float cobertura = total == 0 ? 0f : 1f - vazios / (float)total;

            string quebra = System.Environment.NewLine + "  ";
            string texto = $"{Marcador} Área {maxX * 2:0} × {maxY * 2:0}, {total} pontos " +
                           $"amostrados a cada {Passo} unidades." + quebra +
                           $"Cobertura: {cobertura:P1}  ({vazios} ponto(s) sem chão)";

            if (vazios > 0)
                texto += quebra + "Buracos, primeiros: " + string.Join(", ", piores);

            if (vazios == 0) Debug.Log(texto);
            else Debug.LogError(texto);
        }
    }
}
