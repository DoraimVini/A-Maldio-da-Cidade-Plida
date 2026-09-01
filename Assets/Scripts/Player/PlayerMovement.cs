using UnityEngine;
using UnityEngine.InputSystem;
using FavelaAmarela.Core.Stealth;
using FavelaAmarela.Core.Environment;
using FavelaAmarela.Core.Player;
using FavelaAmarela.Runtime.Config;

namespace FavelaAmarela.Player
{
    public enum MovementMode
    {
        Sneaking,
        Walking,
        Running
    }

    /// <summary>
    /// POCO: Pure C# logic for stealth state — no Unity dependencies except Mathf.
    /// Testable independently from MonoBehaviour.
    /// </summary>
    public class PlayerStealthState
    {
        /// <summary>
        /// Quanto a tempestade abafa o ruído do jogador (0 = não abafa, 1 = abafa tudo
        /// na intensidade máxima). Constante de game-feel; nomeada aqui para não ficar
        /// como literal solto dentro do cálculo.
        /// </summary>
        private const float FatorAbafamentoTempestade = 0.6f;

        public MovementMode CurrentMode { get; private set; } = MovementMode.Walking;
        public float Speed { get; private set; }
        public float NoiseRadius { get; private set; }
        public bool IsOdorMasked { get; set; } = false;

        private readonly float sneakSpeed;
        private readonly float sneakNoise;
        private readonly float walkSpeed;
        private readonly float walkNoise;
        private readonly float runSpeed;
        private readonly float runNoise;

        public PlayerStealthState(
            float sneakSpeed = 2.0f, float sneakNoise = 2.0f,
            float walkSpeed = 4.5f, float walkNoise = 5.5f,
            float runSpeed = 7.5f, float runNoise = 8.5f)
        {
            this.sneakSpeed = sneakSpeed;
            this.sneakNoise = sneakNoise;
            this.walkSpeed = walkSpeed;
            this.walkNoise = walkNoise;
            this.runSpeed = runSpeed;
            this.runNoise = runNoise;
            SetMode(MovementMode.Walking);
        }

        public void SetMode(MovementMode mode)
        {
            CurrentMode = mode;
            (Speed, NoiseRadius) = mode switch
            {
                MovementMode.Sneaking => (sneakSpeed, sneakNoise),
                MovementMode.Running  => (runSpeed, runNoise),
                _                     => (walkSpeed, walkNoise),
            };
        }

        /// <summary>
        /// Returns effective noise radius considering storm dampening.
        /// Storm acts as white noise, reducing how far player sounds propagate.
        /// </summary>
        public float GetCurrentNoiseEmission(bool isMoving, float stormIntensity)
            => GetCurrentNoiseEmission(isMoving, stormIntensity, 0f);

        /// <summary>
        /// Ruído efetivo, agora descontando a <b>Furtividade</b> vinda do equipamento.
        ///
        /// <para><b>Por que o parâmetro existe (2026-08-28).</b> <c>StatType.Furtividade</c>
        /// estava no enum, era rolado pelo <c>Artefato_AnelDoSinalAmarelo</c>, e <b>nenhuma
        /// linha do jogo o lia</b> — o artefato prometia discrição e não entregava nada. Num
        /// jogo cujo pilar é a furtividade, era o atributo mais caro de deixar decorativo.</para>
        ///
        /// <para>Entra como <b>redução de raio</b>, não como multiplicador, porque é assim que o
        /// resto do sistema pensa: <c>EnemyPerception</c> compara distância com raio, e a
        /// tempestade também abafa reduzindo raio. O piso de
        /// <see cref="PisoDeRuidoEmMovimento"/> continua valendo depois — quem se move nunca
        /// fica literalmente inaudível, nem com a tempestade nem com o Anel.</para>
        /// </summary>
        /// <param name="furtividade">Bônus agregado de <c>StatType.Furtividade</c>.</param>
        public float GetCurrentNoiseEmission(bool isMoving, float stormIntensity, float furtividade)
        {
            if (!isMoving) return 0f;

            float reduzido = NoiseRadius - (furtividade < 0f ? 0f : furtividade);

            // O piso vale para a Furtividade também, e a primeira versão disto o furava: eu
            // travava o raio em ZERO antes de chamar o abafamento, e lá dentro "raio 0" quer
            // dizer "parado". Com Anel suficiente, o Damião ficava LITERALMENTE inaudível
            // correndo -- a mesma invisibilidade que o piso existe para impedir na tempestade.
            //
            // O teto do piso é o próprio NoiseRadius: um modo de movimento autorado mais
            // silencioso que o piso não pode ficar MAIS barulhento por causa desta linha.
            float chao = Mathf.Min(NoiseRadius, PisoDeRuidoEmMovimento);

            return AplicarAbafamentoTempestade(Mathf.Max(reduzido, chao), stormIntensity);
        }

