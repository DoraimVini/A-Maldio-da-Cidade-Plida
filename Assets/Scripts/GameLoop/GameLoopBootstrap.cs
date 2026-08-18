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

namespace FavelaAmarela.Runtime.GameLoop
{
    /// <summary>
    /// Camada Runtime. <b>Raiz de composição</b> da cena: cria os POCOs do Core uma vez e
    /// distribui as dependências para os adaptadores, incluindo os componentes focados extraídos
    /// do antigo <c>GameManager</c>.
    ///
    /// <para><b>Exceção arquitetural consciente:</b> o <c>CLAUDE.md</c> proíbe busca de objetos
    /// por tipo em código de produção. A regra existe para impedir que adaptadores se
    /// auto-resolvam — não para impedir que exista um ponto de montagem. Este é esse ponto: a
    /// busca acontece <b>aqui, uma vez, no Awake</b>, e é justamente o que permite que todo o
    /// resto receba suas dependências por injeção.</para>
    ///
    /// <para><b>Ordem de execução −200</b>, à frente do <see cref="GameManager"/> (−100): a casca
    /// de compatibilidade encaminha para os POCOs criados aqui, então eles precisam existir antes
    /// que ela responda a qualquer consulta.</para>
    ///
    /// <para>Extraído do <c>GameManager.InjetarDependencias</c> em 2026-08-14.</para>
    /// </summary>
    [AddComponentMenu("Favela Amarela/GameLoop/Bootstrap da Cena")]
    [DefaultExecutionOrder(-200)]
    public sealed class GameLoopBootstrap : MonoBehaviour
    {
        [Header("Configurações Iniciais")]
        [SerializeField] private float maxResiliencia = 100f;
        [SerializeField] private float fracaoPanico = 0.25f;

        /// <summary>Máquina de estados do loop de jogo desta cena.</summary>
        public GameLoopStateMachine StateMachine { get; private set; }

        /// <summary>Resiliência Mental de Damião.</summary>
        public ResilienciaMental Resiliencia { get; private set; }

        /// <summary>Serviço de propagação sonora — o pilar de furtividade.</summary>
        public SoundBroadcastService SoundBroadcaster { get; private set; }

        /// <summary>Estado do ambiente (tempestade, luz).</summary>
        public EnvironmentState Environment { get; private set; }

        /// <summary>
        /// Driver da tempestade na cena, achado no bootstrap. Permite a scripts como o
        /// <c>QuedaZ4Z5Trigger</c> mudar a faixa da tempestade sem nova referência de Inspector.
        /// </summary>
        public TempestadeAmbiente TempestadeAmbiente { get; private set; }

        /// <summary>
        /// Vitalidade corpórea de Damião, ou <c>null</c> se a bridge não foi achada. Contraparte
        /// da <see cref="Resiliencia"/>: as duas barras de derrota ficam alcançáveis pelo mesmo
        /// caminho, o que evita que cada script que precisa curar o corpo saia procurando a
        /// <c>VitalidadeBridge</c> por conta própria.
        /// </summary>
        public Vitalidade VitalidadeDoJogador => _vitalidadeDamiao?.Vitalidade;

        private VitalidadeBridge _vitalidadeDamiao;

        private void Awake()
        {
            // 1. Lógica pura (Core). A cena de jogo sempre nasce jogando: quem mostra menu é a
            //    `Cena_Menu`.
            StateMachine = new GameLoopStateMachine(GameState.Gameplay);
            Resiliencia = ResilienciaMental.ComThresholdFracional(maxResiliencia, fracaoPanico);
            SoundBroadcaster = new SoundBroadcastService();
            Environment = new EnvironmentState();

            // 2. Adaptadores da cena.
            InjetarNoMundo();

            // 3. Componentes focados irmãos. Depois do mundo, porque o PlayerDeathController e o
            //    CutsceneController precisam da VitalidadeBridge que a busca acima resolveu.
            InjetarNosComponentesFocados();
        }

