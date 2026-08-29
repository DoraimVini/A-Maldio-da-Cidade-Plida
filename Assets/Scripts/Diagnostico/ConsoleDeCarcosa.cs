// ─────────────────────────────────────────────────────────────────────────────
// TODO o arquivo vive dentro desta guarda. Numa build de RELEASE ele compila
// para NADA -- nem a classe existe. É o que separa "ferramenta de teste" de
// "trapaça que vazou para o jogador".
//
// DEVELOPMENT_BUILD é definido pela Unity quando a caixa "Development Build" está
// marcada em File > Build Settings. Marque para testar, desmarque para entregar.
// ─────────────────────────────────────────────────────────────────────────────
#if UNITY_EDITOR || DEVELOPMENT_BUILD

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using FavelaAmarela.Core.Loot;
using FavelaAmarela.Inventario;
using FavelaAmarela.Runtime.Combat;
using FavelaAmarela.Runtime.GameLoop;
using FavelaAmarela.Runtime.Itens;
using FavelaAmarela.Runtime.Progression;

namespace FavelaAmarela.Runtime.Diagnostico
{
    /// <summary>
    /// O <b>Carcosa Debugger dentro do jogo rodando</b> — a metade do Debugger que a build pode
    /// ter.
    ///
    /// <para><b>Por que existe (2026-08-29).</b> O <c>CarcosaDebuggerWindow</c> é um
    /// <c>EditorWindow</c> em <c>Assets/FavelaAmarela/Editor/</c>, e portanto <b>não existe em
    /// build nenhuma</b>: a Unity remove toda pasta chamada <c>Editor</c> do player, e
    /// <c>UnityEditor</c> não existe em runtime. Quem joga uma build não tem como se conceder um
    /// item, subir de nível ou pular para um chefe.</para>
    ///
    /// <para>Isso custa caro <b>neste projeto em particular</b>: o Byakhee fecha a Fase 1, e
    /// chegar nele custa o Deserto e a Tumba inteiros. Foi jogando que o Vini descobriu que a
    /// luta não fechava — e cada tentativa de conferir o conserto custaria uma partida inteira.
    /// Uma ferramenta que economiza vinte minutos por tentativa não é conforto, é a diferença
    /// entre testar e não testar.</para>
    ///
    /// <para><b>O que ele NÃO faz, de propósito.</b> Não cria <c>ItemDef</c> — isso é
    /// <i>autoria</i>, uma build não escreve assets dentro de si mesma, e a Forja do Editor
    /// continua sendo o lugar certo. Ele <b>concede</b> o que já existe, rolado pelas mesmas
    /// regras do jogo.</para>
    ///
    /// <para><b>Nasce sozinho</b>, como o <c>ProgressionBridge</c> e o <c>ItemDatabase</c>. Um
    /// console que precisasse ser posto em cada cena morreria do jeito que este repositório já
    /// catalogou dez vezes: a peça existe, não dá erro, e não está em cena nenhuma.</para>
    /// </summary>
    public sealed class ConsoleDeCarcosa : MonoBehaviour
    {
        private static ConsoleDeCarcosa _instancia;

        /// <summary>Nasce antes de qualquer cena carregar, em toda cena, sem autoria.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void GarantirInstancia()
        {
            if (_instancia != null) return;

            var go = new GameObject("ConsoleDeCarcosa (F1)");
            go.AddComponent<ConsoleDeCarcosa>();   // o Awake faz o DontDestroyOnLoad
        }

        private void Awake()
        {
            if (_instancia != null && _instancia != this) { Destroy(gameObject); return; }

            _instancia = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            if (_instancia == this) _instancia = null;
        }

        // ── Estado da janela ──────────────────────────────────────────────────

        private bool _aberto;
        private float _escalaAnterior = 1f;
        private Vector2 _rolagem;
        private Rect _janela = new Rect(20f, 20f, 520f, 560f);