        /// <summary>
        /// Aplica o abafamento de tempestade a um raio de ruído base. Extraído de
        /// <see cref="GetCurrentNoiseEmission"/> para ser reaproveitado por ruídos
        /// pontuais (ex.: o pulso da Esquiva), que não passam pelo fluxo contínuo
        /// de "está se movendo neste frame".
        /// </summary>
        /// <summary>
        /// Piso do ruído de um ator em movimento. Quem se mexe <b>nunca é literalmente
        /// inaudível</b>.
        ///
        /// <para><b>Por que existe (2026-08-27).</b> Só passou a importar quando os inimigos
        /// começaram a de fato usar o raio do som — antes disso o abafamento não tinha efeito
        /// nenhum. Com a percepção ligada, tempestade cheia levava o Furtivo (2,0) para
        /// <b>0,8</b>: menos que a própria pegada do Cultista, ou seja, seria preciso encostar
        /// nele para ser ouvido.</para>
        ///
        /// <para>Não é um número de design inventado por cima da mecânica — é a mesma classe de
        /// limite que o <c>Clamp01</c> logo abaixo já aplica. A tempestade deve ajudar muito;
        /// não deve conceder invisibilidade. <b>Quanto exatamente, é botão do Vini.</b></para>
        /// </summary>
        public const float PisoDeRuidoEmMovimento = 1.2f;

        public static float AplicarAbafamentoTempestade(float raioBase, float stormIntensity)
        {
            float dampening = 1.0f - Mathf.Clamp01(stormIntensity * FatorAbafamentoTempestade);
            float abafado = raioBase * dampening;

            // Parado não faz barulho, e o piso não pode inventar ruído do nada: ele só impede
            // que um ruído REAL seja abafado até a irrelevância.
            if (raioBase <= 0f) return 0f;

            return Mathf.Max(abafado, PisoDeRuidoEmMovimento);
        }
    }

    /// <summary>
    /// MonoBehaviour Bridge: Connects PlayerStealthState POCO to Unity's
    /// physics (Rigidbody2D) and input (Input System) APIs.
    /// Requires a BoxCollider2D on the same GameObject for wall collision.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(BoxCollider2D))]
    [AddComponentMenu("Favela Amarela/Damião Movement")]
    public class PlayerMovement : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private bool useIsometricGridAlignment = true;

        [Tooltip("Asset de velocidades/ruídos por modo furtivo. Se vazio, usa os defaults do POCO.")]
        [SerializeField] private LocomocaoConfig locomocaoConfig;

        // Período (s) entre broadcasts de som ao andar. Nomeado para não ficar como literal no FixedUpdate.
        private const float IntervaloBroadcastSom = 0.15f;

        [Header("Esquiva")]
        [Tooltip("Raio de ruído emitido no instante da Esquiva. Antes deste fix a Esquiva era 100% silenciosa (o early-return do FixedUpdate pulava o bloco de som), o que deixava o combo Furtivo+Esquiva quebrar a percepção do Cultista na hora.")]
        [SerializeField] private float esquivaNoiseRadius = 6.5f;

        [Header("Debug")]
        [SerializeField] private bool showNoiseGizmo = true;
        [Range(0f, 1f)]
        [SerializeField] private float debugStormIntensity = 0f;

        // Cached references (set once in Awake, never in Update)
        private Rigidbody2D rb;
        private PlayerStealthState stealthState;
        private Vector2 inputDirection;
        private bool isMoving;

        // Fonte única de verdade das ações exclusivas (Esquiva/Salto/Ataque).
        // Substitui as antigas flags-espelho isLeaping/isEsquivando/isAtacando.
        private PlayerStateMachine _fsm;

