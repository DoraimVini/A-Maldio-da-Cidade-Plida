using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using FavelaAmarela.Core.Abilities;
using FavelaAmarela.Core.Combat;
using FavelaAmarela.Core.Dialogo;
using FavelaAmarela.Core.Enemies;
using FavelaAmarela.Core.Persistencia;
using FavelaAmarela.Runtime.Combat;
using FavelaAmarela.Runtime.GameLoop;
using FavelaAmarela.Runtime.Persistencia;
using FavelaAmarela.Runtime.Interaction;
using FavelaAmarela.Runtime.UI;

namespace FavelaAmarela.Runtime.Enemies
{
    /// <summary>
    /// Camada Runtime (MonoBehaviour). Adaptador de <b>Abdul Alhazred</b>, a Aparição
    /// Primordial da Tumba: liga a <see cref="AbdulFSM"/> (regras da luta) à cena
    /// (vitalidade, escudo visual, invocações, drop do Necronomicon).
    ///
    /// <para>Como Aparição Primordial, ele é <b>imune a crítico de furtividade</b>
    /// (<see cref="EhAparicaoPrimordial"/> = true): furtividade serve para chegar até a
    /// luta, não para resolvê-la. E o dano só entra quando o Escudo Mágico está baixo —
    /// a FSM é a fonte de verdade disso via <c>PodeReceberDano</c>.</para>
    ///
    /// <para>A luta <b>não começa por proximidade</b>: enquanto está em Transe, Abdul é um
    /// <see cref="IInteragivel"/> — Damião chega, o prompt oferece falar com ele, e a
    /// conversa termina numa <b>escolha ramificada</b> (decisão dos diretores, 2026-07-30):
    /// <b>lutar</b> (derrotá-lo dropa o Necronomicon e liberta Yug-Neth, o filhote Mi-Go
    /// acorrentado) ou <b>concordar</b> com ele (poupa Abdul — sem Necronomicon, mas ainda
    /// liberta Yug-Neth). Yug-Neth é obrigatório nos dois caminhos: sem ele não se abrem
    /// os Portões de Carcosa. Ambos os caminhos só existem através desta conversa — não
    /// há gatilho separado nas correntes de Yug-Neth. Depois de resolvida a conversa (por
    /// qualquer caminho) ele deixa de ser interagível.</para>
    ///
    /// <para>Yug-Neth (<see cref="YugNethAI"/>) já existe na cena, cativo, vagando perto
    /// de Abdul (nunca instanciado em runtime) — <see cref="LibertarYugNeth"/> só chama
    /// <c>Bind</c> nessa instância existente para ele passar a seguir o jogador. Durante a
    /// luta, ele não é alvo de nada: ainda está sob controle de Abdul.</para>
    ///
    /// Mesma divisão de <c>CultistaFSM</c>/<c>CultistaAI</c>: nenhuma regra de luta aqui.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    [AddComponentMenu("Favela Amarela/Enemies/Abdul Alhazred")]
    public sealed class AbdulAlhazredAI : MonoBehaviour, IDanificavel, IInteragivel,
                                       FavelaAmarela.Runtime.Itens.IFonteDeEspolio
    {
        [Header("Ficha de Atributos")]
        [Tooltip("Ficha do Abdul (Vitalidade, Defesa, Conjuração, Resistência Anômala).")]
        [SerializeField] private FichaAtributosConfig ficha;

        [Tooltip("Nível do Abdul. Escala Vitalidade, Ataque e Defesa pela mesma lei da arma " +
                 "do jogador. 1 = exatamente o que a ficha diz.")]
        [Min(1)]
        [SerializeField] private int nivelDaUnidade = 1;

        [Header("Conversa antes da luta")]
        [Tooltip("Rótulo do prompt de interação enquanto Abdul dorme em Transe.")]
        [SerializeField] private string rotuloDeInteracao = "Falar com o vulto";

        [Tooltip("Falas ditas em sequência antes da luta começar. A última encerra e desperta Abdul.")]
        [TextArea(2, 4)]
        [SerializeField] private string[] falasAntesDaLuta =
        {
            "O vulto não se move. A voz vem de dentro da sua cabeça, seca como areia.",
            "\"Vieste até a minha cripta atrás do livro. Todos vêm.\"",
            "\"Eu o escrevi acordado. Tu não sobreviverias a uma só página.\"",
            "A poeira ao redor começa a subir. O Necronomicon range sob o braço dele.",
        };

        [Tooltip("Caixa de texto usada para as falas (reaproveita a UI de dica por ora).")]
        [SerializeField] private TutorialHintUI caixaDeDialogo;

        [Tooltip("Segundos que cada fala fica na tela antes de liberar a próxima.")]
        [SerializeField] private float duracaoDaFala = 4f;

        [Header("Escolha ao fim da conversa")]
        [Tooltip("Painel que apresenta as duas opções (Lutar / Concordar) após a última fala.")]
        [SerializeField] private PainelDeEscolha painelDeEscolha;

        [Tooltip("Texto da opção que inicia a luta.")]
        [SerializeField] private string textoOpcaoLutar = "Lutar contra ele";

        [Tooltip("Texto da opção que aceita a trégua e liberta Yug-Neth.")]
        [SerializeField] private string textoOpcaoConcordar = "Concordar — poupar Abdul";

        [Header("Yug-Neth acorrentado")]
        [Tooltip("Instância de Yug-Neth já presente na cena (cativo, vagando perto de Abdul). Não é um prefab — é a referência direta ao GameObject da arena. [CENA]")]
        [SerializeField] private YugNethAI yugNethNaArena;

        [Header("Arena")]
        [Tooltip("Tranca as saídas da arena durante a luta — nenhum chefe pode ser abandonado " +
                 "antes do desfecho. Opcional: sem ela, a luta funciona mas dá para fugir. [CENA]")]
        [SerializeField] private TrancaDeArena trancaDaArena;

        private const int OpcaoLutar = 0;
        private const int OpcaoConcordar = 1;

        private int _falaAtual;
        private GameObject _jogadorNaConversa;

        /// <summary>
        /// Para onde as conjurações miram. Capturado de quem conversou com Abdul (a luta só
        /// começa por interação, então o alvo é sempre conhecido quando ela começa) — evita
        /// um <c>FindObjectOfType</c>, proibido em produção.
        /// </summary>
        private Transform _alvoDasConjuracoes;
        private bool _yugNethJaLibertado;

        /// <summary>
        /// <summary>
        /// True quando o jogador escolheu "Concordar" — Abdul fica inerte, sem luta e sem
        /// Necronomicon, <b>enquanto não for atacado</b>. Se o jogador o golpear depois,
        /// isso é traição da trégua: vira <c>false</c> e a luta de verdade começa (ver
        /// <see cref="ReceberGolpe"/>) — a paz não é permanente, só dura até o jogador
        /// decidir quebrá-la.
        /// </summary>
        private bool _poupado;

        [Header("Ritmo da luta")]
        [Tooltip("Fração de vida que dispara a Fase 2 (escudo permanente).")]
        [Range(0.05f, 0.9f)]
        [SerializeField] private float fracaoParaFase2 = 0.35f;

        [Tooltip("Segundos que o escudo fica baixo ao quebrar uma Pedra de Poder (Fase 1).")]
        [SerializeField] private float duracaoEscudoQuebrado = 6f;

        [Tooltip("Magias conjuradas antes de esgotar a mana (Fase 2).")]
        [SerializeField] private int magiasPorCiclo = 3;

        [Tooltip("Segundos de exaustão (escudo baixo) após esgotar a mana.")]
        [SerializeField] private float duracaoExaustao = 5f;

        [Tooltip("Segundos entre conjurações.")]
        [SerializeField] private float intervaloDeConjuracao = 3f;

        [Header("Invocações e conjurações")]
        [Tooltip("Prefab do esqueleto invocado (Fase 1 e 2). [ASSET]")]
        [SerializeField] private GameObject prefabEsqueleto;

        [Tooltip("Pontos de onde os esqueletos surgem. Se vazio, surgem ao redor do Abdul.")]
        [SerializeField] private Transform[] pontosDeInvocacao;

        [Tooltip("Quantos esqueletos por invocação.")]
        [SerializeField] private int esqueletosPorInvocacao = 2;

        [Tooltip("Diagnóstico: loga cada esqueleto invocado (posição e alvo). Deixe ligado " +
                 "enquanto a luta estiver sendo depurada; desligue depois.")]
        [SerializeField] private bool logarInvocacoes = true;

        [Tooltip("Prefab do Cone de Gelo (Fase 2). [ASSET]")]
        [SerializeField] private GameObject prefabConeDeGelo;

        [Header("Pedras de Poder (Fase 1)")]
        [Tooltip("Prefab da Pedra de Poder. Não fica pré-plantada na dungeon — nasce só ao entrar na Fase 1 e some ao virar Fase 2 (o escudo deixa de depender delas). [ASSET]")]
        [SerializeField] private GameObject prefabPedraDePoder;

        [Tooltip("Pontos onde as Pedras nascem. Se vazio, distribui em losango ao redor do Abdul.")]
        [SerializeField] private Transform[] pontosDasPedras;

        [Tooltip("Meia-distância do losango de Pedras quando não há pontos manuais.")]
        [SerializeField] private float raioDasPedras = 4.5f;

        private readonly List<GameObject> _pedrasAtivas = new List<GameObject>();

        [Header("Escudo Mágico")]
        [Tooltip("Objeto visual do escudo — ligado/desligado conforme a FSM. [ASSET]")]
        [SerializeField] private GameObject visualDoEscudo;

        [Tooltip("Cor do sprite enquanto vulnerável (escudo baixo).")]
        [SerializeField] private Color corVulneravel = Color.white;

        [Tooltip("Cor do sprite enquanto protegido pelo escudo.")]
        [SerializeField] private Color corProtegido = new Color(0.55f, 0.75f, 1f);

        [Header("Drop")]
        [Tooltip("Prefab do Necronomicon, dropado ao ser abatido. [ASSET]")]
        [SerializeField] private GameObject prefabNecronomicon;

        [Header("Feedback")]
        [Tooltip("Exibe números de dano flutuantes quando o Abdul é ferido.")]
        [SerializeField] private bool mostrarNumerosDeDano = true;

        [Tooltip("Cor dos números de dano sofridos pelo Abdul.")]
        [SerializeField] private Color corDoDano = new Color(0.7f, 0.9f, 1f);

        [Tooltip("Cor dos números do sangramento (Ferida de Aklo) — distinta do golpe direto.")]
        [SerializeField] private Color corDoSangramento = new Color(0.85f, 0.15f, 0.2f);

        private AbdulFSM _fsm;
        private Vitalidade _vitalidade;
        private readonly Sangramento _sangramento = new Sangramento();

        // Dano de sangramento já sofrido mas ainda não exibido — ver AcumularNumeroDeSangramento.
        private float _danoDeSangramentoPendente;
        private FichaDeAtributos _atributos;

        // Injetado pelo GameLoopBootstrap (Fase 5, 2026-08-18). Usado uma vez, quando Yug-Neth é
        // libertado — antes isso passava por GameManager.Instance.RegistrarYugNeth.
        private FavelaAmarela.Player.CompanionManager _companheiro;

        /// <summary>Liga o registrador de companheiro. Chamado pelo <c>GameLoopBootstrap</c>.</summary>
        public void BindCompanheiro(FavelaAmarela.Player.CompanionManager companheiro)
        {
            _companheiro = companheiro;
        }
        [Header("Animação")]
        [Tooltip("Animator com o Abdul_AC_Mage. Vazio: o boss desenha o quadro parado, como antes.")]
        [SerializeField] private Animator animator;

        private SpriteRenderer _spriteRenderer;

        /// <summary>FSM da luta. Null antes do Awake.</summary>
        public AbdulFSM Fsm => _fsm;

        /// <summary>Vitalidade do boss (para a barra de vida de chefe, quando existir).</summary>
        public Vitalidade Vitalidade => _vitalidade;

        /// <inheritdoc />
        /// <remarks>Sempre true: Aparições Primordiais são imunes a crítico furtivo.</remarks>
        public bool EhAparicaoPrimordial => true;

        private void Awake()
        {
            // A caixa de diálogo vive no prefab persistente do HUD desde 2026-08-22.
            // O campo do Inspector continua valendo para quem quiser uma própria;
            // vazio, cai para a global — senão esta referência viraria nula ao
            // migrar a caixa para fora da cena.
            if (caixaDeDialogo == null) caixaDeDialogo = FavelaAmarela.Runtime.UI.TutorialHintUI.Instancia;

            // Área atingível derivada do sprite — Abdul implementa IDanificavel direto, sem EnemyBase.
            // A garantia vive aqui, no código, e não numa lista de prefabs: listas
            // escritas à mão são o modo de falha mais repetido deste projeto.
            FavelaAmarela.Runtime.Combat.Hurtbox.GarantirPara(gameObject, "EnemyHurtbox");

            _spriteRenderer = GetComponent<SpriteRenderer>();

            if (animator == null) animator = GetComponent<Animator>();

            if (ficha == null)
            {
                Debug.LogError($"[AbdulAlhazredAI] Ficha não atribuída em '{name}'. " +
                               "Usando ficha de emergência (Vitalidade 400, Defesa 10).", this);
                _atributos = new FichaDeAtributos(
                    vitalidadeMax: 400f, ataque: 0f, defesa: 10f,
                    conjuracao: 30f, resistenciaAnomala: 20f);
            }
            else
            {
                _atributos = ficha.CriarFicha(nivelDaUnidade);
            }

            _vitalidade = new Vitalidade(_atributos.VitalidadeMax);

            _fsm = new AbdulFSM(
                fracaoParaFase2: fracaoParaFase2,
                duracaoEscudoQuebrado: duracaoEscudoQuebrado,
                magiasPorCiclo: magiasPorCiclo,
                duracaoExaustao: duracaoExaustao,
                intervaloDeConjuracao: intervaloDeConjuracao);

            _fsm.OnEscudoMudou += HandleEscudoMudou;
            _fsm.OnInvocarEsqueletos += HandleInvocarEsqueletos;
            _fsm.OnConjurarConeDeGelo += HandleConjurarConeDeGelo;
            _fsm.OnDerrotado += HandleDerrotado;
            _fsm.OnStateChanged += HandleEstadoMudou;

            AplicarVisualDeEscudo(_fsm.EscudoAtivo);
        }

        private void OnDestroy()
        {
            if (_fsm == null) return;
            _fsm.OnEscudoMudou -= HandleEscudoMudou;
            _fsm.OnInvocarEsqueletos -= HandleInvocarEsqueletos;
            _fsm.OnConjurarConeDeGelo -= HandleConjurarConeDeGelo;
            _fsm.OnDerrotado -= HandleDerrotado;
            _fsm.OnStateChanged -= HandleEstadoMudou;
        }

        private void FixedUpdate()
        {
            EscoarSangramento(Time.fixedDeltaTime);
            _fsm.Tick(Time.fixedDeltaTime);
        }

        /// <summary>
        /// Escoa a Ferida de Aklo. **Atravessa o Escudo Mágico de propósito**: a ferida foi
        /// aberta na janela de vulnerabilidade e continua drenando enquanto ele se protege.
        ///
        /// <para>É isto que torna o Estilete de Irem — a arma de menor dano do baú — viável
        /// contra um boss cujo escudo fecha a janela de golpe: em vez de disputar dano por
        /// segundo numa janela curta, ele cobra durante a espera. Sem isso, a arma mais
        /// fraca seria só a pior escolha, e a regra "vencível com qualquer uma das 3 armas"
        /// (baú é RNG) não se sustentaria.</para>
        ///
        /// <para>O escoamento não passa pela Defesa: ela já mitigou o golpe que abriu a ferida.</para>
        /// </summary>
        private void EscoarSangramento(float dt)
        {
            if (!_sangramento.Ativo) return;
            if (_fsm.CurrentState == AbdulState.Transe || _fsm.CurrentState == AbdulState.Derrotado) return;

            var tick = _sangramento.Tick(dt);

            if (tick.DanoContinuo > 0f)
            {
                _vitalidade.Ferir(tick.DanoContinuo);
                AcumularNumeroDeSangramento(tick.DanoContinuo);
            }

            if (tick.Explodiu)
            {
                // Estouro percentual: é isto que faz a arma de menor dano do baú valer
                // contra um boss de muita vida. Ver ExplosaoDeSangramento.
                float danoDoEstouro = ExplosaoDeSangramento.Calcular(
                    _atributos.VitalidadeMax, ehAparicaoPrimordial: true);

                if (danoDoEstouro > 0f)
                {
                    _vitalidade.Ferir(danoDoEstouro);
                    if (mostrarNumerosDeDano)
                        DanoFlutuante.Mostrar(transform.position, danoDoEstouro, corDoSangramento);
                }
            }

            // A ferida pode matar: a FSM precisa saber para virar de fase ou derrotar.
            _fsm.AtualizarFracaoDeVida(_vitalidade.Percentual);
        }

        /// <summary>
        /// Junta o dano de sangramento e só mostra um número quando ele vira algo legível.
        /// Mesmo motivo do <c>CultistaAI</c>: o escoamento entrega frações minúsculas por
        /// tick, que o <c>DanoFlutuante</c> arredondava para <b>"0"</b> — dando a impressão
        /// de que a Ferida de Aklo não causava dano nenhum. Também evita instanciar um
        /// GameObject por <c>FixedUpdate</c> (Regra de Ouro 1).
        /// </summary>
        private void AcumularNumeroDeSangramento(float dano)
        {
            if (!mostrarNumerosDeDano) return;

            _danoDeSangramentoPendente += dano;
            if (_danoDeSangramentoPendente < 1f) return;

            DanoFlutuante.Mostrar(transform.position, _danoDeSangramentoPendente, corDoSangramento);
            _danoDeSangramentoPendente = 0f;
        }

        /// <summary>
        /// Começa a luta (chamado pelo gatilho de interação com o grimório). Tira Abdul
        /// do transe: a partir daqui o escudo sobe e ele passa a conjurar.
        /// </summary>
        public void IniciarLuta() => _fsm.IniciarLuta();

        // ── IInteragivel: a conversa que desperta a Aparição ──────────────────

        /// <inheritdoc />
        public string RotuloDeInteracao => rotuloDeInteracao;

        /// <summary>
        /// Só é interagível enquanto dorme em Transe <b>e</b> a conversa ainda não foi
        /// resolvida. Depois de desperto (luta) ou poupado (trégua), o prompt some — não
        /// se conversa de novo no meio de uma luta nem depois de já ter decidido.
        /// </summary>
        public bool PodeInteragir => !_poupado && _fsm != null && _fsm.CurrentState == AbdulState.Transe;

        /// <summary>Prioridade máxima: é o clímax da dungeon, ganha de qualquer coisa ao lado.</summary>
        public int PrioridadeDeInteracao => 100;

        /// <inheritdoc />
        public Vector2 PosicaoDeInteracao => transform.position;

        /// <summary>
        /// Avança a conversa uma fala por aperto. Quando a última fala é dita, apresenta
        /// a escolha — <b>lutar</b> (ganha o Necronomicon e liberta Yug-Neth) ou
        /// <b>concordar</b> (poupa Abdul e liberta só Yug-Neth). É o jogador quem decide,
        /// não a conversa sozinha.
        /// </summary>
        public void Interagir(GameObject quemInterage)
        {
            if (!PodeInteragir) return;

            _jogadorNaConversa = quemInterage;
            if (quemInterage != null) _alvoDasConjuracoes = quemInterage.transform;

            // Sem falas configuradas: pula direto para a escolha.
            if (falasAntesDaLuta == null || falasAntesDaLuta.Length == 0)
            {
                ApresentarEscolha();
                return;
            }

            if (caixaDeDialogo != null)
                caixaDeDialogo.Mostrar(falasAntesDaLuta[_falaAtual], duracaoDaFala);
            else
                Debug.LogWarning("[AbdulAlhazredAI] Caixa de diálogo não atribuída — as falas " +
                                 "antes da luta não aparecerão.", this);

            _falaAtual++;

            if (_falaAtual >= falasAntesDaLuta.Length)
                ApresentarEscolha();
        }

        /// <summary>
        /// Abre a escolha ramificada. Sem painel atribuído, cai no comportamento antigo
        /// (inicia a luta direto) para nunca travar o jogo por peça de UI faltando.
        /// </summary>
        private void ApresentarEscolha()
        {
            if (painelDeEscolha == null)
            {
                Debug.LogWarning("[AbdulAlhazredAI] Painel de Escolha não atribuído — " +
                                 "iniciando a luta direto (sem opção de trégua).", this);
                IniciarLuta();
                return;
            }

            var opcoes = new[]
            {
                new OpcaoDeDialogo(textoOpcaoLutar, OpcaoLutar),
                new OpcaoDeDialogo(textoOpcaoConcordar, OpcaoConcordar),
            };
            painelDeEscolha.Mostrar(opcoes, ResolverEscolha);
        }

        private void ResolverEscolha(int idDaOpcao)
        {
            if (idDaOpcao == OpcaoConcordar)
            {
                _poupado = true;
                GerenciadorDeSave.DefinirValor(ChavesDeSave.AbdulResolvido,
                                               ChavesDeSave.ValorAbdulPoupado);
                LibertarYugNeth();
            }
            else
            {
                IniciarLuta();
            }
        }

        /// <summary>
        /// Liberta Yug-Neth: chama <see cref="YugNethAI.Bind"/> na instância que já existe
        /// na cena (cativa, vagando perto de Abdul) para ele passar a seguir quem
        /// conversou. Chamado tanto pela trégua (aqui) quanto pela derrota em combate
        /// (<see cref="HandleDerrotado"/>) — Yug-Neth é libertado nos dois caminhos; só o
        /// Necronomicon é exclusivo da luta. Idempotente.
        /// </summary>
        private void LibertarYugNeth()
        {
            if (_yugNethJaLibertado) return;
            _yugNethJaLibertado = true;

            if (yugNethNaArena == null)
            {
                Debug.LogError("[AbdulAlhazredAI] Yug-Neth não atribuído — o companheiro " +
                               "obrigatório para abrir os Portões de Carcosa não foi libertado.", this);
                return;
            }

            if (_jogadorNaConversa != null)
                yugNethNaArena.Bind(_jogadorNaConversa.transform);

            // Registro pontual, não hot-path: acontece uma vez, no evento "Yug-Neth acabou de ser
            // libertado". Fase 5 (2026-08-18): o CompanionManager chega por injeção do bootstrap,
            // em vez de ser alcançado por GameManager.Instance.
            if (_companheiro != null)
                _companheiro.RegistrarYugNeth(yugNethNaArena);
            else
                Debug.LogWarning("[AbdulAlhazredAI] Sem CompanionManager ligado — Yug-Neth não " +
                                 "será registrado como companheiro da run.", this);
        }

        /// <summary>
        /// Damião destruiu uma Pedra de Poder da arena. Só derruba o escudo na Fase 1 —
        /// a FSM decide.
        /// </summary>
        public void QuebrarPedraDePoder() => _fsm.QuebrarPedraDePoder();

        /// <summary>
        /// Informa quem Abdul deve mirar nas conjurações. Normalmente capturado na conversa;
        /// existe como método público para o caso da luta começar por outro caminho (ex.: a
        /// traição da trégua, em que o agressor pode não ser quem conversou).
        /// </summary>
        public void MirarEm(Transform alvo)
        {
            if (alvo != null) _alvoDasConjuracoes = alvo;
        }

        /// <inheritdoc />
        public void ReceberGolpe(ArmaResult resultado)
        {
            // Traição da trégua: atacar Abdul depois de "Concordar" reabre a luta de
            // verdade (decisão do Vini, 2026-07-30) — ele ainda pode ser derrotado e
            // dropar o Necronomicon depois. O golpe que trai não causa dano: ele só
            // desperta a luta (mesma regra de sempre — o escudo sobe junto com IniciarLuta
            // e só cai ao quebrar uma Pedra de Poder, igual ao caminho normal da luta).
            if (_poupado)
            {
                _poupado = false;
                IniciarLuta();
                return;
            }

            // O escudo é a regra central: fora da janela de vulnerabilidade, nada entra.
            if (!_fsm.PodeReceberDano)
            {
                // Diagnóstico: sem isto, "bati e não aconteceu nada" é indistinguível de
                // "o golpe nem chegou". Diz qual das duas regras barrou o dano.
                if (logarInvocacoes)
                    Debug.Log($"[Abdul] Golpe recusado — estado={_fsm.CurrentState} " +
                              $"escudo={( _fsm.EscudoAtivo ? "ATIVO" : "baixo")} " +
                              $"pedras={_fsm.PedrasQuebradas}/{_fsm.TotalDePedras}", this);
                return;
            }

            // Ferida de Aklo (Estilete de Irem): acumula na janela de dano, e as feridas
            // **continuam sangrando mesmo depois do escudo voltar** — ver EscoarSangramento.
            if (resultado.AcumulosDeSangramento > 0)
                _sangramento.Aplicar(resultado.AcumulosDeSangramento,
                    resultado.SangramentoPorSegundo, resultado.DuracaoSangramento);

            if (resultado.Dano <= 0f) return;

            float danoFinal = MitigacaoDeDano.Aplicar(resultado.Dano, _atributos.Defesa);
            if (danoFinal <= 0f) return;

            _vitalidade.Ferir(danoFinal);

            // Piscada de dano. Não interrompe se o golpe já o abateu: a FSM vai trocar para
            // Derrotado no AtualizarFracaoDeVida abaixo, e 'hit' passaria por cima da morte.
            if (!_vitalidade.EstaAbatido) TocarAnimacao(Anim.Hit);

            if (mostrarNumerosDeDano)
                DanoFlutuante.Mostrar(transform.position, danoFinal, corDoDano);

            // A FSM decide virada de fase e derrota a partir da vida restante.
            _fsm.AtualizarFracaoDeVida(_vitalidade.Percentual);
        }

        private void HandleEscudoMudou(bool ativo) => AplicarVisualDeEscudo(ativo);

        private void AplicarVisualDeEscudo(bool ativo)
        {
            if (visualDoEscudo != null) visualDoEscudo.SetActive(ativo);
            if (_spriteRenderer != null)
                _spriteRenderer.color = ativo ? corProtegido : corVulneravel;
        }

        private void HandleInvocarEsqueletos()
        {
            TocarAnimacao(Anim.Attack);

            // Antes este `return` era mudo ("sem arte ainda"), o que escondia a causa quando
            // os esqueletos não apareciam em playtest: nada no console, nada na tela.
            if (prefabEsqueleto == null)
            {
                Debug.LogWarning("[AbdulAlhazredAI] Invocação pedida mas 'prefabEsqueleto' " +
                                 "está vazio — nenhum esqueleto vai nascer.", this);
                return;
            }

            for (int i = 0; i < esqueletosPorInvocacao; i++)
            {
                Vector3 posicao = ObterPontoDeInvocacao(i);
                var go = Instantiate(prefabEsqueleto, posicao, Quaternion.identity);

                // Sem alvo injetado o esqueleto nasce parado — a pressão da Fase 1
                // depende dele ir atrás do Damião.
                var esqueleto = go.GetComponent<EsqueletoInvocado>();
                if (esqueleto != null) esqueleto.Bind(_alvoDasConjuracoes);
                else
                    Debug.LogWarning($"[AbdulAlhazredAI] '{go.name}' não tem EsqueletoInvocado — " +
                                     "vai nascer parado e inofensivo.", go);

                if (logarInvocacoes)
                    Debug.Log($"[AbdulAlhazredAI] Esqueleto {i + 1}/{esqueletosPorInvocacao} " +
                              $"invocado em {posicao} (alvo: {(_alvoDasConjuracoes != null ? _alvoDasConjuracoes.name : "NENHUM")}).", go);
            }
        }

        private Vector3 ObterPontoDeInvocacao(int indice)
        {
            if (pontosDeInvocacao != null && pontosDeInvocacao.Length > 0)
            {
                var ponto = pontosDeInvocacao[indice % pontosDeInvocacao.Length];
                if (ponto != null) return ponto.position;
            }

            // Fallback: distribui ao redor do Abdul.
            float angulo = indice * Mathf.PI * 2f / Mathf.Max(1, esqueletosPorInvocacao);
            return transform.position + new Vector3(Mathf.Cos(angulo), Mathf.Sin(angulo), 0f) * 1.5f;
        }

        /// <summary>
        /// Lança um Cone de Gelo na direção do Damião. O dano vem da <c>Conjuracao</c> da
        /// ficha do Abdul — o projétil só transporta; a mitigação (pela Resistência Anômala
        /// do alvo) acontece no impacto.
        /// </summary>
        private void HandleConjurarConeDeGelo()
        {
            // Antes do guard do prefab de propósito: a conjuração acontece na ficção mesmo que
            // o projétil não exista, e ver o boss erguer os braços é o aviso que o jogador lê.
            TocarAnimacao(Anim.Attack);

            if (prefabConeDeGelo == null) return; // sem arte ainda: a luta segue sem cones

            var go = Instantiate(prefabConeDeGelo, transform.position, Quaternion.identity);

            var cone = go.GetComponent<ConeDeGelo>();
            if (cone == null)
            {
                Debug.LogWarning("[AbdulAlhazredAI] Prefab do Cone de Gelo não tem ConeDeGelo — " +
                                 "ele não vai congelar nem causar dano.", this);
                return;
            }

            cone.Lancar(DirecaoParaOAlvo(), _atributos.Conjuracao);
        }

        /// <summary>
        /// Direção do Abdul até o alvo da conjuração. Sem alvo conhecido, atira para a
        /// direita — nunca devolve vetor zero (o projétil ficaria parado no lugar).
        /// </summary>
        private Vector2 DirecaoParaOAlvo()
        {
            if (_alvoDasConjuracoes == null) return Vector2.right;

            Vector2 delta = (Vector2)_alvoDasConjuracoes.position - (Vector2)transform.position;
            return delta.sqrMagnitude > 0.0001f ? delta.normalized : Vector2.right;
        }

        private void HandleDerrotado()
        {
            GerenciadorDeSave.DefinirValor(ChavesDeSave.AbdulResolvido,
                                           ChavesDeSave.ValorAbdulDerrotado);

            InstanciarNecronomicon();

            // A luta também liberta Yug-Neth — só o Necronomicon é exclusivo deste caminho.
            LibertarYugNeth();

            RemoverPedrasRestantes();

            // A luta acabou: a arena reabre.
            trancaDaArena?.Destrancar();

            gameObject.SetActive(false);
        }

        /// <summary>
        /// Larga o Necronomicon no chão. Separado de <see cref="HandleDerrotado"/> porque a
        /// restauração de save também precisa dele: o tomo é instanciado em runtime, então
        /// sair da cena sem recolhê-lo o destruiria para sempre.
        /// </summary>
        /// <summary>
        /// Disparado ao ser derrotado — é por aqui que o <c>DropAoAbater</c> materializa o
        /// espólio. O Necronomicon continua vindo por código (é item de rito, garantido); o
        /// que a tabela acrescenta é a <b>recompensa de progressão</b> que o Vini pediu.
        /// </summary>
        public event System.Action OnAbatido;

        private void InstanciarNecronomicon()
        {
            OnAbatido?.Invoke();

            if (prefabNecronomicon != null)
                Instantiate(prefabNecronomicon, transform.position, Quaternion.identity);
            else
                Debug.LogWarning("[AbdulAlhazredAI] Necronomicon sem prefab — o drop não aconteceu.", this);
        }

        /// <summary>
        /// Reconstrói o estado de Abdul ao carregar uma cena onde a conversa/luta já foi
        /// resolvida. Chamado pelo <c>GameManager</c> no bootstrap.
        ///
        /// <para><b>Não passa pela <c>AbdulFSM</c> de propósito.</b> A FSM não tem (nem
        /// precisa ganhar) um jeito de pular direto para um estado terminal. No caminho
        /// "poupado" ela fica em <c>Transe</c>, que é justamente o correto: <c>PodeInteragir</c>
        /// já checa <c>!_poupado</c>, e a traição da trégua continua funcionando sem nenhuma
        /// mudança, porque <c>IniciarLuta()</c> só exige <c>CurrentState == Transe</c>. No
        /// caminho "derrotado" o objeto some, e ninguém mais consulta a FSM dele.</para>
        /// </summary>
        /// <param name="jogador">Damião — quem Yug-Neth passa a seguir ao ser restaurado.</param>
        public void AplicarEstadoSalvo(GameObject jogador)
        {
            string resolvido = GerenciadorDeSave.ObterValor(ChavesDeSave.AbdulResolvido);
            if (resolvido == null) return; // nunca resolvido: cena começa do zero

            if (resolvido == ChavesDeSave.ValorAbdulPoupado)
            {
                _poupado = true;
            }
            else if (resolvido == ChavesDeSave.ValorAbdulDerrotado)
            {
                // O tomo é spawn de runtime: se o jogador saiu sem pegá-lo, ele precisa
                // renascer, senão a recompensa da luta se perde para sempre.
                if (!GerenciadorDeSave.JaAconteceu(ChavesDeSave.NecronomiconColetado))
                    InstanciarNecronomicon();

                gameObject.SetActive(false);
            }

            // A libertação de Yug-Neth é derivada daqui, não gravada em chave própria: os
            // dois desfechos chamam LibertarYugNeth(), e não existe outro gatilho. Uma
            // segunda chave seria uma segunda fonte da verdade, com risco de dessincronizar.
            _jogadorNaConversa = jogador;
            if (jogador != null) _alvoDasConjuracoes = jogador.transform;
            LibertarYugNeth(); // idempotente; também registra no GameManager

            // Luta já resolvida antes desta carga de cena: a arena não pode nascer trancada.
            trancaDaArena?.Destrancar();
        }

        // ── Pedras de Poder: nascem na Fase 1, somem ao sair dela ────────────

        /// <summary>
        /// As Pedras não ficam pré-plantadas na dungeon — só existem enquanto a Fase 1
        /// estiver em curso (é delas que o escudo depende). Ao entrar na Fase 1, nascem;
        /// ao sair dela (virada de fase ou derrota), qualquer uma que ainda esteja de pé é
        /// removida — na Fase 2 o escudo já não depende mais delas.
        /// </summary>
        private void HandleEstadoMudou(AbdulState anterior, AbdulState novo)
        {
            if (novo == AbdulState.Fase1)
            {
                InvocarPedrasDePoder();

                // Entrar na Fase 1 é o único caminho para o combate começar de verdade
                // (inclusive pela traição da trégua, que passa pelo mesmo IniciarLuta) —
                // então é aqui que a arena fecha. A conversa em Transe não tranca nada.
                trancaDaArena?.Trancar();
            }
            else if (anterior == AbdulState.Fase1)
            {
                RemoverPedrasRestantes();
            }

            // Derrotado tem clipe próprio; todo o resto é a figura parada. O Abdul não anda
            // nem teleporta (verificado no AbdulFSM), então 'walk' fica sem consumidor.
            TocarAnimacao(novo == AbdulState.Derrotado ? Anim.Death : Anim.Idle);
        }

        /// <summary>
        /// Põe o Animator num clipe. <b>Quem manda no estado é a <c>AbdulFSM</c></b> — por isso
        /// o <c>Abdul_AC_Mage</c> não tem teia de transições com condições: duplicar a lógica de
        /// fase lá criaria uma segunda fonte de verdade, divergindo desta em silêncio.
        ///
        /// <para>Degrada em silêncio de propósito: sem Animator (ou sem controller), o boss
        /// desenha o quadro parado, como antes. "Mais simples", nunca "invisível".</para>
        /// </summary>
        private void TocarAnimacao(int clipe)
        {
            if (animator == null || animator.runtimeAnimatorController == null) return;
            animator.Play(clipe, 0, 0f);
        }

        /// <summary>
        /// Hashes do <c>Abdul_AC_Mage</c>, resolvidos uma vez: <see cref="TocarAnimacao"/> é
        /// chamado a cada conjuração e a cada golpe recebido, e a Regra de Ouro 1 proíbe alocar
        /// string em caminho quente.
        ///
        /// <para><c>Attack</c> serve tanto a <see cref="HandleConjurarConeDeGelo"/> quanto a
        /// <see cref="HandleInvocarEsqueletos"/>: a folha do Mage não tem clipe separado de
        /// invocação, e nas duas ele ergue os braços. Inventar um clipe de invocação seria
        /// inventar arte que não existe.</para>
        /// </summary>
        private static class Anim
        {
            internal static readonly int Idle = Animator.StringToHash("idle");
            internal static readonly int Attack = Animator.StringToHash("attack");
            internal static readonly int Hit = Animator.StringToHash("hit");
            internal static readonly int Death = Animator.StringToHash("death");
        }

        private void InvocarPedrasDePoder()
        {
            if (prefabPedraDePoder == null)
            {
                Debug.LogWarning("[AbdulAlhazredAI] Prefab da Pedra de Poder não atribuído — " +
                                 "a Fase 1 não terá pedras para quebrar (escudo permanente por engano).", this);
                return;
            }

            foreach (var posicao in ObterPontosDasPedras())
            {
                var go = Instantiate(prefabPedraDePoder, posicao, Quaternion.identity);
                var pedra = go.GetComponent<PedraDePoder>();
                if (pedra != null)
                    pedra.Bind(this);
                else
                    Debug.LogWarning("[AbdulAlhazredAI] Prefab da Pedra de Poder não tem " +
                                     "PedraDePoder — ela não vai derrubar o escudo ao ser quebrada.", this);

                _pedrasAtivas.Add(go);
            }

            // A FSM precisa saber quantas são para reconhecer a última: sem isto, o escudo
            // voltaria para sempre depois da última Pedra e a luta ficaria invencível.
            _fsm.DefinirTotalDePedras(_pedrasAtivas.Count);
        }

        /// <summary>
        /// Onde as Pedras nascem: os pontos autorados, ou o anel de fallback.
        ///
        /// <para><b>A decisão é pelos pontos USÁVEIS, não pelo tamanho do array</b> — e é essa a
        /// diferença que quebrou a luta do Abdul (achado em 2026-08-28, no playtest do Vini).
        /// A cena da Tumba tinha <c>pontosDasPedras</c> com <b>tamanho 1 e o único elemento
        /// nulo</b>: alguém dimensionou o array no Inspector e nunca arrastou o
        /// <c>Transform</c>. A versão anterior perguntava só <c>Length &gt; 0</c>, entrava neste
        /// ramo, o <c>Where(t =&gt; t != null)</c> devolvia <b>vazio</b>, e o laço de invocação
        /// rodava zero vezes.</para>
        ///
        /// <para><b>E o silêncio era total.</b> A guarda de <c>prefabPedraDePoder == null</c>
        /// passava — o prefab <i>estava</i> atribuído. Nenhum log, nenhuma exceção. O que
        /// acontecia depois é que a coisa fica cara: <c>DefinirTotalDePedras(0)</c> deixa
        /// <c>EscudoDestruido</c> (que exige <c>TotalDePedras &gt; 0</c>) <b>falso para
        /// sempre</b>, e como nada pode chamar <c>QuebrarPedraDePoder</c> sem Pedras de pé, o
        /// escudo nunca cai. <b>O chefe fica invencível</b>, e o sintoma que aparece é "as
        /// pedras não nascem".</para>
        /// </summary>
        private Vector3[] ObterPontosDasPedras()
        {
            var autorados = pontosDasPedras == null
                ? System.Array.Empty<Vector3>()
                : pontosDasPedras.Where(t => t != null).Select(t => t.position).ToArray();

            int declarados = pontosDasPedras?.Length ?? 0;

            if (autorados.Length > 0)
            {
                if (autorados.Length < declarados)
                    Debug.LogWarning($"[AbdulAlhazredAI] {declarados - autorados.Length} de " +
                                     $"{declarados} pontos de Pedra estão vazios no Inspector — " +
                                     "nascem menos Pedras do que a arena promete.", this);

                return autorados;
            }

            if (declarados > 0)
                Debug.LogError($"[AbdulAlhazredAI] 'pontosDasPedras' tem {declarados} entrada(s) " +
                               "e TODAS estão vazias. Sem ponto usável não nasce Pedra nenhuma, " +
                               "e sem Pedra o escudo do Abdul NUNCA cai — a luta fica " +
                               "invencível. Usando o anel de fallback ao redor dele.", this);

            // Fallback: quatro Pedras nas diagonais ao redor de Abdul. As diagonais (e não
            // os eixos) casam com a leitura isométrica do chão — no losango anterior, as
            // Pedras de cima e de baixo caíam quase em cima dele, porque o eixo Y é
            // comprimido pela perspectiva.
            float dx = raioDasPedras;
            float dy = raioDasPedras * 0.5f;   // proporção isométrica 2:1
            Vector3 centro = transform.position;
            return new[]
            {
                centro + new Vector3(-dx, -dy, 0f),
                centro + new Vector3( dx, -dy, 0f),
                centro + new Vector3(-dx,  dy, 0f),
                centro + new Vector3( dx,  dy, 0f),
            };
        }

        private void RemoverPedrasRestantes()
        {
            foreach (var pedra in _pedrasAtivas)
                if (pedra != null) Destroy(pedra);
            _pedrasAtivas.Clear();
        }
    }
}