        private int _abaAtual;
        private static readonly string[] Abas = { "Estado", "Arsenal", "Progressão", "Ir para" };

        private string _nivelDesejado = "3";
        private string _filtroDeItem = "";
        private GrauDeImpregnacao _grauEscolhido = GrauDeImpregnacao.Inerte;
        private bool _rolarGrauPelaCurva = true;

        private ItemDef[] _equipamentos;
        private string _ultimaMensagem = "";

        private void Update()
        {
            // Keyboard.current é nulo em máquina sem teclado e nos primeiros quadros.
            var teclado = Keyboard.current;
            if (teclado == null) return;

            if (teclado.f1Key.wasPressedThisFrame) Alternar();

            // Escape fecha, mas só quando o console é quem está aberto -- senão roubaria o
            // Escape do menu de pausa.
            if (_aberto && teclado.escapeKey.wasPressedThisFrame) Alternar();
        }

        /// <summary>
        /// Abre e fecha, <b>congelando o jogo</b> enquanto aberto.
        ///
        /// <para>Sem o congelamento, digitar um nível de item com um Cultista em cima é como
        /// consertar o carro andando. Restaura a escala anterior, e não o 1: se a partida já
        /// estava pausada quando o console abriu, fechá-lo não pode despausá-la.</para>
        /// </summary>
        private void Alternar()
        {
            _aberto = !_aberto;

            if (_aberto)
            {
                _escalaAnterior = Time.timeScale;
                Time.timeScale = 0f;
                _equipamentos = null;   // recarrega o catálogo a cada abertura
            }
            else
            {
                Time.timeScale = _escalaAnterior;
            }
        }

        private void OnGUI()
        {
            if (!_aberto) return;

            _janela = GUILayout.Window(GetInstanceID(), _janela, Desenhar,
                                       "Carcosa Debugger — runtime (F1 fecha)");
        }

        private void Desenhar(int id)
        {
            _abaAtual = GUILayout.Toolbar(_abaAtual, Abas);
            GUILayout.Space(6f);

            _rolagem = GUILayout.BeginScrollView(_rolagem);

            switch (_abaAtual)
            {
                case 0: DesenharEstado(); break;
                case 1: DesenharArsenal(); break;
                case 2: DesenharProgressao(); break;
                default: DesenharDestinos(); break;
            }

            GUILayout.EndScrollView();

            if (!string.IsNullOrEmpty(_ultimaMensagem))
            {
                GUILayout.Space(4f);
                GUILayout.Label(_ultimaMensagem);
            }

            GUI.DragWindow(new Rect(0f, 0f, 10000f, 20f));
        }

        // ── Aba: Estado ───────────────────────────────────────────────────────