        // Input System actions (cached in Awake)
        private InputAction moveAction;
        private InputAction sneakAction;
        private InputAction runAction;

        // --- Esquiva (dodge) State ---
        private EsquivaBridge esquivaBridge;
        private Vector2 esquivaVelocity;
        private InputAction dodgeAction;

        // --- Congelamento (imposto pelos Cones de Gelo do Abdul) ---
        private CongelamentoBridge congelamentoBridge;

        // --- Mão Física (ataque) State ---
        private MaoFisicaBridge maoFisicaBridge;
        private InputAction attackAction;
        private InputAction habilidadeArmaAction;

        // --- Artefatos (F1–F4, um por slot equipado) ---
        private ArtefatosBridge artefatosBridge;
        private readonly InputAction[] artefatoActions =
            new InputAction[FavelaAmarela.Core.Artefatos.InventarioDeArtefatos.TotalDeSlots];
        
        private float _odorMaskTimer = 0f;
        private float _silencioTimer = 0f;
        private GerenciadorDeVigor _vigor;

        public PlayerStealthState StealthState => stealthState;
        public bool IsMoving => isMoving;
        public Vector2 LookDirection { get; private set; } = Vector2.right;

        // --- Injected Services ---
        private SoundBroadcastService _soundBroadcaster;
        private EnvironmentState _environment;
        private float _soundTimer;

        public void Bind(SoundBroadcastService broadcaster, EnvironmentState env)
        {
            _soundBroadcaster = broadcaster;
            _environment = env;

            // Sem estes dois, Damião anda em silêncio absoluto: como a percepção dos
            // inimigos é 100% sonora, nenhum deles jamais o caçaria — e o sintoma em
            // playtest é "a IA está quebrada", não "faltou injeção".
            if (_soundBroadcaster == null || _environment == null)
                Debug.LogError("[PlayerMovement] Bind recebeu dependência nula — Damião não " +
                               "vai emitir som e nenhum inimigo vai caçá-lo.", this);
        }

        private void Awake()
        {
            // FSM de ações exclusivas criada antes de qualquer early-return abaixo, para
            // nunca ficar nula em Update/FixedUpdate mesmo se o Awake abortar cedo (ex.: rb nulo).
            _fsm = new PlayerStateMachine();

            // --- Rigidbody2D setup for top-down 2D ---
            rb = GetComponent<Rigidbody2D>();
            if (rb == null)
            {
                Debug.LogError("[PlayerMovement] Rigidbody2D not found!", this);
                return;
            }
            rb.gravityScale = 0f;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;

            esquivaBridge = GetComponent<EsquivaBridge>();
            maoFisicaBridge = GetComponent<MaoFisicaBridge>();
            artefatosBridge = GetComponent<ArtefatosBridge>();
            congelamentoBridge = GetComponent<CongelamentoBridge>();

            // FSM injetada nos bridges, que passam a consultá-la para exclusão mútua
            // em vez de manter flags próprias.
            if (esquivaBridge != null) esquivaBridge.BindStateMachine(_fsm);
            if (maoFisicaBridge != null) maoFisicaBridge.BindStateMachine(_fsm);
            if (congelamentoBridge != null) congelamentoBridge.BindStateMachine(_fsm);

            _vigor = GetComponent<GerenciadorDeVigor>();

            // --- POCO init ---
            if (locomocaoConfig != null)
            {
                stealthState = new PlayerStealthState(
                    locomocaoConfig.SneakSpeed, locomocaoConfig.SneakNoise,
                    locomocaoConfig.WalkSpeed, locomocaoConfig.WalkNoise,
                    locomocaoConfig.RunSpeed, locomocaoConfig.RunNoise);
            }
            else
            {
                Debug.LogWarning("[PlayerMovement] LocomocaoConfig não atribuído; usando defaults do POCO.", this);
                stealthState = new PlayerStealthState();
            }

            // --- Input System: safe lookup via FindAction (returns null, never throws) ---
            var playerInput = GetComponent<PlayerInput>();
            if (playerInput != null)
            {
                moveAction  = playerInput.actions.FindAction("Move");
                sneakAction = playerInput.actions.FindAction("Crouch");
                runAction   = playerInput.actions.FindAction("Sprint");
                dodgeAction = playerInput.actions.FindAction("Esquiva"); // Espaço
                attackAction = playerInput.actions.FindAction("Attack"); // botão esquerdo do mouse
                habilidadeArmaAction = playerInput.actions.FindAction("HabilidadeArma"); // tecla Q / ombro direito

                // Uma habilidade por Artefato equipado — teclas F1 a F4.
                for (int i = 0; i < artefatoActions.Length; i++)
                    artefatoActions[i] = playerInput.actions.FindAction($"HabilidadeArtefato{i + 1}");

                if (moveAction == null)
                    Debug.LogWarning("[PlayerMovement] 'Move' action not found in Input Actions asset.", this);
            }
            else
            {
                Debug.LogWarning("[PlayerMovement] No PlayerInput component found. Input disabled.", this);
            }
        }

