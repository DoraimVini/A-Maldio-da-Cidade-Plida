using UnityEngine;
using FavelaAmarela.Core.Combat;
using FavelaAmarela.Core.GameLoop;
using FavelaAmarela.Core.Stealth;
using FavelaAmarela.Core.Environment;
using FavelaAmarela.Player;
using FavelaAmarela.Runtime.UI;
using UnityEngine.InputSystem;

namespace FavelaAmarela.Runtime.GameLoop
{
    [AddComponentMenu("Favela Amarela/Game Manager")]
    // Garante que Instance esteja pronto antes do Awake/OnEnable de qualquer outro script da cena
    // (ex.: CultistaAI se inscreve em GameManager.Instance.SoundBroadcaster no próprio OnEnable).
    [DefaultExecutionOrder(-100)]
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public GameLoopStateMachine StateMachine { get; private set; }
        public ResilienciaMental Resiliencia { get; private set; }
        public SoundBroadcastService SoundBroadcaster { get; private set; }
        public EnvironmentState Environment { get; private set; }

        [Header("Configurações Iniciais")]
        [SerializeField] private float maxResiliencia = 100f;
        [SerializeField] private float fracaoPanico = 0.25f;

        [Header("Referências Opcionais")]
        [SerializeField] private GameObject telaVitoria;
        [SerializeField] private GameObject telaPause;
        [SerializeField] private GameObject gameplayRoot;

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
            Time.timeScale = (atual == GameState.Pausado || atual == GameState.Vitoria) ? 0f : 1f;

            // Ativa/Desativa GameObjects baseado no estado
            if (telaPause != null) telaPause.SetActive(atual == GameState.Pausado);
            if (telaVitoria != null) telaVitoria.SetActive(atual == GameState.Vitoria);
            if (gameplayRoot != null) gameplayRoot.SetActive(atual == GameState.Gameplay || atual == GameState.Colapso);

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

        public void TriggerVitoria()
        {
            StateMachine.TryTransition(GameState.Vitoria);
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
