using System.Collections.Generic;
using UnityEngine;
using FavelaAmarela.Core.Abilities;
using FavelaAmarela.Core.Combat;
using FavelaAmarela.Core.Enemies;
using FavelaAmarela.Player;
using FavelaAmarela.Runtime.GameLoop;

namespace FavelaAmarela.Runtime.Enemies
{
    /// <summary>
    /// Camada Runtime (MonoBehaviour). Adaptador do <see cref="ReiEmAmareloFSM"/> — o
    /// confronto final, no Trono de Aldebaran.
    ///
    /// <para><b>Sem `EnemyBase`</b> — de propósito, como o Abdul. Até 2026-09-10 também sem
    /// `Vitalidade` nem `IDanificavel`: o design dizia "não há barra de vida", e o rito era só
    /// sobrevivido. <b>Isso mudou com a quarta fase</b> (pedido do Vini: <i>"depois dos três
    /// escudos, abre-se uma fase de combate contra ele"</i>). O Rei agora tem carne — ficha,
    /// Vitalidade, hurtbox —, mas ela só responde no Confronto, e só entre os desvelos. Quem
    /// decide isso é a <see cref="ReiEmAmareloFSM.PodeReceberDano"/>; aqui só se lê.</para>
    ///
    /// <para><b>Por que `IDanificavel` direto e não `EnemyBase`.</b> Este objeto já é a única
    /// <see cref="FavelaAmarela.Runtime.Itens.IFonteDeEspolio"/> dele, e o <c>DropAoAbater</c>
    /// resolve a fonte por <c>GetComponent</c> — uma segunda (a do <c>EnemyBase</c>) faria o
    /// espólio cair duas vezes ou nenhuma, dependendo da ordem dos componentes. O Abdul tomou o
    /// mesmo caminho pelo mesmo motivo.</para>
    ///
    /// <para>Toda a regra vive no POCO; aqui só se lê o estado dele, se computa a geometria de
    /// "de costas" a partir de posições reais, e se aplica o resultado (derrota instantânea ou
    /// vitória).</para>
    /// </summary>
    [AddComponentMenu("Favela Amarela/Enemies/Rei em Amarelo AI")]
    public sealed class ReiEmAmareloAI : MonoBehaviour, IDanificavel,
                                         FavelaAmarela.Runtime.Itens.IFonteDeEspolio
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

        [Tooltip("Segundos de calmaria NO CONFRONTO — o tempo de sair do abrigo, ferir e voltar. " +
                 "0 usa o mesmo intervalo acima. Medido: da cúpula mais próxima ao corpo do Rei " +
                 "são ~8 un, 2,1 s de ida e volta correndo.")]
        [Min(0f)]
        [SerializeField] private float intervaloNoConfronto = 0f;

        [Header("Carne (a quarta fase)")]
        [Tooltip("Ficha do Rei — só a Vitalidade, a Defesa e a Resistência Anômala importam: ele " +
                 "não golpeia, o desvelo é o ataque. [ASSET]")]
        [SerializeField] private FichaAtributosConfig ficha;

        [Tooltip("Nível desta unidade. 1 = exatamente o que a ficha diz.")]
        [Min(1)]
        [SerializeField] private int nivelDaUnidade = 1;

        [Tooltip("Barra flutuante ligada à Vitalidade quando o Confronto começa. Vazia: " +
                 "procurada nos filhos. [ASSET]")]
        [SerializeField] private FavelaAmarela.Runtime.UI.BarraDeVidaFlutuante barraDeVida;

        [Header("Os abrigos")]
        [Tooltip("Os escudos dos pontos focais. Vazio: procurados na cena ao começar. [CENA]")]
        [SerializeField] private FavelaAmarela.Runtime.Itens.EscudoDeReliquia[] escudos;

        [Header("Cores de leitura (provisórias, até haver arte)")]
        [SerializeField] private Color corEmRitual = new Color(0.5f, 0.4f, 0.7f);
        [SerializeField] private Color corSelando = new Color(0.7f, 0.6f, 0.2f);
        [SerializeField] private Color corDesvelado = new Color(0.9f, 0.1f, 0.1f);
        [SerializeField] private Color corSelado = new Color(0.9f, 0.85f, 0.6f);

