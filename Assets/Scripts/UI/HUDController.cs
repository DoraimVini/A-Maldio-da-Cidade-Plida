using FavelaAmarela.Core.Combat;
using UnityEngine;

namespace FavelaAmarela.Runtime.UI
{
    /// <summary>
    /// Camada Runtime (MonoBehaviour). Dono do ciclo de vida da UI de HUD e
    /// ponto de injeção da <see cref="ResilienciaMental"/> nas views que a
    /// consomem (a <see cref="ResilienciaBar"/>, futuramente a barra de
    /// Ectoplasma, etc).
    ///
    /// Como a ResilienciaMental é POCO (não vive na cena), alguém em Runtime
    /// precisa instanciá-la e distribuí-la. Este controller é esse ponto.
    /// Numa arquitetura maior, a POCO viria de um sistema de save/entidade e
    /// seria apenas repassada aqui — o método InjetarResiliencia cobre os dois
    /// casos (criar local para teste, ou receber de fora).
    /// </summary>
    [AddComponentMenu("FavelaAmarela/UI/HUD Controller")]
    public sealed class HUDController : MonoBehaviour
    {
        [Header("Views de HUD")]
        [SerializeField] private ResilienciaBar resilienciaBar;

        [Tooltip("Barra do Vigor (Estamina). Alimentada pelo GameManager a partir do Player.")]
        [SerializeField] private VigorBar vigorBar;

        [Tooltip("Barra da Vitalidade corpórea (a 'carne'). Alimentada pelo GameManager.")]
        [SerializeField] private VitalidadeBar vitalidadeBar;

        [Tooltip("Barra de ações da Mão Física (arma empunhada + habilidade). Alimentada pelo GameManager.")]
        [SerializeField] private BarraDeAcoes barraDeAcoes;

        [Tooltip("Barra com as 8 posições do inventário (teclas 1–8). Alimentada pelo GameManager.")]
        [SerializeField] private BarraDeItens barraDeItens;

        [Tooltip("Barra dos 4 Artefatos equipados (teclas F1–F4). Alimentada pelo GameManager.")]
        [SerializeField] private BarraDeArtefatos barraDeArtefatos;

        [Tooltip("Barra da Resiliência do Companheiro (Yug-Neth). Nasce oculta: só aparece " +
                 "quando ele é libertado no meio do jogo.")]
        [SerializeField] private CompanheiroBar companheiroBar;

        [Header("Telas de fluxo (vivem no prefab persistente)")]
        [Tooltip("Overlay do menu de pause. Quem o liga/desliga é o GameStatePresenter, " +
                 "que recebe esta referência do GameLoopBootstrap em runtime.")]
        [SerializeField] private GameObject telaPause;

        [Tooltip("Sequência de Colapso (tela de morte). Entregue ao PlayerDeathController " +
                 "pelo GameLoopBootstrap em runtime.")]
        [SerializeField] private FavelaAmarela.Runtime.GameLoop.SequenciaDeColapso sequenciaColapso;

        /// <summary>
        /// Overlay de pause. Vive no prefab persistente, então <b>não</b> pode ser ligado por
        /// serialização a um componente de cena — o <c>GameLoopBootstrap</c> o entrega ao
        /// <c>GameStatePresenter</c> a cada cena carregada.
        /// </summary>
        public GameObject TelaPause => telaPause;

        /// <summary>
        /// Sequência de Colapso. Mesmo motivo da <see cref="TelaPause"/>: a ligação é feita em
        /// runtime, não gravada na cena.
        /// </summary>
        public FavelaAmarela.Runtime.GameLoop.SequenciaDeColapso SequenciaColapso => sequenciaColapso;

        /// <summary>
        /// Entrega as telas de fluxo. Usado pelo montador do prefab; em runtime elas já vêm
        /// serializadas de dentro do próprio prefab.
        /// </summary>
        public void DefinirTelasDeFluxo(GameObject pause,
                                        FavelaAmarela.Runtime.GameLoop.SequenciaDeColapso colapso)
        {
            telaPause = pause;
            sequenciaColapso = colapso;
        }

        [Header("Config inicial (usado se nenhuma fonte for injetada de fora)")]
        [Tooltip("Resiliência máxima inicial de Damião.")]
        [SerializeField] private float resilienciaMax = 100f;

        [Tooltip("Fração do máximo abaixo da qual o Pânico ativa (0..1).")]
        [Range(0f, 0.99f)]
        [SerializeField] private float fracaoThresholdPanico = 0.25f;

        private ResilienciaMental _resiliencia;
        private Vitalidade _vitalidade;

