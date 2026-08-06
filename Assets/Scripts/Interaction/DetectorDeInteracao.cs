using System;
using UnityEngine;
using UnityEngine.InputSystem;
using FavelaAmarela.Core.Interaction;

namespace FavelaAmarela.Runtime.Interaction
{
    /// <summary>
    /// Componente do Damião que descobre o que está ao alcance, escolhe o melhor alvo e
    /// dispara a interação no botão (ação <c>Interact</c> — E no teclado, botão Norte no
    /// gamepad).
    ///
    /// <para>Divisão de responsabilidade: a Unity responde "quem está por perto?"
    /// (<c>OverlapCircle</c>); o POCO <see cref="SeletorDeInteracao"/> responde "qual
    /// deles vale?". Nenhuma regra de escolha mora aqui.</para>
    ///
    /// <para>Expõe <see cref="OnAlvoMudou"/> para a UI mostrar o prompt ("Pressione E —
    /// Abrir o baú") sem polling, seguindo a Regra de Ouro 8.</para>
    /// </summary>
    [AddComponentMenu("Favela Amarela/Detector de Interação")]
    public sealed class DetectorDeInteracao : MonoBehaviour
    {
        [Header("Alcance")]
        [Tooltip("Distância máxima para usar um objeto (unidades de mundo).")]
        [SerializeField] private float alcance = 1.5f;

        [Tooltip("Camadas onde procurar objetos interagíveis. Vazio = todas.")]
        [SerializeField] private LayerMask camadasInteragiveis;

        // Buffers pré-alocados: o detector roda a cada frame e não pode gerar lixo
        // (Regra de Ouro 1). 8 slots cobrem folgadamente os alvos ao alcance.
        private const int MaxCandidatos = 8;
        private readonly Collider2D[] _hits = new Collider2D[MaxCandidatos];
        private readonly IInteragivel[] _componentes = new IInteragivel[MaxCandidatos];
        private CandidatoDeInteracao[] _candidatos;
        private ContactFilter2D _filtro;

        private SeletorDeInteracao _seletor;
        private InputAction _acaoInteragir;
        private IInteragivel _alvoAtual;

        /// <summary>
        /// Enquanto <c>true</c>, ignora o botão de interação por completo. Ligado por
        /// <see cref="FavelaAmarela.Runtime.UI.PainelDeEscolha"/> enquanto uma escolha está
        /// aberta — sem isto, os dois componentes leem o mesmo aperto de E no mesmo frame:
        /// o mesmo botão que confirma uma opção também reabre a conversa com o NPC (que
        /// ainda está ao alcance), recriando o painel com o cursor de volta no índice 0 e
        /// auto-confirmando a opção errada antes do jogador conseguir navegar. Mesmo padrão
        /// de <c>PlayerMovement.MovimentoBloqueado</c>, aplicado à interação em vez do
        /// movimento.
        /// </summary>
        public bool Bloqueado { get; set; }

        /// <summary>
        /// Disparado quando o alvo sob a mira muda (inclusive para <c>null</c> ao sair de
        /// perto). A UI de prompt observa isto.
        /// </summary>
        public event Action<IInteragivel> OnAlvoMudou;

        /// <summary>Alvo que seria usado se o botão fosse apertado agora. Null se não há.</summary>
        public IInteragivel AlvoAtual => _alvoAtual;

        private void Awake()
        {
            _seletor = new SeletorDeInteracao(alcance);
            _candidatos = new CandidatoDeInteracao[MaxCandidatos];

            _filtro = new ContactFilter2D();
            _filtro.useTriggers = true;
            // Sem máscara definida no Inspector, procura em todas as camadas: é melhor
            // achar demais (o filtro real é ter IInteragivel) do que não achar nada.
            if (camadasInteragiveis.value != 0) _filtro.SetLayerMask(camadasInteragiveis);

            var playerInput = GetComponent<PlayerInput>();
            if (playerInput != null)
            {
                _acaoInteragir = playerInput.actions.FindAction("Interact");
                if (_acaoInteragir == null)
                    Debug.LogError("[DetectorDeInteracao] Ação 'Interact' não encontrada no " +
                                   "asset de Input Actions — o botão de interação não funcionará.", this);
            }
            else
            {
                Debug.LogError("[DetectorDeInteracao] Sem PlayerInput no mesmo GameObject; " +
                               "interação desativada.", this);
            }
        }

        private void Update()
        {
            if (Bloqueado) return;

            AtualizarAlvo();

            if (_alvoAtual == null || _acaoInteragir == null) return;

            if (_acaoInteragir.WasPressedThisFrame())
            {
                // Revalida: o alvo pode ter ficado indisponível entre a mira e o aperto.
                if (_alvoAtual.PodeInteragir)
                    _alvoAtual.Interagir(gameObject);
            }
        }

        /// <summary>
        /// Varre a vizinhança, monta os candidatos e pergunta ao POCO qual vence.
        /// Notifica <see cref="OnAlvoMudou"/> só quando o alvo realmente muda.
        /// </summary>
        private void AtualizarAlvo()
        {
            int total = Physics2D.OverlapCircle(transform.position, alcance, _filtro, _hits);

            int quantidade = 0;
            Vector2 minhaPosicao = transform.position;

            for (int i = 0; i < total && quantidade < MaxCandidatos; i++)
            {
                var interagivel = _hits[i].GetComponentInParent<IInteragivel>();
                if (interagivel == null) continue;

                float distancia = Vector2.Distance(minhaPosicao, interagivel.PosicaoDeInteracao);

                _componentes[quantidade] = interagivel;
                _candidatos[quantidade] = new CandidatoDeInteracao(
                    id: quantidade, // índice no buffer: o Core devolve isto e mapeamos de volta
                    distancia: distancia,
                    disponivel: interagivel.PodeInteragir,
                    prioridade: interagivel.PrioridadeDeInteracao);
                quantidade++;
            }

            int? escolhido = _seletor.Selecionar(_candidatos, quantidade);
            var novoAlvo = escolhido.HasValue ? _componentes[escolhido.Value] : null;

            if (!ReferenceEquals(novoAlvo, _alvoAtual))
            {
                _alvoAtual = novoAlvo;
                OnAlvoMudou?.Invoke(novoAlvo);
            }

            // Limpa as referências do buffer para não segurar objetos destruídos vivos.
            for (int i = 0; i < quantidade; i++) _componentes[i] = null;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.3f, 0.9f, 1f, 0.5f);
            Gizmos.DrawWireSphere(transform.position, alcance);
        }
    }
}