        private void DesenharEstado()
        {
            GUILayout.Label($"Cena: {SceneManager.GetActiveScene().name}");

            var progressao = ProgressionBridge.Instancia?.Progressao;
            if (progressao == null)
            {
                GUILayout.Label("Progressão: AUSENTE (ProgressionBridge não nasceu)");
            }
            else
            {
                GUILayout.Label($"Nível {progressao.NivelAtual} de {progressao.NivelMaximo} — " +
                                $"{progressao.ExposicaoAtual} de Exposição" +
                                (progressao.NoTeto
                                    ? "  (no teto)"
                                    : $", faltam {progressao.ExposicaoAteOProximoNivel}"));

                GUILayout.Label($"Pontos de Eco: {progressao.PontosDeEcoDisponiveis}");
            }

            GUILayout.Space(6f);

            var jogador = GameObject.FindGameObjectWithTag("Player");
            if (jogador == null)
            {
                GUILayout.Label("Damião: não está nesta cena.");
                return;
            }

            var vitalidade = jogador.GetComponent<VitalidadeBridge>();
            if (vitalidade?.Vitalidade != null)
            {
                GUILayout.Label($"Vitalidade: {vitalidade.Vitalidade.Atual:0} / " +
                                $"{vitalidade.Vitalidade.Max:0}" +
                                (vitalidade.IgnorarDano ? "   [INVULNERÁVEL]" : ""));

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Restaurar carne"))
                {
                    vitalidade.Vitalidade.Restaurar(vitalidade.Vitalidade.Max);
                    Dizer("Vitalidade restaurada.");
                }

                if (GUILayout.Button(vitalidade.IgnorarDano
                                     ? "Voltar a sentir dano"
                                     : "Ignorar dano físico"))
                {
                    vitalidade.IgnorarDano = !vitalidade.IgnorarDano;
                    Dizer(vitalidade.IgnorarDano ? "Dano físico ignorado." : "Dano físico ligado.");
                }
                GUILayout.EndHorizontal();
            }

            var resiliencia = jogador.GetComponent<ResilienciaBridge>();
            if (resiliencia != null && resiliencia.Ligada)
            {
                GUILayout.Space(4f);
                GUILayout.Label($"Resiliência Mental: {resiliencia.Atual:0}" +
                                (resiliencia.IgnorarTrauma ? "   [SEM TRAUMA]" : ""));

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Ancorar a mente"))
                {
                    resiliencia.Ancorar(9999f);
                    Dizer("Resiliência ancorada.");
                }

                if (GUILayout.Button(resiliencia.IgnorarTrauma
                                     ? "Voltar a sofrer Trauma"
                                     : "Ignorar Trauma"))
                {
                    resiliencia.IgnorarTrauma = !resiliencia.IgnorarTrauma;
                    Dizer(resiliencia.IgnorarTrauma ? "Trauma ignorado." : "Trauma ligado.");
                }
                GUILayout.EndHorizontal();
            }

            DesenharEquipado();
        }

        /// <summary>
        /// O que está equipado, <b>com o nível do item</b> — o número que decide o dano e que
        /// não aparece em lugar nenhum da UI do jogo.
        /// </summary>
        private void DesenharEquipado()
        {
            var inventario = InventoryManager.Instance;
            if (inventario?.Equipment == null) return;

            GUILayout.Space(6f);
            GUILayout.Label("Equipado:");

            bool algum = false;

            for (int i = 0; i < inventario.Equipment.Capacidade; i++)
            {
                var slot = inventario.Equipment.GetSlot(i);
                if (slot?.Def == null) continue;

                algum = true;
                string linha = $"   {inventario.Equipment.GetSlotType(i)}: {slot.Def.Nome} " +
                               $"[{slot.Grau}, nível {slot.NivelDoItem}]";

                // A conta da arma, que é o que interessa quando se está aferindo uma luta.
                if (slot.Def.Tipo == ItemType.Arma && slot.Def.Base != null)
                {
                    var p = slot.Def.Base.PerfilNoNivel(slot.NivelDoItem);
                    linha += $"\n      dano {p.DanoMin:0.#}–{p.DanoMax:0.#}, " +
                             $"crítico {p.ChanceCritica:P0}×{p.MultiplicadorCritico:0.##}, " +
                             $"precisão {p.Precisao:P0}";
                }

                GUILayout.Label(linha);
            }

            if (!algum) GUILayout.Label("   (nada)");
        }

        // ── Aba: Arsenal ──────────────────────────────────────────────────────

