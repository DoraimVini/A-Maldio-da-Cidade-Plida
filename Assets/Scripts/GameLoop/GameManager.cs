using UnityEngine;
using FavelaAmarela.Core.Combat;
using FavelaAmarela.Core.GameLoop;
using FavelaAmarela.Core.Stealth;
using FavelaAmarela.Core.Environment;
using FavelaAmarela.Player;
using FavelaAmarela.Runtime.Enemies;
using FavelaAmarela.Runtime.Environment;
using FavelaAmarela.Runtime.UI;
using UnityEngine.InputSystem;

namespace FavelaAmarela.Runtime.GameLoop
{
    [AddComponentMenu("Favela Amarela/Game Manager")]
    // Garante que Awake() (e InjetarDependencias(), que faz o Bind() dos POCOs nos
    // adapters) termine antes do Awake/OnEnable de qualquer outro script da cena —
    // ex.: CultistaAI.OnEnable() já encontra o SoundBroadcaster injetado por Bind().
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

        /// <summary>Liga/desliga a invulnerabilidade de cutscene (ver <see cref="JogadorInvulneravel"/>).</summary>
        public void DefinirInvulneravel(bool valor) => JogadorInvulneravel = valor;

        [Header("Configurações Iniciais")]
        [SerializeField] private float maxResiliencia = 100f;
        [SerializeField] private float fracaoPanico = 0.25f;

        [Header("Referências Opcionais")]
        [SerializeField] private GameObject telaTransicaoDeFase;
        [SerializeField] private GameObject telaPause;
        [SerializeField] private GameObject gameplayRoot;
        [Tooltip("Sequência de morte (dissolução + frase) tocada ao entrar em Colapso.")]
        [SerializeField] private SequenciaDeColapso sequenciaColapso;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // 1. Cria a Lógica Pura (Core)
            StateMachine = new GameLoopStateMachine(GameState.Gameplay);
            Resiliencia = ResilienciaMental.ComThresholdFracional(maxResiliencia, fracaoPanico);
            SoundBroadcaster = new SoundBroadcastService();
            Environment = new EnvironmentState();

            // 2. Observa o Core
            StateMachine.OnStateChanged += HandleStateChanged;
            Resiliencia.OnChanged += HandleResilienciaChanged;

            // 3. Injeta a POCO nos sistemas de Runtime
            InjetarDependencias();
        }

        private void InjetarDependencias()
        {
            // Busca o HUDController e injeta
            var hud = FindAnyObjectByType<HUDController>();
            if (hud != null)
                hud.InjetarResiliencia(Resiliencia);

            // Busca o AnomalyPowerBridge e injeta
            var bridge = FindAnyObjectByType<AnomalyPowerBridge>();
            if (bridge != null)
                bridge.Bind(Resiliencia);

            // Busca o PlayerMovement e injeta
            var player = FindAnyObjectByType<PlayerMovement>();
            if (player != null)
                player.Bind(SoundBroadcaster, Environment);

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
            foreach (var cultista in FindObjectsByType<CultistaAI>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                cultista.Bind(SoundBroadcaster);

            foreach (var coisa in FindObjectsByType<CoisaDoCemiterioAI>(FindObjectsInactive.Include, FindObjectsSortMode.None))
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
            // Lida com o timescale
            Time.timeScale = (atual == GameState.Pausado || atual == GameState.TransicaoDeFase) ? 0f : 1f;

            // Ativa/Desativa GameObjects baseado no estado
            if (telaPause != null) telaPause.SetActive(atual == GameState.Pausado);
            if (telaTransicaoDeFase != null) telaTransicaoDeFase.SetActive(atual == GameState.TransicaoDeFase);
            if (gameplayRoot != null) gameplayRoot.SetActive(atual == GameState.Gameplay || atual == GameState.Colapso);

            // Colapso Mental = Game Over diegético: toca a sequência de morte.
            if (atual == GameState.Colapso && sequenciaColapso != null)
            {
                sequenciaColapso.Tocar();
            }

            if (atual == GameState.Menu)
            {
                Resiliencia?.EstabilizarCompletamente();
            }
        }

        private void HandleResilienciaChanged(ResilienciaChangedArgs args)
        {
            if (args.EntrouEmColapso)
            {
                StateMachine.TryTransition(GameState.Colapso);
            }
        }

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
            }
        }
    }
}