        private void OnEnable()
        {
            if (esquivaBridge != null)
            {
                esquivaBridge.OnEsquivaActivada += HandleEsquivaActivated;
            }
        }

        private void OnDisable()
        {
            if (esquivaBridge != null)
            {
                esquivaBridge.OnEsquivaActivada -= HandleEsquivaActivated;
            }
        }

        private void HandleEsquivaActivated(Vector2 direction, float duration, float speedMultiplier)
        {
            // Esquiva é movimento físico comum: colide com paredes normalmente,
            // diferente do Salto (que fica intangível). Nenhuma troca de layer aqui.
            //
            // `direction` JÁ VEM EM ESPAÇO DE MUNDO desde 2026-08-27 (ver ReadInput). Converter
            // aqui de novo aplicaria a base isométrica DUAS VEZES e a esquiva sairia num ângulo
            // que não existe em lugar nenhum do jogo -- 26,6° virariam 53,2°.
            esquivaVelocity = direction.normalized * (stealthState.Speed * speedMultiplier);

            // A Esquiva é um movimento brusco — precisa fazer barulho mesmo em modo
            // Furtivo, senão Furtivo+Esquiva vira um "apagão sonoro" que reseta o
            // temporizador de percepção do Cultista (ver CultistaFSM.TimeSinceLastStimulus).
            if (_soundBroadcaster != null && _environment != null)
            {
                float noise = PlayerStealthState.AplicarAbafamentoTempestade(esquivaNoiseRadius, _environment.StormIntensity);
                _soundBroadcaster.Emitir(new SomEmitido(transform.position, noise));
            }
        }

        /// <summary>
        /// Trava movimento e as ações exclusivas por completo — usado por diálogo
        /// ramificado (<c>PainelDeEscolha</c>) enquanto o jogador navega opções com o
        /// mesmo eixo de movimento, para "cima/baixo" não andar o Damião pela cena.
        /// </summary>
        public bool MovimentoBloqueado { get; set; }