        private void DesenharArsenal()
        {
            GUILayout.Label("Concede um item JÁ AUTORADO, rolado pelas regras do jogo. " +
                            "Criar item novo é na Forja do Editor.");

            GUILayout.Space(4f);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Nível do item:", GUILayout.Width(90f));
            _nivelDesejado = GUILayout.TextField(_nivelDesejado, GUILayout.Width(50f));

            if (GUILayout.Button("= meu nível", GUILayout.Width(90f)))
                _nivelDesejado = (ProgressionBridge.Instancia?.NivelAtual ?? 1).ToString();

            GUILayout.EndHorizontal();

            _rolarGrauPelaCurva = GUILayout.Toggle(_rolarGrauPelaCurva,
                "Rolar o grau pela curva (como o jogo faz)");

            if (!_rolarGrauPelaCurva)
            {
                GUILayout.BeginHorizontal();
                foreach (var grau in new[] { GrauDeImpregnacao.Inerte, GrauDeImpregnacao.Marcado,
                                             GrauDeImpregnacao.Impregnado })
                {
                    if (GUILayout.Toggle(_grauEscolhido == grau, grau.ToString(), "Button"))
                        _grauEscolhido = grau;
                }
                GUILayout.EndHorizontal();
            }

            GUILayout.Space(4f);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Filtro:", GUILayout.Width(45f));
            _filtroDeItem = GUILayout.TextField(_filtroDeItem);
            GUILayout.EndHorizontal();

            GUILayout.Space(6f);

            foreach (var def in Equipamentos())
            {
                if (!string.IsNullOrWhiteSpace(_filtroDeItem) &&
                    def.Nome.IndexOf(_filtroDeItem, System.StringComparison.OrdinalIgnoreCase) < 0)
                    continue;

                GUILayout.BeginHorizontal();
                GUILayout.Label($"{def.Nome}  ({def.Tipo})");

                if (GUILayout.Button("Dar", GUILayout.Width(50f))) Conceder(def, equipar: false);

                if (GUILayout.Button("Dar e equipar", GUILayout.Width(110f)))
                    Conceder(def, equipar: true);

                GUILayout.EndHorizontal();
            }
        }

        /// <summary>Só equipamento: consumível e chave não rolam grau nem escalam por nível.</summary>
        private ItemDef[] Equipamentos()
        {
            if (_equipamentos != null) return _equipamentos;

            _equipamentos = Resources.LoadAll<ItemDef>("")
                .Where(d => d != null && (d.Tipo == ItemType.Arma ||
                                          d.Tipo == ItemType.Armadura ||
                                          d.Tipo == ItemType.Amuleto))
                .OrderBy(d => d.Tipo)
                .ThenBy(d => d.Nome)
                .ToArray();

            return _equipamentos;
        }

        private readonly GeradorDeItem _gerador = new GeradorDeItem();
        private readonly IFonteDeAleatoriedade _fonte = new FonteDeAleatoriedadeUnity();

        /// <summary>
        /// Rola o exemplar pelas <b>mesmas regras</b> do <c>DropAoAbater</c> e do baú. Conceder
        /// por um caminho próprio produziria um item que o jogo não produz — e então o console
        /// estaria testando outra coisa que não o jogo.
        /// </summary>
        private void Conceder(ItemDef def, bool equipar)
        {
            var inventario = InventoryManager.Instance;
            if (inventario == null) { Dizer("InventoryManager ausente nesta cena."); return; }

            if (!int.TryParse(_nivelDesejado, out int nivel) || nivel < 1) nivel = 1;

            int nivelDoJogador = ProgressionBridge.Instancia?.NivelAtual ?? 1;

            var grau = _rolarGrauPelaCurva
                ? CurvaDeGrau.Sortear(nivelDoJogador, GrauDeImpregnacao.Inerte, _fonte)
                : _grauEscolhido;

            var exemplar = _gerador.Gerar(def, grau, nivel, CatalogoDeAfixos.Todos, _fonte);

            if (exemplar == null) { Dizer($"O gerador recusou '{def.Nome}'."); return; }

            if (!inventario.Main.Add(exemplar))
            {
                Dizer($"Mochila cheia — '{def.Nome}' não coube.");
                return;
            }

            string resumo = $"{def.Nome} [{exemplar.Grau}, nível {exemplar.NivelDoItem}]";

            if (!equipar) { Dizer($"Concedido: {resumo}"); return; }

            for (int i = 0; i < inventario.Main.Capacidade; i++)
            {
                if (inventario.Main.GetSlot(i) != exemplar) continue;

                Dizer(inventario.Equipar(i)
                      ? $"Equipado: {resumo}"
                      : $"Concedido (o slot recusou): {resumo}");
                return;
            }

            Dizer($"Concedido: {resumo}");
        }