        /// <summary>Instância corrente. Null antes de Awake/injeção.</summary>
        public ResilienciaMental Resiliencia => _resiliencia;

        /// <summary>Vitalidade corpórea corrente. Null até o GameManager injetar.</summary>
        public Vitalidade Vitalidade => _vitalidade;

        /// <summary>
        /// A instância viva do HUD. Existe uma só, criada antes da primeira cena e mantida
        /// entre elas.
        /// </summary>
        public static HUDController Instancia { get; private set; }

        /// <summary>
        /// Cria o HUD <b>uma vez</b>, antes de qualquer cena carregar, a partir de
        /// <c>Resources/HUD_Gameplay</c>.
        ///
        /// <para><b>Por que persistente (Bloco 6):</b> o HUD não muda entre cenas e não tinha
        /// por que nascer cinco vezes. Enquanto era montado por cena, ele era mais uma das
        /// listas de cenas escritas à mão que envelhecem neste projeto — já foram <b>oito</b> —
        /// e instância por cena ainda aceita <i>override</i>, então um ajuste numa cena
        /// divergia das outras em silêncio.</para>
        ///
        /// <para>Mesmo padrão de <c>InventoryManager</c>, <c>GerenciadorDeSave</c> e
        /// <c>ProgressionBridge</c>, que já fazem isto aqui. Não é arquitetura nova.</para>
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void GarantirInstancia()
        {
            if (Instancia != null) return;

            var prefab = Resources.Load<GameObject>("HUD_Gameplay");
            if (prefab == null)
            {
                Debug.LogError("[HUDController] 'Resources/HUD_Gameplay' não encontrado — o " +
                               "jogo roda sem HUD nenhum. Conserto: " +
                               "'Tools/FavelaAmarela/HUD: extrair para prefab persistente'.");
                return;
            }

            var obj = Instantiate(prefab);
            obj.name = prefab.name;   // sem o "(Clone)"
            DontDestroyOnLoad(obj);
        }

        private void Awake()
        {
            // Guarda de duplicata: recarregar uma cena com DontDestroyOnLoad ativo criaria um
            // segundo HUD por cima do primeiro. Mesmo guarda do InventoryManager.
            if (Instancia != null && Instancia != this)
            {
                Destroy(gameObject);
                return;
            }

            Instancia = this;

            _canvas = GetComponent<Canvas>();

            // Some a cada troca de cena e só reaparece quando um GameLoopBootstrap o reivindica
            // (ver Revelar). É isso que mantém o HUD fora do menu principal sem precisar de uma
            // lista de "cenas que têm HUD" — mais uma lista para envelhecer.
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += HandleCenaCarregada;
            UnityEngine.SceneManagement.SceneManager.sceneUnloaded += HandleCenaDescarregada;

            // Se ninguém injetou uma fonte externa até aqui, cria uma local.
            // Facilita testar a cena de HUD isolada, sem o sistema de entidade.
            if (_resiliencia == null)
            {
                _resiliencia = ResilienciaMental.ComThresholdFracional(
                    resilienciaMax, fracaoThresholdPanico);
            }

            if (resilienciaBar != null)
                resilienciaBar.Bind(_resiliencia);
        }

        private Canvas _canvas;

        private void OnDestroy()
        {
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= HandleCenaCarregada;
            UnityEngine.SceneManagement.SceneManager.sceneUnloaded -= HandleCenaDescarregada;
            if (Instancia == this) Instancia = null;
        }

        /// <summary>
        /// Se o <c>GameLoopBootstrap</c> desta cena já reivindicou o HUD.
        ///
        /// <para><b>O bug que este campo conserta (2026-08-27).</b> O handler de carga chamava
        /// <c>Ocultar()</c> <b>incondicionalmente</b>, e o <c>GameLoopBootstrap</c> chamava
        /// <c>Revelar()</c> no <c>Awake</c> dele. Só que <c>sceneLoaded</c> dispara <b>depois de
        /// todos os Awake</b> — então o HUD era revelado e ocultado em seguida, em <b>toda</b>
        /// carga de cena. <b>O HUD nunca apareceu desde a migração para prefab persistente.</b></para>
        ///
        /// <para>Os testes daquela migração verificavam que <c>Revelar()</c> existia e era
        /// chamado. Nenhum verificava a <b>ordem</b> — medir presença em vez de correção, que é
        /// o erro que este repositório mais produz.</para>
        /// </summary>
        private bool _reivindicadoNestaCena;

        /// <summary>
        /// Zera a reivindicação ao sair de uma cena. Numa carga <c>Single</c> a ordem é
        /// <c>sceneUnloaded</c> → <c>Awake</c> da cena nova → <c>sceneLoaded</c>, então quando o
        /// handler de carga roda o campo já reflete a cena que está entrando.
        /// </summary>
        private void HandleCenaDescarregada(UnityEngine.SceneManagement.Scene cena)
            => _reivindicadoNestaCena = false;

