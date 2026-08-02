using UnityEngine;
using FavelaAmarela.Core.Abilities;
using FavelaAmarela.Core.Combat;
using FavelaAmarela.Core.Enemies;
using FavelaAmarela.Core.Persistencia;
using FavelaAmarela.Core.Stealth;
using FavelaAmarela.Runtime.Combat;
using FavelaAmarela.Runtime.Persistencia;

namespace FavelaAmarela.Runtime.Enemies
{
    [RequireComponent(typeof(SpriteRenderer), typeof(Rigidbody2D))]
    public class CultistaAI : MonoBehaviour, IDanificavel
    {
        private CultistaFSM _fsm;
        private Vitalidade _vitalidade;
        private readonly Sangramento _sangramento = new Sangramento();

        // Dano de sangramento já sofrido mas ainda não exibido — ver AcumularNumeroDeSangramento.
        private float _danoDeSangramentoPendente;

        // Opcional: se presente, o abate deste Cultista é lembrado entre trocas de cena.
        private ObjetoPersistente _persistencia;
        private SpriteRenderer _spriteRenderer;
        private Rigidbody2D _rb;
        private PatrolRoute _patrolRoute;
        private SoundBroadcastService _soundBroadcaster;

        [Header("Patrulha")]
        [SerializeField] private Transform[] waypoints;

        [Header("Ficha de Atributos")]
        [Tooltip("Ficha do Cultista (Vitalidade, Ataque, Defesa...). Atribua Ficha_Cultista.")]
        [SerializeField] private FichaAtributosConfig ficha;

        [Header("Corpo-a-corpo")]
        [Tooltip("Distância em que o Cultista passa de Caça para Atacar e desfere golpes.")]
        [SerializeField] private float alcanceDeGolpe = 1.0f;

        [Tooltip("Segundos entre golpes no corpo-a-corpo.")]
        [SerializeField] private float cadenciaDeAtaque = 1.2f;

        [Tooltip("Camada do Damião, usada para detectar o alvo ao alcance de golpe.")]
        [SerializeField] private LayerMask camadaDoJogador;

        [Header("Feedback")]
        [Tooltip("Exibe números de dano flutuantes quando este Cultista é ferido.")]
        [SerializeField] private bool mostrarNumerosDeDano = true;

        [Tooltip("Cor dos números de dano sofridos pelo Cultista.")]
        [SerializeField] private Color corDoDano = new Color(1f, 0.95f, 0.5f);

        [Tooltip("Cor dos números do sangramento (Ferida de Aklo) — distinta do golpe direto.")]
        [SerializeField] private Color corDoSangramento = new Color(0.85f, 0.15f, 0.2f);

        [Header("Configurações")]
        [SerializeField] private float velocidadeErrante = 1.0f;
        [SerializeField] private float velocidadeCaca = 3.5f;
        [SerializeField] private Color corErrante = Color.white;
        [SerializeField] private Color corAlerta = Color.yellow;
        [SerializeField] private Color corCaca = Color.red;
        [SerializeField] private Color corAtacar = new Color(0.75f, 0f, 0.2f);

        private FichaDeAtributos _atributos;

        // Buffer pré-alocado + filtro para a detecção de proximidade sem alocar lixo
        // por frame (Regra de Ouro 1). 4 slots bastam: só interessa o Damião.
        private readonly Collider2D[] _bufferAlvo = new Collider2D[4];
        private ContactFilter2D _filtroJogador;
        private VitalidadeBridge _vitalidadeDoAlvo;

