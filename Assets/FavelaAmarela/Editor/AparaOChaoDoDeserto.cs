using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace FavelaAmarela.EditorTools
{
    /// <summary>
    /// Apara o chão do Deserto para <b>terminar logo depois das paredes</b>, em vez de se
    /// estender 21 unidades além delas.
    ///
    /// <para><b>O que motivou (2026-09-02).</b> O Vini relatou que a borda do mapa "continua no
    /// mesmo tamanho de antes" depois de o Deserto dobrar. Medido, a queixa é real e a causa é
    /// o inverso do que parecia — as paredes estão certas (±43 × ±31, dobradas junto com o
    /// mapa); é o <b>chão</b> que foi longe demais:</para>
    ///
    /// <code>
    /// chão pintado :  x -64..64    y -35..35
    /// paredes      :  x -43..43    y -31..31
    /// </code>
    ///
    /// <para>São <b>21 unidades de chão visível e inalcançável</b> a leste e a oeste. O jogador
    /// vê um deserto de 128 de largura, anda em 86, e é parado por uma parede <b>invisível</b>
    /// muito antes da borda que os olhos enxergam. A sensação é exatamente a que ele
    /// descreveu.</para>
    ///
    /// <para><b>A culpa é do <c>RepintarOChaoDoDeserto</c></b>, meu, da madrugada de 01/09. Ele
    /// varre o MUNDO a passos de 0,25 e pinta a célula de cada amostra — o que garantiu
    /// cobertura de 100%, que era o defeito da vez. O que ele não faz é o contrário: apagar o
    /// que ficou fora. Num grid isométrico a célula de uma amostra na borda cobre bem mais
    /// mundo do que a amostra, e a união estourou para fora.</para>
    ///
    /// <para><b>Margem de 3 unidades</b>, e não zero: chão terminando exatamente na parede
    /// invisível mostraria o vazio no instante em que o jogador encosta nela. Três unidades dão
    /// a borda de terra que o olho espera e continuam dentro do que a câmera mostra.</para>
    /// </summary>
    public static class AparaOChaoDoDeserto
    {
        private const string Marcador = "[AparaOChao]";
        private const string Cena = "Assets/Scenes/Deserto_Hali.unity";

        /// <summary>Quanto de chão sobra para FORA da parede.</summary>
        private const float Margem = 3f;

        [MenuItem("Tools/FavelaAmarela/Deserto: aparar o chão que passa das paredes")]
        public static void Executar()
        {
            var cena = EditorSceneManager.OpenScene(Cena, OpenSceneMode.Single);
            var raizes = cena.GetRootGameObjects();

            var limites = raizes.SelectMany(r => r.GetComponentsInChildren<Transform>(true))
                                .Where(t => t.name.StartsWith("Limite_"))
                                .ToArray();

            if (limites.Length == 0)
            {
                Debug.LogError($"{Marcador} Sem Limite_* — não dá para saber onde é a borda.");
                return;
            }

            float paredeX = limites.Max(t => Mathf.Abs(t.position.x));
            float paredeY = limites.Max(t => Mathf.Abs(t.position.y));

            float maxX = paredeX + Margem;
            float maxY = paredeY + Margem;

            var mapas = raizes.SelectMany(r => r.GetComponentsInChildren<Tilemap>(true)).ToArray();

            int antes = mapas.Sum(ContarCelulas);
            int apagadas = 0;

            foreach (var mapa in mapas)
            {
                var grade = mapa.layoutGrid;
                var fora = new List<Vector3Int>();

                foreach (var celula in mapa.cellBounds.allPositionsWithin)
                {
                    if (!mapa.HasTile(celula)) continue;

                    // O CENTRO da célula no mundo é a pergunta certa: é onde o jogador vê o
                    // chão, e é o que a conversão inversa (mundo -> célula) nunca devolve
                    // exatamente num grid isométrico.
                    var centro = grade.GetCellCenterWorld(celula);

                    if (Mathf.Abs(centro.x) <= maxX && Mathf.Abs(centro.y) <= maxY) continue;

                    fora.Add(celula);
                }

                if (fora.Count == 0) continue;

                // SetTiles em lote, e não SetTile: medido em 2026-09-01, o singular grava NULL
                // neste projeto e apagou 9.104 células relatando sucesso. Aqui apagar É a
                // intenção, mas a API continua sendo a mesma.
                mapa.SetTiles(fora.ToArray(), new TileBase[fora.Count]);
                apagadas += fora.Count;
            }

            int depois = mapas.Sum(ContarCelulas);

            // GUARDA. Apagar é a intenção, então a conta que importa é a inversa da do repintor:
            // o que sobrou tem de ser exatamente o que não foi apagado.
            if (depois != antes - apagadas)
            {
                Debug.LogError($"{Marcador} RECUSADO — apaguei {apagadas} e o total foi de " +
                               $"{antes} para {depois}, que não fecha. A cena NÃO foi salva.");
                return;
            }

            if (apagadas == 0)
            {
                Debug.Log($"{Marcador} Nada a aparar: o chão já termina em " +
                          $"±{maxX:0} × ±{maxY:0}.");
                return;
            }

            mapas.ToList().ForEach(m => m.CompressBounds());

            EditorSceneManager.MarkSceneDirty(cena);
            EditorSceneManager.SaveScene(cena);

            string quebra = System.Environment.NewLine + "  ";
            Debug.Log($"{Marcador} Concluído:" + quebra +
                      $"paredes em ±{paredeX:0} × ±{paredeY:0}, margem de {Margem:0}" + quebra +
                      $"chão agora termina em ±{maxX:0} × ±{maxY:0}" + quebra +
                      $"células: {antes} → {depois} ({apagadas} apagadas, todas fora da parede)");
        }

        private static int ContarCelulas(Tilemap mapa)
        {
            int n = 0;
            foreach (var c in mapa.cellBounds.allPositionsWithin)
                if (mapa.HasTile(c)) n++;
            return n;
        }
    }
}
