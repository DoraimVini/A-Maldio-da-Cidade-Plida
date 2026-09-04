using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using FavelaAmarela.Runtime.Diagnostico;

namespace FavelaAmarela.EditorTools
{
    /// <summary>
    /// Varre <b>todos os prefabs</b> e <b>todas as cenas do Build Settings</b> e relata a
    /// configuração de física de cada <see cref="Rigidbody2D"/> e a geometria de cada
    /// <see cref="Collider2D"/>, mais os dois pareamentos que costumam estar errados sem
    /// ninguém notar. Sai em dois relatórios: <c>auditoria_rigidbody2d.md</c> e
    /// <c>auditoria_colisores.md</c>.
    ///
    /// <para><b>A medida de colisor não mora aqui.</b> Ela vive em
    /// <see cref="FavelaAmarela.Runtime.Diagnostico.AuditoriaDeColisores"/>, no Runtime, porque
    /// o jogo rodando precisa da <b>mesma</b> conta — o Editor vê o objeto como está no disco,
    /// e só em Play existem a hurtbox criada em <c>Awake</c>, o colisor desligado por i-frame e
    /// o inimigo instanciado por spawner. Duas cópias da mesma medida divergiriam calada, e a
    /// que ninguém olhasse envelheceria errada.</para>
    ///
    /// <para><b>Por que uma ferramenta e não um teste.</b> Já existe guarda de teste para as
    /// pegadas (<c>ColisoresDoElencoTests</c>) e para a hurtbox
    /// (<c>GolpeAlcancaAHurtboxTests</c>). Esta aqui não afirma nada: ela <b>mede e relata</b>,
    /// para uma leitura humana decidir o que é defeito. Fixar em teste o <c>mass</c> ou o
    /// <c>sleepMode</c> de cada corpo travaria ajuste legítimo de design.</para>
    ///
    /// <para><b>Duas correções à especificação, medidas em 2026-09-04:</b></para>
    /// <list type="number">
    ///   <item><b><c>Assets/Prefabs</c> não existe neste projeto.</b> Os 19 prefabs vivem em
    ///   <c>Assets/FavelaAmarela/Art/**</c> (9 só em <c>Enemies</c>) e em
    ///   <c>Assets/FavelaAmarela/Resources</c>. A varredura usa <c>t:Prefab</c> sobre
    ///   <c>Assets</c> inteira, então não depende de convenção de pasta.</item>
    ///   <item><b><c>drag</c> e <c>angularDrag</c> não existem na Unity 6000.4.</b> Foram
    ///   renomeados para <c>linearDamping</c> e <c>angularDamping</c> — a mesma classe de
    ///   rename que pegou <c>velocity</c> → <c>linearVelocity</c>. Conferido na Script
    ///   Reference offline da versão exata do projeto.</item>
    /// </list>
    ///
    /// <para><b>As cenas são abertas em modo aditivo e fechadas em seguida</b>, e o usuário é
    /// consultado antes sobre cenas modificadas. Ler o YAML seria mais barato, mas devolveria
    /// os campos crus em vez do que a Unity de fato aplica.</para>
    /// </summary>
    public static class Rigidbody2DAuditor
    {
        private const string Saida = "Docs/KnowledgeBundle/systems/auditoria_rigidbody2d.md";

        private const string SaidaColisores =
            "Docs/KnowledgeBundle/systems/auditoria_colisores.md";

        private const string SaidaGatilhos =
            "Docs/KnowledgeBundle/systems/auditoria_gatilhos.md";

        /// <summary>Uma linha de gatilho, mais de onde ela veio.</summary>
        private struct GatilhoMedido
        {
            public string Origem;
            public AuditoriaDeGatilhos.Linha L;
        }

        /// <summary>Uma medida de colisor, mais de onde ela veio.</summary>
        private struct ColisorMedido
        {
            public string Origem;
            public AuditoriaDeColisores.Medida M;
        }

        /// <summary>Uma linha do relatório.</summary>
        private sealed class Corpo
        {
            public string Origem;      // prefab ou cena
            public string Caminho;     // hierarquia dentro dele
            public RigidbodyType2D Tipo;
            public float Gravidade, Massa, AmortLinear, AmortAngular;
            public RigidbodyConstraints2D Travas;
            public CollisionDetectionMode2D Deteccao;
            public RigidbodyInterpolation2D Interpolacao;
            public RigidbodySleepMode2D Sono;
            public int Colisores;
        }

