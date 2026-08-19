using UnityEngine;
using FavelaAmarela.Core.Abilities;
using FavelaAmarela.Core.Combat;
using FavelaAmarela.Core.Enemies;
using FavelaAmarela.Runtime.GameLoop;

namespace FavelaAmarela.Runtime.Enemies
{
    /// <summary>
    /// Camada Runtime (MonoBehaviour). Adaptador do <see cref="ByakheeFSM"/> — o chefe dos
    /// Portões das Ruínas, que fecha a Fase 1.
    ///
    /// <para>Toda a regra vive no POCO; aqui só se lê o estado dele para mover o corpo,
    /// pintar o sprite e aplicar dano/dreno. É o mesmo par <c>CultistaFSM</c>+<c>CultistaAI</c>
    /// do resto do projeto.</para>
    ///
    /// <para><b>Imunidade em voo:</b> a `EnemyBase` recebe o golpe, mas este componente liga
    /// <c>IgnorarDano</c> conforme a FSM. Sem isso o jogador acertaria o Byakhee no ar e a
    /// leitura da luta — esperar o pouso — deixaria de existir.</para>
    /// </summary>
    [RequireComponent(typeof(EnemyBase), typeof(SpriteRenderer), typeof(Rigidbody2D))]
    [AddComponentMenu("Favela Amarela/Enemies/Byakhee AI")]
    public sealed class ByakheeAI : MonoBehaviour
    {
        [Header("Arena")]
        [Tooltip("Centro da arena em frente aos Portões. Vazio = a posição inicial dele.")]
        [SerializeField] private Transform centroDaArena;

        [Tooltip("Raio que ele percorre ao circundar, em unidades.")]
        [SerializeField] private float raioDeVoo = 3f;

        [Header("Movimento")]
        [SerializeField] private float velocidadeRasante = 6f;
        [SerializeField] private float velocidadeMergulho = 9f;
        [SerializeField] private float velocidadeCircundando = 4f;

        [Header("Combate")]
        [Tooltip("Dano das garras durante o pouso agressivo.")]
        [SerializeField] private float danoDasGarras = 26f;

        [Tooltip("Trauma do cone de pressão sonora (fase 2+).")]
        [SerializeField] private float traumaDoGrito = 20f;

        [Tooltip("Alcance do cone, em unidades.")]
        [SerializeField] private float alcanceDoGrito = 4f;

        [Tooltip("Alcance das garras no pouso. Fora disso, o golpe não acerta.")]
        [SerializeField] private float alcanceDasGarras = 1.5f;

        [Header("Cores de leitura (provisórias, até haver arte)")]
        [SerializeField] private Color corNoAr = new Color(0.35f, 0.30f, 0.45f);
        [SerializeField] private Color corPousado = new Color(0.85f, 0.75f, 0.25f);
        [SerializeField] private Color corFrenesi = new Color(0.85f, 0.20f, 0.15f);

        [Header("Animação")]
        [Tooltip("Animator com o Byakhee_AC. Se ficar vazio, o boss desenha o quadro parado — " +
                 "degrada para 'mais simples', nunca para 'invisível'.")]
        [SerializeField] private Animator animator;

        private ByakheeFSM _fsm;
        private EnemyBase _enemyBase;
        private SpriteRenderer _sprite;
        private Rigidbody2D _rb;
        private Transform _jogador;
        private FavelaAmarela.Runtime.Combat.ResilienciaBridge _mente;

        private Vector3 _centro;
        private Vector2 _direcaoDoRasante;
        private float _anguloCircundando;

        /// <summary>A FSM da luta, para HUD e cutscenes observarem.</summary>
        public ByakheeFSM Fsm => _fsm;

        private void Awake()
        {
            _fsm = new ByakheeFSM();

            _enemyBase = GetComponent<EnemyBase>();
            _sprite = GetComponent<SpriteRenderer>();
            _rb = GetComponent<Rigidbody2D>();

            if (animator == null) animator = GetComponent<Animator>();

            _rb.gravityScale = 0f;
            _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            _centro = centroDaArena != null ? centroDaArena.position : transform.position;

            _fsm.OnStateChanged += HandleEstadoMudou;
            _fsm.OnGritoEmitido += EmitirCone;
            _fsm.OnDerrotado += HandleDerrotado;

            // Começa intocável: em Espreita ele ainda está no arco.
            _enemyBase.IgnorarDano = true;
        }

