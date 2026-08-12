using UnityEngine;
using FavelaAmarela.Core.Persistencia;
using FavelaAmarela.Runtime.Persistencia;
using FavelaAmarela.Runtime.UI;

namespace FavelaAmarela.Runtime.GameLoop
{
    /// <summary>
    /// Camada Runtime (MonoBehaviour). <b>Poste de Luz / Refúgio</b> — área de luz onde
    /// Damião fica a salvo (ver <c>lore/world_rules.md</c>: "luz é refúgio"). Faz três
    /// coisas ao jogador entrar:
    /// <list type="number">
    ///   <item><b>Ancoragem</b>: devolve Resiliência Mental.</item>
    ///   <item><b>Reanima Yug-Neth</b> se ele estiver incapacitado.</item>
    ///   <item><b>Salva a partida</b> em disco — o GDD §8.3 já decidia que o save acontece
    ///   nos Postes de Luz, e este é o único lugar do jogo que grava.</item>
    /// </list>
    ///
    /// <para>Automático por proximidade (entrar na luz), não por botão — casa com a leitura
    /// diegética de "descansar sob o poste", diferente da interação deliberada (botão E)
    /// usada para objetos que o jogador escolhe usar (baú, patuá, NPC).</para>
    ///
    /// <para><b>Reentrada não repete o efeito completo</b>: sair e voltar não vira uma
    /// bomba de cura infinita. A Ancoragem só acontece de novo depois de
    /// <see cref="intervaloDeAncoragem"/>, mas <b>reanimar Yug-Neth e salvar sempre
    /// valem</b> — são estados, não recursos.</para>
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    [AddComponentMenu("Favela Amarela/GameLoop/Refúgio de Luz")]
    public sealed class RefugioDeLuz : MonoBehaviour
    {
        [Header("Ancoragem")]
        [Tooltip("Quanta Resiliência Mental a luz devolve. 0 = não ancora (poste apagado).")]
        [Min(0f)]
        [SerializeField] private float resilienciaRestaurada = 100f;

        [Tooltip("Fração da Vitalidade máxima que a luz devolve (0.4 = 40%). 0 = não cura o " +
                 "corpo. Parcial de propósito: o jogador chega no próximo Refúgio ferido e " +
                 "precisa decidir se gasta um consumível ou arrisca seguir.")]
        [Range(0f, 1f)]
        [SerializeField] private float fracaoDeVitalidadeRestaurada = 0.4f;

        [Tooltip("Segundos até este Refúgio poder ancorar de novo. Impede farm de cura " +
                 "entrando e saindo da luz.")]
        [Min(0f)]
        [SerializeField] private float intervaloDeAncoragem = 30f;

        [Header("Save")]
        [Tooltip("Grava a partida em disco ao descansar aqui (GDD §8.3: save nos Postes de Luz).")]
        [SerializeField] private bool salvaAoDescansar = true;

        [Header("Feedback")]
        [Tooltip("Caixa de texto do Refúgio (reaproveita a UI de dica por ora).")]
        [SerializeField] private TutorialHintUI caixaDeTexto;

        [TextArea]
        [Tooltip("Mensagem ao descansar sob a luz.")]
        [SerializeField] private string mensagemDeDescanso =
            "A luz te alcança. Por um instante, o amarelo lá fora não parece te ver.";

        [TextArea]
        [Tooltip("Mensagem ao reanimar Yug-Neth aqui.")]
        [SerializeField] private string mensagemDeReanimacao =
            "Sob a luz, Yug-Neth estremece e volta a pulsar. Ele se levanta.";

        private float _proximaAncoragem;

        private void Awake()
        {
            var col = GetComponent<Collider2D>();
            if (!col.isTrigger)
                Debug.LogError($"[RefugioDeLuz] '{name}' precisa de um Collider2D marcado " +
                               "como Trigger — corrigindo em runtime.", this);
            col.isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player")) return;

            bool algoAconteceu = ReanimarCompanheiro();
            algoAconteceu |= Ancorar();

            if (salvaAoDescansar) Salvar();

            if (algoAconteceu && caixaDeTexto != null && !string.IsNullOrWhiteSpace(mensagemDeDescanso))
                caixaDeTexto.Mostrar(mensagemDeDescanso);
        }

        /// <summary>
        /// Devolve Resiliência Mental e parte da Vitalidade, respeitando o intervalo entre
        /// descansos. As duas curas compartilham o <b>mesmo</b> relógio de propósito: um
        /// segundo intervalo só daria ao jogador dois motivos para ficar entrando e saindo da
        /// luz, que é exatamente o que o anti-farm existe para evitar.
        ///
        /// <para>É esta cura que impede o <em>soft-lock</em> físico sem precisar de moeda nem
        /// de recarga de consumível: o Refúgio é o único ponto de save do jogo, então o jogador
        /// passa por ele por design. Ver
        /// <c>Docs/KnowledgeBundle/systems/inventario_e_consumiveis.md</c>.</para>
        /// </summary>
        private bool Ancorar()
        {
            if (Time.time < _proximaAncoragem) return false;

            bool curou = AncorarMente();
            curou |= AncorarCorpo();

            if (curou) _proximaAncoragem = Time.time + intervaloDeAncoragem;
            return curou;
        }