        /// <summary>Um colisor sem corpo, ou um corpo sem colisor.</summary>
        private sealed class Desparelhado
        {
            public string Origem, Caminho, Problema;
        }

        [MenuItem("Tools/FavelaAmarela/Auditar Física 2D")]
        public static void Auditar()
        {
            // O diálogo só existe com alguém na frente. Em batch mode ele travaria a
            // execução esperando um clique que nunca vem -- e é assim que a ferramenta roda
            // em Tools/run_editor_tool.ps1.
            if (!Application.isBatchMode &&
                !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                Debug.Log("[Rigidbody2DAuditor] Cancelado — havia cena modificada por salvar.");
                return;
            }

            var corpos = new List<Corpo>();
            var soltos = new List<Desparelhado>();
            var colisores = new List<ColisorMedido>();
            var gatilhos = new List<GatilhoMedido>();

            VarrerPrefabs(corpos, soltos, colisores, gatilhos);
            int cenas = VarrerCenasDoBuild(corpos, soltos, colisores, gatilhos);

            Escrever(corpos, soltos, cenas);
            EscreverColisores(colisores, cenas);
            EscreverGatilhos(gatilhos, cenas);

            Resumir(corpos, soltos, cenas);
            ResumirColisores(colisores);
            ResumirGatilhos(gatilhos);
        }

        // ── varreduras ───────────────────────────────────────────────────────

        private static void VarrerPrefabs(List<Corpo> corpos, List<Desparelhado> soltos,
                                          List<ColisorMedido> colisores,
                                          List<GatilhoMedido> gatilhos)
        {
            foreach (var guid in AssetDatabase.FindAssets("t:Prefab"))
            {
                string caminho = AssetDatabase.GUIDToAssetPath(guid);
                var raiz = AssetDatabase.LoadAssetAtPath<GameObject>(caminho);
                if (raiz == null) continue;

                Colher(raiz, Path.GetFileNameWithoutExtension(caminho), corpos, soltos,
                       colisores, gatilhos);
            }
        }

        private static int VarrerCenasDoBuild(List<Corpo> corpos, List<Desparelhado> soltos,
                                              List<ColisorMedido> colisores,
                                              List<GatilhoMedido> gatilhos)
        {
            int lidas = 0;

            foreach (var entrada in EditorBuildSettings.scenes)
            {
                if (!entrada.enabled || !File.Exists(entrada.path)) continue;

                Scene cena = EditorSceneManager.OpenScene(entrada.path, OpenSceneMode.Additive);
                lidas++;

                foreach (var raiz in cena.GetRootGameObjects())
                    Colher(raiz, Path.GetFileNameWithoutExtension(entrada.path), corpos,
                           soltos, colisores, gatilhos);

                EditorSceneManager.CloseScene(cena, removeScene: true);
            }

            return lidas;
        }