        private void Awake()
        {
            _persistencia = GetComponent<ObjetoPersistente>();
            _spriteRenderer = GetComponent<SpriteRenderer>();
            _rb = GetComponent<Rigidbody2D>();
            _rb.gravityScale = 0f;
            _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            _fsm = new CultistaFSM(CultistaState.Errante, cadenciaDeAtaque);
            _fsm.OnStateChanged += HandleStateChanged;
            _fsm.OnGolpeDesferido += HandleGolpeDesferido;

            if (ficha == null)
            {
                Debug.LogError($"[CultistaAI] Ficha de atributos não atribuída em '{name}'. " +
                               "Usando ficha de emergência (Vitalidade 100, Ataque 24, Defesa 5).", this);
                _atributos = new FichaDeAtributos(vitalidadeMax: 100f, ataque: 24f, defesa: 5f);
            }
            else
            {
                _atributos = ficha.CriarFicha();
            }

            _vitalidade = new Vitalidade(_atributos.VitalidadeMax);
            _vitalidade.OnChanged += HandleVitalidadeChanged;

            // Fallback seguro: se a camada do jogador ficou vazia no Inspector, usa "Player".
            if (camadaDoJogador.value == 0)
                camadaDoJogador = LayerMask.GetMask("Player");

            _filtroJogador = new ContactFilter2D();
            _filtroJogador.useTriggers = true;
            _filtroJogador.SetLayerMask(camadaDoJogador);

            if (waypoints != null && waypoints.Length > 0)
            {
                var poses = new Vector2[waypoints.Length];
                for (int i = 0; i < waypoints.Length; i++)
                {
                    if (waypoints[i] == null)
                    {
                        Debug.LogError($"[CultistaAI] Waypoint {i} não está atribuído no Inspector. Usando posição atual como fallback.", this);
                        poses[i] = transform.position;
                        continue;
                    }
                    poses[i] = waypoints[i].position;
                }

                _patrolRoute = new PatrolRoute(poses, loop: true);
            }
            else
            {
                _patrolRoute = new PatrolRoute(new[] { (Vector2)transform.position });
            }

            AtualizarVisual(_fsm.CurrentState);
        }

        private void FixedUpdate()
        {
            // Proximidade primeiro: a FSM decide Caça→Atacar com dado do frame atual.
            DetectarAlvoAoAlcance();

            EscoarSangramento(Time.fixedDeltaTime);

            _fsm.Tick(Time.fixedDeltaTime);

            switch (_fsm.CurrentState)
            {
                case CultistaState.Errante:
                    if (_patrolRoute != null)
                    {
                        MoverEmDirecaoA(_patrolRoute.AlvoAtual, velocidadeErrante);
                        _patrolRoute.AtualizarChegada(transform.position, 0.3f);
                    }
                    else
                    {
                        _rb.linearVelocity = Vector2.zero;
                    }
                    break;
                case CultistaState.Alerta:
                    // Parado (Pausa telegrafada antes de caçar)
                    // Poderia apenas rotacionar em direção a _fsm.UltimaOrigemConhecida se desejado
                    _rb.linearVelocity = Vector2.zero;
                    break;
                case CultistaState.Caca:
                    if (_fsm.UltimaOrigemConhecida.HasValue)
                    {
                        MoverEmDirecaoA(_fsm.UltimaOrigemConhecida.Value, velocidadeCaca);
                    }
                    else
                    {
                        _rb.linearVelocity = Vector2.zero;
                    }
                    break;
                case CultistaState.Atacar:
                    // Planta os pés para golpear: o dano sai da cadência da FSM
                    // (OnGolpeDesferido), não do movimento.
                    _rb.linearVelocity = Vector2.zero;
                    break;
                case CultistaState.Atordoado:
                    _rb.linearVelocity = Vector2.zero;
                    break;
            }
        }

        /// <summary>
        /// Detecta por proximidade física (não por visão) se o Damião está ao alcance de
        /// golpe e informa a FSM. Cacheia a <see cref="VitalidadeBridge"/> do alvo para
        /// não chamar <c>GetComponent</c> a cada golpe (Regra de Ouro 1).
        /// </summary>
        private void DetectarAlvoAoAlcance()
        {
            int total = Physics2D.OverlapCircle(
                transform.position, alcanceDeGolpe, _filtroJogador, _bufferAlvo);

            if (total <= 0)
            {
                _fsm.AtualizarAlcanceDoAlvo(false);
                return;
            }

            // Resolve o alvo só quando ainda não temos um cacheado (ou ele foi destruído).
            if (_vitalidadeDoAlvo == null)
            {
                for (int i = 0; i < total; i++)
                {
                    var bridge = _bufferAlvo[i].GetComponentInParent<VitalidadeBridge>();
                    if (bridge != null)
                    {
                        _vitalidadeDoAlvo = bridge;
                        break;
                    }
                }
            }

            _fsm.AtualizarAlcanceDoAlvo(true);
        }

        /// <summary>
        /// Traduz um golpe da FSM em dano no Damião: o <c>Ataque</c> da ficha do Cultista
        /// entra como dano bruto e a <see cref="VitalidadeBridge"/> do alvo aplica a
        /// mitigação pela Defesa dele.
        /// </summary>
        private void HandleGolpeDesferido()
        {
            if (_vitalidadeDoAlvo == null) return;
            _vitalidadeDoAlvo.ReceberDanoFisico(_atributos.Ataque);
        }