        private void Start()
        {
            var jogador = GameObject.FindGameObjectWithTag("Player");
            if (jogador == null)
            {
                Debug.LogError("[Byakhee] Nenhum objeto com a tag Player — ele não terá alvo.", this);
                return;
            }

            _jogador = jogador.transform;

            // A mente de Damião, resolvida uma vez: o grito infrassônico drena todo frame e não
            // pode resolver dependência nem falhar calado.
            _mente = jogador.GetComponentInChildren<FavelaAmarela.Runtime.Combat.ResilienciaBridge>();
            if (_mente == null)
                Debug.LogError("[Byakhee] Damião sem ResilienciaBridge — o grito infrassônico, " +
                               "que é o relógio da luta, não vai drenar nada.", this);
            _enemyBase.OnAbatido += HandleAbatido;
            _enemyBase.OnDanoSofrido += HandleDanoSofrido;
        }

        private void OnDestroy()
        {
            if (_fsm != null)
            {
                _fsm.OnStateChanged -= HandleEstadoMudou;
                _fsm.OnGritoEmitido -= EmitirCone;
                _fsm.OnDerrotado -= HandleDerrotado;
            }

            if (_enemyBase != null)
            {
                _enemyBase.OnAbatido -= HandleAbatido;
                _enemyBase.OnDanoSofrido -= HandleDanoSofrido;
            }
        }

        /// <summary>Desce dos Portões e começa a luta. Chamado pelo gatilho da arena.</summary>
        public void IniciarLuta() => _fsm.IniciarLuta();

        /// <summary>
        /// Corta a asa num rasante (exige a Lâmina do Sinal, que ainda não existe no jogo).
        /// Exposto para quando a arma entrar — hoje o pouso da fase 3 vem do intervalo
        /// espontâneo da FSM.
        /// </summary>
        public bool TentarCortarAsa() => _fsm.CortarAsa();

        private void Update()
        {
            if (_fsm.CurrentState == ByakheeState.Derrotado) return;

            _fsm.Tick(Time.deltaTime);

            SincronizarVulnerabilidade();
            AplicarGritoInfrassonico();
        }

        private void FixedUpdate()
        {
            if (_jogador == null) return;

            switch (_fsm.CurrentState)
            {
                case ByakheeState.Rasante:
                    _rb.linearVelocity = _direcaoDoRasante * velocidadeRasante;
                    break;

                case ByakheeState.MergulhoDeGarras:
                    var paraJogador = ((Vector2)(_jogador.position - transform.position)).normalized;
                    _rb.linearVelocity = paraJogador * velocidadeMergulho;
                    break;

                case ByakheeState.Circundando:
                    Circundar();
                    break;

                default:
                    // Pousado, grito e frenesi acontecem parado: é o que dá ao jogador um
                    // alvo estável justamente quando ele pode acertar.
                    _rb.linearVelocity = Vector2.zero;
                    break;
            }
        }

        /// <summary>
        /// Liga e desliga a imunidade conforme a FSM. É aqui que a regra "imune no ar" vira
        /// comportamento — a `EnemyBase` sozinha aceitaria qualquer golpe.
        /// </summary>
        private void SincronizarVulnerabilidade()
        {
            _enemyBase.IgnorarDano = !_fsm.PodeReceberDano;

            if (_enemyBase.Vitalidade != null)
                _fsm.AtualizarFracaoDeVida(_enemyBase.Vitalidade.Percentual);
        }

        /// <summary>
        /// O grito passivo drena Resiliência sem precisar acertar ninguém — é o relógio da
        /// luta. Quem demora colapsa mesmo intocado.
        /// </summary>
        private void AplicarGritoInfrassonico()
        {
            float dreno = _fsm.DrenoDeResilienciaPorSegundo;
            if (dreno <= 0f) return;

            _mente?.SofrerTrauma(dreno * Time.deltaTime);
        }

        private void Circundar()
        {
            _anguloCircundando += velocidadeCircundando * Time.fixedDeltaTime;

            var alvo = _centro + new Vector3(
                Mathf.Cos(_anguloCircundando) * raioDeVoo,
                Mathf.Sin(_anguloCircundando) * raioDeVoo * 0.6f,   // elipse: o isométrico achata o eixo Y
                0f);

            _rb.linearVelocity = ((Vector2)(alvo - transform.position)) * velocidadeCircundando;
        }

        private void HandleEstadoMudou(ByakheeState anterior, ByakheeState atual)
        {
            switch (atual)
            {
                case ByakheeState.Rasante:
                    // Atravessa a arena pelo eixo do jogador, para o rasante ser evitável
                    // andando de lado — a defesa que o design pede.
                    _direcaoDoRasante = _jogador != null
                        ? ((Vector2)(_jogador.position - transform.position)).normalized
                        : Vector2.right;
                    break;

                case ByakheeState.Pousado:
                    GolpearComGarras();
                    break;
            }

            _sprite.color = atual switch
            {
                ByakheeState.Pousado => corPousado,
                ByakheeState.Frenesi => corFrenesi,
                _ => corNoAr
            };

            TocarAnimacaoDo(atual);
        }

