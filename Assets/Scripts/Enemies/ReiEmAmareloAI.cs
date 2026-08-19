using UnityEngine;
using FavelaAmarela.Core.Enemies;
using FavelaAmarela.Player;
using FavelaAmarela.Runtime.GameLoop;

namespace FavelaAmarela.Runtime.Enemies
{
    /// <summary>
    /// Camada Runtime (MonoBehaviour). Adaptador do <see cref="ReiEmAmareloFSM"/> — o
    /// confronto final, no Trono de Aldebaran.
    ///
    /// <para><b>Sem `EnemyBase`, sem `Vitalidade`, sem `IDanificavel`</b> — de propósito. O
    /// design é explícito: "não há barra de vida". Este não é um inimigo que se ataca; é um
    /// rito que se sobrevive. O par de referência aqui não é `CultistaAI`, é mais perto de
    /// `ColapsoTrigger`/`CoisaDoCemiterioAI`: alguma coisa que pode matar instantaneamente, e
    /// cuja "vitória" é um evento, não uma barra chegando a zero.</para>
    ///
    /// <para>Toda a regra vive no POCO; aqui só se lê o estado dele, se computa a geometria de
    /// "de costas" a partir de posições reais, e se aplica o resultado (derrota instantânea ou
    /// vitória).</para>
    /// </summary>
    [AddComponentMenu("Favela Amarela/Enemies/Rei em Amarelo AI")]
    public sealed class ReiEmAmareloAI : MonoBehaviour
    {
        [Header("Relíquias exigidas")]
        [Tooltip("Ids dos ItemDef de Artefato que o rito exige (ex.: 'necronomicon'). " +
                 "A Coroa de Ossos não tem fonte jogável ainda — não force os 4 do design " +
                 "aqui sem uma forma real de obtê-la (ver boss_rei_em_amarelo.md).")]
        [SerializeField] private string[] idsDasReliquiasExigidas =
        {
            "necronomicon",
            "patua_luas_gemeas",
            "anel_sinal_amarelo",
        };

        [Header("Selamento")]
        [Tooltip("Quantas vezes o Rei se desvela até o rito se completar.")]
        [Min(1)]
        [SerializeField] private int ciclosDeSelamento = 3;

        [Tooltip("Segundos de reação por desvelar — número do design doc, não estimativa.")]
        [SerializeField] private float duracaoDaJanela = 1.5f;

        [Tooltip("Segundos de calmaria entre um desvelar e o próximo.")]
        [SerializeField] private float intervaloEntreCiclos = 6f;

        [Tooltip("Quão de costas o jogador precisa estar. -1 = perfeito, 0 = de perfil. " +
                 "-0,5 aceita ~60° de desvio da direção oposta ao Rei.")]
        [SerializeField] private float limiarDeCostas = -0.5f;

        [Header("Cores de leitura (provisórias, até haver arte)")]
        [SerializeField] private Color corEmRitual = new Color(0.5f, 0.4f, 0.7f);
        [SerializeField] private Color corSelando = new Color(0.7f, 0.6f, 0.2f);
        [SerializeField] private Color corDesvelado = new Color(0.9f, 0.1f, 0.1f);
        [SerializeField] private Color corSelado = new Color(0.9f, 0.85f, 0.6f);

        private ReiEmAmareloFSM _fsm;
        private SpriteRenderer _sprite;
        private Transform _jogador;
        private FavelaAmarela.Runtime.Combat.ResilienciaBridge _mente;
        private PlayerMovement _movimentoDoJogador;

        /// <summary>A FSM do confronto, para HUD, cutscenes e o Carcosa Debugger observarem.</summary>
        public ReiEmAmareloFSM Fsm => _fsm;

        /// <summary>Disparado na vitória — quem monta a cena decide o que fazer com isso.</summary>
        public event System.Action OnVitoria;

