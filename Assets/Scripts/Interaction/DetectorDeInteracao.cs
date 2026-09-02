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

        [Tooltip("Camadas onde procurar objetos interagíveis. Vazio = só a Default.")]
        [SerializeField] private LayerMask camadasInteragiveis;

        /// <summary>
        /// Onde os interagíveis do jogo realmente moram, <b>medido</b> nas cenas e prefabs em
        /// 2026-08-27: 20 na <c>Default</c> (baú, coletáveis, fragmentos, pontos focais,
        /// Cassilda), 1 na <c>Obstacle</c> (<c>Os_Portoes</c>, que é parede e porta ao mesmo
        /// tempo) e 1 na <c>Enemy</c> (o <b>Abdul</b>, com quem se conversa antes de lutar).
        ///
        /// <para>Esta lista é o fallback de quando o Inspector não diz nada — e é o caso hoje:
        /// o <c>Player_Damiao.prefab</c> tem a máscara em zero. Ela é escrita à mão de
        /// propósito, e por isso vem com guarda: <c>InteragivelAlcancavelTests</c> varre toda
        /// cena e todo prefab atrás de quem implementa <c>IInteragivel</c> e reprova se algum
        /// estiver numa camada de fora. Sem esse teste, um NPC novo em outra camada ficaria
        /// <b>mudo em jogo sem uma linha no console</b>.</para>
        /// </summary>
        public static readonly string[] CamadasPadraoDeInteragiveis =
            { "Default", "Obstacle", "Enemy" };

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
        private bool _bloqueado;
        private bool _jaAvisouDeLotacao;

        /// <summary>
        /// Enquanto <c>true</c>, ignora o botão de interação por completo. Ligado por
        /// <see cref="FavelaAmarela.Runtime.UI.PainelDeEscolha"/> enquanto uma escolha está
        /// aberta — sem isto, os dois componentes leem o mesmo aperto de E no mesmo frame:
        /// o mesmo botão que confirma uma opção também reabre a conversa com o NPC (que
        /// ainda está ao alcance), recriando o painel com o cursor de volta no índice 0 e
        /// auto-confirmando a opção errada antes do jogador conseguir navegar. Mesmo padrão
        /// de <c>PlayerMovement.MovimentoBloqueado</c>, aplicado à interação em vez do
        /// movimento.
        ///
        /// <para>Ao ligar, também limpa o alvo e notifica <see cref="OnAlvoMudou"/> com
        /// <c>null</c>: sem isto, o <c>Update</c> para de rodar (abaixo) e o prompt "Pressione
        /// E" do NPC que abriu o painel fica preso na tela, sobreposto à caixa de escolha,
        /// porque nunca mais recebe o evento que o esconderia.</para>
        /// </summary>
        public bool Bloqueado
        {
            get => _bloqueado;
            set
            {
                _bloqueado = value;
                if (_bloqueado && _alvoAtual != null)
                {
                    _alvoAtual = null;
                    OnAlvoMudou?.Invoke(null);
                }
            }
        }

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

            // ── Por que NÃO é mais "todas as camadas" (2026-08-27) ────────────
            // A versão anterior argumentava que "é melhor achar demais do que não achar nada,
            // porque o filtro real é ter IInteragivel". O raciocínio ignora que o buffer tem
            // TAMANHO FIXO: Physics2D.OverlapCircle preenche 8 slots e DESCARTA o resto, em
            // ordem arbitrária. Varrendo todas as camadas, os dois colisores do próprio Damião
            // (corpo + hurtbox) entram sempre, cada inimigo perto gasta mais dois, a parede
            // gasta um, e o gatilho de setor gasta outro. Perto de um baú com dois inimigos por
            // perto, o baú é o que sobra de fora -- e o "E" simplesmente não faz nada, sem uma
            // linha no console.
            _filtro.SetLayerMask(camadasInteragiveis.value != 0
                ? camadasInteragiveis
                : (LayerMask)LayerMask.GetMask(CamadasPadraoDeInteragiveis));

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
            // `Bloqueado` é a trava FINA, ligada à mão pelo PainelDeEscolha desde agosto -- e
            // era o único bloqueio de input que este projeto tinha. O árbitro é a trava grossa:
            // com o inventário ou o console abertos, o E continuava interagindo com o que
            // estivesse sob a mira.
            if (_bloqueado || !FavelaAmarela.Runtime.Entrada.ArbitroDeFoco.JogoNoComando) return;

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

            // Buffer cheio significa que a Unity ENCHEU e parou — o que ficou de fora não é
            // reportado de nenhuma forma. Sem este aviso, o sintoma em jogo é "às vezes o E não
            // funciona", que é indistinguível de estar longe demais.
            if (total >= MaxCandidatos && !_jaAvisouDeLotacao)
            {
                _jaAvisouDeLotacao = true;
                Debug.LogWarning($"[DetectorDeInteracao] {MaxCandidatos} colisores ao alcance: o " +
                                 "buffer encheu e alvos podem estar sendo descartados em silêncio. " +
                                 "Confira a máscara 'camadasInteragiveis' ou aumente MaxCandidatos.",
                                 this);
            }

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
