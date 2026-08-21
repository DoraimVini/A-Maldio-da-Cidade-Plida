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
        private ResilienciaBridge _menteDamiao;

        // Guardado, e não rebuscado depois: a barra do companheiro é ligada em InjetarNos-
        // ComponentesFocados e também por evento em runtime, muito depois do Awake. Um
        // FindAnyObjectByType a cada registro seria busca global fora de hot path, mas ainda
        // assim redundante — o HUD já foi achado uma vez.
        private HUDController _hud;

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
            _hud = hud;
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

            // Golpe e habilidade de arma. Os dois sons existiam em SomDoJogo e em SinteseDeSom,
            // mas ninguém os disparava: atacar um chefe não produzia som nenhum. Era metade do
            // "combate sem feel" relatado no playtest do Byakhee (a outra metade era o próprio
            // Byakhee estar sem AudioDeCombate).
            var audioDoJogador = FindAnyObjectByType<AudioDoJogador>();
            if (audioDoJogador != null && player != null)
            {
                var maoParaOAudio = player.GetComponent<MaoFisicaBridge>();
                if (maoParaOAudio != null) audioDoJogador.Bind(maoParaOAudio);
            }
            else if (audioDoJogador == null)
            {
                Debug.LogWarning("[GameLoopBootstrap] Nenhum AudioDoJogador na cena; os golpes " +
                                 "de Damião saem mudos.", this);
            }

            // Vitalidade corpórea de Damião. Quem observa o abate é o PlayerDeathController;
            // aqui só se resolve a referência.
            _vitalidadeDamiao = player != null
                ? player.GetComponent<VitalidadeBridge>()
                : FindAnyObjectByType<VitalidadeBridge>();

            // A contraparte mental: sem esta bridge, tudo que fere a mente de Damião precisaria
            // de um global (era o caso dos 19 call-sites de GameManager.Instance.Resiliencia).
            // Fica no Damião de propósito — quem o atinge já tem o collider dele em mãos.
            _menteDamiao = player != null
                ? player.GetComponent<ResilienciaBridge>()
                : FindAnyObjectByType<ResilienciaBridge>();

            if (_menteDamiao != null)
                _menteDamiao.Bind(Resiliencia);
            else
                Debug.LogError("[GameLoopBootstrap] Sem ResilienciaBridge no Damião — nada que " +
                               "fere a mente (Cone de Gelo, Coisa do Cemitério, zonas de pressão) " +
                               "vai ter efeito, e em silêncio.", this);

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
            if (cutscene != null) cutscene.Bind(_vitalidadeDamiao, _menteDamiao);

            var pausa = GetComponent<PausaInputHandler>();
            if (pausa != null) pausa.Bind(StateMachine);

            // CompanionManager não recebe Bind: Yug-Neth se registra sob demanda, quando
            // libertado. Ver CompanionManager.RegistrarYugNeth.
            var companheiro = GetComponent<CompanionManager>();

            LigarBarraDoCompanheiro(companheiro);

            InjetarNosConsumidoresDaCena(cutscene, companheiro);
        }

        /// <summary>
        /// Faz a barra do companheiro aparecer no HUD quando Yug-Neth é libertado.
        ///
        /// <para><b>Os dois caminhos são necessários.</b> Assinar o evento cobre a libertação
        /// que acontece durante esta cena. Ligar na hora cobre o caso de o companheiro <b>já</b>
        /// estar registrado quando o bootstrap roda — que é o que acontece ao trocar de cena
        /// depois de libertá-lo, ou ao carregar um save. Só o evento deixaria a barra sumir na
        /// primeira transição de cena depois da Tumba; só a ligação imediata nunca a mostraria
        /// na cena em que ele é solto.</para>
        /// </summary>
        private void LigarBarraDoCompanheiro(CompanionManager companheiro)
        {
            if (companheiro == null || _hud == null) return;

            companheiro.OnCompanheiroRegistrado += HandleCompanheiroRegistrado;
            companheiro.OnCompanheiroAposentado += HandleCompanheiroAposentado;

            if (companheiro.YugNeth != null) _hud.InjetarCompanheiro(companheiro.YugNeth);
        }

        // Método nomeado, não lambda: '-=' com um lambda diferente do usado no '+=' nunca
        // desassina, e esse bug já existe em GerenciadorEfeitosPassivos.
        private void HandleCompanheiroRegistrado(FavelaAmarela.Runtime.Enemies.YugNethAI yugNeth)
        {
            if (_hud != null) _hud.InjetarCompanheiro(yugNeth);
        }

        private void HandleCompanheiroAposentado()
        {
            if (_hud != null) _hud.RetirarCompanheiro();
        }

        private void OnDestroy()
        {
            var companheiro = GetComponent<CompanionManager>();
            if (companheiro != null)
            {
                companheiro.OnCompanheiroRegistrado -= HandleCompanheiroRegistrado;
                companheiro.OnCompanheiroAposentado -= HandleCompanheiroAposentado;
            }
        }

        /// <summary>
        /// Entrega as dependências aos consumidores espalhados pela cena — gatilhos, UI e o Abdul.
        ///
        /// <para><b>Fase 5, 2026-08-18.</b> Estes seis alcançavam <c>GameManager.Instance</c>
        /// diretamente. Injetá-los aqui é o que permite removê-los do encaminhamento
        /// <c>[Obsolete]</c>. Inclui inativos: gatilhos de set-piece costumam nascer desligados e
        /// serem ativados por outro trigger — sem <c>FindObjectsInactive.Include</c> eles
        /// receberiam a injeção nunca.</para>
        ///
        /// <para><b>O que NÃO migrou:</b> os consumidores de <c>.Resiliencia</c> (19 usos em 11
        /// arquivos). Eles têm rodada própria, porque dois deles chamam dentro do <c>Update</c> e
        /// quebram de forma silenciosa se a ordem de bind sair errada — o plano pede testes de
        /// bootstrap escritos antes dessa migração.</para>
        /// </summary>
        private void InjetarNosConsumidoresDaCena(CutsceneController cutscene,
                                                  CompanionManager companheiro)
        {
            foreach (var portao in FindObjectsByType<TransicaoDeFaseTrigger>(
                         FindObjectsInactive.Include))
                portao.Bind(StateMachine);

            foreach (var menu in FindObjectsByType<MenuDePause>(
                         FindObjectsInactive.Include))
                menu.Bind(StateMachine);

            foreach (var retorno in FindObjectsByType<RetornoDoColapso>(
                         FindObjectsInactive.Include))
                retorno.Bind(StateMachine);

            foreach (var travessia in FindObjectsByType<TravessiaDoCompanheiro>(
                         FindObjectsInactive.Include))
                travessia.Bind(companheiro);

            foreach (var queda in FindObjectsByType<QuedaZ4Z5Trigger>(
                         FindObjectsInactive.Include))
                queda.Bind(cutscene, TempestadeAmbiente);

            foreach (var refugio in FindObjectsByType<RefugioDeLuz>(
                         FindObjectsInactive.Include))
                refugio.Bind(companheiro);

            // Abdul já é encontrado em InjetarNoMundo para AplicarEstadoSalvo; aqui recebe o
            // registrador de companheiro, usado quando Yug-Neth é libertado.
            var abdul = FindAnyObjectByType<AbdulAlhazredAI>(FindObjectsInactive.Include);
            if (abdul != null) abdul.BindCompanheiro(companheiro);
        }
    }
}
