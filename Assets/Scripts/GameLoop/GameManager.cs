using UnityEngine;
using FavelaAmarela.Core.Combat;
using FavelaAmarela.Core.GameLoop;
using FavelaAmarela.Core.Stealth;
using FavelaAmarela.Core.Environment;
using FavelaAmarela.Player;
using FavelaAmarela.Runtime.Audio;
using FavelaAmarela.Runtime.Combat;
using FavelaAmarela.Runtime.Enemies;
using FavelaAmarela.Runtime.Environment;
using FavelaAmarela.Runtime.UI;
using UnityEngine.InputSystem;

namespace FavelaAmarela.Runtime.GameLoop
{
    [AddComponentMenu("Favela Amarela/Game Manager")]
    // Garante que Awake() (e InjetarDependencias(), que faz o Bind() dos POCOs nos
    // adapters) termine antes do Awake/OnEnable de qualquer outro script da cena —
    // ex.: EnemyPerception.OnEnable() já encontra o SoundBroadcaster injetado por Bind().
    [DefaultExecutionOrder(-100)]
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public GameLoopStateMachine StateMachine { get; private set; }
        public ResilienciaMental Resiliencia { get; private set; }
        public SoundBroadcastService SoundBroadcaster { get; private set; }
        public EnvironmentState Environment { get; private set; }

        /// <summary>Driver da tempestade na cena (achado no bootstrap). Permite a scripts como o <c>QuedaZ4Z5Trigger</c> mudar a faixa da tempestade sem nova referência de Inspector.</summary>
        public TempestadeAmbiente TempestadeAmbiente { get; private set; }

        /// <summary>
        /// Verdadeiro enquanto Damião está preso numa sequência roteirizada (ex.: a
        /// queda Z4→Z5) e não pode agir. Fontes de morte instantânea por toque/ambiente
        /// (Coisa do Cemitério, <c>ColapsoTrigger</c>) devem respeitar isto e NÃO aplicar
        /// o Colapso — durante a cutscene há só a tensão da ameaça se aproximando, não dano.
        /// </summary>
        public bool JogadorInvulneravel { get; private set; }

        /// <summary>
        /// Liga/desliga a invulnerabilidade de cutscene (ver <see cref="JogadorInvulneravel"/>).
        /// Propaga para a <see cref="VitalidadeBridge"/> de Damião, para que golpes físicos
        /// (ex.: o corpo-a-corpo do Cultista) também sejam ignorados durante a cutscene —
        /// senão ele morreria de porrada no meio de uma sequência roteirizada.
        /// </summary>
        public void DefinirInvulneravel(bool valor)
        {
            JogadorInvulneravel = valor;
            if (_vitalidadeDamiao != null)
                _vitalidadeDamiao.IgnorarDano = valor;
        }

        [Header("Configurações Iniciais")]
        [SerializeField] private float maxResiliencia = 100f;
        [SerializeField] private float fracaoPanico = 0.25f;

        [Header("Referências Opcionais")]
        [SerializeField] private GameObject telaTransicaoDeFase;
        [SerializeField] private GameObject telaPause;
        [SerializeField] private GameObject telaMenu;
        [SerializeField] private GameObject gameplayRoot;

        [Tooltip("Começa no Menu em vez de cair direto no gameplay. Desligue para playtest " +
                 "rápido, entrando direto na cena.")]
        [SerializeField] private bool iniciarNoMenu = true;
        [Tooltip("Sequência de morte (dissolução + frase) tocada ao entrar em Colapso.")]
        [SerializeField] private SequenciaDeColapso sequenciaColapso;

        // Vitalidade corpórea de Damião (achada no bootstrap) e a causa da derrota
        // corrente — a frase final do Colapso depende dela (mental vs corpórea).
        private VitalidadeBridge _vitalidadeDamiao;
        private TipoDeDerrota _tipoDeDerrota = TipoDeDerrota.Mental;

        // Vitalidade do companheiro Yug-Neth — diferente da do Damião, ele já existe na
        // cena (cativo) mas só passa a valer para a run quando o jogador o liberta do
        // Abdul, em algum ponto no meio do jogo. Por isso é registrado sob demanda por
        // RegistrarYugNeth, em vez de procurado em InjetarDependencias.
        private YugNethAI _yugNeth;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // 1. Cria a Lógica Pura (Core)
            StateMachine = new GameLoopStateMachine(iniciarNoMenu ? GameState.Menu : GameState.Gameplay);
            Resiliencia = ResilienciaMental.ComThresholdFracional(maxResiliencia, fracaoPanico);
            SoundBroadcaster = new SoundBroadcastService();
            Environment = new EnvironmentState();