        /// <summary>
        /// Oculta o HUD <b>só</b> se ninguém o reivindicou nesta cena. É o que o mantém fora do
        /// menu principal — uma cena sem <c>GameLoopBootstrap</c> — sem precisar de uma lista
        /// de "cenas que têm HUD", que seria mais uma lista para envelhecer.
        /// </summary>
        private void HandleCenaCarregada(UnityEngine.SceneManagement.Scene cena,
                                         UnityEngine.SceneManagement.LoadSceneMode modo)
        {
            if (_reivindicadoNestaCena) return;

            Ocultar();
        }

        /// <summary>
        /// Esconde o HUD. Chamado a cada troca de cena: o padrão é <b>invisível</b>, e quem
        /// mostra é o bootstrap da cena de jogo.
        ///
        /// <para>Desliga o <c>Canvas</c>, não o <c>GameObject</c>: desativar o objeto pararia o
        /// <c>Update</c> das views e o próprio <c>SceneManager.sceneLoaded</c> deste
        /// componente, e o HUD nunca mais voltaria.</para>
        /// </summary>
        public void Ocultar()
        {
            if (_canvas != null) _canvas.enabled = false;
        }

        /// <summary>
        /// Mostra o HUD. Chamado pelo <c>GameLoopBootstrap</c> depois de ligar as fontes —
        /// então ele só aparece onde há mundo de jogo, e nunca no menu principal.
        /// </summary>
        public void Revelar()
        {
            // Reivindica a cena ANTES de mostrar: o handler de sceneLoaded roda depois dos
            // Awake e leria este campo para decidir se desfaz.
            _reivindicadoNestaCena = true;

            if (_canvas != null) _canvas.enabled = true;
        }

        /// <summary>
        /// Injeta uma ResilienciaMental criada por outro sistema (entidade de
        /// Damião, save game). Deve ser chamado antes de Awake para substituir
        /// a instância local, ou a qualquer momento para re-bind em runtime.
        /// </summary>
        public void InjetarResiliencia(ResilienciaMental fonte)
        {
            if (fonte == null) return;
            _resiliencia = fonte;
            if (resilienciaBar != null)
                resilienciaBar.Bind(_resiliencia);
        }

        /// <summary>
        /// Injeta a <see cref="Vitalidade"/> corpórea de Damião (criada pela
        /// <c>VitalidadeBridge</c> a partir da ficha de atributos e repassada pelo
        /// <c>GameManager</c> no bootstrap). Diferente da Resiliência, o HUD não cria uma
        /// local de fallback: a Vitalidade pertence ao ator na cena, não ao HUD.
        /// </summary>
        public void InjetarVitalidade(Vitalidade fonte)
        {
            if (fonte == null)
            {
                Debug.LogError("[HUDController] InjetarVitalidade recebeu null — a barra de " +
                               "Vitalidade vai ficar parada. Provável ordem de Awake: a " +
                               "VitalidadeBridge ainda não tinha criado a POCO.", this);
                return;
            }

            _vitalidade = fonte;

            if (vitalidadeBar != null)
                vitalidadeBar.Bind(_vitalidade);
            else
                Debug.LogError("[HUDController] Campo 'vitalidadeBar' vazio — a Vitalidade foi " +
                               "injetada mas não há barra ligada para mostrá-la.", this);
        }

        /// <summary>
        /// Injeta a Mão Física de Damião na barra de ações, para o HUD mostrar a arma
        /// empunhada e a recarga da habilidade. Chamado pelo <c>GameManager</c> no bootstrap.
        /// </summary>
        public void InjetarMaoFisica(FavelaAmarela.Player.MaoFisicaBridge fonte)
        {
            if (fonte == null) return;
            if (barraDeAcoes != null)
                barraDeAcoes.Bind(fonte);
        }

        /// <summary>
        /// Injeta o inventário na barra de itens (teclas 1–8).
        ///
        /// <para><b>Fase 4, 2026-08-18.</b> O campo <c>barraDeItens</c> já existia aqui, ligado
        /// nas 4 cenas e <b>lido por nenhuma linha de código</b> — referência serializada morta.
        /// A barra se virava sozinha alcançando <c>InventoryManager.Instance</c> em cinco
        /// pontos, um deles dentro do <c>Update</c>. Agora ela recebe a fonte por aqui, como
        /// todas as outras views do HUD.</para>
        /// </summary>
        public void InjetarInventario(FavelaAmarela.Inventario.InventoryManager fonte)
        {
            if (fonte == null) return;

            if (barraDeItens != null)
            {
                barraDeItens.Bind(fonte);
            }
            else
            {
                Debug.LogWarning("[HUDController] Sem 'barraDeItens' ligada — as teclas 1–8 não " +
                                 "vão consumir nem equipar nada, e a barra fica congelada.", this);
            }
        }