        /// <summary>Percorre a hierarquia inteira, inclusive objetos desativados.</summary>
        private static void Colher(GameObject raiz, string origem,
                                   List<Corpo> corpos, List<Desparelhado> soltos,
                                   List<ColisorMedido> colisores,
                                   List<GatilhoMedido> gatilhos)
        {
            // A GEOMETRIA É MEDIDA PELA CLASSE DE RUNTIME, não por uma cópia daqui. É a mesma
            // AuditoriaDeColisores que o jogo chama com Shift+F11 -- duas contas separadas
            // divergiriam em silêncio, e a que ninguém olhasse envelheceria errada.
            var medidas = new List<AuditoriaDeColisores.Medida>();
            AuditoriaDeColisores.Medir(raiz, medidas);
            foreach (var m in medidas)
                colisores.Add(new ColisorMedido { Origem = origem, M = m });

            var linhas = new List<AuditoriaDeGatilhos.Linha>();
            AuditoriaDeGatilhos.Medir(raiz, linhas);
            foreach (var l in linhas)
                gatilhos.Add(new GatilhoMedido { Origem = origem, L = l });

            foreach (var t in raiz.GetComponentsInChildren<Transform>(includeInactive: true))
            {
                var rb = t.GetComponent<Rigidbody2D>();
                // Colisores do PRÓPRIO objeto e dos filhos: um Rigidbody2D governa os
                // colisores abaixo dele, então contar só o próprio acusaria falso.
                var cols = t.GetComponentsInChildren<Collider2D>(includeInactive: true);

                if (rb != null)
                {
                    corpos.Add(new Corpo
                    {
                        Origem = origem,
                        Caminho = Hierarquia(t),
                        Tipo = rb.bodyType,
                        Gravidade = rb.gravityScale,
                        Massa = rb.mass,
                        // Unity 6000.4: 'drag'/'angularDrag' não existem mais.
                        AmortLinear = rb.linearDamping,
                        AmortAngular = rb.angularDamping,
                        Travas = rb.constraints,
                        Deteccao = rb.collisionDetectionMode,
                        Interpolacao = rb.interpolation,
                        Sono = rb.sleepMode,
                        Colisores = cols.Length,
                    });

                    if (rb.bodyType == RigidbodyType2D.Dynamic && cols.Length == 0)
                        soltos.Add(new Desparelhado
                        {
                            Origem = origem, Caminho = Hierarquia(t),
                            Problema = "Rigidbody2D **Dynamic** sem nenhum Collider2D — o corpo " +
                                       "é simulado, cai no vazio e não colide com nada.",
                        });

                    continue;
                }

                // Colisor SÓLIDO sem corpo em lugar nenhum acima = colisor estático. Correto
                // para cenário; caro e propenso a atravessamento se o objeto se mover.
                var proprio = t.GetComponent<Collider2D>();
                if (proprio == null || proprio.isTrigger) continue;
                if (t.GetComponentInParent<Rigidbody2D>() != null) continue;

                soltos.Add(new Desparelhado
                {
                    Origem = origem, Caminho = Hierarquia(t),
                    Problema = "Collider2D **sólido** sem Rigidbody2D — é colisor estático. " +
                               "Correto se nunca se move; se mover, a Unity reconstrói a árvore " +
                               "estática a cada passo e o atravessamento fica provável.",
                });
            }
        }

        private static string Hierarquia(Transform t)
        {
            var partes = new List<string>();
            for (var a = t; a != null; a = a.parent) partes.Add(a.name);
            partes.Reverse();
            return string.Join("/", partes);
        }

        // ── saída ────────────────────────────────────────────────────────────

        private static string Travas(RigidbodyConstraints2D c)
        {
            if (c == RigidbodyConstraints2D.None) return "—";
            var p = new List<string>();
            if ((c & RigidbodyConstraints2D.FreezePositionX) != 0) p.Add("PosX");
            if ((c & RigidbodyConstraints2D.FreezePositionY) != 0) p.Add("PosY");
            if ((c & RigidbodyConstraints2D.FreezeRotation) != 0) p.Add("Rot");
            return string.Join("+", p);
        }

        private static void Escrever(List<Corpo> corpos, List<Desparelhado> soltos, int cenas)
        {
            var md = new StringBuilder();
            md.AppendLine("---");
            md.AppendLine("type: Game System");
            md.AppendLine("title: Auditoria de Rigidbody2D");
            md.AppendLine("description: Configuração de física de todo Rigidbody2D em prefabs e " +
                          "cenas do Build Settings. Gerado por Tools/FavelaAmarela/Auditar Rigidbody2D.");
            md.AppendLine($"date: {System.DateTime.Now:yyyy-MM-dd}");
            md.AppendLine("---");
            md.AppendLine();
            md.AppendLine("# Auditoria de Rigidbody2D");
            md.AppendLine();
            md.AppendLine("> **Gerado por ferramenta.** Rode " +
                          "`Tools/FavelaAmarela/Auditar Rigidbody2D` para atualizar — edições à " +
                          "mão neste arquivo são perdidas.");
            md.AppendLine();
            md.AppendLine($"{corpos.Count} corpo(s) em {cenas} cena(s) do Build Settings e nos " +
                          "prefabs do projeto.");
            md.AppendLine();
            md.AppendLine("| origem | objeto | tipo | grav | massa | amort. lin | amort. ang | " +
                          "travas | detecção | interpolação | sono | colisores |");
            md.AppendLine("|---|---|---|---|---|---|---|---|---|---|---|---|");

            foreach (var c in corpos.OrderBy(x => x.Origem).ThenBy(x => x.Caminho))
                md.AppendLine($"| {c.Origem} | `{c.Caminho}` | {c.Tipo} | {c.Gravidade:0.##} | " +
                              $"{c.Massa:0.##} | {c.AmortLinear:0.##} | {c.AmortAngular:0.##} | " +
                              $"{Travas(c.Travas)} | {c.Deteccao} | {c.Interpolacao} | " +
                              $"{c.Sono} | {c.Colisores} |");

            md.AppendLine();
            md.AppendLine("## Pareamentos suspeitos");
            md.AppendLine();

            if (soltos.Count == 0)
            {
                md.AppendLine("Nenhum. Todo corpo Dynamic tem colisor, e todo colisor sólido " +
                              "tem corpo ou é cenário parado de propósito.");
            }
            else
            {
                md.AppendLine("| origem | objeto | problema |");
                md.AppendLine("|---|---|---|");
                foreach (var d in soltos.OrderBy(x => x.Origem).ThenBy(x => x.Caminho))
                    md.AppendLine($"| {d.Origem} | `{d.Caminho}` | {d.Problema} |");
            }

            md.AppendLine();
            md.AppendLine("> **Nota da 6000.4:** as colunas de amortecimento são " +
                          "`linearDamping` e `angularDamping`. `drag` e `angularDrag` **não " +
                          "existem mais** nesta versão da Unity.");

            Directory.CreateDirectory(Path.GetDirectoryName(Saida));
            File.WriteAllText(Saida, md.ToString());
            AssetDatabase.Refresh();
        }