        private void Update()
        {
            if (_odorMaskTimer > 0)
            {
                _odorMaskTimer -= Time.deltaTime;
                if (_odorMaskTimer <= 0)
                {
                    StealthState.IsOdorMasked = false;
                }
            }

            if (_silencioTimer > 0f) _silencioTimer -= Time.deltaTime;

            // Avança o relógio das ações exclusivas (substitui os Invoke(EndX) do modelo antigo).
            _fsm.Tick(Time.deltaTime);

            if (MovimentoBloqueado)
            {
                inputDirection = Vector2.zero;
                isMoving = false;
                return;
            }

            if (!_fsm.EstaLivre) return; // Lock de input enquanto uma ação exclusiva está em curso

            // Read input from New Input System only
            inputDirection = moveAction?.ReadValue<Vector2>() ?? Vector2.zero;
            isMoving = inputDirection.sqrMagnitude > 0.01f;

            // A direção de MUNDO, calculada uma vez e usada por tudo que não é movimento.
            //
            // Até 2026-08-27 só o movimento convertia para o espaço isométrico; LookDirection e
            // as três ações abaixo recebiam o input CRU. O corpo ia para um lado e a mira, o
            // sprite e toda geometria de "costas" apontavam para outro -- 26,6° de desvio na
            // horizontal e 63,4° na vertical. O Vini viu isso jogando como "as 8 direções...
            // tudo parece meio fora".
            //
            // Não era sistema quebrado: eram dois espaços de coordenada que ninguém reconciliou.
            Vector2 direcaoNoMundo =
                BaseIsometrica.DirecaoDeMundo(inputDirection, useIsometricGridAlignment);

            if (isMoving)
            {
                // LookDirection é "para onde o personagem ENCARA no mundo". Todo consumidor dela
                // é geometria de mundo: o bucket de sprite (AnimadorDoDamiao), o cone de costas
                // da Máscara Pálida (ReiEmAmareloAI), o Eco de Carcosa e a Pressão Psíquica.
                LookDirection = direcaoNoMundo;
            }

            // A direção das AÇÕES não é a do input: é para onde o personagem ENCARA.
            //
            // Parado, o input é Vector2.zero, e as três ações abaixo começam com
            // `if (direcao == Vector2.zero) return;` -- guarda correta, porque golpe sem direção
            // não tem para onde apontar a hitbox. O resultado era o golpe morrer na primeira
            // linha, sem um log: "o boneco só ataca andando" (playtest de 2026-08-28).
            //
            // Em movimento isto é idêntico a direcaoNoMundo, então nada muda; parado, vale a
            // última encarada, que é a única resposta que o jogador espera.
            Vector2 direcaoDaAcao = BaseIsometrica.DirecaoDeAcao(
                inputDirection, LookDirection, useIsometricGridAlignment);

            // Trigger Esquiva
            if (dodgeAction != null && dodgeAction.WasPressedThisFrame() && esquivaBridge != null)
            {
                esquivaBridge.TryActivateEsquiva(direcaoDaAcao);
                if (!_fsm.EstaLivre) return; // Esquiva pegou
            }

            // Trigger Ataque (Mão Física)
            if (attackAction != null && attackAction.WasPressedThisFrame() && maoFisicaBridge != null)
            {
                maoFisicaBridge.TryAtacar(direcaoDaAcao);
                if (!_fsm.EstaLivre) return; // Ataque pegou
            }

            // Trigger Habilidade da Arma (botão separado do ataque básico)
            if (habilidadeArmaAction != null && habilidadeArmaAction.WasPressedThisFrame() && maoFisicaBridge != null)
            {
                maoFisicaBridge.TryUsarHabilidade(direcaoDaAcao);
                if (!_fsm.EstaLivre) return; // Habilidade pegou
            }

            // Trigger das habilidades de Artefato (F1–F4, uma por slot equipado).
            // Não travam a FSM: invocar um Artefato não é ação exclusiva como golpear.
            if (artefatosBridge != null)
            {
                for (int i = 0; i < artefatoActions.Length; i++)
                {
                    if (artefatoActions[i] != null && artefatoActions[i].WasPressedThisFrame())
                        artefatosBridge.TryUsarArtefato(i);
                }
            }

            // Determine stealth mode from modifier keys
            bool podeCorrer = _vigor == null || (!_vigor.EstaExausto && _vigor.VigorAtual > 0f);

            if (sneakAction != null && sneakAction.IsPressed())
            {
                stealthState.SetMode(MovementMode.Sneaking);
            }
            else if (runAction != null && runAction.IsPressed() && podeCorrer)
            {
                stealthState.SetMode(MovementMode.Running);
                if (isMoving && _vigor != null)
                {
                    _vigor.ConsumirCorrida(Time.deltaTime);
                }
            }
            else
            {
                stealthState.SetMode(MovementMode.Walking);
            }
        }

        private void FixedUpdate()
        {
            switch (_fsm.CurrentState)
            {
                case PlayerState.Esquivando:
                    rb.linearVelocity = esquivaVelocity;
                    return;
                case PlayerState.Atacando:
                    rb.linearVelocity = Vector2.zero; // ataque trava Damião no lugar
                    return;
                case PlayerState.Congelado:
                    rb.linearVelocity = Vector2.zero; // congelado não anda nem age
                    return;
            }

            if (!isMoving)
            {
                rb.linearVelocity = Vector2.zero;
                return;
            }

            // Mesma conversão que o resto do frame já fez, pela MESMA função. Ter duas
            // implementações da base isométrica no mesmo arquivo era o convite para elas
            // divergirem -- e foi assim que movimento e mira acabaram em espaços diferentes.
            Vector2 movement = useIsometricGridAlignment
                ? BaseIsometrica.ParaMundo(inputDirection)
                : inputDirection.normalized;

            rb.linearVelocity = movement * stealthState.Speed;

            // Broadcast de som a cada 0.15s se estiver movendo
            if (_soundBroadcaster != null && _environment != null)
            {
                _soundTimer += Time.fixedDeltaTime;
                if (_soundTimer >= IntervaloBroadcastSom)
                {
                    _soundTimer = 0f;
                    float currentNoise = stealthState.GetCurrentNoiseEmission(
                        isMoving, _environment.StormIntensity, FurtividadeEquipada);
                    if (currentNoise > 0f && !PassosSilenciados)
                    {
                        _soundBroadcaster.Emitir(new SomEmitido(transform.position, currentNoise));
                    }
                }
            }
        }

