using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace FavelaAmarela.EditorTools
{
    /// <summary>
    /// Põe a colisão de tilemap no padrão que a Unity documenta: <b>um</b> corpo estático com
    /// <c>CompositeCollider2D</c> mesclando as células, na camada <c>Obstacle</c>.
    ///
    /// <para><b>Por que existe (2026-08-27).</b> As cinco cenas com parede de tilemap somavam
    /// <b>2 622 formas de colisão</b> — uma por célula pintada (1 708 no Castelo, 528 na Arena,
    /// 528 nos Portões, 270 nas Ruínas Pálidas, 116 no Santuário). Cada uma é um proxy próprio
    /// na broadphase, e cada uma tem arestas internas que só existem para encostar na vizinha.
    /// O <c>CompositeCollider2D</c> funde tudo no contorno.</para>
    ///
    /// <para><b>Isto já tinha sido tentado e abandonado.</b> O comentário em
    /// <c>BuildIsoCollisionFromFloor</c> registra: <i>"Sem Rigidbody/CompositeCollider2D de
    /// propósito: o Composite (bordas mescladas) exige Rigidbody e deu MissingComponentException;
    /// fica como polish futuro."</i> A exigência é real — o <c>CompositeCollider2D</c> tem
    /// <c>[RequireComponent(typeof(Rigidbody2D))]</c> —, e o cuidado que faltava é que o
    /// <c>Rigidbody2D</c> criado junto nasce <b>Dynamic</b>. Parede Dynamic com
    /// <c>gravityScale 0</c> não cai, mas é <b>empurrada</b>: o primeiro esbarrão do Damião
    /// levaria o mapa embora. Aqui ele é forçado a <c>Static</c>.</para>
    ///
    /// <para><b>Outlines, não Polygons.</b> A doc da 6.4 é explícita:
    /// <i>"This is usually the most efficient geometry to use as it produces far less edges.
    /// Continuous edges do not cause unwanted collisions because all edges are connected."</i>
    /// O preço é que <i>"nothing will collide in the interior of such geometry"</i> — quem já
    /// estiver <b>dentro</b> da parede não é expulso. Não é problema aqui: o anel de colisão
    /// tem duas células de espessura e todo ator que se move está em
    /// <c>CollisionDetectionMode2D.Continuous</c>, então ninguém entra sem cruzar uma aresta.
    /// <c>Polygons</c> é o que a doc chama de <i>"least efficient"</i> e serve para detectar o
    /// interior — caso de trigger, não de parede.</para>
    ///
    /// <para><b>A camada era o defeito silencioso do lote.</b> Quatro ferramentas constroem esse
    /// mesmo tilemap; três põem em <c>Obstacle</c> e a quarta
    /// (<c>MontarArenaDeTestes.MontarBordaDeColisao</c>) <b>nunca setou camada nenhuma</b>. As
    /// paredes da Arena e dos Portões ficaram na <c>Default</c>. Elas ainda barram o Damião — a
    /// matriz deixa <c>Default × Player</c> colidir —, mas <b>nenhuma consulta que pergunta por
    /// <c>Obstacle</c> as enxerga</b>: o Cone de Gelo do Abdul (<c>camadasQueBloqueiam</c> = 512)
    /// as atravessaria, e a linha de visão do Cortesão passaria através delas. O mesmo modo de
    /// falha da máscara vazia — a peça existe, não dá erro, e a checagem não acontece.</para>
    /// </summary>
    public static class ConsolidarColisaoDosTilemaps
    {
        private const string PastaDeCenas = "Assets/Scenes";
        private const string CamadaDeParede = "Obstacle";

        /// <summary>
        /// Extrusão por célula, em unidades de mundo. É o padrão da própria Unity para
        /// <c>TilemapCollider2D</c>. A doc: <i>"The amount of Collider shapes each Tile extrudes
        /// to facilitate compositing with neighboring Tiles. This eliminates fine gaps between
        /// Tiles when using a CompositeCollider2D."</i> As cenas estavam em <b>zero</b>, que é o
        /// valor que deixa duas células vizinhas dependerem de igualdade exata de ponto
        /// flutuante para não abrir uma fresta.
        /// </summary>
        private const float ExtrusaoPadrao = 0.00001f;

        [MenuItem("Tools/FavelaAmarela/Física: consolidar a colisão dos tilemaps")]
        public static void Executar()
        {
            var resumo = new List<string>();

            foreach (var caminho in Cenas())
            {
                var cena = EditorSceneManager.OpenScene(caminho, OpenSceneMode.Single);
                bool mudou = false;

                foreach (var colisor in Object.FindObjectsByType<TilemapCollider2D>(
                             FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    string linha = Tratar(colisor, ref mudou);
                    resumo.Add($"{Path.GetFileName(caminho)} · {linha}");
                }

                if (!mudou) continue;

                EditorSceneManager.MarkSceneDirty(cena);
                if (!EditorSceneManager.SaveScene(cena))
                    resumo.Add($"{Path.GetFileName(caminho)}: SaveScene RECUSOU");
            }

            AssetDatabase.SaveAssets();
            Debug.Log("[ColisaoDeTilemap] Concluído:\n  " + string.Join("\n  ", resumo));
        }

        /// <summary>
        /// As cenas saem do disco, não de uma lista: a Arena de Testes não está no Build
        /// Settings e é justamente onde se mede mudança de renderização e de colisão. Uma lista
        /// escrita à mão a deixaria de fora — é a décima vez que este projeto troca lista por
        /// derivação.
        /// </summary>
        private static IEnumerable<string> Cenas() =>
            Directory.GetFiles(PastaDeCenas, "*.unity", SearchOption.AllDirectories)
                     .Select(c => c.Replace('\\', '/'))
                     .OrderBy(c => c);

        private static string Tratar(TilemapCollider2D colisor, ref bool mudou)
        {
            var go = colisor.gameObject;
            var tilemap = go.GetComponent<Tilemap>();
            int celulas = ContarCelulas(tilemap);

            // ── Caso 1: tilemap sem célula nenhuma ───────────────────────────
            // O 'Batente' dos Portões é um SpriteRenderer decorativo (escala 10,1 × 9,1) que
            // ganhou um Tilemap vazio e um TilemapCollider2D junto. Zero célula = zero forma:
            // o colisor não colide com nada e o trigger nunca dispara. Não é uma parede a
            // consolidar, é peça morta a remover.
            if (celulas == 0)
            {
                Object.DestroyImmediate(colisor);
                if (tilemap != null) Object.DestroyImmediate(tilemap);

                var renderer = go.GetComponent<TilemapRenderer>();
                if (renderer != null) Object.DestroyImmediate(renderer);

                mudou = true;
                return $"'{go.name}': Tilemap VAZIO — colisor, tilemap e renderer removidos " +
                       "(prometiam colisão e não geravam forma nenhuma)";
            }

            var notas = new List<string>();
            Padronizar(colisor, notas);

            if (notas.Count > 0) mudou = true;

            if (notas.Count == 0)
                return $"'{go.name}': {celulas} células — já consolidado";

            var composto = go.GetComponent<CompositeCollider2D>();
            return $"'{go.name}': {celulas} células → {(composto != null ? composto.pathCount : 0)} " +
                   $"contorno(s); " + string.Join(", ", notas);
        }

        /// <summary>
        /// Põe <b>um</b> <c>TilemapCollider2D</c> no padrão de parede: camada <c>Obstacle</c>,
        /// corpo estático, e as células mescladas num <c>CompositeCollider2D</c> de contorno.
        ///
        /// <para>É público porque as ferramentas que <b>constroem</b> as cenas chamam daqui em
        /// vez de repetir a configuração. Já havia quatro cópias desse mesmo trecho espalhadas
        /// pelo Editor, e a quarta — a da Arena — foi a que esqueceu a camada. Repetir era o
        /// defeito; uma quinta cópia seria só a próxima a divergir.</para>
        /// </summary>
        /// <param name="colisor">O colisor do tilemap de parede.</param>
        /// <param name="notas">
        /// Recebe uma linha por ajuste feito. Vazio ao final = já estava no padrão. Pode ser
        /// <c>null</c> quando não interessa o relatório.
        /// </param>
        public static void Padronizar(TilemapCollider2D colisor, List<string> notas = null)
        {
            if (colisor == null) return;

            notas ??= new List<string>();
            var go = colisor.gameObject;

            // ── Camada ────────────────────────────────────────────────────────
            // Só a parede sólida vira Obstacle. Um tilemap trigger é gatilho de área, e pôr um
            // gatilho na camada de parede o faria bloquear consulta de linha de visão.
            if (!colisor.isTrigger)
            {
                int camada = LayerMask.NameToLayer(CamadaDeParede);
                if (camada < 0)
                {
                    notas.Add($"camada '{CamadaDeParede}' NÃO EXISTE no TagManager");
                }
                else if (go.layer != camada)
                {
                    notas.Add($"camada {LayerMask.LayerToName(go.layer)} → {CamadaDeParede}");
                    go.layer = camada;
                }
            }

            // ── Corpo estático ────────────────────────────────────────────────
            // O CompositeCollider2D exige Rigidbody2D e o cria Dynamic. Parede Dynamic é
            // parede que o Damião empurra — é este o passo que faltava na tentativa anterior.
            var corpo = go.GetComponent<Rigidbody2D>();
            if (corpo == null)
            {
                corpo = go.AddComponent<Rigidbody2D>();
                notas.Add("Rigidbody2D criado");
            }

            if (corpo.bodyType != RigidbodyType2D.Static)
            {
                notas.Add($"bodyType {corpo.bodyType} → Static");
                corpo.bodyType = RigidbodyType2D.Static;
            }

            // Static ignora gravidade, mas a skill favela-isometric-standards manda zerar em
            // TODO Rigidbody2D do projeto — e o dia em que alguém trocar o bodyType, o zero já
            // está lá.
            if (!Mathf.Approximately(corpo.gravityScale, 0f))
            {
                notas.Add($"gravityScale {corpo.gravityScale} → 0");
                corpo.gravityScale = 0f;
            }

            // ── Composite ─────────────────────────────────────────────────────
            var composite = go.GetComponent<CompositeCollider2D>();
            if (composite == null)
            {
                composite = go.AddComponent<CompositeCollider2D>();
                notas.Add("CompositeCollider2D criado");
            }

            if (composite.geometryType != CompositeCollider2D.GeometryType.Outlines)
            {
                composite.geometryType = CompositeCollider2D.GeometryType.Outlines;
                notas.Add("geometryType → Outlines");
            }

            // O composite passa a ditar isTrigger e sharedMaterial de quem ele mescla — a doc
            // diz que o Editor ignora esses campos no membro. Copiamos a intenção do tilemap
            // para o composite antes que ela se perca.
            if (composite.isTrigger != colisor.isTrigger)
            {
                composite.isTrigger = colisor.isTrigger;
                notas.Add($"composite.isTrigger → {colisor.isTrigger}");
            }

            if (colisor.compositeOperation != Collider2D.CompositeOperation.Merge)
            {
                colisor.compositeOperation = Collider2D.CompositeOperation.Merge;
                notas.Add("compositeOperation → Merge");
            }

            if (!Mathf.Approximately(colisor.extrusionFactor, ExtrusaoPadrao))
            {
                notas.Add($"extrusionFactor {colisor.extrusionFactor} → {ExtrusaoPadrao}");
                colisor.extrusionFactor = ExtrusaoPadrao;
            }
        }

        /// <summary>
        /// Quantas células o tilemap tem pintadas. <c>CompressBounds</c> primeiro porque
        /// <c>cellBounds</c> guarda a maior extensão que o tilemap já teve, não a atual.
        /// </summary>
        private static int ContarCelulas(Tilemap tilemap)
        {
            if (tilemap == null) return 0;

            tilemap.CompressBounds();

            int n = 0;
            foreach (var cell in tilemap.cellBounds.allPositionsWithin)
                if (tilemap.HasTile(cell)) n++;

            return n;
        }
    }
}
