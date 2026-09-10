using UnityEngine;
using UnityEngine.UI;

namespace FavelaAmarela.Runtime.UI
{
    /// <summary>
    /// A tela de <b>Créditos</b>: quem fez o jogo e de quem é cada arte.
    ///
    /// <para><b>Por que existe (2026-09-10).</b> Não é cortesia: é <b>condição de licença</b>. O
    /// Sucart (Rei em Amarelo) escreveu <i>"as long as you credit me using the name Sucart"</i>; o
    /// Warren Clark (Damião, Abdul) autorizou o uso a quem lhe desse crédito; Kenney, CraftPix e
    /// Hypnobius pedem crédito sem exigir. Uma build sem esta tela viola a primeira e falta com
    /// as outras — e a submissão do edital é uma build.</para>
    ///
    /// <para><b>O texto é um <c>TextAsset</c></b> (<c>Resources/CREDITOS.txt</c>), não código: o
    /// Vini edita um .txt e a tela muda. Mesmo desenho do <see cref="PainelDeOpcoes"/>: nasce
    /// sozinha, persiste, toma o foco enquanto aberta, fecha por botão ou Esc.</para>
    /// </summary>
    [AddComponentMenu("Favela Amarela/UI/Painel de Créditos")]
    public sealed class PainelDeCreditos : MonoBehaviour
    {
        private static PainelDeCreditos _instancia;

        /// <summary>Instância única. Nula fora de Play.</summary>
        public static PainelDeCreditos Instancia => _instancia;

        /// <summary>Nome do <c>TextAsset</c> em <c>Resources</c> com o texto dos créditos.</summary>
        public const string NomeDoTexto = "CREDITOS";

        [Header("Raiz")]
        [Tooltip("O objeto que é ligado e desligado. Vazio = este mesmo GameObject.")]
        [SerializeField] private GameObject conteudo;

        [Header("Controles")]
        [Tooltip("Onde o texto dos créditos é escrito. [ASSET]")]
        [SerializeField] private Text corpo;

        [Tooltip("Rolagem do texto — volta ao topo a cada abertura. [ASSET]")]
        [SerializeField] private ScrollRect rolagem;

        [SerializeField] private Button botaoDeFechar;

        [Header("Texto")]
        [Tooltip("Texto dos créditos. Vazio = carrega Resources/CREDITOS.txt.")]
        [SerializeField] private TextAsset texto;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void GarantirInstancia()
        {
            if (_instancia != null) return;

            var prefab = Resources.Load<GameObject>("Painel_Creditos");
            if (prefab == null)
            {
                Debug.LogError("[PainelDeCreditos] 'Resources/Painel_Creditos' não encontrado — o " +
                               "jogo roda sem tela de créditos, e o Sucart exige crédito. Conserto: " +
                               "'Tools/FavelaAmarela/UI: montar o painel de créditos'.");
                return;
            }

            var obj = Instantiate(prefab);
            obj.name = prefab.name;
            DontDestroyOnLoad(obj);
        }

        private void Awake()
        {
            if (_instancia != null && _instancia != this) { Destroy(gameObject); return; }

            _instancia = this;
            DontDestroyOnLoad(gameObject);

            if (texto == null) texto = Resources.Load<TextAsset>(NomeDoTexto);
            if (texto == null)
                Debug.LogError($"[PainelDeCreditos] 'Resources/{NomeDoTexto}.txt' não encontrado — a " +
                               "tela de créditos abriria vazia.", this);

            if (corpo != null) corpo.text = texto != null ? texto.text : "";
            else Debug.LogError("[PainelDeCreditos] Sem Text ligado em 'corpo' — não há onde escrever.", this);

            if (botaoDeFechar != null) botaoDeFechar.onClick.AddListener(Fechar);

            // Nasce fechada, direto na raiz: Fechar() devolveria um foco que ninguém tomou.
            Raiz.SetActive(false);
        }

        private void OnDestroy()
        {
            if (_instancia == this) _instancia = null;
        }

        private GameObject Raiz => conteudo != null ? conteudo : gameObject;

        /// <summary>Se está aberta agora.</summary>
        public bool EstaAberta => Raiz.activeSelf;

        /// <summary>O texto que está sendo mostrado (para os guardas conferirem os nomes).</summary>
        public string Texto => corpo != null ? corpo.text : "";

        /// <summary>Abre a tela, com a rolagem no topo, e toma o comando do teclado.</summary>
        public void Abrir()
        {
            if (EstaAberta) return;

            Raiz.SetActive(true);
            if (rolagem != null) rolagem.verticalNormalizedPosition = 1f;

            FavelaAmarela.Runtime.Entrada.ArbitroDeFoco.Tomar(
                FavelaAmarela.Core.Entrada.CamadaDeEntrada.PainelModal);
        }

        /// <summary>Fecha a tela e devolve o comando.</summary>
        public void Fechar()
        {
            if (!EstaAberta) return;

            Raiz.SetActive(false);
            FavelaAmarela.Runtime.Entrada.ArbitroDeFoco.Devolver(
                FavelaAmarela.Core.Entrada.CamadaDeEntrada.PainelModal);
        }

        /// <summary>Abre de qualquer lugar, se existir.</summary>
        public static void AbrirSeExistir()
        {
            if (_instancia != null) _instancia.Abrir();
            else Debug.LogWarning("[PainelDeCreditos] Nenhuma instância — o botão de Créditos não " +
                                  "tem o que abrir.");
        }

        /// <summary>Se o Esc deste quadro foi gasto fechando a tela (ver <see cref="PainelDeOpcoes"/>).</summary>
        public bool ConsumiuEscNesteQuadro => _quadroDoEsc == Time.frameCount;

        private int _quadroDoEsc = -1;

        private void Update()
        {
            if (!EstaAberta) return;

            var teclado = UnityEngine.InputSystem.Keyboard.current;
            if (teclado == null || !teclado.escapeKey.wasPressedThisFrame) return;

            _quadroDoEsc = Time.frameCount;
            Fechar();
        }
    }
}
