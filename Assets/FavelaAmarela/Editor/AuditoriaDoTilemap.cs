using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace FavelaAmarela.EditorTools
{
    /// <summary>
    /// Varre os Tilemaps das cenas do Build Settings e escreve um relatório em Markdown.
    ///
    /// <para><b>Cinco perguntas</b>, todas medidas e nenhuma deduzida: que Tiles cada tilemap usa
    /// e em quantas células; se alguma sprite de tile diverge no import (PPU, pivô, malha, filtro,
    /// compressão); se há Tile assets diferentes desenhando a mesma sprite; que Rule Tile tem
    /// regra sem condição de vizinhança; e — a que motivou — <b>qual sprite a regra de fato
    /// desenha em cada célula</b>.</para>
    ///
    /// <para><b>Por que a última é medida e não calculada.</b> O <c>RuleTile</c> no modo
    /// <c>Random</c> escolhe por <c>Mathf.PerlinNoise((x + 100000) × escala, (y + 100000) ×
    /// escala)</c>. O Perlin da Unity é implementação dela, não reproduzível fora da engine —
    /// qualquer conta minha em Python seria palpite com cara de número. <c>Tilemap.GetSprite</c>
    /// pergunta ao próprio tile o que ele desenha naquela célula, e a doc da 6000.4 é explícita:
    /// <i>"Gets the Sprite used in a Tile given the XYZ coordinates of a cell"</i>.</para>
    ///
    /// <para><b>O que motivou, e o que a medição respondeu (2026-09-09).</b> A consolidação do
    /// chão de areia passou 6 851 células de tiles crus para a <c>RuleTile_Areia</c>, e eu avisei
    /// que os detalhes iam de 2,7% para "muito mais". <b>Estava exagerado</b>: aqueles 2,7% eram
    /// só a fatia pintada à mão, e a regra já governava 77,3% do Deserto com a mesma
    /// distribuição. O efeito real foi <c>sand_crack</c> de 13,6% para 16,1%.</para>
    ///
    /// <para><b>O que a medição revelou é maior, e anterior a tudo isso:</b> o sorteio é
    /// fortemente desigual — <c>sand_03</c> 48,4%, <c>sand_02</c> 19,3%, <c>sand_crack</c>
    /// 16,1%, <c>sand_01</c> 8,1%, <c>sand_pebbles</c> 7,9%. A causa é
    /// <c>FloorToInt(perlin × 5)</c>: ruído Perlin <b>agrupa em torno de 0,5</b>, então quem cai
    /// no índice do meio de <c>m_Sprites</c> leva quase metade do mapa e quem cai nas pontas
    /// leva pouco. <b>A ordem da lista é que define a frequência</b>, e ninguém a escolheu
    /// pensando nisso: um tile de detalhe está em 1 de cada 6 células.</para>
    ///
    /// <para><b>Varre as cenas do Build Settings, e não "a cena atual"</b>, por dois motivos: em
    /// batch mode não existe cena atual, e um relatório que cobre só onde alguém estava parado
    /// esconde justamente a cena que ninguém abriu.</para>
    /// </summary>
    public static class AuditoriaDoTilemap
    {
        private const string Saida = "Docs/KnowledgeBundle/auditoria_tilemap.md";

        /// <summary>Uso de um Tile num tilemap: quantas células, e o que saiu desenhado nelas.</summary>
        private sealed class Uso
        {
            public string Cena;
            public string Tilemap;
            public string Tile;
            public int Celulas;
            public readonly Dictionary<string, int> SpritesDesenhadas = new Dictionary<string, int>();
        }

        [MenuItem("Tools/FavelaAmarela/Auditoria: tilemap e tiles")]
        public static void Executar()
        {
            if (!Application.isBatchMode &&
                !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                Debug.Log("[AuditoriaTilemap] Cancelado — havia cena modificada por salvar.");
                return;
            }

            var usos = new List<Uso>();
            var tilesVistos = new Dictionary<string, TileBase>();

            // As sprites REAIS que a varredura encontrou. A primeira versão guardava só os
            // nomes e depois tentava reencontrá-las com AssetDatabase.FindAssets("nome
            // t:Sprite") -- que não acha sub-asset de textura, e a seção de import saiu com a
            // TABELA VAZIA anunciando "nenhuma divergência". Relatório que diz "conferi e está
            // tudo bem" sem ter conferido nada é pior que relatório ausente.
            var spritesVistas = new HashSet<Sprite>();

            foreach (var entrada in EditorBuildSettings.scenes)
            {
                if (!entrada.enabled || !File.Exists(entrada.path)) continue;

                Scene cena = EditorSceneManager.OpenScene(entrada.path, OpenSceneMode.Single);
                string nomeDaCena = Path.GetFileNameWithoutExtension(entrada.path);

                foreach (var mapa in cena.GetRootGameObjects()
                             .SelectMany(r => r.GetComponentsInChildren<Tilemap>(true)))
                {
                    var porTile = new Dictionary<TileBase, Uso>();

                    foreach (var celula in mapa.cellBounds.allPositionsWithin)
                    {
                        var tile = mapa.GetTile(celula);
                        if (tile == null) continue;

                        if (!porTile.TryGetValue(tile, out var uso))
                        {
                            porTile[tile] = uso = new Uso
                            {
                                Cena = nomeDaCena,
                                Tilemap = mapa.name,
                                Tile = tile.name,
                            };
                            tilesVistos[tile.name] = tile;
                        }

                        uso.Celulas++;

                        // A pergunta central: o que ESTA célula desenha. Para um Tile simples é
                        // sempre a mesma sprite; para um Rule Tile é a escolha da regra.
                        var sprite = mapa.GetSprite(celula);
                        if (sprite != null) spritesVistas.Add(sprite);

                        string nome = sprite != null ? sprite.name : "(nenhuma)";
                        uso.SpritesDesenhadas[nome] =
                            uso.SpritesDesenhadas.TryGetValue(nome, out var n) ? n + 1 : 1;
                    }

                    usos.AddRange(porTile.Values);
                }
            }

            var texto = Montar(usos, tilesVistos, spritesVistas);

            Directory.CreateDirectory(Path.GetDirectoryName(Saida) ?? ".");
            File.WriteAllText(Saida, texto);

            Debug.Log($"[AuditoriaTilemap] {usos.Count} uso(s) de tile em " +
                      $"{usos.Select(u => u.Cena).Distinct().Count()} cena(s). " +
                      $"Relatório em {Saida}");
        }

        private static string Montar(List<Uso> usos, Dictionary<string, TileBase> tiles,
                                     HashSet<Sprite> sprites)
        {
            var md = new StringBuilder();

            md.AppendLine("---");
            md.AppendLine("type: Audit");
            md.AppendLine("title: Auditoria do Tilemap");
            md.AppendLine("description: Tiles em uso, import das sprites, duplicatas, regras sem " +
                          "vizinhança e o que cada regra de fato desenha.");
            md.AppendLine("tags: [tilemap, tiles, isometrico, auditoria]");
            md.AppendLine("---");
            md.AppendLine();
            md.AppendLine("# Auditoria do Tilemap");
            md.AppendLine();
            md.AppendLine("> Gerado por `Tools/FavelaAmarela/Auditoria: tilemap e tiles`. " +
                          "**Não edite à mão** — rode a ferramenta.");
            md.AppendLine();

            SecaoUso(md, usos);
            SecaoImport(md, sprites);
            SecaoDuplicatas(md, usos, tiles);
            SecaoRegras(md, usos);
            SecaoDesenhado(md, usos);

            return md.ToString();
        }

        // ── 1. tiles em uso ──────────────────────────────────────────────────
        private static void SecaoUso(StringBuilder md, List<Uso> usos)
        {
            md.AppendLine("## 1. Tiles em uso");
            md.AppendLine();
            md.AppendLine("| cena | tilemap | tile | células |");
            md.AppendLine("|---|---|---|---|");

            foreach (var u in usos.OrderBy(u => u.Cena).ThenBy(u => u.Tilemap)
                                  .ThenByDescending(u => u.Celulas))
                md.AppendLine($"| {u.Cena} | {u.Tilemap} | `{u.Tile}` | {u.Celulas} |");

            md.AppendLine();
        }

        // ── 2. import das sprites ────────────────────────────────────────────
        private static void SecaoImport(StringBuilder md, HashSet<Sprite> sprites)
        {
            md.AppendLine("## 2. Import das sprites de tile");
            md.AppendLine();
            md.AppendLine("| sprite | PPU | pivô | malha | filtro | compressão |");
            md.AppendLine("|---|---|---|---|---|---|");

            var divergem = new List<string>();
            var ppus = new HashSet<float>();
            var pivos = new HashSet<string>();

            if (sprites.Count == 0)
            {
                md.AppendLine("| — | — | — | — | — | — |");
                md.AppendLine();
                md.AppendLine("**NENHUMA sprite conferida.** A varredura não devolveu sprite " +
                              "nenhuma, então esta seção não mediu nada — não leia como " +
                              "aprovação.\n");
                return;
            }

            foreach (var sprite in sprites.OrderBy(s => s.name))
            {
                string caminho = AssetDatabase.GetAssetPath(sprite);
                if (AssetImporter.GetAtPath(caminho) is not TextureImporter imp) continue;

                var cfg = new TextureImporterSettings();
                imp.ReadTextureSettings(cfg);

                string pivo = cfg.spriteAlignment == (int)SpriteAlignment.Center
                    ? "Center"
                    : $"{(SpriteAlignment)cfg.spriteAlignment}";

                ppus.Add(cfg.spritePixelsPerUnit);
                pivos.Add(pivo);

                md.AppendLine($"| `{sprite.name}` | {cfg.spritePixelsPerUnit:0.##} | {pivo} | " +
                              $"{cfg.spriteMeshType} | {imp.filterMode} | {imp.textureCompression} |");

                if (cfg.spriteMeshType != SpriteMeshType.FullRect &&
                    cfg.spriteAlignment == (int)SpriteAlignment.Center)
                    divergem.Add($"`{sprite.name}`: malha **{cfg.spriteMeshType}** num tile de " +
                                 "chão — a malha vira o losango em vez da célula, e os vizinhos " +
                                 "deixam de encostar");

                if (imp.filterMode != FilterMode.Point)
                    divergem.Add($"`{sprite.name}`: filtro **{imp.filterMode}** — a arte sai borrada");
            }

            md.AppendLine();

            if (ppus.Count > 1)
                divergem.Add($"**PPU inconsistente entre tiles**: {string.Join(", ", ppus.OrderBy(p => p))}. " +
                             "Dois tiles com PPU diferente têm pixels de tamanhos diferentes na tela.");

            if (pivos.Count > 1)
                md.AppendLine($"> Pivôs em uso: {string.Join(", ", pivos.OrderBy(p => p))}. " +
                              "Mais de um é **esperado** aqui: chão é `Center`, parede é " +
                              "`BottomCenter`.\n");

            md.AppendLine(divergem.Count == 0
                ? "**Nenhuma divergência de import.**\n"
                : "### Divergências\n\n- " + string.Join("\n- ", divergem) + "\n");
        }

        // ── 3. duplicatas ────────────────────────────────────────────────────
        private static void SecaoDuplicatas(StringBuilder md, List<Uso> usos,
                                            Dictionary<string, TileBase> tiles)
        {
            md.AppendLine("## 3. Tile assets duplicados");
            md.AppendLine();
            md.AppendLine("> Dois Tile assets com a **mesma sprite** podem ter colisor ou cor " +
                          "diferentes, e aí dois pedaços de chão idênticos se comportam " +
                          "diferente sem que nada na tela explique.");
            md.AppendLine();

            var porSprite = new Dictionary<Sprite, List<Tile>>();

            foreach (var tile in tiles.Values.OfType<Tile>())
            {
                if (tile.sprite == null) continue;
                if (!porSprite.TryGetValue(tile.sprite, out var lista))
                    porSprite[tile.sprite] = lista = new List<Tile>();
                lista.Add(tile);
            }

            var dup = porSprite.Where(p => p.Value.Count > 1).ToList();

            if (dup.Count == 0)
            {
                md.AppendLine($"**Nenhuma duplicata**: {porSprite.Count} Tile asset(s) simples " +
                              "sobre o mesmo número de sprites distintas, um para um.\n");
                return;
            }

            foreach (var (sprite, lista) in dup.Select(p => (p.Key, p.Value)))
            {
                md.AppendLine($"### `{sprite.name}` — {lista.Count} tiles");
                md.AppendLine();
                md.AppendLine("| tile | colisor | cor |");
                md.AppendLine("|---|---|---|");
                foreach (var t in lista)
                    md.AppendLine($"| `{t.name}` | {t.colliderType} | {t.color} |");
                md.AppendLine();
            }
        }

        // ── 4. regras sem vizinhança ─────────────────────────────────────────
        private static void SecaoRegras(StringBuilder md, List<Uso> usos)
        {
            md.AppendLine("## 4. Rule Tiles e condições de vizinhança");
            md.AppendLine();
            md.AppendLine("> Uma regra **sem** condição de vizinhança casa com tudo. Isso é " +
                          "legítimo quando o Rule Tile serve só para variar a sprite — mas " +
                          "significa que **não há borda**: nenhuma célula vai desenhar transição, " +
                          "e o `m_DefaultSprite` nunca é alcançado por falta de regra.");
            md.AppendLine();

            var regras = usos.Select(u => u.Tile).Distinct()
                .Select(nome => AssetDatabase.FindAssets($"{nome} t:TileBase")
                    .Select(AssetDatabase.GUIDToAssetPath)
                    .FirstOrDefault(p => Path.GetFileNameWithoutExtension(p) == nome))
                .Where(p => !string.IsNullOrEmpty(p) && File.ReadAllText(p).Contains("RuleTile"))
                .Distinct()
                .ToList();

            if (regras.Count == 0)
            {
                md.AppendLine("**Nenhum Rule Tile em uso.**\n");
                return;
            }

            md.AppendLine("| rule tile | regras | com vizinhança | sprites por regra |");
            md.AppendLine("|---|---|---|---|");

            var semBorda = new List<string>();

            foreach (var caminho in regras.OrderBy(p => p))
            {
                string yaml = File.ReadAllText(caminho);
                string nome = Path.GetFileNameWithoutExtension(caminho);

                int total = System.Text.RegularExpressions.Regex.Matches(yaml, @"(?m)^\s+- m_Id:").Count;
                int comVizinho = System.Text.RegularExpressions.Regex
                    .Matches(yaml, @"m_NeighborPositions:\s*\n\s*- \{").Count;
                // Só o que está dentro dos blocos m_Sprites das regras. Contar todo
                // "type: 3" incluiria o m_Script e o m_DefaultSprite, e a primeira versão
                // reportou "~6" para uma regra que tem 5.
                int sprites = System.Text.RegularExpressions.Regex
                    .Matches(yaml, @"m_Sprites:\s*\n((?:\s+- \{fileID: [^\n]*\n)+)")
                    .Cast<System.Text.RegularExpressions.Match>()
                    .Sum(m => System.Text.RegularExpressions.Regex
                        .Matches(m.Groups[1].Value, @"fileID:").Count);

                md.AppendLine($"| `{nome}` | {total} | {comVizinho} | {sprites} |");

                if (comVizinho == 0)
                    semBorda.Add($"`{nome}`: {total} regra(s), **nenhuma** com condição de " +
                                 "vizinhança — ele varia a sprite e não desenha borda nenhuma");
            }

            md.AppendLine();

            md.AppendLine(semBorda.Count == 0
                ? "Todos os Rule Tiles têm ao menos uma regra de vizinhança.\n"
                : "### Sem borda\n\n- " + string.Join("\n- ", semBorda) +
                  "\n\nNão é defeito hoje: **não existe transição de terreno neste projeto** — " +
                  "cada cena usa uma família de chão só. Vira defeito no dia em que duas " +
                  "superfícies diferentes se encontrarem.\n");
        }

        // ── 5. o que a regra realmente desenha ───────────────────────────────
        private static void SecaoDesenhado(StringBuilder md, List<Uso> usos)
        {
            md.AppendLine("## 5. O que cada tile de fato desenha");
            md.AppendLine();
            md.AppendLine("> Medido célula a célula com `Tilemap.GetSprite`, que pergunta ao " +
                          "próprio tile. Para um Tile simples é sempre a mesma sprite; para um " +
                          "Rule Tile em modo `Random`, é a escolha do ruído Perlin — e é a única " +
                          "forma honesta de saber a proporção, porque o `Mathf.PerlinNoise` da " +
                          "Unity não é reproduzível fora da engine.");
            md.AppendLine();

            foreach (var u in usos.Where(u => u.SpritesDesenhadas.Count > 1)
                                  .OrderByDescending(u => u.Celulas))
            {
                md.AppendLine($"### {u.Cena} / {u.Tilemap} — `{u.Tile}` ({u.Celulas} células)");
                md.AppendLine();
                md.AppendLine("| sprite | células | % |");
                md.AppendLine("|---|---|---|");

                foreach (var par in u.SpritesDesenhadas.OrderByDescending(p => p.Value))
                    md.AppendLine($"| `{par.Key}` | {par.Value} | " +
                                  (100.0 * par.Value / u.Celulas).ToString("0.0", CultureInfo.InvariantCulture) +
                                  "% |");

                md.AppendLine();
            }

            if (usos.All(u => u.SpritesDesenhadas.Count <= 1))
                md.AppendLine("Nenhum tile desenha mais de uma sprite — não há variação em uso.\n");
        }

    }
}