        [Header("Animação")]
        [Tooltip("Animator com o ReiEmAmarelo_AC. Vazio: o Rei desenha o quadro parado.")]
        [SerializeField] private Animator animator;

        [Header("Fala")]
        [Tooltip("Caixa onde o rito fala com Damião. Se vazia, usa a do HUD. [CENA]")]
        [SerializeField] private FavelaAmarela.Runtime.UI.TutorialHintUI caixaDeTexto;

        private ReiEmAmareloFSM _fsm;
        private SpriteRenderer _sprite;
        private Transform _jogador;
        private FavelaAmarela.Runtime.Combat.ResilienciaBridge _mente;

        /// <summary>O escudo aceso agora — um por vez, por decisão de design.</summary>
        private FavelaAmarela.Runtime.Itens.EscudoDeReliquia _abrigoDoCiclo;

        private FichaDeAtributos _atributos;
        private Vitalidade _vitalidade;

        /// <summary>A carne do Rei — para HUD, testes e o Carcosa Debugger.</summary>
        public Vitalidade Vitalidade => _vitalidade;

        /// <summary>Os atributos resolvidos da ficha, no nível desta unidade.</summary>
        public FichaDeAtributos Atributos => _atributos;

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
                intervaloEntreCiclos,
                intervaloNoConfronto);

            CriarACarne();

            // Área atingível derivada do sprite — o mesmo caminho do Abdul, que também
            // implementa IDanificavel sem EnemyBase. A garantia vive aqui, no código: lista
            // de prefabs escrita à mão é o modo de falha mais repetido deste projeto.
            FavelaAmarela.Runtime.Combat.Hurtbox.GarantirPara(gameObject, "EnemyHurtbox");

            if (barraDeVida == null)
                barraDeVida = GetComponentInChildren<FavelaAmarela.Runtime.UI.BarraDeVidaFlutuante>(true);

            _fsm.OnComecouOConfronto += HandleComecouOConfronto;
            _fsm.OnStateChanged += HandleEstadoMudou;
            _fsm.OnSelado += HandleSelado;
            _fsm.OnReliquiaAtivada += HandleReliquiaAtivada;
            _fsm.OnComecouADesvelar += HandleComecouADesvelar;
            _fsm.OnCicloSobrevivido += HandleCicloSobrevivido;
            _fsm.OnEscudoAceso += HandleEscudoAceso;
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
            ResolverEscudos();

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
            _fsm.OnComecouOConfronto -= HandleComecouOConfronto;
            _fsm.OnStateChanged -= HandleEstadoMudou;
            _fsm.OnSelado -= HandleSelado;
            _fsm.OnReliquiaAtivada -= HandleReliquiaAtivada;
            _fsm.OnComecouADesvelar -= HandleComecouADesvelar;
            _fsm.OnCicloSobrevivido -= HandleCicloSobrevivido;
            _fsm.OnEscudoAceso -= HandleEscudoAceso;
        }

        /// <summary>
        /// Resolve a ficha e cria a Vitalidade. Sem ficha, usa uma de emergência e avisa —
        /// nunca deixa o Confronto sem carne, que seria uma fase impossível de vencer.
        /// </summary>
        private void CriarACarne()
        {
            if (ficha == null)
            {
                Debug.LogError("[ReiEmAmarelo] Ficha não atribuída. Usando ficha de emergência " +
                               "(Vitalidade 1000, Defesa 12) — o Confronto precisa de carne.", this);
                _atributos = new FichaDeAtributos(vitalidadeMax: 1000f, ataque: 0f, defesa: 12f);
            }
            else
            {
                _atributos = ficha.CriarFicha(nivelDaUnidade);
            }

            _vitalidade = new Vitalidade(_atributos.VitalidadeMax);
        }

        // ── IDanificavel ─────────────────────────────────────────────────────

        /// <inheritdoc />
        public bool EhAparicaoPrimordial => true;

        /// <inheritdoc />
        public bool EstaAbatido => _vitalidade != null && _vitalidade.EstaAbatido;

        /// <inheritdoc />
        /// <remarks>
        /// A regra inteira está na FSM: só no Confronto, e só na calmaria. Fora disso o golpe
        /// é recusado <b>em silêncio</b> — nenhum som de carne quer dizer que nada entrou, que é
        /// a informação certa quando o jogador bate no Rei ainda mascarado.
        /// </remarks>
        public bool PodeSerFerido => _fsm != null && _fsm.PodeReceberDano && !EstaAbatido;

        /// <inheritdoc />
        public void ReceberGolpe(ArmaResult resultado)
        {
            if (!PodeSerFerido) return;
            if (resultado.Dano <= 0f) return;

            float danoFinal = MitigacaoDeDano.Aplicar(resultado.Dano, _atributos.Defesa);
            if (danoFinal <= 0f) return;

            _vitalidade.Ferir(danoFinal);

            if (animator != null && animator.runtimeAnimatorController != null)
                animator.Play(Anim.Dano, 0, 0f);

            // A Vitalidade zerou: o rito termina por abate. A FSM faz a transição para Selado
            // e dispara OnSelado, que já leva a vitória e o espólio pelo caminho de sempre.
            if (_vitalidade.EstaAbatido) _fsm.Abater();
        }

        /// <summary>
        /// A Máscara caiu. Liga a barra à carne e diz ao jogador o que mudou — a regra da
        /// fase é nova e contraintuitiva (sair do abrigo que acabou de aprender a procurar).
        /// </summary>
        private void HandleComecouOConfronto()
        {
            if (barraDeVida != null) barraDeVida.Bind(_vitalidade);

            Dizer("A Máscara Pálida cai. Ele sangra entre os desvelos — fere-o, e volta ao " +
                  "clarão antes que ele te encare.", 5f);
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

            bool abrigado = _abrigoDoCiclo != null
                            && _jogador != null
                            && _abrigoDoCiclo.Protege(_jogador.position);

            _fsm.Tick(Time.deltaTime, abrigado);
        }

        /// <summary>
        /// Acha os escudos dos pontos focais se ninguém os arrastou no Inspector.
        ///
        /// <para><c>FindObjectsByType</c> é caro e proibido em caminho quente — aqui roda uma
        /// vez, no <c>Start</c>, pela mesma razão que o alvo é resolvido uma vez: campo de cena
        /// vazio é o modo de falha número um deste projeto, e um abrigo que não é encontrado é
        /// uma luta impossível de vencer.</para>
        /// </summary>
        private void ResolverEscudos()
        {
            if (escudos == null || escudos.Length == 0)
                escudos = FindObjectsByType<FavelaAmarela.Runtime.Itens.EscudoDeReliquia>();

            if (escudos == null || escudos.Length == 0)
            {
                Debug.LogError("[ReiEmAmarelo] Nenhum EscudoDeReliquia na cena — sem abrigo, " +
                               "o rito de selamento não tem resposta possível.", this);
                return;
            }

            foreach (var escudo in escudos)
                if (escudo != null) escudo.Apagar();
        }

        /// <summary>
        /// Uma relíquia ergueu o abrigo deste ciclo: acende o dela e apaga os outros.
        ///
        /// <para><b>Um por vez</b> é a mecânica inteira — dois acesos dariam ao jogador uma
        /// escolha, e o que a luta pede é uma corrida.</para>
        /// </summary>
        private void HandleEscudoAceso(string artefatoId)
        {
            _abrigoDoCiclo = null;
            if (escudos == null) return;

            foreach (var escudo in escudos)
            {
                if (escudo == null) continue;

                if (escudo.ArtefatoId == artefatoId)
                {
                    escudo.Acender();
                    _abrigoDoCiclo = escudo;
                }
                else
                {
                    escudo.Apagar();
                }
            }

            if (_abrigoDoCiclo == null)
            {
                Debug.LogError($"[ReiEmAmarelo] Nenhum escudo com artefatoId '{artefatoId}' — " +
                               "este ciclo não teria abrigo nenhum.", this);
                return;
            }

            AnunciarOAbrigo(artefatoId);
        }

        /// <summary>
        /// Diz para onde correr, pelo <b>nome diegético</b> da relíquia.
        ///
        /// <para>Não é conforto: a sala do Trono tem 30 unidades de largura e a câmera mostra
        /// 20 — os dois altares das pontas estão a 20 unidades um do outro, então
        /// <b>de um deles não se enxerga o outro</b>. Sem esta fala, um terço dos ciclos
        /// começaria com o abrigo fora da tela. A cúpula tem 7,5 unidades de altura de
        /// propósito pelo mesmo motivo: ela aparece por cima da borda do quadro antes de o
        /// altar aparecer.</para>
        /// </summary>
        private void AnunciarOAbrigo(string artefatoId)
        {
            string nome = NomeDaReliquia(artefatoId);

            // O primeiro ciclo ensina a regra; os seguintes só dizem para onde.
            Dizer(_fsm.CiclosSobrevividos == 0
                ? $"{nome} ergue um clarão. Não o encares — abriga-te dentro antes que ele se " +
                  "desvele."
                : $"{nome} arde. Corre para o clarão.", 4f);
        }

        /// <summary>
        /// O nome diegético da relíquia. "anel_sinal_amarelo" não é coisa que se diga — mesma
        /// regra que o <c>PontoFocalDeReliquia</c> já segue ao falar com o jogador.
        /// </summary>
        private string NomeDaReliquia(string artefatoId)
        {
            var artefatos = _jogador != null
                ? _jogador.GetComponent<ArtefatosBridge>()
                : null;

            return artefatos?.Def(artefatoId)?.Nome ?? "A relíquia";
        }

        /// <summary>
        /// A caixa de fala em uso, resolvida na hora do uso (o HUD é persistente e o campo nasce
        /// vazio em prefab-asset).
        /// </summary>
        private FavelaAmarela.Runtime.UI.TutorialHintUI CaixaDeFala
            => caixaDeTexto != null ? caixaDeTexto
                                    : FavelaAmarela.Runtime.UI.TutorialHintUI.Instancia;

        private void Dizer(string texto, float duracao = 3f)
        {
            var caixa = CaixaDeFala;
            if (caixa != null) caixa.Mostrar(texto, duracao);
        }

        /// <summary>
        /// O Rei se desvelou: a janela de reação abriu AGORA.
        ///
        /// <para><b>Por que isto é som, e não é polimento (2026-09-02).</b> O telegrafo desta
        /// luta era só visual — cor e animação, trocadas por
        /// <see cref="HandleEstadoMudou"/>. Mas a ação correta é <b>não olhar</b>: quem está
        /// jogando certo tem Damião de costas e <b>não pode ver</b> o aviso. Uma luta cujo único
        /// sinal exige olhar, e cuja resposta certa é não olhar, é invencível por construção.
        /// O canal sonoro é o único que atravessa a mecânica.</para>
        ///
        /// <para>O evento existia desde que a FSM foi escrita e tinha <b>zero assinantes</b> —
        /// encontrado pela auditoria de ligação (<c>LigacaoDoJogoTests</c>).</para>
        /// </summary>
        private void HandleComecouADesvelar()
        {
            FavelaAmarela.Runtime.Audio.MixerDeAudio.Instancia?.Tocar(
                FavelaAmarela.Runtime.Audio.SomDoJogo.EntrouEmPanico, transform.position);
        }

        /// <summary>
        /// Sobreviveu a um ciclo. Sem isto o jogador não sabe se acertou nem quanto falta — o
        /// rito seria três esperas idênticas no escuro.
        /// </summary>
        private void HandleCicloSobrevivido()
        {
            // No Confronto a fala é a do escudo que acende — cada ciclo é "corre para o
            // clarão", e a contagem de selos não faz mais sentido.
            if (_fsm.EmConfronto) return;

            int faltam = Mathf.Max(0, ciclosDeSelamento - _fsm.CiclosSobrevividos);

            if (faltam > 0) Dizer($"O selo aperta. Ainda faltam {faltam}.");
            // faltam == 0: o HandleComecouOConfronto fala, e fala a regra nova.
        }

        private void HandleEstadoMudou(ReiEmAmareloState anterior, ReiEmAmareloState atual)
        {
            if (_sprite != null)
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

            // Fim do rito, de um jeito ou de outro: nenhum abrigo fica aceso.
            if (atual == ReiEmAmareloState.Selado || atual == ReiEmAmareloState.Colapso)
                ApagarTodosOsEscudos();

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

        private void ApagarTodosOsEscudos()
        {
            _abrigoDoCiclo = null;
            if (escudos == null) return;

            foreach (var escudo in escudos)
                if (escudo != null) escudo.Apagar();
        }

        private void HandleSelado()
        {
            OnVitoria?.Invoke();
            OnAbatido?.Invoke();
        }
    }
}
