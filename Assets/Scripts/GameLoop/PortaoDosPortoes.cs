using UnityEngine;
using FavelaAmarela.Runtime.Interaction;

namespace FavelaAmarela.Runtime.GameLoop
{
    /// <summary>
    /// Camada Runtime. Os <b>Portões das Ruínas</b> em si: duas folhas de pedra que barram a
    /// saída da Fase 1 e que só cedem depois que o Byakhee cai.
    ///
    /// <para><b>Abater não abre — abater destranca.</b> Quem abre é o jogador, encostando e
    /// apertando o botão de interação. A distinção é do pedido do Vini e é melhor design: o
    /// portão abrindo sozinho no instante do abate rouba o gesto do jogador e ainda joga a
    /// transição de fase por cima da animação de morte do chefe. Assim a luta termina, o mundo
    /// respira, e a passagem é uma escolha.</para>
    ///
    /// <para><b>O portão muda de estado sem sumir</b>: um portão que desaparece lê como bug, um
    /// que se acende lê como consequência da luta.</para>
    ///
    /// <para><b>Por que por cor, e não por troca de quadro (2026-08-20):</b> a arte usada é
    /// <c>Entrada_PortoesDeCarcosa</c> — a mesma que já marca os Portões no Deserto de Hali, a
    /// pedido do Vini. Ela existe num quadro só, e nele o portal está <b>aceso</b>: o brilho
    /// dourado no vão é o elemento que domina a silhueta. Então o estado se lê pelo brilho —
    /// apagado enquanto o Byakhee vive, aceso quando ele cai. Tentei derivar um quadro fechado
    /// por edição de pixel e saiu pior que o original: a máscara de dourado não separa o portal
    /// das runas dos pilares nem da pedra quente da plataforma.</para>
    ///
    /// <para>A troca de quadro continua suportada: se um dia existir arte de portão fechado,
    /// basta preencher <c>spriteFechado</c> e ela passa a valer junto com a cor.</para>
    /// </summary>
    [AddComponentMenu("Favela Amarela/Level/Portão dos Portões")]
    public sealed class PortaoDosPortoes : MonoBehaviour, IInteragivel
    {
        [Header("Batente")]
        [Tooltip("O SpriteRenderer do portão. Troca de quadro ao abrir.")]
        [SerializeField] private SpriteRenderer batente;

        [Tooltip("Quadro do portão fechado.")]
        [SerializeField] private Sprite spriteFechado;

        [Tooltip("Quadro do portão escancarado. Opcional: sem ele, o estado se lê pela cor.")]
        [SerializeField] private Sprite spriteAberto;

        [Tooltip("Cor com o portal morto. Escurece e esfria o brilho dourado do vão.")]
        [SerializeField] private Color corFechado = new Color(0.34f, 0.33f, 0.40f);

        [Tooltip("Cor com o portal aceso. Branco = a arte como foi pintada.")]
        [SerializeField] private Color corAberto = Color.white;

        [Tooltip("Segundos entre a interação e o portão aparecer aberto — o peso da pedra.")]
        [Min(0f)]
        [SerializeField] private float duracaoDaAbertura = 0.6f;

        [Header("Passagem")]
        [Tooltip("O PortalDeCena para o Castelo. Nasce desligado; acende com os Portões abertos.")]
        [SerializeField] private GameObject passagemParaOCastelo;

        [Tooltip("O colisor que barra a saída. Desligado ao abrir.")]
        [SerializeField] private Collider2D barreira;

        [Header("Texto")]
        [Tooltip("Rótulo do prompt de interação. Infinitivo, diegético (favela-lore-enforcer).")]
        [SerializeField] private string rotulo = "Abrir os Portões";

        private bool _destrancado;
        private bool _abrindo;
        private bool _aberto;
        private float _tempoDeAbertura;

        /// <summary>Se o Byakhee já caiu e os Portões aceitam ser abertos.</summary>
        public bool Destrancado => _destrancado;

        /// <summary>Se os Portões já estão abertos — a Fase 1 acabou.</summary>
        public bool Aberto => _aberto;

        // ── IInteragivel ──────────────────────────────────────────────────────

        /// <inheritdoc />
        public string RotuloDeInteracao => rotulo;

        /// <summary>
        /// Só aceita interação com o guardião abatido. Enquanto o Byakhee vive, os Portões nem
        /// aparecem no prompt — em vez de oferecerem uma ação que não faria nada.
        /// </summary>
        public bool PodeInteragir => _destrancado && !_aberto && !_abrindo;

        /// <inheritdoc />
        public int PrioridadeDeInteracao => 0;

        /// <inheritdoc />
        public Vector2 PosicaoDeInteracao => transform.position;

        /// <inheritdoc />
        public void Interagir(GameObject quemInterage) => Abrir();

        // ── Ciclo ─────────────────────────────────────────────────────────────

        private void Awake()
        {
            if (batente == null) batente = GetComponentInChildren<SpriteRenderer>();

            if (batente == null)
            {
                Debug.LogError("[PortaoDosPortoes] Sem batente — os Portões abririam sem nada " +
                               "mudar na tela, o que lê como bug.", this);
            }
            else
            {
                if (spriteFechado != null) batente.sprite = spriteFechado;
                batente.color = corFechado;
            }

            if (barreira == null)
            {
                barreira = GetComponent<Collider2D>();
                if (barreira == null)
                    Debug.LogError("[PortaoDosPortoes] Sem colisor de barreira — os Portões " +
                                   "estariam abertos desde o começo.", this);
            }

            if (passagemParaOCastelo == null)
                Debug.LogError("[PortaoDosPortoes] Sem passagem para o Castelo — abrir os " +
                               "Portões não levaria a lugar nenhum.", this);
            else
                passagemParaOCastelo.SetActive(false);
        }

        /// <summary>
        /// Libera a interação. Chamado pelo gatilho da arena quando o Byakhee é abatido — o
        /// <c>ByakheeAI</c> não abre os Portões sozinho de propósito.
        /// </summary>
        public void Destrancar()
        {
            if (_destrancado) return;
            _destrancado = true;

            Debug.Log("[PortaoDosPortoes] O Byakhee caiu — os Portões cederam. Aperte para abrir.",
                      this);
        }

        /// <summary>
        /// Abre de fato. Público para o Carcosa Debugger e para uma cutscene poderem destravar a
        /// passagem sem exigir a luta inteira a cada teste.
        /// </summary>
        public void Abrir()
        {
            if (_abrindo || _aberto) return;

            _abrindo = true;
            _tempoDeAbertura = 0f;

            // O quadro troca já — o atraso é só para a passagem acender depois, dando um
            // instante de leitura. A barreira cai junto: esperar faria o jogador esbarrar num
            // portão visivelmente aberto.
            if (batente != null)
            {
                if (spriteAberto != null) batente.sprite = spriteAberto;
                batente.color = corAberto;
            }

            if (barreira != null) barreira.enabled = false;
        }

        private void Update()
        {
            if (!_abrindo) return;

            _tempoDeAbertura += Time.deltaTime;
            if (_tempoDeAbertura < duracaoDaAbertura) return;

            _abrindo = false;
            _aberto = true;

            if (passagemParaOCastelo != null) passagemParaOCastelo.SetActive(true);

            Debug.Log("[PortaoDosPortoes] Os Portões das Ruínas estão abertos.", this);
        }
    }
}
