using System.Collections.Generic;
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
    public sealed class ReiEmAmareloAI : MonoBehaviour, FavelaAmarela.Runtime.Itens.IFonteDeEspolio
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

        /// <summary>
        /// Os ids de Artefato que o rito de selamento exige — o Rei é a <b>fonte da verdade</b>
        /// do que é preciso trazer ao Trono.
        ///
        /// <para>Existe para que ferramentas (o Carcosa Debugger, o montador da cena) não
        /// mantenham uma segunda cópia da lista: uma cópia que saísse de sincronia concederia
        /// as relíquias erradas e o rito nunca fecharia, sem erro nenhum aparecendo.</para>
        /// </summary>
        public IReadOnlyList<string> ReliquiasExigidas => idsDasReliquiasExigidas;

        /// <summary>Quantas reliquias o rito ainda espera. Zero quando todas estao ativas.</summary>
        public int ReliquiasFaltando =>
            _fsm == null ? idsDasReliquiasExigidas.Length
                         : _fsm.TotalDeReliquiasExigidas - _fsm.ReliquiasAtivas;

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

        [Header("Animação")]
        [Tooltip("Animator com o ReiEmAmarelo_AC. Vazio: o Rei desenha o quadro parado.")]
        [SerializeField] private Animator animator;

        private ReiEmAmareloFSM _fsm;
        private SpriteRenderer _sprite;
        private Transform _jogador;
        private FavelaAmarela.Runtime.Combat.ResilienciaBridge _mente;
        private PlayerMovement _movimentoDoJogador;

        /// <summary>A FSM do confronto, para HUD, cutscenes e o Carcosa Debugger observarem.</summary>
        public ReiEmAmareloFSM Fsm => _fsm;

        /// <summary>Disparado na vitória — quem monta a cena decide o que fazer com isso.</summary>
        public event System.Action OnVitoria;

        /// <summary>
        /// <see cref="FavelaAmarela.Runtime.Itens.IFonteDeEspolio"/>. Dispara junto com
        /// <see cref="OnVitoria"/>, no selamento.
        ///
        /// <para><b>Sim, "abatido" para quem é selado.</b> O nome da interface descreve o que o
        /// <c>DropAoAbater</c> precisa saber — <i>"quem sabe avisar que foi derrotado"</i> —, e
        /// selar o Rei <b>é</b> derrotá-lo neste jogo. Renomear a interface por causa deste caso
        /// mexeria em três atores que já a implementam para ganhar precisão em zero deles.</para>
        ///
        /// <para><b>O que isto conserta:</b> o Rei não é <c>EnemyBase</c> nem
        /// <c>IDanificavel</c> (não tem barra de vida, por decisão de design), então ficava de
        /// fora do espólio <b>por construção</b> — exatamente o buraco que a interface foi criada
        /// para tapar quando o Abdul caiu nele. O último confronto do Vertical Slice largava
        /// <b>zero</b> equipamento.</para>
        /// </summary>
        public event System.Action OnAbatido;

        private void Awake()
        {
            _sprite = GetComponent<SpriteRenderer>();

            if (animator == null) animator = GetComponent<Animator>();

            _fsm = new ReiEmAmareloFSM(
                idsDasReliquiasExigidas,
                ciclosDeSelamento,
                duracaoDaJanela,
                intervaloEntreCiclos);

            _fsm.OnStateChanged += HandleEstadoMudou;
            _fsm.OnSelado += HandleSelado;
            _fsm.OnReliquiaAtivada += HandleReliquiaAtivada;
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

            // O RITO COMEÇA AQUI (2026-09-02). Antes, `IniciarRitual()` tinha UM chamador em
            // todo o projeto: o Carcosa Debugger, que é janela de Editor. Nada no jogo o
            // chamava. A máquina ficava em Aguardando para sempre, e AtivarReliquia recusa
            // fora de AtivandoReliquias -- então os três pontos focais do Trono recusavam TODA
            // relíquia, em silêncio, e o Rei era impossível de selar.
            //
            // O Vini relatou como "os altares das relíquias não estão funcionando". Estava
            // certo: eles não funcionavam. A causa era um passo acima deles.
            //
            // Start() é o gancho certo e não inventa design: AtivandoReliquias é um PORTÃO
            // puro -- sem relógio, sem pressão, e o próprio Tick não avança nesse estado (ver
            // ReiEmAmareloFSM.Tick). Quem carregou a cena do Trono já está na arena.
            IniciarRitual();
        }

        private void OnDestroy()
        {
            if (_fsm == null) return;
            _fsm.OnStateChanged -= HandleEstadoMudou;
            _fsm.OnSelado -= HandleSelado;
            _fsm.OnReliquiaAtivada -= HandleReliquiaAtivada;
        }

        /// <summary>
        /// O Rei recua quando uma relíquia trava.
        ///
        /// <para>Ele <b>não leva dano</b> — não tem <c>Vitalidade</c> nem <c>IDanificavel</c>,
        /// por decisão de design: é selado por rito, não abatido. Mas travar uma relíquia é o
        /// equivalente ficcional de acertá-lo, e sem nenhum retorno visual o jogador não sabe
        /// que a ação surtiu efeito. É o que o clipe <c>dano</c> do pacote serve aqui.</para>
        /// </summary>
        private void HandleReliquiaAtivada(string id, int total)
        {
            if (animator == null || animator.runtimeAnimatorController == null) return;
            animator.Play(Anim.Dano, 0, 0f);
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

            TocarAnimacaoDo(atual);
        }

        /// <summary>
        /// Põe o Animator no clipe do estado. <b>Quem manda é a <c>ReiEmAmareloFSM</c></b> — por
        /// isso o <c>ReiEmAmarelo_AC</c> não tem teia de transições com condições: duplicar a
        /// lógica de ritual lá criaria uma segunda fonte de verdade, divergindo desta em
        /// silêncio.
        ///
        /// <para><c>Selado</c> usa <c>queda</c>: selar o Rei é a vitória do jogador, e o corpo
        /// tombando é o desfecho. <c>Colapso</c> é o contrário — o Rei venceu — e ele fica em
        /// <c>idle</c>, de pé, enquanto Damião sucumbe.</para>
        ///
        /// <para>Degrada em silêncio: sem Animator, o Rei desenha o quadro parado, como antes.</para>
        /// </summary>
        private void TocarAnimacaoDo(ReiEmAmareloState estado)
        {
            if (animator == null || animator.runtimeAnimatorController == null) return;

            int clipe = estado switch
            {
                ReiEmAmareloState.Selando => Anim.Selar,
                ReiEmAmareloState.Desvelado => Anim.Desvelo,
                ReiEmAmareloState.Selado => Anim.Queda,
                _ => Anim.Idle,
            };

            animator.Play(clipe, 0, 0f);
        }

        /// <summary>
        /// Hashes do <c>ReiEmAmarelo_AC</c>, resolvidos uma vez em campo estático: a troca de
        /// estado acontece a cada ciclo do ritual, e a Regra de Ouro 1 proíbe alocar string em
        /// caminho quente.
        /// </summary>
        private static class Anim
        {
            internal static readonly int Idle = Animator.StringToHash("idle");
            internal static readonly int Selar = Animator.StringToHash("selar");
            internal static readonly int Desvelo = Animator.StringToHash("desvelo");
            internal static readonly int Dano = Animator.StringToHash("dano");
            internal static readonly int Queda = Animator.StringToHash("queda");
        }

        private void HandleSelado()
        {
            OnVitoria?.Invoke();
            OnAbatido?.Invoke();
        }
    }
}
