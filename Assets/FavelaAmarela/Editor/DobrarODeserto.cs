using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace FavelaAmarela.EditorTools
{
    /// <summary>
    /// Dobra o Deserto de Hali: <b>o dobro em cada eixo</b>, quatro vezes a área.
    ///
    /// <para><b>Os números que motivaram (auditoria de 2026-08-31).</b> A área jogável era
    /// <b>43 × 31 unidades</b> — atravessável andando em <b>10 segundos</b>. Pior: a
    /// <c>Chegada_TumbaAlhazred</c> ficava a <b>5,2 unidades</b> da <c>Entrada_TumbaAlhazred</c>,
    /// e o raio de ruído do Damião andando é <b>5,5</b>. Marcos <b>mais próximos entre si do que
    /// o alcance do próprio barulho do jogador</b> — não havia decisão espacial a tomar, nem de
    /// rota nem de aproximação.</para>
    ///
    /// <para><b>Multiplica posição, não escala.</b> Dobrar o <c>localScale</c> dos objetos
    /// deixaria tudo gigante; dobrar a <b>posição</b> mantém cada peça do tamanho que é e
    /// afasta uma da outra. A topologia que o Vini desenhou — Tumba a oeste, Santuário a
    /// noroeste, Templo a leste, Portões ao norte, chegada ao sul — fica <b>intacta</b>, e só as
    /// distâncias crescem.</para>
    ///
    /// <para><b>As paredes-limite são a exceção</b>: elas precisam do comprimento dobrado além
    /// da posição, senão o mapa novo vaza pelas pontas.</para>
    ///
    /// <para><b>Idempotência é impossível aqui</b>, e por isso a ferramenta <b>verifica antes de
    /// agir</b>: se os limites já estiverem na largura dobrada, ela recusa. Rodar duas vezes
    /// quadruplicaria o mapa em silêncio.</para>
    /// </summary>
    public static class DobrarODeserto
    {
        private const string Marcador = "[Deserto]";
        private const string Cena = "Assets/Scenes/Deserto_Hali.unity";

        private const float Fator = 2f;

        /// <summary>Meia-largura esperada ANTES de dobrar. É o que impede a segunda passagem.</summary>
        private const float MeiaLarguraOriginal = 21.5f;

        /// <summary>
        /// Os grupos cujos <b>filhos</b> se afastam. Os grupos em si estão na origem; quem
        /// carrega posição é o filho, então espalhar significa mexer neles.
        /// </summary>
        private static readonly string[] GruposQueEspalham =
        {
            "Deserto_Root",
            "Inimigos_Deserto",
            "Consumiveis_Deserto",
            "Refugios",
            "Setores_Tempestade",
        };

        /// <summary>Objetos soltos na raiz que também se afastam.</summary>
        private static readonly string[] SoltosQueMovem =
        {
            "Chegada_TumbaAlhazred",
            "Chegada_VoltaDoSantuario",
            "Fragmento_0",
            "Travessia_YugNeth",
        };

        /// <summary>
        /// Quem <b>não</b> se move, com a razão. Gerenciadores e a câmera vivem fora do espaço
        /// do mundo; movê-los não afasta nada e pode tirá-los do lugar que o bootstrap espera.
        /// </summary>
        private static readonly string[] NaoMovem =
        {
            "Main Camera", "EventSystem", "GameManager", "GerenciadorDeSave",
            "MixerDeAudio", "TempestadeDeMemoria", "DesertFloorGrid",
        };

        [MenuItem("Tools/FavelaAmarela/Deserto: dobrar o tamanho do mapa")]
        public static void Executar()
        {
            var cena = EditorSceneManager.OpenScene(Cena, OpenSceneMode.Single);
            var resumo = new List<string>();

            var raizes = cena.GetRootGameObjects();

            // ── A guarda contra a segunda passagem ────────────────────────────
            var limite = raizes.SelectMany(r => r.GetComponentsInChildren<Transform>(true))
                               .FirstOrDefault(t => t.name == "Limite_Leste");

            if (limite == null)
            {
                Debug.LogError($"{Marcador} 'Limite_Leste' não encontrado — sem ele não dá para " +
                               "saber se o mapa já foi dobrado, e rodar às cegas quadruplicaria.");
                return;
            }

            if (Mathf.Abs(limite.position.x) > MeiaLarguraOriginal * 1.5f)
            {
                Debug.LogError($"{Marcador} RECUSADO: o mapa já parece dobrado " +
                               $"(Limite_Leste em x={limite.position.x:0.#}, esperava " +
                               $"~{MeiaLarguraOriginal}). Rodar de novo quadruplicaria em " +
                               "silêncio.");
                return;
            }

            // ── Espalhar ──────────────────────────────────────────────────────
            int movidos = 0;

            foreach (var nome in GruposQueEspalham)
            {
                var grupo = raizes.FirstOrDefault(r => r.name == nome);
                if (grupo == null) { resumo.Add($"{nome}: AUSENTE"); continue; }

                int n = 0;
                foreach (Transform filho in grupo.transform)
                {
                    filho.localPosition *= Fator;
                    n++;
                }

                movidos += n;
                resumo.Add($"{nome}: {n} filho(s) afastados");
            }

            foreach (var nome in SoltosQueMovem)
            {
                var go = raizes.FirstOrDefault(r => r.name == nome);
                if (go == null) { resumo.Add($"{nome}: AUSENTE"); continue; }

                go.transform.position *= Fator;
                movidos++;
            }

            resumo.Add($"{SoltosQueMovem.Length} objeto(s) solto(s) afastados");

            // ── As paredes crescem, além de se afastar ────────────────────────
            foreach (var t in raizes.SelectMany(r => r.GetComponentsInChildren<Transform>(true))
                         .Where(t => t.name.StartsWith("Limite_")))
            {
                var e = t.localScale;

                // Só o eixo LONGO dobra. Dobrar os dois engrossaria a parede, comendo área
                // jogável justamente nas bordas.
                t.localScale = e.x > e.y
                    ? new Vector3(e.x * Fator, e.y, e.z)
                    : new Vector3(e.x, e.y * Fator, e.z);

                resumo.Add($"{t.name}: comprimento {(e.x > e.y ? e.x : e.y):0} → " +
                           $"{(e.x > e.y ? e.x : e.y) * Fator:0}");
            }

            // ── O chão precisa cobrir o mapa novo ─────────────────────────────
            resumo.Add(RepintarOChao(raizes));

            EditorSceneManager.MarkSceneDirty(cena);
            EditorSceneManager.SaveScene(cena);

            string quebra = System.Environment.NewLine + "  ";
            Debug.Log($"{Marcador} Concluído — {movidos} objeto(s) afastados:" + quebra +
                      string.Join(quebra, resumo));
        }

        /// <summary>
        /// Estende o tilemap de chão até os limites novos, pintando com o
        /// <c>RuleTile_Areia</c> — que sorteia entre as cinco variações, então a área nova não
        /// sai com a repetição visível que o chão atual tem.
        /// </summary>
        private static string RepintarOChao(GameObject[] raizes)
        {
            var chao = raizes.SelectMany(r => r.GetComponentsInChildren<Tilemap>(true))
                             .FirstOrDefault(t => t.name.Contains("Floor"));

            if (chao == null) return "chão: nenhum Tilemap com 'Floor' no nome — NÃO repintado";

            var pincel = AssetDatabase.LoadAssetAtPath<TileBase>(
                "Assets/FavelaAmarela/Art/Tiles/Regras/RuleTile_Areia.asset");

            if (pincel == null)
                return "chão: RuleTile_Areia ausente — rode 'Arte: montar os Rule Tiles' antes";

            var limites = raizes.SelectMany(r => r.GetComponentsInChildren<Transform>(true))
                                .Where(t => t.name.StartsWith("Limite_"))
                                .ToArray();

            if (limites.Length == 0) return "chão: sem Limite_* para medir a área — NÃO repintado";

            float maxX = limites.Max(t => Mathf.Abs(t.position.x));
            float maxY = limites.Max(t => Mathf.Abs(t.position.y));

            var grade = chao.layoutGrid;
            var canto = grade.WorldToCell(new Vector3(-maxX, -maxY, 0f));
            var oposto = grade.WorldToCell(new Vector3(maxX, maxY, 0f));

            int minX = Mathf.Min(canto.x, oposto.x) - 2;
            int maxCx = Mathf.Max(canto.x, oposto.x) + 2;
            int minY = Mathf.Min(canto.y, oposto.y) - 2;
            int maxCy = Mathf.Max(canto.y, oposto.y) + 2;

            int pintados = 0;

            for (int x = minX; x <= maxCx; x++)
            for (int y = minY; y <= maxCy; y++)
            {
                var celula = new Vector3Int(x, y, 0);

                // Só onde falta. Repintar o que já existe trocaria o chão autorado à mão pela
                // regra, e isso é decisão do Vini, não efeito colateral de aumentar o mapa.
                if (chao.HasTile(celula)) continue;

                chao.SetTile(celula, pincel);
                pintados++;
            }

            chao.CompressBounds();

            return $"chão: {pintados} célula(s) novas pintadas com RuleTile_Areia " +
                   $"(área agora {maxX * 2:0} × {maxY * 2:0})";
        }
    }
}