        /// <summary>
        /// Põe o Animator no clipe do estado. <b>Quem manda é a FSM</b> — por isso o
        /// <c>Byakhee_AC</c> não tem teia de transições com condições: duplicar a lógica de
        /// estado lá criaria uma segunda fonte de verdade, que divergiria desta em silêncio.
        ///
        /// <para><c>Circundando</c> e <c>Frenesi</c> reaproveitam <c>rasante</c>: nos dois ele
        /// está no ar em deslocamento, e a folha não tem quadros próprios para eles. A leitura
        /// de qual é qual continua vindo da cor (<c>corFrenesi</c>), como antes.</para>
        /// </summary>
        private void TocarAnimacaoDo(ByakheeState estado)
        {
            if (animator == null || animator.runtimeAnimatorController == null) return;

            int clipe = estado switch
            {
                ByakheeState.Rasante => Anim.Rasante,
                ByakheeState.MergulhoDeGarras => Anim.Garras,
                ByakheeState.GritoDirecionado => Anim.Grito,
                ByakheeState.Derrotado => Anim.Derrota,
                ByakheeState.Circundando => Anim.Rasante,
                ByakheeState.Frenesi => Anim.Rasante,
                _ => Anim.Espreita,
            };

            animator.Play(clipe, 0, 0f);
        }

        /// <summary>
        /// Piscada de dano. Só interrompe se ele ainda estiver vivo: depois de
        /// <see cref="ByakheeState.Derrotado"/> o corpo fica no último quadro da queda, e um
        /// golpe no cadáver não pode ressuscitar a animação.
        /// </summary>
        private void HandleDanoSofrido(float _)
        {
            if (animator == null || animator.runtimeAnimatorController == null) return;
            if (_fsm != null && _fsm.CurrentState == ByakheeState.Derrotado) return;

            animator.Play(Anim.Dano, 0, 0f);
        }

        /// <summary>
        /// Hashes dos estados do <c>Byakhee_AC</c>. <c>Animator.StringToHash</c> resolvido uma
        /// vez em campo estático: <see cref="TocarAnimacaoDo"/> é chamado a cada troca de estado
        /// da luta, e a Regra de Ouro 1 proíbe alocar string em caminho quente.
        /// </summary>
        private static class Anim
        {
            internal static readonly int Espreita = Animator.StringToHash("espreita");
            internal static readonly int Rasante = Animator.StringToHash("rasante");
            internal static readonly int Garras = Animator.StringToHash("garras");
            internal static readonly int Grito = Animator.StringToHash("grito");
            internal static readonly int Dano = Animator.StringToHash("dano");
            internal static readonly int Derrota = Animator.StringToHash("derrota");
        }

        /// <summary>
        /// Golpe de garras no instante do pouso.
        ///
        /// <para><b>Bug corrigido em 2026-08-11:</b> esta função feria o jogador
        /// <b>incondicionalmente</b> a cada pouso, mesmo do outro lado da arena — 26 de dano
        /// bruto de graça, sem chance de reagir. Com 5–7 pousos numa luta, isso sozinho podia
        /// matar o corpo de Damião mesmo com Resiliência de sobra.</para>
        ///
        /// <para>Com o alcance, quem está perto o bastante para revidar corpo-a-corpo é quem
        /// está perto o bastante para levar o golpe: a troca de risco que a "janela de dano"
        /// do design sempre pediu, e não um imposto fixo por pouso.</para>
        /// </summary>
        private void GolpearComGarras()
        {
            if (_jogador == null) return;

            float distancia = Vector2.Distance(transform.position, _jogador.position);
            if (distancia > alcanceDasGarras) return;

            var alvo = _jogador.GetComponent<IDanificavel>();
            alvo?.ReceberGolpe(new ArmaResult(true, 0f, 0f, false, 0f, danoDasGarras));
        }

        /// <summary>Cone de pressão sonora: fere a mente, não o corpo.</summary>
        private void EmitirCone()
        {
            if (_jogador == null) return;

            float distancia = Vector2.Distance(transform.position, _jogador.position);
            if (distancia > alcanceDoGrito) return;

            _mente?.SofrerTrauma(traumaDoGrito);
        }

        private void HandleAbatido()
        {
            // A FSM decide o que "derrotado" significa; o EnemyBase só avisa que a vida acabou.
            _fsm.AtualizarFracaoDeVida(0f);
        }

        private void HandleDerrotado()
        {
            _rb.linearVelocity = Vector2.zero;

            // O espólio sai pelo DropAoAbater, que já escuta o EnemyBase — aqui só paramos o
            // corpo. Os Portões abrindo são responsabilidade do gatilho da arena.
            enabled = false;
        }
    }
}