        /// <summary>
        /// Acha os adaptadores da cena e entrega a cada um o POCO de que ele depende.
        /// </summary>
        private void InjetarNoMundo()
        {
            var hud = FindAnyObjectByType<HUDController>();
            if (hud != null)
                hud.InjetarResiliencia(Resiliencia);
            else
                // Silêncio aqui já custou caro: sem HUDController ninguém chama Bind() nas
                // barras, e elas ficam congeladas parecendo bug de lógica de dano.
                Debug.LogError("[GameLoopBootstrap] Nenhum HUDController na cena — as barras do " +
                               "HUD não serão ligadas e vão parecer travadas. Rode " +
                               "'Tools/FavelaAmarela/Montar HUDController na cena'.", this);

            var player = FindAnyObjectByType<PlayerMovement>();
            if (player != null)
                player.Bind(SoundBroadcaster, Environment);
            else
                Debug.LogError("[GameLoopBootstrap] Nenhum PlayerMovement na cena — Damião não " +
                               "emite ruído nem reage ao ambiente.", this);

            // Áudio: dá voz ao ruído que Damião emite (o pilar de furtividade sonora) e às
            // viradas de estado mental. Sem isto, a mecânica central é imperceptível.
            var audioStealth = FindAnyObjectByType<AudioDeStealth>();
            if (audioStealth != null)
                audioStealth.Bind(SoundBroadcaster);
            else
                Debug.LogWarning("[GameLoopBootstrap] Nenhum AudioDeStealth na cena; o jogador " +
                                 "não vai ouvir o próprio ruído — o pilar sonoro fica invisível.",
                                 this);

            var audioResiliencia = FindAnyObjectByType<AudioDeResiliencia>();
            if (audioResiliencia != null)
                audioResiliencia.Bind(Resiliencia);

            // Vitalidade corpórea de Damião. Quem observa o abate é o PlayerDeathController;
            // aqui só se resolve a referência.
            _vitalidadeDamiao = player != null
                ? player.GetComponent<VitalidadeBridge>()
                : FindAnyObjectByType<VitalidadeBridge>();

            if (_vitalidadeDamiao != null)
            {
                // O HUD é dono das suas views: repassa a POCO, não a bridge.
                if (hud != null)
                    hud.InjetarVitalidade(_vitalidadeDamiao.Vitalidade);
            }
            else
            {
                // Este aviso estava pendurado no `else` errado no GameManager (no `if` do HUD, em
                // vez de no da bridge): cena sem HUD dizia "sem VitalidadeBridge" mesmo tendo
                // uma, e cena sem bridge não dizia nada. Agora ele fala do que checou.
                Debug.LogWarning("[GameLoopBootstrap] Nenhuma VitalidadeBridge na cena; Damião " +
                                 "não terá vitalidade corpórea nem morte física.", this);
            }

            // Abdul: se a conversa/luta já foi resolvida numa visita anterior, reconstrói o estado
            // dele (poupado ou derrotado) antes que o jogador consiga interagir. Inclui inativos
            // porque um Abdul derrotado volta da cena já com SetActive(false).
            if (player != null)
            {
                var abdul = FindAnyObjectByType<AbdulAlhazredAI>(FindObjectsInactive.Include);
                if (abdul != null) abdul.AplicarEstadoSalvo(player.gameObject);
            }

            // Inventário: o InventoryManager nasce sozinho, antes de qualquer cena
            // ([RuntimeInitializeOnLoadMethod(BeforeSceneLoad)] a partir de
            // Resources/InventoryManager.prefab, com DontDestroyOnLoad). O bootstrap não o cria —
            // só o ENTREGA a quem precisa. A BarraDeItens deixou de alcançá-lo sozinha na Fase 4
            // (2026-08-18): buscava o singleton em 5 pontos, um deles dentro do Update.
            if (hud != null)
                hud.InjetarInventario(FavelaAmarela.Inventario.InventoryManager.Instance);

            // Yug-Neth é companheiro, não obstáculo: o corpo dele não pode barrar a passagem de
            // Damião (relatado em playtest — ele entalava o jogador na arena). Feito aqui porque
            // é onde o colisor do jogador já é conhecido; resolver por layer exigiria uma camada
            // nova fora da taxonomia fechada.
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

            TempestadeAmbiente = FindAnyObjectByType<TempestadeAmbiente>();
            if (TempestadeAmbiente != null)
                TempestadeAmbiente.Bind(Environment);

            var tempestadeOverlay = FindAnyObjectByType<TempestadeVisualOverlay>();
            if (tempestadeOverlay != null)
                tempestadeOverlay.Bind(Environment);

            // Inimigos sound-first: eles não buscam mais o manager sozinhos no próprio OnEnable
            // (seguem o mesmo padrão de Bind() do PlayerMovement). Inclui inativos: um inimigo de
            // emboscada (começa desligado, ligado depois por trigger) já nasce com o serviço
            // injetado, e seu OnEnable o encontra ao ativar. A injeção é ordem-independente —
            // cada inimigo recebe o mesmo serviço, sem índice nem acumulação entre iterações.
            foreach (var perception in FindObjectsByType<EnemyPerception>(FindObjectsInactive.Include))
                perception.Bind(SoundBroadcaster);

            foreach (var coisa in FindObjectsByType<CoisaDoCemiterioAI>(FindObjectsInactive.Include))
                coisa.Bind(SoundBroadcaster);
        }

        /// <summary>
        /// Liga os componentes focados que vivem no mesmo GameObject. Cada um é opcional: uma
        /// cena que não os tenha perde a função correspondente, mas não quebra o bootstrap.
        /// </summary>
        private void InjetarNosComponentesFocados()
        {
            var presenter = GetComponent<GameStatePresenter>();
            if (presenter != null) presenter.Bind(StateMachine);
            else
                Debug.LogWarning("[GameLoopBootstrap] Sem GameStatePresenter no mesmo GameObject: " +
                                 "pausar não vai congelar o tempo nem mostrar a tela.", this);

            var morte = GetComponent<PlayerDeathController>();
            if (morte != null) morte.Bind(StateMachine, Resiliencia, _vitalidadeDamiao);
            else
                Debug.LogError("[GameLoopBootstrap] Sem PlayerDeathController no mesmo GameObject: " +
                               "Damião não morre — nem por Colapso mental nem por abate físico.",
                               this);

            var cutscene = GetComponent<CutsceneController>();
            if (cutscene != null) cutscene.Bind(_vitalidadeDamiao);

            var pausa = GetComponent<PausaInputHandler>();
            if (pausa != null) pausa.Bind(StateMachine);

            // CompanionManager não recebe Bind: Yug-Neth se registra sob demanda, quando
            // libertado. Ver CompanionManager.RegistrarYugNeth.
        }
    }
}