        private void Awake()
        {
            _sprite = GetComponent<SpriteRenderer>();

            _fsm = new ReiEmAmareloFSM(
                idsDasReliquiasExigidas,
                ciclosDeSelamento,
                duracaoDaJanela,
                intervaloEntreCiclos);

            _fsm.OnStateChanged += HandleEstadoMudou;
            _fsm.OnSelado += HandleSelado;
        }

        private void Start()
        {
            var jogadorGo = GameObject.FindGameObjectWithTag("Player");
            if (jogadorGo == null)
            {
                Debug.LogError("[ReiEmAmarelo] Nenhum objeto com a tag Player — o rito não " +
                               "tem quem observar.", this);
                return;
            }

            _jogador = jogadorGo.transform;

            // Resolvida uma vez, junto do alvo — o Colapso do Rei é morte súbita e não pode
            // depender de um global existir no momento exato.
            _mente = jogadorGo.GetComponentInChildren<FavelaAmarela.Runtime.Combat.ResilienciaBridge>();
            if (_mente == null)
                Debug.LogError("[ReiEmAmarelo] Damião sem ResilienciaBridge — o Colapso final " +
                               "não teria efeito.", this);
            _movimentoDoJogador = jogadorGo.GetComponent<PlayerMovement>();

            if (_movimentoDoJogador == null)
                Debug.LogError("[ReiEmAmarelo] Player sem PlayerMovement — sem LookDirection, " +
                               "a Máscara Pálida nunca poderia ser evitada.", this);
        }

        private void OnDestroy()
        {
            if (_fsm == null) return;
            _fsm.OnStateChanged -= HandleEstadoMudou;
            _fsm.OnSelado -= HandleSelado;
        }

        /// <summary>Começa o confronto: libera os pontos focais das relíquias.</summary>
        public void IniciarRitual() => _fsm.Iniciar();

        /// <summary>Chamado pelo <see cref="PontoFocalDeReliquia"/> ao ativar uma relíquia.</summary>
        public bool AtivarReliquia(string artefatoId) => _fsm.AtivarReliquia(artefatoId);

        private void Update()
        {
            if (_fsm.CurrentState == ReiEmAmareloState.Selado
                || _fsm.CurrentState == ReiEmAmareloState.Colapso)
                return;

            bool deCostas = CalcularSeEstaDeCostas();
            _fsm.Tick(Time.deltaTime, deCostas);
        }

        /// <summary>
        /// Geometria pura (<see cref="DetectorDeCostas"/>) alimentada com posições e olhar
        /// reais. Fora do rito ainda diz a verdade — só importa de verdade durante o
        /// desvelar, que é quando a FSM realmente olha para este valor.
        /// </summary>
        private bool CalcularSeEstaDeCostas()
        {
            if (_jogador == null || _movimentoDoJogador == null) return false;

            return DetectorDeCostas.EstaDeCostas(
                _jogador.position,
                _movimentoDoJogador.LookDirection,
                transform.position,
                limiarDeCostas);
        }

        private void HandleEstadoMudou(ReiEmAmareloState anterior, ReiEmAmareloState atual)
        {
            if (_sprite == null) return;

            _sprite.color = atual switch
            {
                ReiEmAmareloState.AtivandoReliquias => corEmRitual,
                ReiEmAmareloState.Selando => corSelando,
                ReiEmAmareloState.Desvelado => corDesvelado,
                ReiEmAmareloState.Selado => corSelado,
                _ => corEmRitual
            };

            // Colapso é instantâneo e mata pela mente — mesmo mecanismo dos outros gatilhos de
            // morte súbita (ColapsoTrigger, CoisaDoCemiterioAI). A proteção de cutscene é
            // respeitada DENTRO da ResilienciaBridge, num lugar só, em vez de replicada aqui.
            if (atual == ReiEmAmareloState.Colapso) _mente?.ForcarColapso();
        }

        private void HandleSelado() => OnVitoria?.Invoke();
    }
}