        // ── Aba: Progressão ───────────────────────────────────────────────────

        private void DesenharProgressao()
        {
            var bridge = ProgressionBridge.Instancia;
            var progressao = bridge?.Progressao;

            if (progressao == null)
            {
                GUILayout.Label("ProgressionBridge não nasceu — nada a fazer aqui.");
                return;
            }

            GUILayout.Label($"Nível {progressao.NivelAtual}, {progressao.ExposicaoAtual} " +
                            "de Exposição acumulada.");

            GUILayout.Space(4f);
            GUILayout.Label("Somar Exposição:");

            GUILayout.BeginHorizontal();
            foreach (int quanto in new[] { 25, 100, 300, 1000 })
            {
                if (GUILayout.Button($"+{quanto}")) bridge.AdicionarExposicao(quanto);
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(6f);
            GUILayout.Label("Ir direto para o nível:");

            // Só sobe. Descer exigiria zerar a Exposição e os Ecos já gastos, e um botão que
            // apaga progresso em silêncio é pior que a ausência dele.
            for (int linha = 0; linha < 3; linha++)
            {
                GUILayout.BeginHorizontal();
                for (int coluna = 1; coluna <= 4; coluna++)
                {
                    int nivel = linha * 4 + coluna;
                    if (nivel > progressao.NivelMaximo) break;

                    GUI.enabled = nivel > progressao.NivelAtual;

                    if (GUILayout.Button($"{nivel}"))
                    {
                        int falta = progressao.ExposicaoParaONivel(nivel)
                                    - progressao.ExposicaoAtual;

                        if (falta > 0) bridge.AdicionarExposicao(falta);
                        Dizer($"Nível {progressao.NivelAtual}.");
                    }

                    GUI.enabled = true;
                }
                GUILayout.EndHorizontal();
            }

            GUILayout.Space(6f);
            GUILayout.Label("O nível do jogador governa: a faixa de dano da arma que cai, o pool " +
                            "de afixos e a curva de raridade. No nível 3 a luta do Byakhee vira " +
                            "uma troca de 9 golpes por 9.");
        }

        // ── Aba: Ir para ──────────────────────────────────────────────────────

        /// <summary>
        /// Os destinos que importam para aferir uma luta, com o que espera em cada um.
        /// </summary>
        private static readonly (string Cena, string Rotulo)[] Destinos =
        {
            ("Deserto_Hali", "Deserto de Hali — 11 Cultistas, a tempestade"),
            ("Playtest_RuinasPalidas", "Tumba de Alhazred — o Baú e o Abdul"),
            ("Portoes_Das_Ruinas", "Portões das Ruínas — o BYAKHEE"),
            ("Santuario_Yhtill", "Santuário de Yhtill"),
            ("Castelo_Carcosa", "Castelo de Carcosa — o Rei em Amarelo"),
            ("Cena_Menu", "Menu principal"),
        };

        private void DesenharDestinos()
        {
            GUILayout.Label("Troca de cena preservando o progresso da sessão " +
                            "(NavegacaoDeCenas.IrPara).");
            GUILayout.Space(4f);

            foreach (var (cena, rotulo) in Destinos)
            {
                if (!GUILayout.Button(rotulo)) continue;

                // Fecha ANTES de trocar: o console sobrevive à troca de cena (DontDestroyOnLoad)
                // e ficaria aberto com timeScale 0 na cena nova -- o jogo abriria congelado.
                Alternar();
                NavegacaoDeCenas.IrPara(cena);
                return;
            }
        }

        private void Dizer(string mensagem)
        {
            _ultimaMensagem = mensagem;
            Debug.Log($"[ConsoleDeCarcosa] {mensagem}");
        }
    }
}

#endif
