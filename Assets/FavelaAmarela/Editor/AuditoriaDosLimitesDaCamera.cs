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
    /// Mede, cena a cena, <b>de onde a câmera consegue tirar os limites do mapa</b> — e se o
    /// que ela acha hoje cobre a área jogável.
    ///
    /// <para><b>Por que isto existe (2026-09-09).</b> O travamento da câmera foi escrito olhando
    /// o Deserto de Hali, onde as quatro paredes de borda se chamam <c>Limite_Norte</c>,
    /// <c>Limite_Sul</c>, <c>Limite_Leste</c> e <c>Limite_Oeste</c>. Eu li um cena e generalizei
    /// para seis. <b>Só o Deserto usa esse nome.</b> O Santuário tem as quatro paredes
    /// equivalentes chamadas <c>Parede_*</c>, e Castelo, Tumba e Portões não têm paredes
    /// nomeadas — a colisão deles é um Tilemap chamado <c>Colisao</c>.</para>
    ///
    /// <para>É o Corolário 2 do COMMANDMENT em forma pura: <i>"comparar atores lado a lado expõe
    /// o fora-da-curva; estudar um ator isolado o esconde"</i>. E o modo de falha dominante do
    /// projeto — código que existe, compila, tem teste verde, e não está ligado em 5 das 6
    /// cenas.</para>
    ///
    /// <para><b>Não escreve nada.</b> A pergunta que decide o conserto é qual fonte diz a
    /// verdade sobre a área jogável, e a resposta não cabe num palpite: um Tilemap de chão pode
    /// passar <i>além</i> das paredes (transbordo decorativo) ou parar <i>antes</i> delas
    /// (câmera mostraria vazio não pintado). Este relatório mede as duas e compara.</para>
    ///
    /// <para><b>Extensão declarada vs. pintada.</b> Reporta as duas. <c>Tilemap.localBounds</c>
    /// sai de <c>m_Size</c>, que é a área <i>alocada</i> — e este projeto já foi mordido pela
    /// mesma distinção quando o <c>m_TileAssetArray</c> guardava tiles com contagem zero e
    /// acusou mistura onde não havia. <c>CompressBounds()</c> encolhe para o que está de fato
    /// pintado. A cena é aberta e <b>nunca salva</b>, então a compressão não persiste.</para>
    /// </summary>
    public static class AuditoriaDosLimitesDaCamera
    {
        private const string Saida = "Docs/KnowledgeBundle/auditoria_limites_camera.md";
        private const string Marcador = "[LimitesDaCamera]";

        /// <summary>Os prefixos que o <c>ResolverLimites</c> da câmera procura hoje.</summary>
        private static readonly string[] PrefixosConhecidos = { "Limite_" };

        /// <summary>Prefixos que <b>também</b> nomeiam parede de borda neste projeto.</summary>
        private static readonly string[] PrefixosCandidatos = { "Limite_", "Parede_", "Muro_" };

        private sealed class Achado
        {
            public string Cena;
            public bool TemCamera;
            public float TamanhoOrtografico;
            public int ParedesConhecidas;
            public int ParedesCandidatas;
            public Bounds? PorParedesConhecidas;
            public Bounds? PorParedesCandidatas;
            public Bounds? PorTilemapsDeclarado;
            public Bounds? PorTilemapsPintado;
            public readonly List<string> Tilemaps = new List<string>();
        }

        [MenuItem("Tools/Camera/Auditar limites da câmera")]
        public static void Executar()
        {
            if (!Application.isBatchMode &&
                !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                Debug.Log(Marcador + " Cancelado — havia cena modificada por salvar.");
                return;
            }

            var achados = new List<Achado>();

            foreach (var entrada in EditorBuildSettings.scenes)
            {
                if (!entrada.enabled || !File.Exists(entrada.path)) continue;

                Scene cena = EditorSceneManager.OpenScene(entrada.path, OpenSceneMode.Single);
                var raizes = cena.GetRootGameObjects();

                var a = new Achado { Cena = Path.GetFileNameWithoutExtension(entrada.path) };

                // ── a câmera desta cena ──
                var camera = raizes.SelectMany(r => r.GetComponentsInChildren<Camera>(true))
                                   .FirstOrDefault(c => c.orthographic);
                a.TemCamera = camera != null;
                if (camera != null) a.TamanhoOrtografico = camera.orthographicSize;

                // ── paredes nomeadas ──
                var colisores = raizes
                    .SelectMany(r => r.GetComponentsInChildren<Collider2D>(true))
                    .ToArray();

                var conhecidas = colisores.Where(c => Comeca(c.name, PrefixosConhecidos)).ToArray();
                var candidatas = colisores.Where(c => Comeca(c.name, PrefixosCandidatos)).ToArray();

                a.ParedesConhecidas = conhecidas.Length;
                a.ParedesCandidatas = candidatas.Length;
                a.PorParedesConhecidas = Unir(conhecidas.Select(c => c.bounds));
                a.PorParedesCandidatas = Unir(candidatas.Select(c => c.bounds));

                // ── tilemaps ──
                var mapas = raizes.SelectMany(r => r.GetComponentsInChildren<Tilemap>(true))
                                  .ToArray();

                var declarados = new List<Bounds>();
                var pintados = new List<Bounds>();

                foreach (var mapa in mapas)
                {
                    a.Tilemaps.Add(mapa.name);
                    declarados.Add(ParaMundo(mapa, mapa.localBounds));

                    // Encolhe para o que está PINTADO. A cena nunca é salva, então não persiste.
                    mapa.CompressBounds();
                    pintados.Add(ParaMundo(mapa, mapa.localBounds));
                }

                a.PorTilemapsDeclarado = Unir(declarados);
                a.PorTilemapsPintado = Unir(pintados);

                achados.Add(a);

                Debug.Log($"{Marcador} {a.Cena}: {a.ParedesConhecidas} parede(s) 'Limite_*', " +
                          $"{a.ParedesCandidatas} candidata(s), {mapas.Length} tilemap(s).");
            }

            Escrever(achados);
        }

        private static bool Comeca(string nome, string[] prefixos)
            => prefixos.Any(p => nome.StartsWith(p, System.StringComparison.Ordinal));

        /// <summary>
        /// Leva a caixa local do tilemap para o mundo pelos <b>dois cantos</b>.
        ///
        /// <para>Serve porque os tilemaps deste projeto não giram — a skill isométrica proíbe
        /// rotação, e a profundidade é <c>sortingOrder</c>. Com rotação, dois cantos não
        /// bastariam.</para>
        /// </summary>
        private static Bounds ParaMundo(Tilemap mapa, Bounds local)
        {
            Vector3 a = mapa.transform.TransformPoint(local.min);
            Vector3 b = mapa.transform.TransformPoint(local.max);

            var caixa = new Bounds((a + b) * 0.5f, Vector3.zero);
            caixa.Encapsulate(a);
            caixa.Encapsulate(b);
            return caixa;
        }

        private static Bounds? Unir(IEnumerable<Bounds> caixas)
        {
            Bounds? uniao = null;

            foreach (var c in caixas)
            {
                if (uniao == null) { uniao = c; continue; }

                Bounds b = uniao.Value;
                b.Encapsulate(c);
                uniao = b;
            }

            return uniao;
        }

        private static string Caixa(Bounds? b)
            => b == null
                ? "—"
                : string.Format(CultureInfo.InvariantCulture,
                                "({0:0.0}, {1:0.0}) a ({2:0.0}, {3:0.0}) · {4:0.0} × {5:0.0}",
                                b.Value.min.x, b.Value.min.y, b.Value.max.x, b.Value.max.y,
                                b.Value.size.x, b.Value.size.y);

        private static void Escrever(List<Achado> achados)
        {
            var sb = new StringBuilder();

            sb.AppendLine("---");
            sb.AppendLine("type: Audit Report");
            sb.AppendLine("title: Limites da câmera por cena");
            sb.AppendLine("---");
            sb.AppendLine();
            sb.AppendLine("# Limites da câmera por cena");
            sb.AppendLine();
            sb.AppendLine("Gerado por `Tools/Camera/Auditar limites da câmera`. **Não escreve " +
                          "nada nas cenas.**");
            sb.AppendLine();

            // ── 1. o que a câmera acha HOJE ──
            sb.AppendLine("## 1. O que o `ResolverLimites` encontra hoje");
            sb.AppendLine();
            sb.AppendLine("Ele procura colisores cujo nome comece com `Limite_`.");
            sb.AppendLine();
            sb.AppendLine("| cena | câmera | `Limite_*` | trava hoje? |");
            sb.AppendLine("|---|---|---|---|");

            foreach (var a in achados)
                sb.AppendLine($"| {a.Cena} | {(a.TemCamera ? "sim" : "**NÃO**")} | " +
                              $"{a.ParedesConhecidas} | " +
                              $"{(a.ParedesConhecidas > 0 ? "sim" : "**não**")} |");

            int semTrava = achados.Count(a => a.ParedesConhecidas == 0);
            sb.AppendLine();
            sb.AppendLine($"**{semTrava} de {achados.Count} cenas não travam a câmera.**");
            sb.AppendLine();

            // ── 2. as fontes candidatas ──
            sb.AppendLine("## 2. As fontes candidatas, medidas");
            sb.AppendLine();
            sb.AppendLine("| cena | paredes nomeadas (`Limite_`/`Parede_`/`Muro_`) | tilemaps (pintado) |");
            sb.AppendLine("|---|---|---|");

            foreach (var a in achados)
                sb.AppendLine($"| {a.Cena} | {a.ParedesCandidatas}× {Caixa(a.PorParedesCandidatas)} | " +
                              $"{a.Tilemaps.Count}× {Caixa(a.PorTilemapsPintado)} |");

            sb.AppendLine();

            // ── 3. declarado vs pintado ──
            sb.AppendLine("## 3. Tilemap: extensão declarada vs. pintada");
            sb.AppendLine();
            sb.AppendLine("`localBounds` sai de `m_Size`, a área **alocada**. `CompressBounds()` " +
                          "encolhe para o que está de fato pintado. Divergência aqui significa " +
                          "que usar `localBounds` cru daria um limite maior que o nível real.");
            sb.AppendLine();
            sb.AppendLine("| cena | declarado | pintado | diferença |");
            sb.AppendLine("|---|---|---|---|");

            foreach (var a in achados)
            {
                string dif = "—";
                if (a.PorTilemapsDeclarado != null && a.PorTilemapsPintado != null)
                {
                    Vector3 d = a.PorTilemapsDeclarado.Value.size - a.PorTilemapsPintado.Value.size;
                    dif = string.Format(CultureInfo.InvariantCulture, "{0:0.0} × {1:0.0}", d.x, d.y);
                }

                sb.AppendLine($"| {a.Cena} | {Caixa(a.PorTilemapsDeclarado)} | " +
                              $"{Caixa(a.PorTilemapsPintado)} | {dif} |");
            }

            sb.AppendLine();

            // ── 4. a pergunta que decide o conserto ──
            sb.AppendLine("## 4. Parede vs. tilemap: qual diz a verdade?");
            sb.AppendLine();
            sb.AppendLine("Se o tilemap for **maior** que as paredes, travar por ele deixa a " +
                          "câmera passar da área jogável. Se for **menor**, travar pelas " +
                          "paredes deixa a câmera mostrar chão não pintado.");
            sb.AppendLine();
            sb.AppendLine("| cena | tilemap − parede (largura) | (altura) | leitura |");
            sb.AppendLine("|---|---|---|---|");

            foreach (var a in achados)
            {
                if (a.PorParedesCandidatas == null || a.PorTilemapsPintado == null)
                {
                    sb.AppendLine($"| {a.Cena} | — | — | " +
                                  (a.PorParedesCandidatas == null
                                      ? "sem parede nomeada: o tilemap é a **única** fonte"
                                      : "sem tilemap") + " |");
                    continue;
                }

                Vector3 d = a.PorTilemapsPintado.Value.size - a.PorParedesCandidatas.Value.size;
                string leitura = d.x > 0.5f || d.y > 0.5f
                    ? "tilemap **maior** — travar por ele passaria da área jogável"
                    : d.x < -0.5f || d.y < -0.5f
                        ? "tilemap **menor** — travar pela parede mostraria chão não pintado"
                        : "concordam";

                sb.AppendLine(string.Format(CultureInfo.InvariantCulture,
                    "| {0} | {1:0.0} | {2:0.0} | {3} |", a.Cena, d.x, d.y, leitura));
            }

            sb.AppendLine();

            string caminho = Path.Combine(Directory.GetCurrentDirectory(), Saida);
            Directory.CreateDirectory(Path.GetDirectoryName(caminho));
            File.WriteAllText(caminho, sb.ToString());

            Debug.Log($"{Marcador} Relatório escrito em {Saida} — {achados.Count} cena(s), " +
                      $"{semTrava} sem travamento.");
        }
    }
}
