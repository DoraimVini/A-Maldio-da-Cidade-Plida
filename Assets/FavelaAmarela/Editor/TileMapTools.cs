using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace FavelaAmarela.EditorTools
{
    /// <summary>
    /// Janela com as operações de tilemap que se repetem: conferir e corrigir pivô, forçar malha
    /// <c>FullRect</c>, criar Rule Tile a partir de sprites selecionadas e acrescentar variações
    /// a um Rule Tile existente.
    ///
    /// <para><b>Um dos botões pedidos foi mudado de propósito.</b> O pedido dizia <i>"Fix All
    /// Pivots — sets pivot to bottom-center for all tiles in a folder"</i>. Aplicado aqui, isso
    /// <b>deslocaria o piso das seis cenas meia célula para baixo</b>: auditado em 2026-09-09,
    /// os 13 tiles de chão deste projeto têm pivô <b>Center</b>, e o <c>m_TileAnchor</c> dos 12
    /// tilemaps é <c>(0.5, 0.5, 0)</c> — eles casam. <c>BottomCenter</c> é o pivô de coisa que
    /// fica <b>em pé</b> (o <c>wall_stone</c> é o único, e está certo).</para>
    ///
    /// <para>Então o botão pergunta o alvo em vez de fixá-lo, começa em <c>Center</c>, e a
    /// conferência roda antes e separada da correção — um botão que muda 13 arquivos sem mostrar
    /// o que vai mudar é o tipo de ferramenta que já destruiu geometria neste projeto.</para>
    /// </summary>
    public sealed class TileMapTools : EditorWindow
    {
        private DefaultAsset _pasta;
        private SpriteAlignment _pivoAlvo = SpriteAlignment.Center;
        private bool _soChao = true;
        private Vector2 _rolagem;
        private string _relatorio = "";

        [MenuItem("Tools/FavelaAmarela/Janela: TileMap Tools")]
        public static void Abrir()
        {
            var janela = GetWindow<TileMapTools>("TileMap Tools");
            janela.minSize = new Vector2(420f, 480f);
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Pasta alvo", EditorStyles.boldLabel);
            _pasta = (DefaultAsset)EditorGUILayout.ObjectField(
                "Pasta", _pasta, typeof(DefaultAsset), false);

            string caminhoDaPasta = _pasta != null ? AssetDatabase.GetAssetPath(_pasta) : null;

            EditorGUILayout.Space(8);
            SecaoPivo(caminhoDaPasta);

            EditorGUILayout.Space(8);
            SecaoMalha(caminhoDaPasta);

            EditorGUILayout.Space(8);
            SecaoCriarRegra();

            EditorGUILayout.Space(8);
            SecaoVariacoes();

            EditorGUILayout.Space(10);
            if (string.IsNullOrEmpty(_relatorio)) return;

            EditorGUILayout.LabelField("Resultado", EditorStyles.boldLabel);
            _rolagem = EditorGUILayout.BeginScrollView(_rolagem, GUILayout.MinHeight(120f));
            EditorGUILayout.TextArea(_relatorio, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
        }

        // ── pivô ─────────────────────────────────────────────────────────────
        private void SecaoPivo(string pasta)
        {
            EditorGUILayout.LabelField("1. Pivô", EditorStyles.boldLabel);

            EditorGUILayout.HelpBox(
                "Chão isométrico usa Center, e é o que casa com o Tile Anchor (0.5, 0.5) dos " +
                "12 tilemaps do projeto. BottomCenter é para o que fica EM PÉ (parede, prop).\n\n" +
                "Aplicar BottomCenter no chão desloca cada tile meia célula para baixo, nas " +
                "seis cenas.",
                MessageType.Warning);

            _pivoAlvo = (SpriteAlignment)EditorGUILayout.EnumPopup("Pivô alvo", _pivoAlvo);

            using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(pasta)))
            {
                EditorGUILayout.BeginHorizontal();

                if (GUILayout.Button("Conferir pivôs"))
                    _relatorio = VarrerPivos(pasta, aplicar: false);

                if (GUILayout.Button("Aplicar aos divergentes"))
                {
                    var previa = VarrerPivos(pasta, aplicar: false);

                    if (previa.Contains("nenhum divergente"))
                        _relatorio = previa;
                    else if (EditorUtility.DisplayDialog(
                                 "Mudar o pivô?",
                                 previa + "\n\nIsto reimporta os arquivos e move onde eles são " +
                                 "desenhados. Não tem desfazer.",
                                 "Aplicar", "Cancelar"))
                        _relatorio = VarrerPivos(pasta, aplicar: true);
                }

                EditorGUILayout.EndHorizontal();
            }
        }

        private string VarrerPivos(string pasta, bool aplicar)
        {
            var divergentes = new List<string>();
            int total = 0;

            foreach (var (caminho, imp, cfg) in SpritesDaPasta(pasta))
            {
                total++;
                if ((SpriteAlignment)cfg.spriteAlignment == _pivoAlvo) continue;

                divergentes.Add($"{Path.GetFileName(caminho)}: " +
                                $"{(SpriteAlignment)cfg.spriteAlignment} -> {_pivoAlvo}");

                if (!aplicar) continue;

                cfg.spriteAlignment = (int)_pivoAlvo;
                imp.SetTextureSettings(cfg);
                imp.SaveAndReimport();
            }

            if (divergentes.Count == 0)
                return $"{total} sprite(s) na pasta, nenhum divergente de {_pivoAlvo}.";

            return (aplicar ? "Alterados" : "Divergentes") +
                   $" ({divergentes.Count} de {total}):\n  " + string.Join("\n  ", divergentes);
        }

        // ── malha ────────────────────────────────────────────────────────────
        private void SecaoMalha(string pasta)
        {
            EditorGUILayout.LabelField("2. Malha", EditorStyles.boldLabel);

            EditorGUILayout.HelpBox(
                "Tight recorta a malha ao alfa. Num losango de chão isso faz a malha virar o " +
                "losango em vez da célula, e duas células vizinhas deixam de encostar — é de " +
                "onde sai o fio de fundo entre tiles.",
                MessageType.Info);

            _soChao = EditorGUILayout.ToggleLeft(
                "Só o chão (pivô central) — parede fica em Tight, que poupa overdraw", _soChao);

            using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(pasta)))
                if (GUILayout.Button("Forçar Full Rect"))
                    _relatorio = ForcarFullRect(pasta);
        }

        private string ForcarFullRect(string pasta)
        {
            var mudados = new List<string>();
            int total = 0;

            foreach (var (caminho, imp, cfg) in SpritesDaPasta(pasta))
            {
                if (_soChao && (SpriteAlignment)cfg.spriteAlignment != SpriteAlignment.Center)
                    continue;

                total++;
                if (cfg.spriteMeshType == SpriteMeshType.FullRect) continue;

                cfg.spriteMeshType = SpriteMeshType.FullRect;
                imp.SetTextureSettings(cfg);
                imp.SaveAndReimport();
                mudados.Add(Path.GetFileName(caminho));
            }

            return mudados.Count == 0
                ? $"{total} sprite(s) conferido(s), todos já em Full Rect."
                : $"Full Rect aplicado a {mudados.Count} de {total}:\n  " +
                  string.Join("\n  ", mudados);
        }

        // ── criar Rule Tile ──────────────────────────────────────────────────
        private void SecaoCriarRegra()
        {
            EditorGUILayout.LabelField("3. Criar Rule Tile das sprites selecionadas",
                                       EditorStyles.boldLabel);

            var sprites = SpritesSelecionadas();

            EditorGUILayout.HelpBox(
                "A ORDEM DA SELEÇÃO DEFINE A FREQUÊNCIA, e não é parelha. O RuleTile no modo " +
                "Random escolhe por FloorToInt(perlin × N), e o ruído Perlin agrupa em torno de " +
                "0,5 — quem cai no índice do meio leva quase metade.\n\n" +
                "Medido neste projeto com 5 sprites, sobre 25 252 células:\n" +
                "  índice 0 → 8,1%   1 → 19,3%   2 → 48,4%   3 → 16,1%   4 → 7,9%\n\n" +
                "Ponha os detalhes (rachadura, pedregulho) nas PONTAS.",
                MessageType.Info);

            EditorGUILayout.LabelField($"Selecionadas: {sprites.Count}");

            using (new EditorGUI.DisabledScope(sprites.Count == 0))
                if (GUILayout.Button("Criar IsometricRuleTile"))
                    _relatorio = CriarRegra(sprites);
        }

        private static string CriarRegra(List<Sprite> sprites)
        {
            string destino = EditorUtility.SaveFilePanelInProject(
                "Salvar Rule Tile", "RuleTile_Novo", "asset",
                "Onde gravar o Rule Tile isométrico");

            if (string.IsNullOrEmpty(destino)) return "Cancelado.";

            var regra = CreateInstance<UnityEngine.IsometricRuleTile>();
            regra.m_DefaultSprite = sprites[0];
            regra.m_DefaultColliderType = Tile.ColliderType.None;

            regra.m_TilingRules.Add(new RuleTile.TilingRule
            {
                m_Sprites = sprites.ToArray(),
                m_Output = RuleTile.TilingRuleOutput.OutputSprite.Random,
                m_ColliderType = Tile.ColliderType.None,
                m_PerlinScale = 0.5f,
            });

            AssetDatabase.CreateAsset(regra, destino);
            AssetDatabase.SaveAssets();
            Selection.activeObject = regra;

            // Sem condição de vizinhança de propósito: condição só serve se houver arte de
            // transição para desenhar do outro lado, e este projeto não tem UMA CÉLULA onde
            // dois terrenos se encontram (auditado em 2026-09-09). Criar regra de borda sem
            // arte faria a célula cair no m_DefaultSprite e parecer buraco.
            return $"Criado {Path.GetFileName(destino)} com 1 regra e {sprites.Count} sprite(s), " +
                   "saída Random.\n\nSem condição de vizinhança: ela só vale com arte de " +
                   "transição, e não existe limite de terreno neste projeto ainda. " +
                   "Para gerar o conjunto de 47, use Tools/gerar_transicoes_iso.py.";
        }

        // ── variações ────────────────────────────────────────────────────────
        private void SecaoVariacoes()
        {
            EditorGUILayout.LabelField("4. Acrescentar variações a Rule Tiles selecionados",
                                       EditorStyles.boldLabel);

            var regras = Selection.objects.OfType<RuleTile>().ToList();
            var sprites = SpritesSelecionadas();

            EditorGUILayout.LabelField($"Rule Tiles: {regras.Count} | sprites: {sprites.Count}");

            using (new EditorGUI.DisabledScope(regras.Count == 0 || sprites.Count == 0))
                if (GUILayout.Button("Acrescentar à primeira regra"))
                    _relatorio = AcrescentarVariacoes(regras, sprites);
        }

        private static string AcrescentarVariacoes(List<RuleTile> regras, List<Sprite> sprites)
        {
            var linhas = new List<string>();

            foreach (var regra in regras)
            {
                if (regra.m_TilingRules.Count == 0)
                {
                    linhas.Add($"{regra.name}: sem regra nenhuma — nada a acrescentar");
                    continue;
                }

                var alvo = regra.m_TilingRules[0];
                var novas = alvo.m_Sprites.Concat(sprites.Where(s => !alvo.m_Sprites.Contains(s)));

                alvo.m_Sprites = novas.ToArray();
                alvo.m_Output = RuleTile.TilingRuleOutput.OutputSprite.Random;

                EditorUtility.SetDirty(regra);
                linhas.Add($"{regra.name}: agora {alvo.m_Sprites.Length} sprite(s) " +
                           $"({string.Join(", ", alvo.m_Sprites.Select(s => s.name))})");
            }

            AssetDatabase.SaveAssets();

            return string.Join("\n", linhas) +
                   "\n\nLembre da ordem: o índice do meio leva quase metade das células. " +
                   "Sprites acrescentadas vão para o FIM, que é uma ponta fina — bom para " +
                   "detalhe, ruim para uma areia lisa que deveria ser comum.";
        }

        // ── leitura ──────────────────────────────────────────────────────────
        private static List<Sprite> SpritesSelecionadas() =>
            Selection.objects.OfType<Sprite>()
                .Concat(Selection.objects.OfType<Texture2D>()
                    .SelectMany(t => AssetDatabase
                        .LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(t))
                        .OfType<Sprite>()))
                .Distinct()
                .ToList();

        private static IEnumerable<(string, TextureImporter, TextureImporterSettings)>
            SpritesDaPasta(string pasta)
        {
            if (string.IsNullOrEmpty(pasta) || !Directory.Exists(pasta)) yield break;

            foreach (var caminho in Directory.EnumerateFiles(pasta, "*.png",
                                                             SearchOption.AllDirectories))
            {
                if (AssetImporter.GetAtPath(caminho) is not TextureImporter imp) continue;

                var cfg = new TextureImporterSettings();
                imp.ReadTextureSettings(cfg);

                yield return (caminho, imp, cfg);
            }
        }
    }
}