        /// <summary>
        /// Converts screen-space WASD input to isometric world-space direction.
        /// </summary>
        // ConvertToIsometric saiu daqui em 2026-08-27. Virou Core.Player.BaseIsometrica,
        // que é POCO testável e recebe a altura da célula do Grid como PARÂMETRO -- porque a
        // doc da Unity 6.4 diz que esse número É o cellSize.y do Grid, e um literal aqui seria
        // mais uma constante para divergir do mundo desenhado.

        /// <summary>
        /// Bônus agregado de <c>StatType.Furtividade</c> do equipamento. Mesmo padrão de
        /// <c>MaoFisicaBridge.BonusPassivo</c>: zero quando o gerenciador ainda não existe.
        /// </summary>
        /// <summary>
        /// Raio de ruído que o Damião emite <b>neste instante</b>, já com tempestade e
        /// Furtividade aplicadas. Zero quer dizer parado.
        ///
        /// <para>Existe para o console de diagnóstico: quando um inimigo não persegue, a
        /// primeira pergunta é se há som para ouvir — e adivinhar isso custou uma sessão
        /// inteira de análise estática.</para>
        /// </summary>
        public float RuidoAtual => stealthState == null ? 0f
            : stealthState.GetCurrentNoiseEmission(
                isMoving,
                _environment != null ? _environment.StormIntensity : 0f,
                FurtividadeEquipada);

        /// <summary>Se o serviço de som e o ambiente foram injetados. Falso = ninguém ouve nada.</summary>
        public bool EmissaoDeSomLigada => _soundBroadcaster != null && _environment != null;

        private static float FurtividadeEquipada =>
            GerenciadorEfeitosPassivos.Instance
                ?.GetBonus(FavelaAmarela.Inventario.StatType.Furtividade) ?? 0f;

        private void OnDrawGizmos()
        {
            if (!showNoiseGizmo || stealthState == null) return;

            float stormIntensity = _environment != null ? _environment.StormIntensity : debugStormIntensity;
            float currentNoise = stealthState.GetCurrentNoiseEmission(
                isMoving, stormIntensity, FurtividadeEquipada);
            if (currentNoise <= 0f) return;

            // Filled circle (projected sphere in 2D ortho looks like a disk)
            Gizmos.color = new Color(1f, 0.92f, 0.016f, 0.15f);
            Gizmos.DrawSphere(transform.position, currentNoise);

            // Outline
            Gizmos.color = new Color(1f, 0.92f, 0.016f, 0.7f);
            Gizmos.DrawWireSphere(transform.position, currentNoise);
        }
        public void MascararOdor(float duracaoSegundos)
        {
            if (StealthState != null)
            {
                StealthState.IsOdorMasked = true;
                _odorMaskTimer = duracaoSegundos;
            }
        }

        /// <summary>
        /// Se os passos de Damião estão calados neste instante — o Resguardo do Sinal.
        /// </summary>
        public bool PassosSilenciados => _silencioTimer > 0f;

        /// <summary>
        /// Cala os passos por um tempo: Damião continua andando, mas deixa de emitir ruído.
        /// Vale só para o broadcast contínuo do caminhar — a Esquiva segue fazendo barulho de
        /// propósito, senão Resguardo + Esquiva viraria um apagão sonoro completo.
        /// </summary>
        /// <param name="duracaoSegundos">Renova o silêncio se já houver um em curso mais curto.</param>
        public void SilenciarPassos(float duracaoSegundos)
        {
            if (duracaoSegundos <= 0f) return;
            _silencioTimer = Mathf.Max(_silencioTimer, duracaoSegundos);
        }
    }
}