        private bool AncorarMente()
        {
            if (resilienciaRestaurada <= 0f) return false;

            var resiliencia = GameManager.Instance != null ? GameManager.Instance.Resiliencia : null;
            if (resiliencia == null) return false;

            resiliencia.Ancorar(resilienciaRestaurada);
            return true;
        }

        /// <summary>
        /// Cura uma fração da Vitalidade <b>máxima</b>, não um valor absoluto como a Resiliência.
        ///
        /// <para><b>A assimetria é deliberada:</b> o teto de Resiliência é fixo no
        /// <c>GameManager</c>, mas <c>Vitalidade.Max</c> é dinâmico — <c>SetValorMaximo</c>
        /// reage aos bônus de <c>StatType.VitMaxima</c> das armaduras. Um valor absoluto aqui
        /// envelheceria mal: curaria metade da barra no começo do jogo e uma lasca dela depois
        /// de algumas peças de equipamento.</para>
        /// </summary>
        private bool AncorarCorpo()
        {
            if (fracaoDeVitalidadeRestaurada <= 0f) return false;

            var vitalidade = GameManager.Instance != null ? GameManager.Instance.VitalidadeDoJogador : null;
            if (vitalidade == null) return false;

            vitalidade.Curar(vitalidade.Max * fracaoDeVitalidadeRestaurada);
            return true;
        }

        /// <summary>
        /// Reanima Yug-Neth se ele tiver caído. <b>Sem intervalo</b>: é um estado que precisa
        /// ser desfeito, não um recurso — e ele bloqueia os Portões de Carcosa enquanto
        /// estiver caído.
        /// </summary>
        private bool ReanimarCompanheiro()
        {
            var yugNeth = GameManager.Instance != null ? GameManager.Instance.YugNeth : null;
            if (yugNeth == null || !yugNeth.EstaIncapacitado) return false;

            yugNeth.Reanimar();

            if (caixaDeTexto != null) caixaDeTexto.Mostrar(mensagemDeReanimacao);
            return true;
        }

        /// <summary>
        /// Fotografa o estado e grava em disco. É o <b>único ponto do jogo que salva</b> —
        /// até aqui o <c>GerenciadorDeSave</c> tinha a gravação pronta mas ninguém a chamava,
        /// então fechar o jogo perdia tudo.
        /// </summary>
        private void Salvar()
        {
            var gerenciador = GerenciadorDeSave.Instancia;
            if (gerenciador == null) return;

            gerenciador.CapturarTudo();
            MarcarComoPontoDeRenascimento();
            gerenciador.GravarEmDisco();
        }

        /// <summary>
        /// Registra este Refúgio como o lugar para onde a morte devolve o jogador.
        ///
        /// <para>Guardado à parte da cena atual porque as duas respondem a perguntas
        /// diferentes: "onde eu parei" (o Continuar do menu) e "onde é seguro voltar" (a
        /// morte). Sem esta distinção, morrer devolveria o jogador ao corredor onde
        /// morreu — direto para a mesma morte.</para>
        /// </summary>
        private void MarcarComoPontoDeRenascimento()
        {
            var cena = gameObject.scene;
            if (cena.IsValid()) GerenciadorDeSave.DefinirValor(ChavesDeSave.RefugioCena, cena.name);

            // O ponto de chegada irmão é o que o renascimento usa para posicionar Damião
            // exatamente sob a luz, e não no ponto padrão da cena.
            var ponto = GetComponent<PontoDeChegada>();
            if (ponto != null && !string.IsNullOrWhiteSpace(ponto.Identificador))
            {
                GerenciadorDeSave.DefinirValor(ChavesDeSave.RefugioPonto, ponto.Identificador);
            }
            else
            {
                // Sem ponto irmão o renascimento ainda funciona — cai na posição padrão da
                // cena. Avisar aqui evita a caça ao "por que renasci longe do poste".
                Debug.LogWarning($"[RefugioDeLuz] '{name}' não tem PontoDeChegada irmão: o " +
                                 "renascimento vai cair na posição padrão da cena.", this);
            }
        }

        // TODO(design): pausar o dreno de RM da tempestade enquanto o jogador estiver na luz
        // (o dreno em si ainda não existe — decisão do Vini de a tempestade afetar só inimigos).
    }
}