        private void MoverEmDirecaoA(Vector2 alvo, float velocidade)
        {
            Vector2 direcao = (alvo - (Vector2)transform.position).normalized;
            _rb.linearVelocity = direcao * velocidade;
        }

        /// <summary>
        /// Injeta o serviço de som (chamado por <c>GameManager.InjetarDependencias()</c>
        /// no bootstrap, antes do Awake/OnEnable deste componente — mesma garantia de
        /// ordem que já existe para <c>PlayerMovement.Bind()</c>). Substitui a busca por
        /// <c>GameManager.Instance</c> a cada (des)inscrição.
        /// </summary>
        public void Bind(SoundBroadcastService soundBroadcaster) => _soundBroadcaster = soundBroadcaster;

        private void OnEnable()
        {
            if (_soundBroadcaster != null)
            {
                _soundBroadcaster.OnSomEmitido += HandleSomEmitido;
            }
        }

        private void OnDisable()
        {
            if (_soundBroadcaster != null)
            {
                _soundBroadcaster.OnSomEmitido -= HandleSomEmitido;
            }
        }

        private void HandleSomEmitido(SomEmitido som)
        {
            float distancia = Vector2.Distance(transform.position, som.Origem);
            _fsm.ReceberEstimuloSonoro(som.Origem, distancia, som.RaioEfetivo);
        }

        /// <summary>Cultista Amarelo não é boss — leva crítico de furtividade normalmente.</summary>
        public bool EhAparicaoPrimordial => false;

        /// <summary>
        /// Recebe o resultado de um golpe de arma física (via <c>MaoFisicaBridge</c>).
        /// Consome o dano bruto (<see cref="ArmaResult.Dano"/>) da <see cref="Vitalidade"/>
        /// corpórea — ao ser abatido, o Cultista sai de cena — e reage ao atordoamento
        /// do golpe (interrompe a FSM). Um golpe desarmado chega aqui com <c>Dano</c> = 0,
        /// então empurra/atordoa mas não fere: manter é seguro.
        /// </summary>
        public void ReceberGolpe(ArmaResult resultado)
        {
            if (resultado.Dano > 0f)
            {
                // A Defesa da ficha do Cultista mitiga o golpe (mesma fórmula usada
                // contra o Damião — MitigacaoDeDano é simétrica).
                float danoFinal = MitigacaoDeDano.Aplicar(resultado.Dano, _atributos.Defesa);

                if (danoFinal > 0f)
                {
                    _vitalidade.Ferir(danoFinal);

                    if (mostrarNumerosDeDano)
                        DanoFlutuante.Mostrar(transform.position, danoFinal, corDoDano);
                }

                // Não segue processando o golpe se este ferimento já abateu o Cultista.
                if (_vitalidade.EstaAbatido) return;
            }

            // Ferida de Aklo (Estilete de Irem): acumula; ao chegar ao teto, estoura.
            if (resultado.AcumulosDeSangramento > 0)
                _sangramento.Aplicar(resultado.AcumulosDeSangramento,
                    resultado.SangramentoPorSegundo, resultado.DuracaoSangramento);

            if (resultado.Atordoou)
            {
                _fsm.AtordoarPor(resultado.DuracaoAtordoamento);
            }
        }

        /// <summary>
        /// Escoa o sangramento ativo: a ferida cobra por tempo, não por golpe. A defesa
        /// <b>não</b> mitiga o escoamento — ela já foi aplicada no golpe que abriu a ferida.
        /// </summary>
        private void EscoarSangramento(float dt)
        {
            if (!_sangramento.Ativo || _vitalidade.EstaAbatido) return;

            var tick = _sangramento.Tick(dt);

            if (tick.DanoContinuo > 0f)
            {
                _vitalidade.Ferir(tick.DanoContinuo);
                AcumularNumeroDeSangramento(tick.DanoContinuo);
            }

            if (!tick.Explodiu) return;

            // Estouro: contra inimigo comum é dano fixo (percentual seria irrelevante
            // numa vitalidade pequena). Ver ExplosaoDeSangramento.
            float danoDoEstouro = ExplosaoDeSangramento.Calcular(
                _atributos.VitalidadeMax, ehAparicaoPrimordial: false);

            if (danoDoEstouro <= 0f) return;

            _vitalidade.Ferir(danoDoEstouro);
            if (mostrarNumerosDeDano)
                DanoFlutuante.Mostrar(transform.position, danoDoEstouro, corDoSangramento);
        }