        /// <summary>
        /// Injeta o Vigor de Damião na barra correspondente. Chamado pelo <c>GameManager</c>
        /// no bootstrap.
        /// </summary>
        public void InjetarVigor(FavelaAmarela.Player.GerenciadorDeVigor fonte)
        {
            if (fonte == null) return;

            if (vigorBar != null)
            {
                vigorBar.Bind(fonte);
            }
            else
            {
                // Era o único Injetar* que falhava em silêncio — a VigorBar ficou órfã (0
                // cenas, 0 prefabs) sem que nada no console apontasse a causa. Ver
                // Docs/KnowledgeBundle/systems para o histórico (2026-08-13).
                Debug.LogError("[HUDController] Campo 'vigorBar' vazio — o Vigor foi injetado " +
                               "mas não há barra ligada para mostrá-lo.", this);
            }
        }

        /// <summary>
        /// Injeta os Artefatos de Damião na barra de artefatos, para o HUD mostrar os quatro
        /// slots e suas recargas. Chamado pelo <c>GameManager</c> no bootstrap.
        /// </summary>
        public void InjetarArtefatos(FavelaAmarela.Player.ArtefatosBridge fonte)
        {
            if (fonte == null) return;
            if (barraDeArtefatos != null) barraDeArtefatos.Bind(fonte);
        }

        /// <summary>
        /// Revela e liga a barra do companheiro. Chamado quando Yug-Neth é <b>libertado</b>, não
        /// no bootstrap — no arranque ele ainda é cativo e não vale para a run.
        ///
        /// <para>Ativa o objeto <b>antes</b> de ligar: a barra nasce desativada na cena de
        /// propósito (uma barra vazia no HUD desde o menu anunciaria um recurso que o jogador
        /// ainda não tem, e leria como recurso zerado).</para>
        /// </summary>
        public void InjetarCompanheiro(FavelaAmarela.Runtime.Enemies.YugNethAI companheiro)
        {
            if (companheiro == null) return;

            var corpo = companheiro.Vitalidade;
            if (corpo == null)
            {
                Debug.LogError("[HUDController] Yug-Neth registrado sem VitalidadeBridge — a " +
                               "barra do companheiro não teria o que mostrar.", this);
                return;
            }

            if (companheiroBar == null)
            {
                // Mesma falha silenciosa que a VigorBar teve em 2026-08-13: o dado chega, não há
                // view ligada, e nada no console aponta a causa.
                Debug.LogError("[HUDController] Campo 'companheiroBar' vazio — Yug-Neth foi " +
                               "libertado mas não há barra ligada para mostrá-lo. Rode " +
                               "'Tools/FavelaAmarela/Montar HUD Completo'.", this);
                return;
            }

            if (!companheiroBar.gameObject.activeSelf) companheiroBar.gameObject.SetActive(true);
            companheiroBar.Bind(corpo.Vitalidade);
        }

        /// <summary>
        /// Esconde a barra do companheiro. Chamado quando Yug-Neth se <b>aposenta</b> — ao entrar
        /// no Castelo ele vira NPC e deixa de ser companheiro da run.
        ///
        /// <para><c>Unbind</c> antes de esconder: um <c>GameObject</c> desativado não roda
        /// <c>OnDisable</c> de novo, então a barra ficaria assinada na Vitalidade de alguém que
        /// não é mais companheiro — um assinante fantasma que só apareceria como vazamento.</para>
        /// </summary>
        public void RetirarCompanheiro()
        {
            if (companheiroBar == null) return;

            companheiroBar.Unbind();
            companheiroBar.gameObject.SetActive(false);
        }

        // ── Atalhos de teste (removíveis) ────────────────────────────────────
        // Facilitam validar a barra no editor sem um sistema de combate real.
        // Marcados com ContextMenu para uso manual no Inspector.

        [ContextMenu("Teste — Sofrer 30 de trauma")]
        private void TesteTrauma() => _resiliencia?.SofrerTrauma(30f);

        [ContextMenu("Teste — Ancorar 20")]
        private void TesteAncora() => _resiliencia?.Ancorar(20f);

        [ContextMenu("Teste — Forçar colapso")]
        private void TesteColapso() => _resiliencia?.ForcarColapso();
    }
}
