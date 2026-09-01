using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace FavelaAmarela.EditorTools
{
    /// <summary>
    /// Troca os <b>pisos placeholder</b> do Santuário de Yhtill, dos Portões (arena do Byakhee)
    /// e do Castelo por tiles com sombreado e variação.
    ///
    /// <para><b>A pergunta do Vini (2026-09-01):</b> <i>"por que o chão do Santuário e da luta
    /// contra a Byakhee são diferentes das outras tilemaps?"</i> Ele viu certo, e a causa é
    /// literal: <c>santuario_piso_placeholder.png</c> e <c>arena_piso_placeholder.png</c> têm
    /// <b>duas cores</b> — um losango chapado mais a transparência. As areias do Deserto têm
    /// <b>5 variantes</b> com rampa de 5 tons e detalhes. Não é a grade, que é idêntica nas
    /// cinco cenas (célula 1 × 0,5, isométrica): é que um lado tem arte e o outro tem um
    /// losango pintado de bege.</para>
    ///
    /// <para><b>DUAS EXECUÇÕES, e a razão é sangue no chão.</b> A primeira versão criava os
    /// <c>Tile</c> e repintava na <b>mesma</b> execução em batch mode. Os assets recém-criados
    /// não estavam referenciáveis na hora de salvar a cena, e cada <c>SetTile</c> gravou
    /// <b>null</b>: o chão do Santuário foi de 900 células para 116, o dos Portões de 4.624 para
    /// 528, o do Castelo de 5.932 para 1.708. E a ferramenta relatou
    /// <i>"784 células trocadas"</i>, que era verdade sobre as chamadas e mentira sobre o
    /// resultado — Corolário 4 do <c>CLAUDE.md</c> outra vez.</para>
    ///
    /// <para>Por isso: <b>rode primeiro</b> "Arte: criar os tiles de pedra", deixe a Unity
    /// importar, e <b>só então</b> rode a troca. E a troca <b>confere e se recusa a salvar</b>
    /// se o total de células cair.</para>
    ///
    /// <para><b>A variação vem do HASH da célula, não de <c>Random</c>:</b> rodar duas vezes tem
    /// de dar o mesmo chão, senão cada execução vira um diff ilegível na cena.</para>
    /// </summary>
    public static class TrocarPisosPlaceholder
    {
        private const string Marcador = "[PisosDeVerdade]";
        private const string PastaDosTiles = "Assets/FavelaAmarela/Art/Tiles";

        private readonly struct Troca
        {
            public readonly string Cena, Placeholder;
            public readonly string[] Variantes;
            public readonly string Razao;

            public Troca(string cena, string placeholder, string[] variantes, string razao)
            {
                Cena = cena; Placeholder = placeholder; Variantes = variantes; Razao = razao;
            }
        }

        private static readonly Troca[] Trocas =
        {
            new Troca("Assets/Scenes/Santuario_Yhtill.unity", "santuario_piso_placeholder",
                new[] { "santuario_piso_01", "santuario_piso_02", "santuario_piso_03" },
                "pedra pálida: a rampa parte da cor que já estava lá, para o Santuário não " +
                "mudar de leitura — só ganhar volume, junta de laje e uma rachadura"),

            new Troca("Assets/Scenes/Portoes_Das_Ruinas.unity", "arena_piso_placeholder",
                new[] { "arena_piso_01", "arena_piso_02", "arena_piso_03" },
                "arena do Byakhee: pedra escura. É a luta mais longa do Vertical Slice e o " +
                "jogador passa minutos olhando para este chão"),

            new Troca("Assets/Scenes/Castelo_Carcosa.unity", "arena_piso_placeholder",
                new[] { "arena_piso_01", "arena_piso_02", "arena_piso_03" },
                "o Castelo compartilha o piso da arena — mesmo placeholder, mesma troca"),
        };

        // ── Passo 1, em execução própria ──────────────────────────────────────

        /// <summary>
        /// Cria os <see cref="Tile"/> e <b>termina</b>. Nada de repintar aqui: é justamente a
        /// mistura dos dois passos numa execução só que apagou três chãos.
        /// </summary>
        [MenuItem("Tools/FavelaAmarela/Arte: criar os tiles de pedra (passo 1)")]
        public static void CriarOsTiles()
        {
            var feitos = new List<string>();

            foreach (string nome in Trocas.SelectMany(t => t.Variantes).Distinct())
            {
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{PastaDosTiles}/{nome}.png");
                if (sprite == null) { feitos.Add($"{nome}: PNG NÃO IMPORTADO"); continue; }

                string caminho = $"{PastaDosTiles}/{nome}.asset";
                var tile = AssetDatabase.LoadAssetAtPath<Tile>(caminho);

                bool novo = tile == null;
                if (novo)
                {
                    tile = ScriptableObject.CreateInstance<Tile>();
                    AssetDatabase.CreateAsset(tile, caminho);
                }

                tile.sprite = sprite;
                // Chão não colide -- quem bloqueia é a camada Obstacle. Mesmo do sand_01.
                tile.colliderType = Tile.ColliderType.None;
                tile.color = Color.white;

                EditorUtility.SetDirty(tile);
                feitos.Add(nome + (novo ? " [CRIADO]" : " [atualizado]"));
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            string quebra = System.Environment.NewLine + "  ";
            Debug.Log($"{Marcador} Tiles prontos:" + quebra + string.Join(quebra, feitos) + quebra +
                      "AGORA rode 'Arte: trocar os pisos placeholder (passo 2)' — numa execução " +
                      "NOVA, para a Unity terminar de importar.");
        }

        // ── Passo 2, em execução própria ──────────────────────────────────────

        [MenuItem("Tools/FavelaAmarela/Arte: trocar os pisos placeholder (passo 2)")]
        public static void Executar()
        {
            var resumo = new List<string>();
            bool algumErro = false;

            foreach (var t in Trocas)
            {
                var (linha, erro) = Aplicar(t);
                resumo.Add(linha);
                algumErro |= erro;
            }

            string quebra = System.Environment.NewLine + "  ";
            string texto = $"{Marcador} Concluído:" + quebra + string.Join(quebra, resumo);

            if (algumErro) Debug.LogError(texto);
            else Debug.Log(texto);
        }

        private static (string Linha, bool Erro) Aplicar(Troca t)
        {
            string nomeDaCena = System.IO.Path.GetFileName(t.Cena);

            var variantes = t.Variantes
                .Select(v => AssetDatabase.LoadAssetAtPath<TileBase>($"{PastaDosTiles}/{v}.asset"))
                .Where(v => v != null)
                .ToArray();

            if (variantes.Length != t.Variantes.Length)
                return ($"{nomeDaCena}: só {variantes.Length} de {t.Variantes.Length} variantes " +
                        "carregaram — rode o passo 1 primeiro. NADA foi tocado.", true);

            var placeholder = AssetDatabase.LoadAssetAtPath<TileBase>(
                $"{PastaDosTiles}/{t.Placeholder}.asset");

            if (placeholder == null)
                return ($"{nomeDaCena}: placeholder '{t.Placeholder}' ausente. NADA foi tocado.",
                        true);

            var cena = EditorSceneManager.OpenScene(t.Cena, OpenSceneMode.Single);

            var mapas = cena.GetRootGameObjects()
                .SelectMany(r => r.GetComponentsInChildren<Tilemap>(true))
                .ToArray();

            // A CONTA QUE IMPORTA. Contar SetTile não responde se o chão continua lá -- foi
            // exatamente assim que a primeira versão apagou 9.104 células relatando sucesso.
            int antes = mapas.Sum(ContarCelulas);

            int trocadas = 0;
            var porVariante = new int[variantes.Length];

            foreach (var mapa in mapas)
            {
                var posicoes = new List<Vector3Int>();
                var tiles = new List<TileBase>();

                foreach (var celula in mapa.cellBounds.allPositionsWithin)
                {
                    if (mapa.GetTile(celula) != placeholder) continue;

                    // Hash da célula, não Random: a segunda execução tem de produzir
                    // exatamente o mesmo chão.
                    int h = celula.x * 73856093 ^ celula.y * 19349663;
                    int i = Mathf.Abs(h) % variantes.Length;

                    posicoes.Add(celula);
                    tiles.Add(variantes[i]);
                    porVariante[i]++;
                    trocadas++;
                }

                if (posicoes.Count == 0) continue;

                // SetTiles EM LOTE, e não SetTile uma a uma. Não é otimização: medido em
                // 2026-09-01 nesta versão da Unity, o SetTile singular grava NULL neste
                // tilemap -- inclusive numa célula vazia --, enquanto o SetTiles grava certo.
                // Foi o SetTile singular que apagou 9.104 células relatando sucesso.
                mapa.SetTiles(posicoes.ToArray(), tiles.ToArray());
            }

            int depois = mapas.Sum(ContarCelulas);

            if (depois < antes)
                return ($"{nomeDaCena}: RECUSADO — o chão iria de {antes} para {depois} células " +
                        $"({antes - depois} perdidas). A cena NÃO foi salva. Os Tile provavelmente " +
                        "ainda não estão importados: rode o passo 1 e tente de novo.", true);

            if (trocadas == 0)
                return ($"{nomeDaCena}: nada a trocar (já está com os tiles novos)", false);

            EditorSceneManager.MarkSceneDirty(cena);
            EditorSceneManager.SaveScene(cena);

            return ($"{nomeDaCena}: {trocadas} trocada(s) ({string.Join("/", porVariante)}), " +
                    $"total de células {antes} → {depois} — {t.Razao}", false);
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
