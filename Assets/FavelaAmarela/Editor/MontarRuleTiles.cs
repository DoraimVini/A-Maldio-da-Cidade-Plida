using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace FavelaAmarela.EditorTools
{
    /// <summary>
    /// Cria os <b>Rule Tiles</b> do projeto — pincéis que decidem o sprite sozinhos, em vez de
    /// tile colocado à mão.
    ///
    /// <para><b>O gargalo que isto ataca (2026-09-01).</b> A auditoria do tilemap mediu: nove
    /// assets de tile, <b>zero Rule Tiles</b>, nenhuma Tile Palette, e quatro ferramentas de
    /// Editor construindo tilemap <i>por código</i>. Construir por código é ótimo para
    /// repetibilidade e péssimo para desenhar — você não sente o nível, você o compila. É o
    /// gargalo que decide se a Dungeon 2 sai, e ele não produz bug nenhum: só produz
    /// lentidão.</para>
    ///
    /// <para><b>Usa <c>IsometricRuleTile</c></b>, e não o <c>RuleTile</c> genérico. Em runtime
    /// os dois são idênticos; a diferença está no <b>editor</b>, que desenha a matriz de
    /// vizinhança em <b>losango</b> em vez de cruz. Autorar regra isométrica num editor
    /// quadrado é o caminho curto para regras erradas que parecem certas.</para>
    ///
    /// <para><b>O que NÃO dá para entregar, e por quê.</b> Regra de terreno com bordas e cantos
    /// — areia encontrando rocha, muro dobrando esquina — exige <b>arte de canto</b>. Os nove
    /// tiles do projeto são todos <c>spriteMode: Single</c>, um sprite cada: <b>não existe arte
    /// de borda</b>. Isso é falta de arte, não falta de sistema, e nenhuma ferramenta
    /// resolve.</para>
    /// </summary>
    public static class MontarRuleTiles
    {
        private const string Marcador = "[RuleTiles]";
        private const string PastaDeTiles = "Assets/FavelaAmarela/Art/Tiles";
        private const string PastaDeRegras = "Assets/FavelaAmarela/Art/Tiles/Regras";

        [MenuItem("Tools/FavelaAmarela/Arte: montar os Rule Tiles")]
        public static void Executar()
        {
            Directory.CreateDirectory(PastaDeRegras);

            var resumo = new List<string>
            {
                MontarAreia(),
                MontarMuro(),
            };

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            string quebra = System.Environment.NewLine + "  ";
            Debug.Log($"{Marcador} Concluído:" + quebra + string.Join(quebra, resumo));
        }

        // ── Chão do Deserto ───────────────────────────────────────────────────

        /// <summary>
        /// Um pincel de areia que <b>sorteia</b> entre as cinco variações a cada célula.
        ///
        /// <para>Hoje o Deserto é pintado com um tile repetido, e a repetição se vê da tela
        /// inteira. Cinco variações espalhadas por sorteio quebram o padrão sem exigir que
        /// ninguém escolha célula por célula qual areia usar.</para>
        ///
        /// <para><b>Sem colisão</b>, e isso é conserto de um defeito real: os cinco tiles de
        /// areia estão em <c>ColliderType.Sprite</c>, ou seja, cada um gera geometria de colisão
        /// a partir do contorno do PNG. Chão não colide. Fica inerte hoje porque o tilemap de
        /// chão não tem <c>TilemapCollider2D</c> — é mina, não bug: no dia em que alguém
        /// acrescentar um, o Deserto inteiro vira parede.</para>
        /// </summary>
        private static string MontarAreia()
        {
            var sprites = new[] { "sand_01", "sand_02", "sand_03", "sand_crack", "sand_pebbles" }
                .Select(CarregarSprite)
                .Where(s => s != null)
                .ToArray();

            if (sprites.Length == 0) return "RuleTile_Areia: nenhum sprite de areia encontrado";

            var regra = new RuleTile.TilingRule
            {
                m_Sprites = sprites,

                // Sorteio, não animação: cada célula escolhe uma variação e fica com ela.
                m_Output = RuleTile.TilingRuleOutput.OutputSprite.Random,
                m_PerlinScale = 0.5f,
                m_RandomTransform = RuleTile.TilingRuleOutput.Transform.Fixed,

                m_ColliderType = Tile.ColliderType.None,
            };

            // Sem vizinhos declarados a regra casa SEMPRE — é o que faz dela um pincel de
            // preenchimento em vez de uma regra de borda. Bordas exigiriam arte de canto, que
            // este projeto não tem.
            regra.m_Neighbors.Clear();
            regra.m_NeighborPositions.Clear();

            return Gravar("RuleTile_Areia", sprites[0], Tile.ColliderType.None, regra,
                          $"{sprites.Length} variações sorteadas, sem colisão");
        }

        // ── Muro / ruína ──────────────────────────────────────────────────────

        /// <summary>
        /// Um pincel de muro que <b>colide</b>, com a geometria seguindo o losango da célula.
        ///
        /// <para><b>É a peça que torna o Deserto construível.</b> A auditoria mediu que a
        /// geometria sólida do Deserto de Hali são quatro bordas e o Lago de Hali — um
        /// obstáculo dentro da área jogável inteira. Com este pincel, desenhar ruína vira
        /// pintar, e a colisão vem junto.</para>
        ///
        /// <para><b><c>ColliderType.Grid</c>, não <c>Sprite</c></b>: num grid isométrico, Grid
        /// dá o <b>losango</b> da célula, e Sprite daria o retângulo do PNG — os cantos vazios
        /// virariam parede, e o jogador "encostaria no nada" perto das quinas. É o defeito
        /// clássico do isométrico em Unity, e é invisível olhando a cena.</para>
        ///
        /// <para>Uniforme, sem bordas: <c>wall_stone.png</c> é um sprite só. Muro com esquina
        /// exige arte de canto.</para>
        /// </summary>
        private static string MontarMuro()
        {
            var pedra = CarregarSprite("wall_stone");
            if (pedra == null) return "RuleTile_Muro: 'wall_stone' não encontrado";

            var regra = new RuleTile.TilingRule
            {
                m_Sprites = new[] { pedra },
                m_Output = RuleTile.TilingRuleOutput.OutputSprite.Single,
                m_ColliderType = Tile.ColliderType.Grid,
            };

            regra.m_Neighbors.Clear();
            regra.m_NeighborPositions.Clear();

            return Gravar("RuleTile_Muro", pedra, Tile.ColliderType.Grid, regra,
                          "colisão em losango (Grid), uniforme — bordas exigiriam arte de canto");
        }

        // ── Apoio ─────────────────────────────────────────────────────────────

        private static string Gravar(string nome, Sprite padrao, Tile.ColliderType colisao,
                                     RuleTile.TilingRule regra, string descricao)
        {
            string caminho = $"{PastaDeRegras}/{nome}.asset";

            var existente = AssetDatabase.LoadAssetAtPath<IsometricRuleTile>(caminho);
            bool novo = existente == null;

            var tile = novo ? ScriptableObject.CreateInstance<IsometricRuleTile>() : existente;

            tile.m_DefaultSprite = padrao;
            tile.m_DefaultColliderType = colisao;
            tile.m_TilingRules.Clear();
            tile.m_TilingRules.Add(regra);

            if (novo) AssetDatabase.CreateAsset(tile, caminho);
            else EditorUtility.SetDirty(tile);

            AssetDatabase.SaveAssetIfDirty(tile);

            return $"{nome} [{(novo ? "CRIADO" : "atualizado")}]: {descricao}";
        }

        /// <summary>
        /// Carrega o <see cref="Sprite"/> de um PNG, inclusive quando a textura está em modo
        /// Multiple — onde o sprite é sub-asset e <c>LoadAssetAtPath&lt;Sprite&gt;</c> devolve
        /// nulo. Foi essa armadilha que manteve as barras do HUD sem sprite por meses.
        /// </summary>
        private static Sprite CarregarSprite(string nome)
        {
            string caminho = $"{PastaDeTiles}/{nome}.png";

            var direto = AssetDatabase.LoadAssetAtPath<Sprite>(caminho);
            if (direto != null) return direto;

            return AssetDatabase.LoadAllAssetsAtPath(caminho).OfType<Sprite>().FirstOrDefault();
        }
    }
}