        /// <summary>
        /// Junta o dano de sangramento e só mostra um número quando ele vira algo legível.
        ///
        /// <para><b>Por que não mostrar por tick:</b> o escoamento entrega frações minúsculas
        /// (1 acúmulo × 4/s × 0,02 s = 0,08). O <c>DanoFlutuante</c> arredonda para inteiro,
        /// então cada tick exibia <b>"0"</b> — o jogador via o sangramento como "não causa
        /// dano" mesmo com a Vitalidade caindo de verdade. Além disso, um número por
        /// <c>FixedUpdate</c> instanciava ~50 GameObjects por segundo por inimigo sangrando,
        /// alocação em hot path proibida pela Regra de Ouro 1.</para>
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
        /// Observa a vitalidade corpórea. No frame em que o Cultista é abatido, remove-o
        /// de cena. Ponto único onde a morte física vira efeito no mundo — futuramente
        /// dispara animação de queda / drop antes do <c>Destroy</c>.
        /// </summary>
        private void HandleVitalidadeChanged(VitalidadeChangedArgs args)
        {
            if (args.AcabouDeAbater)
            {
                Abater();
            }
        }

        /// <summary>Tira o Cultista de cena ao ser abatido (morte física).</summary>
        private void Abater()
        {
            _rb.linearVelocity = Vector2.zero;

            // Registra o abate para ele não ressuscitar ao recarregar a cena. Sem isto,
            // sair da dungeon e voltar repovoava tudo o que o jogador já tinha limpado.
            var chave = ChavesDeSave.ChaveDeAbatido(_persistencia != null ? _persistencia.Chave : null);
            if (chave != null) GerenciadorDeSave.MarcarAconteceu(chave);

            Destroy(gameObject);
        }

        /// <summary>
        /// Some antes do primeiro frame se este Cultista já tinha sido abatido nesta partida.
        ///
        /// <para>Roda no <c>Start</c>, não no <c>Awake</c>: o <c>GerenciadorDeSave</c> pode
        /// ainda não ter acordado. Sem <c>ObjetoPersistente</c> o Cultista simplesmente
        /// respawna — degradação graciosa, não erro: um inimigo sem chave é conteúdo novo
        /// que nunca foi salvo.</para>
        /// </summary>
        private void Start()
        {
            if (_persistencia == null) return;

            var chave = ChavesDeSave.ChaveDeAbatido(_persistencia.Chave);
            if (chave == null) return;

            if (GerenciadorDeSave.JaAconteceu(chave)) Destroy(gameObject);
        }

        private void HandleStateChanged(CultistaState anterior, CultistaState atual)
        {
            AtualizarVisual(atual);
        }

        private void AtualizarVisual(CultistaState estado)
        {
            switch (estado)
            {
                case CultistaState.Errante:
                    _spriteRenderer.color = corErrante;
                    break;
                case CultistaState.Alerta:
                    _spriteRenderer.color = corAlerta;
                    break;
                case CultistaState.Caca:
                    _spriteRenderer.color = corCaca;
                    break;
                case CultistaState.Atacar:
                    _spriteRenderer.color = corAtacar;
                    break;
            }
        }

        private void OnDestroy()
        {
            if (_fsm != null)
            {
                _fsm.OnStateChanged -= HandleStateChanged;
                _fsm.OnGolpeDesferido -= HandleGolpeDesferido;
            }

            if (_vitalidade != null)
            {
                _vitalidade.OnChanged -= HandleVitalidadeChanged;
            }
        }

        private void OnDrawGizmos()
        {
            if (_fsm == null) return;

            Gizmos.color = _spriteRenderer != null ? _spriteRenderer.color : Color.white;
            Gizmos.DrawWireSphere(transform.position, 1.5f);

            // Desenha raio de vibração
            Gizmos.color = new Color(0.5f, 0, 0.5f, 0.2f);
            Gizmos.DrawWireSphere(transform.position, 3f);

            // Alcance de golpe corpo-a-corpo (onde Caça vira Atacar)
            Gizmos.color = new Color(0.9f, 0.1f, 0.2f, 0.5f);
            Gizmos.DrawWireSphere(transform.position, alcanceDeGolpe);
        }
    }
}
