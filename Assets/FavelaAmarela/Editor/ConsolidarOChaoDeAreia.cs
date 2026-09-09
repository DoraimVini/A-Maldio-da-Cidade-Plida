using System.Collections.Generic;
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
    /// Repinta com a <c>RuleTile_Areia</c> toda célula que hoje usa uma sprite de areia
    /// <b>crua</b>, para o chão de areia ter um dono só.
    ///
    /// <para><b>O defeito que isto fecha (2026-09-09).</b> O <c>DesertFloor</c> do Deserto de
    /// Hali tinha 25 252 células: <b>19 512 pela <c>RuleTile_Areia</c> e 5 740 com
    /// <c>sand_01/02/03/pebbles/crack</c> cruas</b> — e a regra sorteia exatamente essas cinco
    /// sprites, então as duas metades desenhavam a mesma coisa. A Tumba pintava 1 111 células só
    /// com areia crua, sem Rule Tile nenhum.</para>
    ///
    /// <para>Isso é invisível <b>hoje</b>, porque a regra da areia tem uma regra só, sem nenhuma
    /// condição de vizinhança. No dia em que alguém acrescentar uma borda — que é o motivo de
    /// existir um Rule Tile — cada célula crua vira <b>um buraco no terreno aos olhos da
    /// regra</b>: ela pergunta "este vizinho sou eu?", e uma célula crua responde que não, por
    /// mais idêntico que seja o pixel. As vizinhas passariam a desenhar borda contornando
    /// pedaços de chão. A costura seria fabricada pela mistura.</para>
    ///
    /// <para><b>O que muda visualmente.</b> A paleta é a mesma — a regra sorteia as mesmas cinco
    /// sprites —, mas <b>qual</b> delas cai em cada célula convertida passa a ser decidido pela
    /// regra e não pelo que estava pintado. Autorizado pelo Vini em 2026-09-09.</para>
    ///
    /// <para><b>Escreve com <c>SetTiles</c> em lote, nunca <c>SetTile</c>.</b> Não é otimização:
    /// medido em 2026-09-01 nesta mesma versão da Unity, em batch mode o singular <b>grava
    /// NULL</b> num tilemap que já tem conteúdo — apagou 9 104 células em três cenas enquanto a
    /// ferramenta relatava sucesso. E conta as células antes e depois, <b>recusando-se a
    /// salvar</b> se o total cair; foi essa guarda que pegou aquele estrago sozinha.</para>
    /// </summary>
    public static class ConsolidarOChaoDeAreia
    {
        private const string NomeDaRegra = "RuleTile_Areia";

        /// <summary>
        /// Os tiles crus que a <c>RuleTile_Areia</c> já sabe sortear. Só estes são trocados —
        /// é o que impede a ferramenta de pintar regra de areia no piso do Castelo.
        /// </summary>
        private static readonly string[] TilesCrus =
        {
            "sand_01", "sand_02", "sand_03", "sand_pebbles", "sand_crack",
        };

        [MenuItem("Tools/FavelaAmarela/Cena: consolidar o chão de areia na RuleTile")]
        public static void Executar()
        {
            if (!Application.isBatchMode &&
                !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                Debug.Log("[ChaoDeAreia] Cancelado — havia cena modificada por salvar.");
                return;
            }

            var regra = CarregarTile(NomeDaRegra);
            if (regra == null)
            {
                Debug.LogError($"[ChaoDeAreia] '{NomeDaRegra}' não encontrado. Nada foi tocado.");
                return;
            }

            var crus = new HashSet<TileBase>();
            foreach (var nome in TilesCrus)
            {
                var t = CarregarTile(nome);
                if (t != null) crus.Add(t);
                else Debug.LogWarning($"[ChaoDeAreia] Tile cru '{nome}' não encontrado — as " +
                                      "células dele, se existirem, ficam como estão.");
            }

            if (crus.Count == 0)
            {
                Debug.LogError("[ChaoDeAreia] Nenhum tile cru encontrado. Nada foi tocado.");
                return;
            }

            var log = new StringBuilder("[ChaoDeAreia]\n");

            foreach (var entrada in EditorBuildSettings.scenes)
            {
                if (!entrada.enabled || !File.Exists(entrada.path)) continue;

                Scene cena = EditorSceneManager.OpenScene(entrada.path, OpenSceneMode.Single);
                var (relato, salvar) = Consolidar(cena, regra, crus);

                log.AppendLine("   " + relato);

                if (!salvar) continue;

                EditorSceneManager.MarkSceneDirty(cena);
                EditorSceneManager.SaveScene(cena);
            }

            Debug.Log(log.ToString());
        }

        private static (string Relato, bool Salvar) Consolidar(
            Scene cena, TileBase regra, HashSet<TileBase> crus)
        {
            string nomeDaCena = cena.name;

            var mapas = cena.GetRootGameObjects()
                .SelectMany(r => r.GetComponentsInChildren<Tilemap>(true))
                .ToArray();

            if (mapas.Length == 0) return ($"{nomeDaCena}: sem tilemap", false);

            int antes = mapas.Sum(ContarCelulas);
            int trocadas = 0;

            foreach (var mapa in mapas)
            {
                var posicoes = new List<Vector3Int>();

                foreach (var celula in mapa.cellBounds.allPositionsWithin)
                    if (crus.Contains(mapa.GetTile(celula)))
                        posicoes.Add(celula);

                if (posicoes.Count == 0) continue;

                var tiles = new TileBase[posicoes.Count];
                for (int i = 0; i < tiles.Length; i++) tiles[i] = regra;

                // SetTiles EM LOTE, e não SetTile uma a uma. Ver a doc da classe: em batch mode
                // o singular grava NULL num tilemap com conteúdo, e já apagou 9 104 células
                // deste projeto relatando sucesso.
                mapa.SetTiles(posicoes.ToArray(), tiles);
                trocadas += posicoes.Count;
            }

            if (trocadas == 0)
                // A mensagem importa: "nada a consolidar" numa cena que nunca teve areia
                // (Castelo, Portões, Santuário usam outro piso) NÃO é a mesma coisa que numa
                // cena já consolidada, e a primeira versão dizia "já é tudo RuleTile_Areia"
                // para as duas. Log que mente é pior que log ausente.
                return ($"{nomeDaCena}: nenhuma célula de areia crua ({antes} célula(s) de " +
                        "outro piso, intocadas)", false);

            int depois = mapas.Sum(ContarCelulas);

            if (depois < antes)
                return ($"{nomeDaCena}: RECUSADO — o chão iria de {antes} para {depois} células " +
                        $"({antes - depois} perdidas). A cena NÃO foi salva.", false);

            return ($"{nomeDaCena}: {trocadas} célula(s) consolidada(s); total {antes} -> {depois}",
                    true);
        }

        private static int ContarCelulas(Tilemap mapa)
        {
            int n = 0;
            foreach (var celula in mapa.cellBounds.allPositionsWithin)
                if (mapa.GetTile(celula) != null) n++;
            return n;
        }

        private static TileBase CarregarTile(string nome)
        {
            foreach (var guid in AssetDatabase.FindAssets($"{nome} t:TileBase"))
            {
                string caminho = AssetDatabase.GUIDToAssetPath(guid);
                if (Path.GetFileNameWithoutExtension(caminho) != nome) continue;

                var tile = AssetDatabase.LoadAssetAtPath<TileBase>(caminho);
                if (tile != null) return tile;
            }

            return null;
        }
    }
}