            // 2. Observa o Core
            StateMachine.OnStateChanged += HandleStateChanged;
            Resiliencia.OnChanged += HandleResilienciaChanged;

            // 3. Injeta a POCO nos sistemas de Runtime
            InjetarDependencias();

            // 4. Aplica a apresentação do estado INICIAL.
            //
            // `OnStateChanged` só dispara em transições, e no arranque não há transição —
            // então começar no Menu deixava o jogo num limbo: o estado era Menu, mas nenhuma
            // tela aparecia e o mundo rodava solto por trás, porque o timeScale nunca era
            // tocado. Aplicar aqui alinha o que a máquina de estados diz com o que se vê.
            HandleStateChanged(StateMachine.CurrentState, StateMachine.CurrentState);
        }

        /// <summary>
        /// Distribui os POCOs recém-criados aos adaptadores de Runtime da cena, via os
        /// respectivos <c>Bind()</c>.
        ///
        /// <para><b>Exceção arquitetural consciente:</b> o <c>CLAUDE.md</c> proíbe busca de
        /// objetos por tipo em código de produção — a regra existe para impedir que
        /// adaptadores se auto-resolvam por conta própria (era o caso dos inimigos, que
        /// consultavam <c>GameManager.Instance</c> no próprio <c>OnEnable</c>). Este método
        /// é o <b>bootstrap</b>: o único ponto onde a montagem do grafo de dependências
        /// acontece, uma vez, no <c>Awake</c>. Concentrar a busca aqui é justamente o que
        /// permite que todo o resto receba suas dependências por injeção.</para>
        /// </summary>
        private void InjetarDependencias()
        {
            // Busca o HUDController e injeta
            var hud = FindAnyObjectByType<HUDController>();
            if (hud != null)
                hud.InjetarResiliencia(Resiliencia);
            else
                // Silêncio aqui já custou caro: sem HUDController ninguém chama Bind() nas
                // barras, e elas ficam congeladas parecendo bug de lógica de dano.
                Debug.LogError("[GameManager] Nenhum HUDController na cena — as barras do HUD " +
                               "não serão ligadas e vão parecer travadas. Rode " +
                               "'Tools/FavelaAmarela/Montar HUDController na cena'.", this);

            // Busca o PlayerMovement e injeta
            var player = FindAnyObjectByType<PlayerMovement>();
            if (player != null)
                player.Bind(SoundBroadcaster, Environment);

            // Áudio: dá voz ao ruído que Damião emite (o pilar de furtividade sonora) e às
            // viradas de estado mental. Sem isto, a mecânica central é imperceptível.
            var audioStealth = FindAnyObjectByType<AudioDeStealth>();
            if (audioStealth != null)
                audioStealth.Bind(SoundBroadcaster);
            else
                Debug.LogWarning("[GameManager] Nenhum AudioDeStealth na cena; o jogador não vai " +
                                 "ouvir o próprio ruído — o pilar sonoro fica invisível.", this);

            var audioResiliencia = FindAnyObjectByType<AudioDeResiliencia>();
            if (audioResiliencia != null)
                audioResiliencia.Bind(Resiliencia);

            // Vitalidade corpórea de Damião: observa o abate para levar ao Colapso
            // (mesmo fim de jogo da Resiliência a zero, com frases de morte corpórea).
            _vitalidadeDamiao = player != null
                ? player.GetComponent<VitalidadeBridge>()
                : FindAnyObjectByType<VitalidadeBridge>();

            if (_vitalidadeDamiao != null)
            {
                _vitalidadeDamiao.OnAbatido += HandleDamiaoAbatido;
                // O HUD é dono das suas views: repassa a POCO, não a bridge.
                if (hud != null)
                    hud.InjetarVitalidade(_vitalidadeDamiao.Vitalidade);
            }

            // Abdul: se a conversa/luta já foi resolvida numa visita anterior, reconstrói o
            // estado dele (poupado ou derrotado) antes que o jogador consiga interagir.
            // Include inativos porque um Abdul derrotado volta da cena já com SetActive(false).
            if (player != null)
            {
                var abdul = FindAnyObjectByType<AbdulAlhazredAI>(FindObjectsInactive.Include);
                if (abdul != null) abdul.AplicarEstadoSalvo(player.gameObject);
            }

            // Inventário: O InventoryManager agora é um Singleton e auto-resolve suas 
            // dependências (BarraDeItens já assina sozinha, e VitalidadeBridge já se 
            // conecta ao evento de consumo). Não precisamos injetá-lo aqui.

            // Yug-Neth é companheiro, não obstáculo: o corpo dele não pode barrar a
            // passagem de Damião (relatado em playtest — ele entalava o jogador na arena).
            // Feito aqui, no bootstrap, porque é onde o colisor do jogador já é conhecido;
            // resolver por layer exigiria uma camada nova fora da taxonomia fechada.
            if (player != null)
            {
                var yugNethNaCena = FindAnyObjectByType<YugNethAI>(FindObjectsInactive.Include);
                var colisorDoJogador = player.GetComponent<Collider2D>();
                if (yugNethNaCena != null && colisorDoJogador != null)
                    yugNethNaCena.IgnorarColisaoCom(colisorDoJogador);
            }

            // Barra de ações: mostra a arma empunhada e a recarga da habilidade.
            if (hud != null && player != null)
            {
                var maoFisica = player.GetComponent<MaoFisicaBridge>();
                if (maoFisica != null)
                    hud.InjetarMaoFisica(maoFisica);

                var vigor = player.GetComponent<GerenciadorDeVigor>();
                if (vigor != null)
                    hud.InjetarVigor(vigor);

                // Artefatos: a barra F1–F4 e a fonte de passivas dependem da mesma bridge.
                var artefatos = player.GetComponent<ArtefatosBridge>();
                if (artefatos != null)
                {
                    hud.InjetarArtefatos(artefatos);

                    if (GerenciadorEfeitosPassivos.Instance != null)
                        GerenciadorEfeitosPassivos.Instance.Bind(artefatos);
                }
            }
            else
            {
                Debug.LogWarning("[GameManager] Nenhuma VitalidadeBridge encontrada na cena; " +
                                 "Damião não terá vitalidade corpórea nem morte física.", this);
            }

            // Busca o driver da tempestade e o overlay visual e injeta
            TempestadeAmbiente = FindAnyObjectByType<TempestadeAmbiente>();
            if (TempestadeAmbiente != null)
                TempestadeAmbiente.Bind(Environment);

            var tempestadeOverlay = FindAnyObjectByType<TempestadeVisualOverlay>();
            if (tempestadeOverlay != null)
                tempestadeOverlay.Bind(Environment);

            // Injeta o serviço de som em todos os inimigos sound-first da cena — eles
            // não buscam mais GameManager.Instance sozinhos no próprio OnEnable
            // (inconsistência corrigida: agora seguem o mesmo padrão de Bind() do PlayerMovement).
            // Inclui inativos: um inimigo de emboscada (começa desligado, ligado depois por
            // trigger) já nasce com o serviço injetado, e seu OnEnable o encontra ao ativar.
            // A sobrecarga com FindObjectsSortMode foi depreciada na Unity 6 (CS0618).
            // Migrar é seguro aqui: já se pedia SortMode.None e a injeção é
            // ordem-independente — cada inimigo recebe o mesmo serviço, sem índice
            // nem acumulação entre iterações.
            foreach (var perception in FindObjectsByType<EnemyPerception>(FindObjectsInactive.Include))
                perception.Bind(SoundBroadcaster);

            foreach (var coisa in FindObjectsByType<CoisaDoCemiterioAI>(FindObjectsInactive.Include))
                coisa.Bind(SoundBroadcaster);
        }

        private void Update()
        {
            // Toggle de pause (Esc)
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                if (StateMachine.CurrentState == GameState.Gameplay)
                    StateMachine.TryTransition(GameState.Pausado);
                else if (StateMachine.CurrentState == GameState.Pausado)
                    StateMachine.TryTransition(GameState.Gameplay);
            }
        }

        private void HandleStateChanged(GameState anterior, GameState atual)
        {
            // Lida com o timescale. O Menu congela junto com Pausado: sem isso o mundo
            // continuaria rodando atrás da tela inicial — inimigos andando e a tempestade
            // drenando Resiliência —, e dava para morrer olhando o menu.
            bool mundoCongelado = atual == GameState.Pausado
                                  || atual == GameState.TransicaoDeFase
                                  || atual == GameState.Menu;
            Time.timeScale = mundoCongelado ? 0f : 1f;

            // Ativa/Desativa GameObjects baseado no estado
            if (telaPause != null) telaPause.SetActive(atual == GameState.Pausado);
            if (telaMenu != null) telaMenu.SetActive(atual == GameState.Menu);
            if (telaTransicaoDeFase != null) telaTransicaoDeFase.SetActive(atual == GameState.TransicaoDeFase);
            if (gameplayRoot != null) gameplayRoot.SetActive(atual == GameState.Gameplay || atual == GameState.Colapso);

            // Colapso = Game Over diegético: toca a sequência de morte. A causa
            // (mente ou corpo) escolhe o pool de frases finais.
            if (atual == GameState.Colapso && sequenciaColapso != null)
            {
                sequenciaColapso.Tocar(_tipoDeDerrota);
            }

            if (atual == GameState.Menu)
            {
                Resiliencia?.EstabilizarCompletamente();
                
                // Restaura Vigor quando for pro Menu, assim como a Resiliência
                var player = FindAnyObjectByType<PlayerMovement>();
                if (player != null)
                {
                    player.GetComponent<GerenciadorDeVigor>()?.RestaurarCompletamente();
                }
            }
        }

        private void HandleResilienciaChanged(ResilienciaChangedArgs args)
        {
            if (args.EntrouEmColapso)
            {
                _tipoDeDerrota = TipoDeDerrota.Mental;
                StateMachine.TryTransition(GameState.Colapso);
            }
        }

        /// <summary>
        /// Damião foi abatido fisicamente (Vitalidade a zero). Leva ao mesmo fim de jogo
        /// do Colapso Mental, mas marcado como derrota <b>corpórea</b> — a frase final
        /// fala do corpo caído, não de lucidez dissolvida.
        /// </summary>
        private void HandleDamiaoAbatido()
        {
            _tipoDeDerrota = TipoDeDerrota.Corporea;
            StateMachine.TryTransition(GameState.Colapso);
        }

        /// <summary>
        /// Registra o companheiro Yug-Neth assim que ele é libertado (chamado por
        /// <c>AbdulAlhazredAI</c> logo após <c>Bind</c>). Diferente do resto de
        /// <see cref="InjetarDependencias"/>, isto acontece <b>em runtime</b>, não no
        /// bootstrap — Yug-Neth só passa a valer para a run quando o jogador escolhe lutar
        /// ou concordar com Abdul. Passar <c>null</c> não faz nada (chamada defensiva de
        /// quem não sabe se já registrou).
        /// </summary>
        public void RegistrarYugNeth(YugNethAI yugNeth)
        {
            if (yugNeth == null || _yugNeth == yugNeth) return;
            _yugNeth = yugNeth;
        }

        /// <summary>
        /// O companheiro Yug-Neth, se já libertado (null antes disso). Exposto para quem
        /// precisar consultar <see cref="YugNethAI.EstaIncapacitado"/> — hoje, o gatilho dos
        /// Portões de Carcosa: sem ele reanimado, os Portões não abrem.
        ///
        /// <para><b>Nota histórica (2026-07-31):</b> a morte de Yug-Neth já foi fim de run
        /// imediato (<c>TipoDeDerrota.EscoltaPerdida</c>, estilo escolta da Ashley em
        /// Resident Evil 4, sem resgate). Revogado: agora ele fica <b>incapacitado</b>
        /// (cai no lugar) e é reanimado num <c>RefugioDeLuz</c> — bloqueia o progresso, não
        /// a run inteira.</para>
        /// </summary>
        public YugNethAI YugNeth => _yugNeth;

        /// <summary>
        /// Dispara a transição de fim de fase/dungeon (ex.: ao vencer o miniboss de um portão de
        /// saída). Não é uma tela de "Vitória" — ver <see cref="GameState.TransicaoDeFase"/>.
        /// </summary>
        public void TriggerTransicaoDeFase()
        {
            StateMachine.TryTransition(GameState.TransicaoDeFase);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
                if (StateMachine != null)
                    StateMachine.OnStateChanged -= HandleStateChanged;
                if (Resiliencia != null)
                    Resiliencia.OnChanged -= HandleResilienciaChanged;
                if (_vitalidadeDamiao != null)
                    _vitalidadeDamiao.OnAbatido -= HandleDamiaoAbatido;
            }
        }
    }
}
