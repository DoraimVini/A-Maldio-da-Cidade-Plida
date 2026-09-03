using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace FavelaAmarela.Tests.EditMode
{
    /// <summary>
    /// Guarda contra o <b>modo de falha dominante deste projeto</b>: a peça existe, não dá erro,
    /// e a ligação não acontece.
    ///
    /// <para><b>O que motivou (2026-09-02).</b> Numa sessão inteira de playtest do Vini, TODO
    /// defeito encontrado foi de ligação, e nenhum de lógica:</para>
    ///
    /// <list type="bullet">
    ///   <item><c>ReiEmAmareloAI.IniciarRitual()</c> tinha <b>um</b> chamador em todo o projeto —
    ///         uma janela de Editor. A máquina do Rei ficava em <c>Aguardando</c> para sempre, os
    ///         três altares do Trono recusavam toda relíquia <b>em silêncio</b>, e o chefe final
    ///         era impossível de selar.</item>
    ///   <item><c>PainelDeEscolha.Confirmar()</c> não devolvia a camada de foco: escolher uma
    ///         opção prendia o comando do jogador para sempre.</item>
    ///   <item>O Byakhee ficou meses <b>sem <c>Collider2D</c></b> — estruturalmente impossível de
    ///         acertar — com o item marcado como pronto no roadmap.</item>
    /// </list>
    ///
    /// <para><b>Nenhuma boa prática de arquitetura pega isso.</b> Interface, ScriptableObject,
    /// FSM, corrotina, pooling: o código está certo em todos os casos acima. O que falta é a
    /// <b>conexão</b>, e ela falha sem exceção, sem log e sem teste vermelho.</para>
    ///
    /// <para><b>Por que este teste lê o código como texto, e isso está certo aqui.</b> A pergunta
    /// é sobre a <i>existência de um chamador</i> — um fato estático do repositório, que o
    /// arquivo guarda inteiro. É o oposto dos defeitos de layout, que só existem quando a Unity
    /// roda (esses moram em <c>Assets/Tests/PlayMode</c>).</para>
    ///
    /// <para><b>Linha de base:</b> <c>ligacao_conhecida.txt</c>. Este teste falha só em entrada
    /// <b>nova</b>. O arquivo carrega o motivo de cada caso e serve de lista de trabalho — as
    /// linhas marcadas <c>divida</c> devem sumir com o tempo.</para>
    /// </summary>
    public sealed class LigacaoDoJogoTests
    {
        private const string Base = "Assets/Tests/EditMode/ligacao_conhecida.txt";
        private const string Fontes = "Assets/Scripts";
        private const string Ferramentas = "Assets/FavelaAmarela/Editor";
        private const string Testes = "Assets/Tests";

        /// <summary>
        /// Mensagens da Unity: o motor as chama por reflexão, então "sem chamador" é o normal.
        /// </summary>
        private static readonly HashSet<string> MensagensDaUnity = new HashSet<string>
        {
            "Awake", "Start", "Update", "FixedUpdate", "LateUpdate", "OnEnable", "OnDisable",
            "OnDestroy", "OnValidate", "OnDrawGizmos", "OnDrawGizmosSelected", "OnGUI",
            "OnTriggerEnter2D", "OnTriggerExit2D", "OnTriggerStay2D",
            "OnCollisionEnter2D", "OnCollisionExit2D", "OnCollisionStay2D",
            "OnApplicationQuit", "OnApplicationPause", "OnApplicationFocus",
            "OnBecameVisible", "OnBecameInvisible", "OnPointerEnter", "OnPointerExit",
            "OnPointerClick", "OnPointerDown", "OnPointerUp", "OnBeginDrag", "OnDrag",
            "OnEndDrag", "OnDrop", "OnSelect", "OnDeselect", "OnSubmit", "OnCancel",
        };

        // ── os três testes ───────────────────────────────────────────────────────

        [Test]
        public void NenhumPontoDeEntradaNovoFicaSemChamador()
        {
            var achados = PontosDeEntradaSemChamador();
            Cobrar("metodo", achados,
                "Método público de MonoBehaviour que NADA no jogo chama — só ferramenta de " +
                "Editor, só teste, ou ninguém. É como o IniciarRitual() do Rei em Amarelo: a " +
                "peça está lá, não dá erro, e o jogo nunca a aciona.");
        }

        [Test]
        public void NenhumEventoNovoFicaSemAssinante()
        {
            var achados = EventosSemAssinante();
            Cobrar("evento", achados,
                "Evento público sem um único assinante. Ele dispara e ninguém escuta — o " +
                "sistema tem o gancho de feedback e a tela não mostra nada.");
        }

        [Test]
        public void NenhumCampoDeCenaNovoFicaSemQuemOInjete()
        {
            var achados = CamposDeCenaOrfaos();
            Cobrar("campo", achados,
                "Campo marcado [CENA] que está NULO em todas as instâncias e não tem quem o " +
                "injete em runtime (nem setter Definir*/Bind*, nem fallback para uma " +
                "instância global).");
        }

        /// <summary>
        /// A própria varredura tem de estar lendo o projeto. Um teste que analisa zero arquivo
        /// passa verde e não afirma nada — foi exatamente esse silêncio que deixou 27 casos se
        /// acumularem sem ninguém ver.
        /// </summary>
        [Test]
        public void AVarreduraEstaLendoOProjeto()
        {
            var arquivos = Arquivos(Fontes).ToArray();
            Assert.Greater(arquivos.Length, 150,
                $"Só achei {arquivos.Length} arquivos em {Fontes}. Esta varredura não está " +
                "lendo o projeto, e os outros testes desta classe passariam vazios.");

            Assert.IsTrue(File.Exists(Base),
                $"A linha de base '{Base}' não existe. Sem ela este teste não sabe o que já era " +
                "conhecido, e acusaria o projeto inteiro de uma vez.");

            int metodos = TodosOsPontosDeEntrada().Count;
            Assert.Greater(metodos, 100,
                $"Só extraí {metodos} métodos públicos de MonoBehaviour. O reconhecimento de " +
                "assinatura quebrou — provavelmente uma mudança de estilo no código.");
        }

        // ── análise ──────────────────────────────────────────────────────────────

        private static IEnumerable<string> Arquivos(string pasta)
            => Directory.Exists(pasta)
                ? Directory.EnumerateFiles(pasta, "*.cs", SearchOption.AllDirectories)
                : Enumerable.Empty<string>();

        /// <summary>
        /// Tira comentários e literais de string. Sem isto, um <c>// chama Foo()</c> num
        /// comentário contaria como chamador e esconderia justamente o defeito procurado.
        /// </summary>
        private static string SoCodigo(string s)
        {
            s = Regex.Replace(s, @"/\*.*?\*/", " ", RegexOptions.Singleline);
            s = Regex.Replace(s, @"//[^\n]*", " ");
            s = Regex.Replace(s, "\"(?:[^\"\\\\\n]|\\\\.)*\"", "\"\"");
            return s;
        }

        private static readonly Regex Assinatura = new Regex(
            @"^\s*public\s+(?:static\s+|virtual\s+|override\s+|async\s+|sealed\s+|new\s+)*" +
            @"[\w<>\[\],\.\?]+\s+(\w+)\s*\(",
            RegexOptions.Multiline | RegexOptions.Compiled);

        private sealed class Entrada
        {
            public string Arquivo;
            public string Classe;
            public string Metodo;
            public string Chave => $"{Classe}.{Metodo}";
        }

        private static List<Entrada> TodosOsPontosDeEntrada()
        {
            var saida = new List<Entrada>();

            foreach (var f in Arquivos(Fontes))
            {
                // Core/ é POCO puro: quem o chama é o adaptador, e a ausência de adaptador é o
                // que os OUTROS dois testes desta classe pegam.
                if (f.Replace('\\', '/').Contains("/Core/")) continue;

                string bruto = File.ReadAllText(f);
                if (!bruto.Contains("MonoBehaviour")) continue;

                string codigo = SoCodigo(bruto);
                string classe = Path.GetFileNameWithoutExtension(f);

                foreach (Match m in Assinatura.Matches(codigo))
                {
                    string nome = m.Groups[1].Value;
                    if (MensagensDaUnity.Contains(nome)) continue;
                    if (nome == classe) continue;                       // construtor
                    if (saida.Any(e => e.Chave == $"{classe}.{nome}")) continue;   // sobrecarga

                    saida.Add(new Entrada { Arquivo = f, Classe = classe, Metodo = nome });
                }
            }

            return saida;
        }

        private static List<string> PontosDeEntradaSemChamador()
        {
            var entradas = TodosOsPontosDeEntrada();

            var runtime = Arquivos(Fontes).ToDictionary(f => f, f => SoCodigo(File.ReadAllText(f)));
            string ferramentas = string.Join("\n", Arquivos(Ferramentas).Select(File.ReadAllText));
            string testes = string.Join("\n", Arquivos(Testes).Select(File.ReadAllText));

            // UnityEvent ligado no Inspector: a chamada mora no YAML, não no código.
            string yaml = string.Join("\n", Cenas().Select(File.ReadAllText));

            var saida = new List<string>();

            foreach (var e in entradas)
            {
                // O NOME, e não "nome seguido de parêntese". Referência de método como
                // delegate não tem parêntese nenhum:
                //     botaoDeOpcoes.onClick.AddListener(PainelDeOpcoes.AbrirSeExistir);
                // A primeira versão desta guarda exigia "(" e por isso declarou morto todo
                // manipulador de evento do projeto -- inclusive o botão Opções, que funciona.
                // Errar para o lado de acusar menos é o certo aqui: guarda que grita à toa
                // é guarda que se aprende a ignorar.

                // No arquivo que DECLARA o método, apagar a assinatura antes de procurar a
                // chamada. Ignorar o arquivo inteiro seria grosseiro: `IniciarRitual()` do Rei é
                // chamado pelo `Start()` da própria classe, e isso é ligação legítima -- foi
                // exatamente assim que o rito passou a começar em jogo (2026-09-02).
                bool noJogo = runtime.Any(p => Menciona(
                    p.Key == e.Arquivo ? SemAsAssinaturasDe(p.Value, e.Metodo) : p.Value,
                    e.Metodo));

                if (noJogo) continue;

                if (Regex.IsMatch(yaml, @"m_MethodName: " + Regex.Escape(e.Metodo) + @"\s*$",
                                  RegexOptions.Multiline)) continue;

                string onde = Menciona(ferramentas, e.Metodo) ? "só Editor"
                            : Menciona(testes, e.Metodo) ? "só teste"
                            : "ninguém";

                saida.Add($"{e.Chave} ({onde})");
            }

            return saida;
        }

        /// <summary>
        /// Se o identificador aparece no código como <b>palavra inteira</b>.
        ///
        /// <para><b>Procura o NOME, e não "nome seguido de parêntese".</b> Referência de método
        /// como delegate não tem parêntese nenhum:</para>
        ///
        /// <code>botaoDeOpcoes.onClick.AddListener(PainelDeOpcoes.AbrirSeExistir);</code>
        ///
        /// <para>A primeira versão desta guarda exigia o parêntese e por isso declarou morto
        /// <b>todo manipulador de evento do projeto</b> — inclusive o botão Opções, que funciona.
        /// Errar para o lado de acusar de menos é o certo aqui: guarda que grita à toa é guarda
        /// que se aprende a ignorar.</para>
        ///
        /// <para>Busca por caractere, e não por <c>Regex</c>, de propósito: é mais rápida numa
        /// varredura de 242 arquivos × 205 métodos, e não depende de escape nenhum.</para>
        /// </summary>
        private static bool Menciona(string codigo, string nome)
        {
            int i = 0;
            while ((i = codigo.IndexOf(nome, i, StringComparison.Ordinal)) >= 0)
            {
                int fim = i + nome.Length;

                bool antesLivre = i == 0 || !EhDeIdentificador(codigo[i - 1]);
                bool depoisLivre = fim >= codigo.Length || !EhDeIdentificador(codigo[fim]);

                if (antesLivre && depoisLivre) return true;
                i = fim;
            }

            return false;
        }

        private static bool EhDeIdentificador(char c)
            => char.IsLetterOrDigit(c) || c == '_';

        /// <summary>
        /// O código sem as <b>assinaturas</b> deste método — só as chamadas dele sobram.
        /// </summary>
        private static string SemAsAssinaturasDe(string codigo, string metodo)
            => Regex.Replace(codigo,
                @"^\s*public\s+(?:static\s+|virtual\s+|override\s+|async\s+|sealed\s+|new\s+)*" +
                @"[\w<>\[\],\.\?]+\s+" + Regex.Escape(metodo) + @"\s*\(",
                " ", RegexOptions.Multiline);

        private static List<string> EventosSemAssinante()
        {
            var saida = new List<string>();

            var todos = Arquivos(Fontes).ToDictionary(f => f, f => SoCodigo(File.ReadAllText(f)));

            foreach (var (f, codigo) in todos.Select(p => (p.Key, p.Value)))
            {
                string classe = Path.GetFileNameWithoutExtension(f);

                foreach (Match m in Regex.Matches(codigo,
                             @"public\s+event\s+[\w<>\[\],\.\s]+?(\w+)\s*(?:;|=)"))
                {
                    string nome = m.Groups[1].Value;

                    bool assinado = todos.Values.Any(c =>
                        Regex.IsMatch(c, Regex.Escape(nome) + @"\s*\+="));

                    if (!assinado) saida.Add($"{classe}.{nome}");
                }
            }

            return saida;
        }

        /// <summary>
        /// Cenas e prefabs. Os prefabs varrem <c>Assets</c> inteiro de propósito — o HUD, os
        /// inimigos e os coletáveis moram fora de <c>Assets/Scenes</c>, e é neles que a maior
        /// parte das referências serializadas vive.
        /// </summary>
        private static IEnumerable<string> Cenas()
        {
            if (Directory.Exists("Assets/Scenes"))
                foreach (var f in Directory.EnumerateFiles("Assets/Scenes", "*.unity",
                                                           SearchOption.AllDirectories))
                    yield return f;

            if (Directory.Exists("Assets"))
                foreach (var f in Directory.EnumerateFiles("Assets", "*.prefab",
                                                           SearchOption.AllDirectories))
                    yield return f;
        }

        private static List<string> CamposDeCenaOrfaos()
        {
            var saida = new List<string>();

            // campo -> script que o declara, para os marcados [CENA] no Tooltip
            var declarados = new List<(string Classe, string Campo, string Codigo)>();

            foreach (var f in Arquivos(Fontes))
            {
                string bruto = File.ReadAllText(f);
                if (!bruto.Contains("[CENA]")) continue;

                string classe = Path.GetFileNameWithoutExtension(f);

                foreach (Match m in Regex.Matches(bruto,
                             @"\[Tooltip\((?<txt>(?:[^)]|\)(?!\]))*)\)\]\s*(?:\[[^\]]*\]\s*)*" +
                             @"\[SerializeField\][^;]*?\b(?<campo>\w+)\s*(?:=[^;]*)?;",
                             RegexOptions.Singleline))
                {
                    if (!m.Groups["txt"].Value.Contains("[CENA]")) continue;
                    declarados.Add((classe, m.Groups["campo"].Value, bruto));
                }
            }

            var yaml = Cenas().ToDictionary(f => f, File.ReadAllText);

            foreach (var (classe, campo, codigo) in declarados)
            {
                // Injetado em runtime? Um Bind/Definir que atribui o campo, ou uma propriedade
                // que cai para uma instância global, tornam o nulo em disco CORRETO.
                //   - PlayerDeathController.sequenciaColapso é nulo nas 6 cenas e está certo:
                //     o GameLoopBootstrap injeta a partir do HUD.
                //   - caixaDeTexto é nulo em 31 de 31 e está certo: os consumidores caem para
                //     TutorialHintUI.Instancia.
                bool injetado =
                    Regex.IsMatch(codigo, @"\b" + Regex.Escape(campo) + @"\s*=\s*\w+\s*;") ||
                    Regex.IsMatch(codigo, Regex.Escape(campo) + @"\s*!=\s*null\s*\?") ||
                    Regex.IsMatch(codigo, @"\b" + Regex.Escape(campo) + @"\s*\?\?");

                if (injetado) continue;

                int total = 0, nulos = 0;
                foreach (var texto in yaml.Values)
                {
                    foreach (Match m in Regex.Matches(texto,
                                 @"^  " + Regex.Escape(campo) + @": \{fileID: (-?\d+)",
                                 RegexOptions.Multiline))
                    {
                        total++;
                        if (m.Groups[1].Value == "0") nulos++;
                    }

                    // OVERRIDE DE PREFAB. Uma instância em cena guarda o campo em
                    // m_Modifications, e não como linha direta — o prefab-asset fica nulo (não
                    // pode referenciar objeto de cena) e a instância é que aponta para o objeto
                    // certo. Ler só a linha direta acusava o Abdul de estar sem Tranca de Arena
                    // e sem Yug-Neth, com os dois ligados na cena. Medido em 2026-09-02.
                    foreach (Match m in Regex.Matches(texto,
                                 @"propertyPath: " + Regex.Escape(campo) + @"\s*\n" +
                                 @"\s*value:[^\n]*\n\s*objectReference: \{fileID: (-?\d+)\}",
                                 RegexOptions.Multiline))
                    {
                        total++;
                        if (m.Groups[1].Value == "0") nulos++;
                    }
                }

                if (total > 0 && nulos == total)
                    saida.Add($"{classe}.{campo} ({total} instância(s), todas nulas)");
            }

            return saida;
        }

        // ── linha de base ────────────────────────────────────────────────────────

        private static HashSet<string> Conhecidos(string tipo)
        {
            var set = new HashSet<string>();
            if (!File.Exists(Base)) return set;

            foreach (var linha in File.ReadAllLines(Base))
            {
                string l = linha.Trim();
                if (l.Length == 0 || l.StartsWith("#")) continue;

                var partes = l.Split('|');
                var cabeca = partes[0].Trim().Split(new[] { ' ' }, 3);
                if (cabeca.Length < 3) continue;
                if (cabeca[1] != tipo) continue;

                set.Add(cabeca[2].Trim());
            }

            return set;
        }

        private static void Cobrar(string tipo, List<string> achados, string explicacao)
        {
            var conhecidos = Conhecidos(tipo);

            // A chave da linha de base ignora o parêntese explicativo do achado.
            var novos = achados
                .Where(a => !conhecidos.Contains(Chave(a)))
                .OrderBy(a => a)
                .ToArray();

            var some = conhecidos
                .Where(c => !achados.Any(a => Chave(a) == c))
                .OrderBy(c => c)
                .ToArray();

            Assert.IsEmpty(novos,
                explicacao + Environment.NewLine + Environment.NewLine +
                $"{novos.Length} caso(s) NOVO(S), fora da linha de base:" + Environment.NewLine +
                string.Join(Environment.NewLine, novos.Select(n => "  " + n)) +
                Environment.NewLine + Environment.NewLine +
                $"Se for ligação legítima, acrescente em {Base}:" + Environment.NewLine +
                string.Join(Environment.NewLine,
                    novos.Select(n => $"  ok {tipo} {Chave(n)} | <por que não precisa de ligação>")));

            Assert.IsEmpty(some,
                $"A linha de base tem {some.Length} entrada(s) de '{tipo}' que NÃO existem mais " +
                "no código. Ou a ligação foi feita (apague a linha, e comemore), ou o membro foi " +
                "renomeado e a isenção agora protege um fantasma:" + Environment.NewLine +
                string.Join(Environment.NewLine, some.Select(s => "  " + s)));
        }

        private static string Chave(string achado)
        {
            int p = achado.IndexOf(" (", StringComparison.Ordinal);
            return p < 0 ? achado : achado.Substring(0, p);
        }
    }
}