        /// <summary>
        /// Escreve o relatório de colisores — o que cada um é, quanto mede, e onde ele destoa
        /// do <b>esperado para o papel dele</b>.
        ///
        /// <para><b>Por que não há uma coluna "% do sprite".</b> Era o que o pedido descrevia e
        /// é o que não serve aqui: medido, a hurtbox deste projeto nasce em 0,72 × 0,86 da
        /// silhueta e a pegada de movimento é uma área de chão de 0,60 × 0,30 num corpo de
        /// 1,00 × 2,53. As duas são deliberadas. Um limiar cego de 20% contra o sprite marcaria
        /// <b>todo ator do elenco</b> e o relatório não distinguiria mais nada. A coluna
        /// "queixa" compara contra o esperado do papel; ver
        /// <see cref="AuditoriaDeColisores"/>.</para>
        ///
        /// <para><b>Ressalva de prefab:</b> num prefab não instanciado as posições são locais à
        /// raiz, não do mundo. Como a comparação é sempre colisor <b>contra</b> sprite do mesmo
        /// objeto, a diferença fecha; só não leia a coluna de centro como coordenada de
        /// cena.</para>
        /// </summary>
        private static void EscreverColisores(List<ColisorMedido> colisores, int cenas)
        {
            var md = new StringBuilder();
            md.AppendLine("---");
            md.AppendLine("type: Game System");
            md.AppendLine("title: Auditoria de Colisores 2D");
            md.AppendLine("description: Forma, tamanho, offset e material de todo Collider2D em " +
                          "prefabs e cenas do Build Settings, comparado ao corpo desenhado. " +
                          "Gerado por Tools/FavelaAmarela/Auditar Física 2D.");
            md.AppendLine($"date: {System.DateTime.Now:yyyy-MM-dd}");
            md.AppendLine("---");
            md.AppendLine();
            md.AppendLine("# Auditoria de Colisores 2D");
            md.AppendLine();
            md.AppendLine("> **Gerado por ferramenta.** Rode " +
                          "`Tools/FavelaAmarela/Auditar Física 2D` para atualizar — edições à " +
                          "mão neste arquivo são perdidas.");
            md.AppendLine();
            md.AppendLine($"{colisores.Count} colisor(es) em {cenas} cena(s) do Build Settings " +
                          "e nos prefabs do projeto.");
            md.AppendLine();
            md.AppendLine("## Como ler a coluna \"queixa\"");
            md.AppendLine();
            md.AppendLine("Comparar todo colisor com o sprite inteiro **marcaria 100% do " +
                          "elenco**, porque os dois maiores desvios são de propósito:");
            md.AppendLine();
            md.AppendLine("- a **hurtbox** nasce em `0,72 × 0,86` da silhueta " +
                          "(`Hurtbox.GarantirPara`) — sozinho isso já daria −28% e −14%;");
            md.AppendLine("- a **pegada de movimento** é uma área de *chão* de `0,60 × 0,30`, " +
                          "na proporção 2:1 da célula isométrica, num corpo que o sprite " +
                          "desenha com ~2,5 de altura — daria −40% e −88%.");
            md.AppendLine();
            md.AppendLine("Comparar **por papel** também não bastou enquanto o papel era " +
                          "grosseiro: a primeira versão chamava de pegada todo colisor sólido " +
                          "e acusou 57 de 141 — entre eles as paredes do Santuário, o Lago de " +
                          "Hali e os limites do Deserto, que não têm pegada a respeitar. O que " +
                          "vale hoje:");
            md.AppendLine();
            md.AppendLine("| papel | o que é | o que se confere |");
            md.AppendLine("|---|---|---|");
            md.AppendLine("| `Hurtbox` | camada 13/14, ou componente `Hurtbox` | tamanho " +
                          "contra a silhueta × `0,72 / 0,86`, e o centro (limiares **20%** e " +
                          "**0,2** unidade) |");
            md.AppendLine("| `Pegada` | colisor sólido de um **ator** — corpo não-estático " +
                          "*mais* sprite | a **proporção de chão** (2:1 ±0,5) e a **linha do " +
                          "pé** (±0,2). *Não* o tamanho absoluto |");
            md.AppendLine("| `Cenario` | colisor sólido sem corpo ou sem sprite: parede, " +
                          "tilemap, limite de mapa, portão | nada |");
            md.AppendLine("| `Gatilho` | trigger que não é hurtbox: zona, portal, coletável | " +
                          "nada |");
            md.AppendLine();
            md.AppendLine("> **Por que a `Pegada` não confere tamanho.** " +
                          "`ColisoresDoElencoTests` já guarda os quatro humanos " +
                          "(Damião, Cultista, Abdul, EspectroHali) pelo caminho do prefab, com " +
                          "a pegada calibrada de `0,60 × 0,30`. Repetir a regra aqui, com uma " +
                          "identificação pior, criaria a segunda fonte da verdade que o doc " +
                          "daquele arquivo chama de modo de falha mais repetido do projeto. " +
                          "A proporção e a linha do pé, essas, **ninguém mais confere** — e " +
                          "valem para qualquer espécie.");
            md.AppendLine();

            var comQueixa = colisores.Where(c => !string.IsNullOrEmpty(c.M.Queixa)).ToList();

            md.AppendLine("## Fora do esperado");
            md.AppendLine();
            if (comQueixa.Count == 0)
            {
                md.AppendLine("Nenhum. Todo colisor bate com o esperado para o papel dele.");
            }
            else
            {
                md.AppendLine("| origem | objeto | tipo | papel | queixa |");
                md.AppendLine("|---|---|---|---|---|");
                foreach (var c in comQueixa.OrderBy(x => x.Origem).ThenBy(x => x.M.Caminho))
                    md.AppendLine($"| {c.Origem} | `{c.M.Caminho}` | {c.M.Tipo} | " +
                                  $"{c.M.Funcao} | {c.M.Queixa} |");
            }

            md.AppendLine();
            md.AppendLine("## Todos os colisores");
            md.AppendLine();
            md.AppendLine("Tamanho e centro em **unidades de mundo** (já multiplicados pela " +
                          "escala); offset é local, como aparece no Inspector.");
            md.AppendLine();
            md.AppendLine("| origem | objeto | tipo | papel | tam. (L×A) | raio | offset | " +
                          "centro | trigger | composição | material | sprite (L×A) |");
            md.AppendLine("|---|---|---|---|---|---|---|---|---|---|---|---|");

            foreach (var c in colisores.OrderBy(x => x.Origem).ThenBy(x => x.M.Caminho))
            {
                var m = c.M;
                md.AppendLine(
                    $"| {c.Origem} | `{m.Caminho}` | {m.Tipo} | {m.Funcao} | " +
                    $"{m.Tamanho.x:0.##}×{m.Tamanho.y:0.##} | " +
                    $"{(m.Raio > 0f ? m.Raio.ToString("0.##") : "—")} | " +
                    $"{m.Offset.x:0.##}, {m.Offset.y:0.##} | " +
                    $"{m.CentroMundo.x:0.##}, {m.CentroMundo.y:0.##} | " +
                    $"{(m.EhTrigger ? "sim" : "não")} | " +
                    $"{m.Composicao} | {m.Material} | " +
                    $"{(m.TemSprite ? $"{m.TamanhoDoSprite.x:0.##}×{m.TamanhoDoSprite.y:0.##}" : "—")} |");
            }

            md.AppendLine();
            md.AppendLine("> **Polygon, Edge e Composite** aparecem com tamanho medido por " +
                          "`bounds`, que a doc da 6000.4 diz ficar **vazio com o colisor " +
                          "desligado ou o objeto inativo** — nesses casos a linha traz `0×0`, " +
                          "que aqui significa *não medido*, não *vazio*.");

            Directory.CreateDirectory(Path.GetDirectoryName(SaidaColisores));
            File.WriteAllText(SaidaColisores, md.ToString());
            AssetDatabase.Refresh();
        }

        private static void ResumirColisores(List<ColisorMedido> colisores)
        {
            var resumo = new StringBuilder();
            resumo.AppendLine($"[AuditoriaDeColisores] {colisores.Count} colisor(es).");

            foreach (var g in colisores.GroupBy(c => c.M.Funcao).OrderByDescending(g => g.Count()))
                resumo.AppendLine($"  papel {g.Key}: {g.Count()}");

            foreach (var g in colisores.GroupBy(c => c.M.Tipo).OrderByDescending(g => g.Count()))
                resumo.AppendLine($"  forma {g.Key}: {g.Count()}");

            var comQueixa = colisores.Where(c => !string.IsNullOrEmpty(c.M.Queixa)).ToList();

            resumo.AppendLine(comQueixa.Count == 0
                ? "  fora do esperado: nenhum"
                : $"  fora do esperado: {comQueixa.Count}");

            foreach (var c in comQueixa.OrderBy(x => x.Origem).ThenBy(x => x.M.Caminho))
                resumo.AppendLine($"     {c.Origem} / {c.M.Caminho} [{c.M.Funcao}] — {c.M.Queixa}");

            resumo.AppendLine($"  relatório em {SaidaColisores}");
            Debug.Log(resumo.ToString());
        }

        /// <summary>
        /// Escreve o relatório de gatilhos: para que serve cada um, e se o callback casa com o
        /// colisor.
        ///
        /// <para><b>A contagem de <c>OnCollision*2D</c> sai do <c>TypeCache</c></b>, e não da
        /// varredura de cenas: um script pode existir no projeto sem estar em cena nenhuma, e
        /// afirmar "não existe nenhum" com base no que está montado seria afirmar sobre a
        /// amostra em vez de sobre o projeto.</para>
        /// </summary>
        private static void EscreverGatilhos(List<GatilhoMedido> gatilhos, int cenas)
        {
            var comCollision = TiposComCallbackDeColisao();

            var md = new StringBuilder();
            md.AppendLine("---");
            md.AppendLine("type: Game System");
            md.AppendLine("title: Auditoria de Gatilhos e Callbacks");
            md.AppendLine("description: Todo Collider2D marcado como trigger, para que serve, e " +
                          "se o callback do script casa com o colisor. Gerado por " +
                          "Tools/FavelaAmarela/Auditar Física 2D.");
            md.AppendLine($"date: {System.DateTime.Now:yyyy-MM-dd}");
            md.AppendLine("---");
            md.AppendLine();
            md.AppendLine("# Auditoria de Gatilhos e Callbacks");
            md.AppendLine();
            md.AppendLine("> **Gerado por ferramenta.** Rode " +
                          "`Tools/FavelaAmarela/Auditar Física 2D` para atualizar.");
            md.AppendLine();

            md.AppendLine("## As regras, citadas da 6000.4");
            md.AppendLine();
            md.AppendLine("Da Script Reference offline de `MonoBehaviour.OnTriggerEnter2D`:");
            md.AppendLine();
            md.AppendLine("> *\"This message is sent to the trigger Collider2D and the " +
                          "Rigidbody2D (if any) that the trigger Collider2D belongs to, and to " +
                          "the Rigidbody2D (or the Collider2D if there is no Rigidbody2D) that " +
                          "touches the trigger.\"*");
            md.AppendLine(">");
            md.AppendLine("> *\"Note: Trigger events are only sent if one of the Colliders also " +
                          "has a Rigidbody2D attached.\"*");
            md.AppendLine();
            md.AppendLine("Três consequências que mudam o que conta como defeito:");
            md.AppendLine();
            md.AppendLine("1. **Script em objeto SÓLIDO com `OnTriggerEnter2D` é legítimo** — " +
                          "ele recebe ao *entrar* no gatilho de outro. Acusar isso seria " +
                          "acusar o padrão certo.");
            md.AppendLine("2. **Zona sem `Rigidbody2D` funciona**, desde que quem entre tenha " +
                          "um. Aqui quem entra é o Damião, que tem. Acusar zona sem corpo " +
                          "seria falso positivo.");
            md.AppendLine("3. **A mensagem vai para o GameObject do colisor.** Script no pai " +
                          "com o trigger num filho **nunca recebe** — este é defeito de " +
                          "verdade, e silencioso.");
            md.AppendLine();

            md.AppendLine("## `OnCollision*2D` no projeto");
            md.AppendLine();
            if (comCollision.Count == 0)
            {
                md.AppendLine("**Nenhum script declara `OnCollisionEnter2D`, `Stay` ou `Exit`.** " +
                              "Medido pelo `TypeCache`, sobre todos os tipos do projeto.");
                md.AppendLine();
                md.AppendLine("Isso responde metade da pergunta \"scripts em objetos sólidos " +
                              "usam callback de colisão?\": **não há nenhum**. Nada neste jogo " +
                              "depende de evento de colisão sólida — o colisor sólido só barra " +
                              "movimento, e a física resolve sozinha. Não é lacuna: é o modelo " +
                              "do projeto, em que dano sai de consulta " +
                              "(`Physics2D.OverlapCircle`) e não de sobreposição de colisor.");
            }
            else
            {
                md.AppendLine("| tipo | callbacks |");
                md.AppendLine("|---|---|");
                foreach (var t in comCollision.OrderBy(x => x.Key))
                    md.AppendLine($"| `{t.Key}` | {t.Value} |");
            }
            md.AppendLine();

            var problemas = gatilhos.Where(g => g.L.Diagnostico !=
                                           AuditoriaDeGatilhos.Veredito.Ok).ToList();

            md.AppendLine("## Fora do esperado");
            md.AppendLine();
            if (problemas.Count == 0)
            {
                md.AppendLine("Nenhum. Todo gatilho tem dono, e todo callback casa com o colisor.");
            }
            else
            {
                md.AppendLine("| origem | objeto | diagnóstico | o quê |");
                md.AppendLine("|---|---|---|---|");
                foreach (var g in problemas.OrderBy(x => x.Origem).ThenBy(x => x.L.Caminho))
                    md.AppendLine($"| {g.Origem} | `{g.L.Caminho}` | **{g.L.Diagnostico}** | " +
                                  $"{g.L.Explicacao} |");
            }
            md.AppendLine();

            var triggers = gatilhos.Where(g => g.L.EhTrigger).ToList();

            md.AppendLine($"## Os {triggers.Count} gatilhos");
            md.AppendLine();
            md.AppendLine("| origem | objeto | forma | propósito | corpo? | callbacks |");
            md.AppendLine("|---|---|---|---|---|---|");
            foreach (var g in triggers.OrderBy(x => x.L.Funcao.ToString())
                                      .ThenBy(x => x.Origem).ThenBy(x => x.L.Caminho))
            {
                md.AppendLine($"| {g.Origem} | `{g.L.Caminho}` | {g.L.Tipo} | {g.L.Funcao} | " +
                              $"{(g.L.TemCorpoNaHierarquia ? "sim" : "não")} | {g.L.Callbacks} |");
            }

            Directory.CreateDirectory(Path.GetDirectoryName(SaidaGatilhos));
            File.WriteAllText(SaidaGatilhos, md.ToString());
            AssetDatabase.Refresh();
        }

        /// <summary>Tipos do projeto que declaram algum <c>OnCollision*2D</c>.</summary>
        private static Dictionary<string, string> TiposComCallbackDeColisao()
        {
            var nomes = new[] { "OnCollisionEnter2D", "OnCollisionStay2D", "OnCollisionExit2D" };
            var achados = new Dictionary<string, string>();

            foreach (var t in TypeCache.GetTypesDerivedFrom<MonoBehaviour>())
            {
                if (t.Assembly.FullName.StartsWith("Unity")) continue;

                var deste = nomes.Where(n => t.GetMethod(n,
                        System.Reflection.BindingFlags.Instance |
                        System.Reflection.BindingFlags.Public |
                        System.Reflection.BindingFlags.NonPublic |
                        System.Reflection.BindingFlags.DeclaredOnly) != null).ToList();

                if (deste.Count > 0) achados[t.Name] = string.Join(", ", deste);
            }

            return achados;
        }

        private static void ResumirGatilhos(List<GatilhoMedido> gatilhos)
        {
            var resumo = new StringBuilder();
            var triggers = gatilhos.Where(g => g.L.EhTrigger).ToList();

            resumo.AppendLine($"[AuditoriaDeGatilhos] {triggers.Count} gatilho(s).");

            foreach (var g in triggers.GroupBy(x => x.L.Funcao)
                                      .OrderByDescending(g => g.Count()))
                resumo.AppendLine($"  propósito {g.Key}: {g.Count()}");

            int comCollision = TiposComCallbackDeColisao().Count;
            resumo.AppendLine($"  tipos com OnCollision*2D no projeto: {comCollision}");

            var problemas = gatilhos.Where(g => g.L.Diagnostico !=
                                           AuditoriaDeGatilhos.Veredito.Ok).ToList();

            resumo.AppendLine(problemas.Count == 0
                ? "  fora do esperado: nenhum"
                : $"  fora do esperado: {problemas.Count}");

            foreach (var g in problemas.OrderBy(x => x.Origem).ThenBy(x => x.L.Caminho))
                resumo.AppendLine($"     [{g.L.Diagnostico}] {g.Origem} / {g.L.Caminho}");

            resumo.AppendLine($"  relatório em {SaidaGatilhos}");
            Debug.Log(resumo.ToString());
        }

        private static void Resumir(List<Corpo> corpos, List<Desparelhado> soltos, int cenas)
        {
            var resumo = new StringBuilder();
            resumo.AppendLine($"[Rigidbody2DAuditor] {corpos.Count} corpo(s), {cenas} cena(s) do " +
                              "Build Settings + todos os prefabs.");

            foreach (var g in corpos.GroupBy(c => c.Tipo))
                resumo.AppendLine($"  tipo {g.Key}: {g.Count()}");

            // O que interessa num resumo é o que DIVERGE, não o que se repete.
            Divergencia(resumo, corpos, "gravityScale", c => c.Gravidade.ToString("0.##"));
            Divergencia(resumo, corpos, "collisionDetectionMode", c => c.Deteccao.ToString());
            Divergencia(resumo, corpos, "interpolation", c => c.Interpolacao.ToString());
            Divergencia(resumo, corpos, "constraints", c => Travas(c.Travas));
            Divergencia(resumo, corpos, "sleepMode", c => c.Sono.ToString());

            resumo.AppendLine(soltos.Count == 0
                ? "  pareamentos suspeitos: nenhum"
                : $"  pareamentos suspeitos: {soltos.Count}");

            foreach (var d in soltos)
                resumo.AppendLine($"     {d.Origem} / {d.Caminho}");

            resumo.AppendLine($"  relatório em {Saida}");
            Debug.Log(resumo.ToString());
        }

        /// <summary>
        /// Mostra um campo só quando ele <b>não é unânime</b>.
        ///
        /// <para>Um resumo que repete "gravityScale 0" dezoito vezes esconde o único que está em
        /// 1. O que precisa de olho é o outlier.</para>
        /// </summary>
        private static void Divergencia(StringBuilder sb, List<Corpo> corpos, string campo,
                                        System.Func<Corpo, string> ler)
        {
            var grupos = corpos.GroupBy(ler).OrderByDescending(g => g.Count()).ToList();
            if (grupos.Count <= 1)
            {
                sb.AppendLine($"  {campo}: unânime ({grupos.FirstOrDefault()?.Key ?? "—"})");
                return;
            }

            sb.AppendLine($"  {campo}: DIVERGE — " +
                          string.Join(", ", grupos.Select(g => $"{g.Key}×{g.Count()}")));

            foreach (var g in grupos.Skip(1))
                foreach (var c in g)
                    sb.AppendLine($"     {g.Key}: {c.Origem} / {c.Caminho}");
        }
    }
}
