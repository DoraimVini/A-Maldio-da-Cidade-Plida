using UnityEngine;
using UnityEngine.Tilemaps;
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

        /// <summary>
        /// O chão da arena: <b>onde há tile pintado, ele pode voar</b>. Vazio: o maior Tilemap
        /// da cena, resolvido uma vez ao começar.
        ///
        /// <para><b>A coleira é a SALA, não uma forma inventada (2026-09-10).</b> A versão
        /// anterior prendia o Byakhee numa elipse de 9 × 5 dimensionada para caber na
        /// <i>câmera</i>. Mas a câmera segue o jogador, e o jogador não tem coleira nenhuma: a
        /// sala dos Portões é um losango de <b>63 × 31</b>. Bastava Damião pisar fora da elipse
        /// — cinco unidades ao norte do centro — e o chefe ficava pregado na borda, mirando nele
        /// e sendo empurrado de volta a cada quadro. É o que o Vini relatou: <i>"presa a uma
        /// faixa da arena e não andando livremente"</i>.</para>
        ///
        /// <para>Usar o mesmo Tilemap que barra o jogador fecha isso por construção: onde ele
        /// pode pisar, o Byakhee pode voar. Não há região do jogador que a coleira não alcance.
        /// O que a elipse resolvia de verdade — rasantes encadeados saindo do mapa — o chão
        /// resolve igual: fora do chão, ele volta.</para>
        /// </summary>
        [SerializeField] private Tilemap chaoDaArena;

        /// <summary>
        /// A linha dos Portões, que o Byakhee não passa: <b>eles são o fundo da luta</b>, e fundo
        /// só funciona se a luta acontecer na frente dele. Vazio: sem teto. [CENA]
        /// </summary>
        [Tooltip("Transform do colisor dos Portões. Acima do y dele, o Byakhee volta. [CENA]")]
        [SerializeField] private Transform muralhaNorte;

        [Tooltip("Velocidade com que ele se arrasta na direção do jogador enquanto está " +
                 "POUSADO e longe demais para ser alcançado.")]
        /// <summary>
        /// Trauma que o pouso do Byakhee gera. 0,5 sacode com peso sem saturar: um golpe cheio
        /// do jogador gera 0,45, e a criatura chegando no chão deve ler como mais que isso.
        /// </summary>
        private const float TraumaDoPouso = 0.5f;

        [SerializeField] private float velocidadeNoChao = 2.6f;

        [Header("Movimento")]
        [SerializeField] private float velocidadeRasante = 6f;
        [SerializeField] private float velocidadeMergulho = 9f;
        [SerializeField] private float velocidadeCircundando = 4f;

        [Tooltip("Quanto o rasante segue ALÉM do jogador antes de encerrar (un). Era um relógio " +
                 "fixo de 12 un: com o jogador perto, ela o atravessava e ia até 12 além, para " +
                 "voltar se arrastando — 11 meias-voltas em 10 s no playtest de 2026-09-10. " +
                 "Ainda atravessa (o rasante tem de ser esquivável de lado), mas para logo depois.")]
        [Min(0.5f)]
        [SerializeField] private float alcanceAlemDoJogador = 3f;

        /// <summary>Buffer fixo para <c>Rigidbody2D.GetContacts</c> — nada alocado no FixedUpdate.</summary>
        private readonly ContactPoint2D[] _contatos = new ContactPoint2D[8];

        [Header("Combate")]
        [Tooltip("Dano das garras durante o pouso agressivo. FALLBACK: a fonte da verdade e o " +
                 "Ataque da ficha (ver DanoDasGarras). So e usado se a ficha nao autorar Ataque.")]
        [SerializeField] private float danoDasGarras = 26f;

        /// <summary>
        /// O dano das garras: o <b>Ataque da ficha</b> quando existe, e so entao o campo local.
        ///
        /// <para><b>O defeito que isto fecha (2026-08-28).</b> A <c>Ficha_Byakhee</c> autora
        /// <c>Ataque 26</c> e este campo tambem dizia <c>26</c> — <b>dois numeros independentes
        /// mantidos a mao</b>, que so por sorte concordavam. Rebalancear o chefe pela ficha nao
        /// mudaria nada em jogo, e a <c>ficha_de_atributos.md</c> documentava contas baseadas
        /// num numero que ninguem lia.</para>
        ///
        /// <para>E e o que faz o <c>nivelDaUnidade</c> valer: o Ataque escala pela
        /// <c>EscalaDeNivel</c>, o campo local nao escala com nada.</para>
        /// </summary>
        private float DanoDasGarras =>
            _enemyBase != null && _enemyBase.Atributos != null && _enemyBase.Atributos.Ataque > 0f
                ? _enemyBase.Atributos.Ataque
                : danoDasGarras;

        [Tooltip("Trauma do cone de pressão sonora (fase 2+).")]
        [SerializeField] private float traumaDoGrito = 20f;

        [Tooltip("Alcance do cone, em unidades.")]
        [SerializeField] private float alcanceDoGrito = 4f;

        [Tooltip("Alcance das garras no pouso. Fora disso, o golpe não acerta. " +
                 "Usado só como reserva, se a hitbox não estiver ligada.")]
        [SerializeField] private float alcanceDasGarras = 1.5f;

        [Tooltip("Área de acerto das garras. Sem ela o golpe volta a ser um teste " +
                 "instantâneo de distância — impossível de esquivar no tempo.")]
        [SerializeField] private FavelaAmarela.Runtime.Combat.Hitbox hitboxDasGarras;

        [Tooltip("Quanto tempo as garras ficam perigosas, em segundos. É esta janela que " +
                 "transforma a esquiva numa decisão de tempo em vez de um teste de posição.")]
        [Min(0.02f)]
        [SerializeField] private float janelaDasGarras = 0.25f;

        [Header("Cores de leitura (provisórias, até haver arte)")]
        /// <summary>
        /// Tinta dos estados de voo. <b>Branco: a arte não se tinge.</b>
        ///
        /// <para><b>Era <c>(0.35, 0.30, 0.45)</c> até 2026-09-09</b> — roxo-acinzentado que,
        /// multiplicado, deixava o chefe a <b>32% do brilho</b> durante a maior parte da luta.
        /// A tinta era placeholder de quando não havia arte; o <c>AnimadorDoByakhee</c> chegou
        /// com 26 quadros por estado, e a documentação dele diz que <i>"substitui o tingimento
        /// por cor"</i> e que <i>"arte real não se tinge"</i>. Os dois ficaram rodando juntos, e
        /// o placeholder venceu.</para>
        /// </summary>
        [SerializeField] private Color corNoAr = Color.white;
        [SerializeField] private Color corPousado = new Color(0.85f, 0.75f, 0.25f);
        [SerializeField] private Color corFrenesi = new Color(0.85f, 0.20f, 0.15f);

        private ByakheeFSM _fsm;
        private EnemyBase _enemyBase;
        private SpriteRenderer _sprite;
        private Runtime.Rendering.SombraDeChao _sombra;
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
            _sombra = GetComponent<Runtime.Rendering.SombraDeChao>();
            _rb = GetComponent<Rigidbody2D>();

            _rb.gravityScale = 0f;
            _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            _centro = centroDaArena != null ? centroDaArena.position : transform.position;
            ResolverChaoDaArena();

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
        }

        private void OnDestroy()
        {
            if (_fsm != null)
            {
                _fsm.OnStateChanged -= HandleEstadoMudou;
                _fsm.OnGritoEmitido -= EmitirCone;
                _fsm.OnDerrotado -= HandleDerrotado;
            }

            if (_enemyBase != null) _enemyBase.OnAbatido -= HandleAbatido;
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
                    if (ORasanteChegouAoFim()) _fsm.EncerrarRasante();
                    break;

                case ByakheeState.MergulhoDeGarras:
                    Mergulhar();
                    break;

                case ByakheeState.Circundando:
                    Circundar();
                    break;

                case ByakheeState.Pousado:
                case ByakheeState.Frenesi:
                    ArrastarAteOJogador();
                    break;

                default:
                    // Grito e telegrafo acontecem parado: é o que dá ao jogador um alvo
                    // estável justamente quando ele pode reagir.
                    _rb.linearVelocity = Vector2.zero;
                    break;
            }

            ManterNaArena();
        }

        /// <summary>
        /// Mergulha em linha reta até o jogador e <b>para em cima dele</b>.
        ///
        /// <para><b>O defeito que isto conserta (2026-09-10, achado pelo DetectorDeOscilacao).</b>
        /// A versão anterior recalculava a direção até o jogador a cada FixedUpdate e escrevia
        /// 9 un/s nela. Ao alcançá-lo, passava 0,18 un além num passo de física, invertia, passava
        /// 0,18 do outro lado, invertia — <b>46 inversões de velocidade em 10 s</b>, todas dentro
        /// do mergulho, com a criatura vibrando ±0,09 un em cima do Damião a 50 Hz. Com o
        /// <c>flipX</c> seguindo a velocidade, o sprite piscava de lado a 25 Hz: é o "virando de
        /// um lado para outro" que sobrava depois de a folha ser corrigida.</para>
        /// </summary>
        private void Mergulhar()
        {
            Vector2 paraJogador = (Vector2)_jogador.position - _rb.position;

            // Um passo de física a 9 un/s anda 0,18: abaixo disto ela já está no alvo, e mirar
            // de novo só produziria o vaivém. Para e fica — o mergulho termina pelo relógio da FSM.
            if (paraJogador.sqrMagnitude <= ChegadaDoMergulho * ChegadaDoMergulho)
            {
                _rb.linearVelocity = Vector2.zero;
                return;
            }

            _rb.linearVelocity = paraJogador.normalized * velocidadeMergulho;
        }

        /// <summary>Raio em que o mergulho considera o jogador alcançado (ver <see cref="Mergulhar"/>).</summary>
        private const float ChegadaDoMergulho = 0.5f;

        /// <summary>
        /// O rasante acaba quando cumpriu o papel ou quando não tem mais para onde ir: já passou
        /// <see cref="alcanceAlemDoJogador"/> além do jogador, ou está <b>prensado numa parede</b>
        /// (contato sólido com normal contra a direção do voo).
        ///
        /// <para><b>Medido em 2026-09-10</b>, antes disto: com o jogador junto da muralha leste
        /// ela passava 2 s do rasante empurrando a parede (2055 de 3000 quadros em contato); com
        /// o jogador perto, 12 un de ida e uma volta arrastada a cada fase — 11 inversões de
        /// direção em 10 s. Os dois saem daqui; a FSM só oferece <c>EncerrarRasante</c>.</para>
        /// </summary>
        private bool ORasanteChegouAoFim()
        {
            if (_jogador != null)
            {
                Vector2 paraOJogador = (Vector2)_jogador.position - _rb.position;
                if (Vector2.Dot(paraOJogador, _direcaoDoRasante) < -alcanceAlemDoJogador) return true;
            }

            int n = _rb.GetContacts(_contatos);
            for (int i = 0; i < n; i++)
            {
                var c = _contatos[i];
                if (c.collider == null || c.collider.isTrigger) continue;
                // A normal do contato aponta do outro colisor para este corpo: parede à frente
                // tem normal contra a direção do voo.
                if (Vector2.Dot(c.normal, _direcaoDoRasante) < -0.5f) return true;
            }

            return false;
        }

        /// <summary>
        /// Enquanto está no chão e <b>vulnerável</b>, ele se arrasta na direção do jogador até
        /// ficar ao alcance — e só então para.
        ///
        /// <para><b>O defeito que isto conserta (2026-09-09).</b> O rasante corre por
        /// <c>duracaoRasante</c> (2 s) a <c>velocidadeRasante</c> (6 un/s): <b>12 unidades em
        /// linha reta</b>, atravessando o jogador e seguindo adiante. E <c>DepoisDoVoo</c>
        /// alterna mergulho e pouso — ou seja, <b>um pouso em cada dois acontece a 12 unidades
        /// do jogador</b>, que então tem 2 segundos de janela para percorrer 12 unidades
        /// correndo a 7,5 un/s. Não dá: ele chega com a janela fechando, e a fase 2 encurta a
        /// janela para 1,5 s.</para>
        ///
        /// <para>Somado ao dreno passivo de Resiliência (2/s, que corre o tempo todo), metade
        /// das janelas de dano ser inalcançável é o que faz a luta não fechar. O Vini relatou
        /// as três coisas juntas — "não dá para ganhar", "sai do mapa" e "difícil de atingir" —
        /// e as três saem daqui.</para>
        ///
        /// <para><b>Por que arrastar em vez de encurtar o rasante.</b> O rasante atravessar o
        /// jogador é o que o torna esquivável andando de lado, que é a defesa que o design
        /// pede; encurtá-lo tiraria isso. Arrastar preserva o telegrafo <i>e</i> devolve a
        /// leitura: quando ele está no chão vindo na sua direção, é a hora de bater.</para>
        /// </summary>
        private void ArrastarAteOJogador()
        {
            if (_jogador == null)
            {
                _rb.linearVelocity = Vector2.zero;
                return;
            }

            Vector2 paraOJogador = (Vector2)_jogador.position - _rb.position;

            // Ele não anda até "perto": anda até um ponto AO LADO do jogador, NIVELADO com ele
            // em profundidade de chão.
            //
            // Isto é o terceiro pedaço do relato do Vini, o "a hurtbox dela é muito difícil de
            // atingir" -- e ele NÃO é da hurtbox, que mede 8,45 × 10,09 unidades contra um corpo
            // desenhado de 5,72. É do PORTÃO DE PROFUNDIDADE da Hitbox, que aceita meia célula
            // (0,5) de diferença em Y entre quem bate e quem apanha. A órbita de Circundando usa
            // `Sin(ângulo) × raioDeVoo × 0,6` = até 1,8 unidade em Y: ele parava ao NORTE ou ao
            // SUL do Damião, três vezes e meia fora da faixa, e todo golpe para o lado era
            // rejeitado sem nada na tela dizendo por quê.
            //
            // Parar ao lado resolve na geometria em vez de afrouxar o portão -- que existe
            // porque uma unidade de mundo em Y vale DUAS células de chão neste isométrico, e
            // alargá-lo devolveria o golpe que alcança três células de profundidade.
            float lado = paraOJogador.x >= 0f ? -1f : 1f;
            Vector2 destino = (Vector2)_jogador.position + new Vector2(lado * DistanciaDeParada, 0f);

            Vector2 paraODestino = destino - _rb.position;

            // Chegou: para. O alvo estável que o design pede continua existindo — só deixa de
            // acontecer do outro lado da arena, e fora do alcance do golpe.
            if (paraODestino.sqrMagnitude <= TolerenciaDeChegada * TolerenciaDeChegada)
            {
                _rb.linearVelocity = Vector2.zero;
                return;
            }

            _rb.linearVelocity = paraODestino.normalized * velocidadeNoChao;
        }

        /// <summary>
        /// Distância em que ele para de se arrastar. Folgada de propósito: o alcance do Alfanje
        /// é 1,6 mais 0,85 de raio, e o corpo dele é largo.
        /// </summary>
        private const float DistanciaDeParada = 2.2f;

        /// <summary>
        /// Quão perto do ponto de parada conta como chegado. Meia célula isométrica: menos que
        /// isso e ele fica tremendo em torno do destino, porque a velocidade de um
        /// <c>FixedUpdate</c> a 2,6 un/s já anda 0,05.
        /// </summary>
        private const float TolerenciaDeChegada =
            Core.Player.BaseIsometrica.AlturaDeCelulaPadrao * 0.5f;

        /// <summary>
        /// <b>Coleira da arena.</b> Se ele passou da elipse da arena, a velocidade que
        /// aponta para fora é trocada por uma que aponta para dentro.
        ///
        /// <para><b>O defeito que isto conserta.</b> Nada limitava a posição dele. O rasante são
        /// 12 unidades em linha reta na direção em que o jogador estava, e a órbita de
        /// <c>Circundando</c> segue o <b>jogador</b>, não o centro. Encadeando rasantes, ele
        /// caminha para fora da arena e não volta — o "ela continua saindo do mapa" do
        /// playtest. Esta cena não tem <c>Limite_*</c>: não há parede para segurá-lo.</para>
        ///
        /// <para><b>A forma da coleira é a sala</b> — ver <see cref="chaoDaArena"/>. Duas
        /// formas anteriores foram medidas e descartadas: o círculo de raio 12 (deixava o chefe
        /// sair de quadro) e a elipse de 9 × 5 (cabia na câmera, não na sala, e pregava o chefe
        /// na borda sempre que o jogador saía dela).</para>
        ///
        /// <para>Corrige por <b>velocidade</b>, e não escrevendo <c>transform.position</c>: com
        /// <c>Auto Sync Transforms</c> desligado, mover o transform de um corpo deixa o colisor
        /// para trás até o próximo passo de física — e a hurtbox dele iria junto.</para>
        /// </summary>
        private void ManterNaArena()
        {
            Vector2 posicao = _rb.position;

            bool foraDoChao = chaoDaArena != null
                              && !chaoDaArena.HasTile(chaoDaArena.WorldToCell(posicao));
            bool alemDaMuralha = muralhaNorte != null && posicao.y > muralhaNorte.position.y;

            if (!foraDoChao && !alemDaMuralha) return;

            // Só o teto estourou: volta para o sul, sem puxar para o centro — puxar para o
            // centro num chefe que está no canto norte-leste o arrastaria para oeste também,
            // e ele deslizaria ao longo do portão em vez de simplesmente recuar dele.
            Vector2 paraDentro = foraDoChao
                ? ((Vector2)_centro - posicao).normalized
                : Vector2.down;

            // Só corrige o que aponta para fora: um movimento que já volta é preservado, senão
            // ele ficaria colado na borda em vez de retomar o padrão.
            if (Vector2.Dot(_rb.linearVelocity, paraDentro) > 0f) return;

            _rb.linearVelocity = paraDentro * Mathf.Max(_rb.linearVelocity.magnitude,
                                                        velocidadeCircundando);
        }

        /// <summary>
        /// Acha o chão da arena se ninguém o arrastou: o Tilemap com mais células da cena — o
        /// mesmo critério do <c>IsometricCameraController</c> para os limites do mapa. Roda
        /// uma vez; <c>FindObjectsByType</c> é proibido em caminho quente.
        /// </summary>
        private void ResolverChaoDaArena()
        {
            if (chaoDaArena != null) return;

            chaoDaArena = ChaoComMaisTiles(FindObjectsByType<Tilemap>());

            if (chaoDaArena == null)
                Debug.LogWarning("[Byakhee] Nenhum Tilemap na cena — sem chão, a coleira está " +
                                 "desligada e rasantes encadeados podem sair do mapa.", this);
        }

        /// <summary>
        /// O Tilemap com mais células <b>pintadas</b> — o chão da sala.
        ///
        /// <para><b>Por que contar tiles e não a caixa (2026-09-10).</b> A primeira versão
        /// escolhia pelo <c>cellBounds</c>, e escolheu o Tilemap <c>Colisao</c>: o anel de
        /// paredes tem só 528 células pintadas, mas a caixa dele (68 × 68) envolve a do chão
        /// (64 × 64, todas pintadas). Com a coleira apontando para o anel, <c>HasTile</c> daria
        /// falso em todo o chão e o Byakhee seria puxado ao centro o tempo inteiro — pior que o
        /// defeito que se estava consertando. <c>GetUsedTilesCount</c> também não serve: conta
        /// <i>tipos</i> de tile, não células.</para>
        ///
        /// <para>Público e estático para a ferramenta de cena e o teste usarem o <b>mesmo</b>
        /// critério — três cópias divergiriam em silêncio.</para>
        /// </summary>
        public static Tilemap ChaoComMaisTiles(Tilemap[] mapas)
        {
            Tilemap maior = null;
            int maisPintadas = 0;

            foreach (var mapa in mapas)
            {
                mapa.CompressBounds();
                int pintadas = 0;
                foreach (var tile in mapa.GetTilesBlock(mapa.cellBounds))
                    if (tile != null) pintadas++;

                if (pintadas <= maisPintadas) continue;

                maisPintadas = pintadas;
                maior = mapa;
            }

            return maior;
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

            // Circunda o JOGADOR, nao o centro da arena. Orbitar um ponto fixo fazia o chefe
            // girar sozinho no meio do mapa, ignorando quem ele esta cacando -- foi o
            // "perdido, girando 360 graus" que o Vini relatou no playtest. "Circunda" no
            // design descreve rodear a presa, e e isso que a orbita precisa exprimir.
            Vector3 eixo = _jogador != null ? _jogador.position : _centro;

            var alvo = eixo + new Vector3(
                Mathf.Cos(_anguloCircundando) * raioDeVoo,
                Mathf.Sin(_anguloCircundando) * raioDeVoo * 0.6f,   // elipse: o isométrico achata o eixo Y
                0f);

            // Proporcional PERTO, limitada LONGE. A versao anterior multiplicava a distancia
            // inteira pela velocidade: a 20 unidades do alvo, com velocidade 4, ele saia a 80
            // un/s -- atravessava a arena num quadro e voltava, o que le como teletransporte.
            var paraOAlvo = (Vector2)(alvo - transform.position);
            _rb.linearVelocity = Vector2.ClampMagnitude(paraOAlvo * 2f, velocidadeCircundando);
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
                    // O pouso sacode o chão ANTES do golpe: é o aviso de que a criatura chegou.
                    // Pelo canal do mundo, não por referência -- este é um prefab e a câmera é
                    // objeto de cena, e prefab não referencia cena.
                    Core.Camera.TremorDoMundo.Sacudir(TraumaDoPouso);
                    GolpearComGarras();
                    break;
            }

            _sprite.color = atual switch
            {
                ByakheeState.Pousado => corPousado,
                ByakheeState.Frenesi => corFrenesi,
                _ => corNoAr
            };

            if (_sombra != null) _sombra.Altura = AlturaDoEstado(atual);
        }

        /// <summary>
        /// A altura que a sombra mostra em cada estado — <b>o indicador da janela de dano</b>.
        ///
        /// <para><b>Por que a sombra e não a cor (2026-09-09).</b> Tinta multiplicativa só
        /// <i>escurece</i>, e a arte do Byakhee já é dourada e brilhante: o
        /// <c>corPousado (0.85, 0.75, 0.25)</c> aplicado a ela produzia quase a mesma imagem.
        /// Dava para escurecer o chefe, nunca para destacá-lo — o sinal da janela de dano era
        /// fraco <b>por construção</b>, enquanto o estado imune era o marcante. Estava
        /// invertido.</para>
        ///
        /// <para><b>E a leitura cai redonda:</b> dos oito estados, <c>Pousado</c> é o
        /// <b>único</b> em que ele está no chão — <c>Espreita</c> é "pousado no topo do arco",
        /// que é alto. Sombra fechada e escura significa que ele desceu, e é exatamente quando
        /// pode ser ferido. É o que o Vini relatou não conseguir ler: <i>"a hurtbox dela é muito
        /// difícil de atingir"</i>.</para>
        ///
        /// <para>O <c>MergulhoDeGarras</c> fica <b>baixo, mas não zero</b>: a sombra apertando
        /// enquanto ele desce telegrafa onde o mergulho vai cair, e ele continua imune no
        /// caminho.</para>
        /// </summary>
        private static float AlturaDoEstado(ByakheeState estado) => estado switch
        {
            ByakheeState.Pousado => 0f,             // no chão: A janela de dano
            ByakheeState.Derrotado => 0f,
            ByakheeState.MergulhoDeGarras => 1.2f,  // descendo — a sombra aperta antes de cair
            ByakheeState.Frenesi => 2.0f,
            ByakheeState.Espreita => 3.0f,          // no topo do arco, o mais alto
            _ => 2.5f,                              // Rasante, GritoDirecionado, Circundando
        };

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

            var golpe = new ArmaResult(true, 0f, 0f, false, 0f, DanoDasGarras);

            if (hitboxDasGarras != null)
            {
                // Baque de pouso: radial de propósito (sem direção), porque o corpo inteiro
                // desaba. O que faz a diferença aqui é a JANELA — antes isto era um teste de
                // distância de um quadro só, então não havia como esquivar no tempo, apenas
                // estar longe naquele instante exato. Ver Hitbox para o porquê completo.
                hitboxDasGarras.Armar(golpe, janelaDasGarras);
                return;
            }

            Debug.LogError($"[ByakheeAI] '{name}' está sem hitboxDasGarras — o golpe caiu para " +
                           "o teste instantâneo de distância, que não é esquivável no tempo. " +
                           "Rode 'Tools/FavelaAmarela/Combate: montar hitbox e hurtbox'.", this);

            float distancia = Vector2.Distance(transform.position, _jogador.position);
            if (distancia > alcanceDasGarras) return;

            var alvo = _jogador.GetComponent<IDanificavel>();
            alvo?.ReceberGolpe(golpe);
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
