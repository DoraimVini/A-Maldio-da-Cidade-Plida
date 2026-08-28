using UnityEngine;
using FavelaAmarela.Core.Dialogo;
using FavelaAmarela.Core.Persistencia;
using FavelaAmarela.Core.Quests;
using FavelaAmarela.Runtime.Interaction;
using FavelaAmarela.Runtime.Persistencia;
using FavelaAmarela.Runtime.UI;

namespace FavelaAmarela.Runtime.Quests
{
    /// <summary>
    /// Camada Runtime (MonoBehaviour). <b>Rainha Cassilda</b>, presa ao Santuário de Yhtill
    /// pela geometria de Carcosa. Dá e recebe a quest "A Canção Incompleta" — ver
    /// <c>lore/cassilda_e_byakhee.md</c> para o roteiro completo e o perfil da personagem.
    ///
    /// <para>Ela <b>não pode sair do Santuário</b>: os fragmentos dos diários de seus nobres
    /// estão espalhados pelo mundo e alguém precisa trazê-los para que ela possa cantar o
    /// nome de cada um. É essa a troca — não uma tarefa, um pedido.</para>
    ///
    /// <para><b>Entrega automática ao falar.</b> Cassilda recebe de uma vez todos os
    /// fragmentos que Damião estiver carregando, com uma fala por entrega. Não há tela de
    /// seleção: pedir ao jogador para escolher página por página seria burocracia num
    /// momento que é de luto.</para>
    ///
    /// <para><b>O primeiro encontro tem uma escolha A/B/C</b> (2026-08-02, roteiro do lore
    /// nunca antes ligado) — puramente cosmética: muda só a reação dela à saudação, não a
    /// quest nem o pedido que vem em seguida. Sem <c>painelDeEscolha</c> atribuído, pula a
    /// escolha e segue no mesmo ritmo de conversa.</para>
    ///
    /// <para><b>Ter tudo entregue não é ter a canção completa</b> (decisão do Vini,
    /// 2026-08-02). Com os 3 fragmentos na mão, ela pede as duas últimas estrofes — não
    /// consegue mais <i>evocá</i>-las depois de eras cantando, mas <i>reconhece</i> quando
    /// Damião diz certo. Errar não custa nada além de tentar de novo: o Santuário é área de
    /// calmaria, e uma penalidade ali seria contradição. Ver <see cref="CancaoIncompleta"/>
    /// e <see cref="RecitalDaCancao"/> (Core).</para>
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    [AddComponentMenu("Favela Amarela/Quests/Cassilda")]
    public sealed class CassildaNPC : MonoBehaviour, IInteragivel
    {
        [Header("Quest")]
        [Tooltip("Quantos fragmentos a quest exige. Reduzido para 3 porque os fragmentos 4 " +
                 "e 5 do design ficam no Templo da Serpente, que não existe.")]
        [Min(1)]
        [SerializeField] private int totalDeFragmentos = CancaoIncompleta.TotalPadrao;

        [Header("Recompensa")]
        [Tooltip("Prefab do Patuá das Luas Gêmeas, largado ao concluir a quest. [ASSET]")]
        [SerializeField] private GameObject prefabPatua;

        [Header("Falas")]
        [Tooltip("Caixa de texto da conversa (reaproveita a UI de dica por ora). [CENA]")]
        [SerializeField] private TutorialHintUI caixaDeTexto;

        [TextArea(3, 8)]
        [Tooltip("Primeira fala, ao encontrá-la.")]
        [SerializeField] private string falaDeSaudacao =
            "Você cheira a Hali, forasteiro. E a algo mais — a morte recente. " +
            "Bem-vindo ao Santuário de Yhtill. Que reste ainda um santuário para ser chamado assim.";

        [TextArea(3, 8)]
        [Tooltip("O pedido da quest.")]
        [SerializeField] private string falaDoPedido =
            "Meus nobres partiram há tempo não mensurável. Deixaram diários, cartas, " +
            "fragmentos de nossas vidas antes de Carcosa. Traga-os. Para que eu possa cantar " +
            "a canção de cada nome deles direito.";

        [Header("Primeiro encontro (ramificado)")]
        [Tooltip("Resposta de Damião à saudação — puramente cosmético, não muda a quest " +
                 "nem o pedido que vem depois. Roteiro em lore/cassilda_e_byakhee.md §III.")]
        [SerializeField] private string[] opcoesDoPrimeiroEncontro =
        {
            "Onde estou?",
            "Você está presa aqui?",
            "(Ficar em silêncio)",
        };

        [TextArea(2, 6)]
        [Tooltip("Reação de Cassilda a cada opção acima, mesma ordem.")]
        [SerializeField] private string[] reacoesDoPrimeiroEncontro =
        {
            "No coração de Carcosa, onde os sóis gêmeos esqueceram como se pôr. Este é o " +
            "Santuário de Yhtill — o que resta da corte do Rei em Amarelo antes que ele " +
            "deixasse de ser apenas um personagem de peça e passasse a ser um fato.",

            "Presa. Sim. Essa palavra serve. A geometria de Carcosa tem predileção por " +
            "ironias: a rainha que fundou este santuário não pode sair dele. Mas meus " +
            "nobres partiram. Foram buscar fragmentos de uma saída que não existe. Ou " +
            "talvez existe — simplesmente não voltaram para me contar.",

            "Silêncio. Isso é raro aqui. A maioria dos que chegam aqui ou gritam ou choram. " +
            "Sente-se, forasteiro. Ou não. A geometria de Carcosa não se importa com sua postura.",
        };

        [TextArea(2, 6)]
        [Tooltip("Uma fala por fragmento, na ordem dos índices. Se faltar, usa uma genérica.")]
        [SerializeField] private string[] falasPorFragmento;

        [TextArea(3, 8)]
        [Tooltip("Fala quando ainda faltam fragmentos. {0} vira o número restante.")]
        [SerializeField] private string falaDeEspera = "Ainda faltam {0}. Quando os tiver, volte.";

        [Header("Recital das estrofes finais")]
        [Tooltip("Painel de escolha usado nas duas perguntas do recital (mesmo componente " +
                 "da conversa com Abdul). [CENA]")]
        [SerializeField] private PainelDeEscolha painelDeEscolha;

        [TextArea(4, 10)]
        [Tooltip("Primeira fala ao abrir o recital, com o último fragmento entregue: por que " +
                 "ela pede versos que já foram dela.")]
        [SerializeField] private string falaDeAberturaDoRecital =
            "Vaine ouviu o final e não escreveu — achou que me pouparia. Mas em Carcosa o " +
            "silêncio é a pior das maldições: os nomes deles não descansam enquanto a canção " +
            "não terminar.\n\nE eu não a tenho mais, forasteiro. Cantei estes versos por " +
            "tantas eras que gastei as palavras até o osso. Não consigo mais chamá-las... " +
            "mas eu as reconheço. Se você disser certo, eu vou saber.";

        [TextArea(4, 10)]
        [Tooltip("Segunda fala do recital: Cassilda recita as duas estrofes que ainda " +
                 "lembra (releitura embutida — o jogador não tem inventário para reler os " +
                 "fragmentos sozinho).")]
        [SerializeField] private string falaDeRecapitulacao =
            "\"Ao longo da costa as ondas de nuvem se quebram,\nOs sóis gêmeos afundam por " +
            "trás do lago,\nAs sombras se alongam\nEm Carcosa.\n\nEstranha é a noite em que " +
            "as estrelas negras sobem,\nE estranhas luas circulam pelos céus...\nMas ainda " +
            "mais estranha é\na Perdida Carcosa.\"\n\nAté aqui, eu me lembro. É daqui em " +
            "diante que a canção me escapa.";

        [TextArea(2, 4)]
        [Tooltip("Pergunta da 3ª estrofe.")]
        [SerializeField] private string perguntaEstrofe3 =
            "As ondas de nuvem. Os sóis gêmeos. As estranhas luas. Depois delas vêm as " +
            "Híades — e o que elas cantam?";

        [TextArea(2, 4)]
        [Tooltip("As 3 opções da 3ª estrofe, na ordem em que aparecem no painel. Só uma é " +
                 "certa (índice fixo em código — ver RespostaCertaEstrofe3).")]
        [SerializeField] private string[] opcoesEstrofe3 =
        {
            "Onde batem os farrapos do Rei, / devem reinar sobre as cinzas da / Fosca Carcosa.",
            "Onde batem os farrapos do Rei, / devem morrer não ouvidas na / Fosca Carcosa.",
            "Onde os deuses caem e sangram, / devem afundar para sempre no lago da / Perdida Carcosa.",
        };

        [TextArea(2, 4)]
        [Tooltip("Reação ao acertar a 3ª estrofe — não pergunta a 4ª ainda; isso vem na " +
                 "próxima fala (perguntaEstrofe4), no ritmo de uma fala por aperto.")]
        [SerializeField] private string falaDeAcertoEstrofe3 =
            "Sim... morrer não ouvidas. Como eles morreram.";

        [TextArea(2, 4)]
        [Tooltip("Pergunta da 4ª e última estrofe.")]
        [SerializeField] private string perguntaEstrofe4 =
            "Falta o último suspiro. A minha parte. O que a minha alma pede?";

        [TextArea(2, 5)]
        [Tooltip("As 3 opções da 4ª estrofe. Só uma é certa (índice fixo em código — ver " +
                 "RespostaCertaEstrofe4).")]
        [SerializeField] private string[] opcoesEstrofe4 =
        {
            "Canção de minha alma, minha voz se ergue; / queima tu, iluminada, como brasas " +
            "não extintas / vão arder e viver na / Eterna Carcosa.",
            "Canção de minha alma, a corte está morta; / que os reis lamentem, como servos " +
            "sem coroa / vão secar e morrer na / Fosca Carcosa.",
            "Canção de minha alma, minha voz está morta; / morre tu, não cantada, como as " +
            "lágrimas não choradas / vão secar e morrer na / Perdida Carcosa.",
        };

        [TextArea(2, 5)]
        [Tooltip("Reação a qualquer resposta errada, nas duas estrofes. Sem punição " +
                 "mecânica (decisão do Vini, 2026-08-02) — só a frieza dela; a mesma " +
                 "pergunta reabre no próximo aperto.")]
        [SerializeField] private string falaDeErroNoRecital =
            "Não. Essa não é a nossa melodia.\n\nAlguma sombra sussurrou mentira no seu " +
            "caminho. Ouça de novo o que eles escreveram — e tente outra vez.";

        [TextArea(4, 10)]
        [Tooltip("Lamento recitado ao acertar as duas estrofes, antes da fala de conclusão " +
                 "(Patuá). Vazio pula direto para a conclusão.")]
        [SerializeField] private string falaDoLamentoFinal =
            "\"Nas luas que não piscam, Seraphel se foi rápida.\nMorthis pisou devagar até " +
            "não mais poder.\nVaine escreveu até onde ainda era seguro escrever.\nQue as " +
            "sombras descansem na areia de Hali — e que Aldaron, onde quer que esteja, " +
            "ainda seja lembrado por um nome.\"\n\nA canção está completa. Os nomes deles " +
            "têm permissão para secar e morrer, finalmente.";

        [TextArea(3, 8)]
        [Tooltip("Fala final, ao entregar o Patuá.")]
        [SerializeField] private string falaDeConclusao =
            "Tome. O Patuá das Luas Gêmeas — feito com fios das vestes de Yhtill. Ele " +
            "desacelera o que o escuro faz com a sua mente. Use as pausas que ele lhe dá.";

        /// <summary>Índice (na ordem de <see cref="opcoesEstrofe3"/>) da opção certa.</summary>
        private const int RespostaCertaEstrofe3 = 1;

        /// <summary>Índice (na ordem de <see cref="opcoesEstrofe4"/>) da opção certa.</summary>
        private const int RespostaCertaEstrofe4 = 2;

        private CancaoIncompleta _quest;

        /// <summary>Já mostrou a saudação nesta visita ao Santuário.</summary>
        private bool _saudacaoMostrada;

        /// <summary>Já resolveu a escolha A/B/C do primeiro encontro (ou não havia painel).</summary>
        private bool _escolhaInicialResolvida;

        /// <summary>Já mostrou a fala de abertura do recital nesta visita ao Santuário.</summary>
        private bool _introDoRecitalMostrada;

        /// <summary>Já recitou as duas estrofes conhecidas nesta visita ao Santuário.</summary>
        private bool _recapitulacaoMostrada;

        /// <summary>A quest corrente. Nunca null depois do <c>Awake</c>.</summary>
        public CancaoIncompleta Quest => _quest;

        // ── IInteragivel ─────────────────────────────────────────────────────

        /// <inheritdoc />
        public string RotuloDeInteracao => "Falar com Cassilda";

        /// <summary>Sempre interagível: ela continua no Santuário depois da quest.</summary>
        public bool PodeInteragir => true;

        /// <summary>Prioridade máxima: numa conversa, o NPC ganha de qualquer cenário.</summary>
        public int PrioridadeDeInteracao => 20;

        /// <inheritdoc />
        public Vector2 PosicaoDeInteracao => transform.position;

        private void Awake()
        {
            _quest = new CancaoIncompleta(totalDeFragmentos, RespostaCertaEstrofe3, RespostaCertaEstrofe4);
        }

        private void Start()
        {
            // Reconstrói o progresso: cada fragmento já recolhido conta como entregue, e a
            // quest volta ao ponto onde estava mesmo depois de trocar de cena. O recital em
            // si NÃO é persistido — sair do Santuário com a canção pela metade refaz as
            // duas perguntas ao voltar (decisão do Vini: são 20 segundos de conversa, não
            // vale o custo de mais chaves de save).
            for (int i = 0; i < _quest.Total; i++)
                if (GerenciadorDeSave.JaAconteceu(ChaveDeEntrega(i)))
                    _quest.Entregar(i);

            if (GerenciadorDeSave.JaAconteceu(ChaveDaConclusao)) _quest.Concluir();
        }

        /// <inheritdoc />
        public void Interagir(GameObject quemInterage)
        {
            switch (_quest.Estado)
            {
                case EstadoDaQuest.Concluida:
                    Falar(falaDeConclusao);
                    return;

                case EstadoDaQuest.NaoIniciada:
                    InteragirPrimeiroEncontro();
                    return;

                case EstadoDaQuest.Recitando:
                    InteragirRecital();
                    return;

                default: // EmAndamento
                    ReceberFragmentos();
                    return;
            }
        }

        /// <summary>
        /// Avança o primeiro encontro uma fala por aperto: saudação (com a escolha A/B/C
        /// aberta junto, puramente cosmética), depois o pedido da quest — que é quando ela
        /// de fato começa (<see cref="CancaoIncompleta.Iniciar"/>).
        /// </summary>
        private void InteragirPrimeiroEncontro()
        {
            if (!_saudacaoMostrada)
            {
                _saudacaoMostrada = true;
                Falar(falaDeSaudacao);

                if (painelDeEscolha == null || opcoesDoPrimeiroEncontro == null
                    || opcoesDoPrimeiroEncontro.Length == 0)
                {
                    // Nunca trava o jogo por peça de UI faltando — a escolha vira uma
                    // decoração perdida, mas o encontro segue no mesmo ritmo de 2 apertos.
                    _escolhaInicialResolvida = true;
                    return;
                }

                AbrirEscolhaDoPrimeiroEncontro();
                return;
            }

            if (!_escolhaInicialResolvida) return; // painel ainda aberto, aguardando escolha

            _quest.Iniciar();
            Falar(falaDoPedido);
        }

        private void AbrirEscolhaDoPrimeiroEncontro()
        {
            var opcoes = new OpcaoDeDialogo[opcoesDoPrimeiroEncontro.Length];
            for (int i = 0; i < opcoesDoPrimeiroEncontro.Length; i++)
                opcoes[i] = new OpcaoDeDialogo(opcoesDoPrimeiroEncontro[i], i);

            painelDeEscolha.Mostrar(opcoes, ResolverEscolhaDoPrimeiroEncontro);
        }

        private void ResolverEscolhaDoPrimeiroEncontro(int opcaoEscolhida)
        {
            _escolhaInicialResolvida = true;
            Falar(ReacaoDoPrimeiroEncontro(opcaoEscolhida));
        }

        private string ReacaoDoPrimeiroEncontro(int i)
            => reacoesDoPrimeiroEncontro != null && i >= 0 && i < reacoesDoPrimeiroEncontro.Length
                ? reacoesDoPrimeiroEncontro[i]
                : "...";

        /// <summary>
        /// Recebe todos os fragmentos que Damião carrega. Se essa entrega completar os 3 e a
        /// canção não tiver estrofes pendentes (recital vazio), fecha a quest direto — é o
        /// que mantém o comportamento anterior ao recital existir. Caso contrário, a última
        /// entrega só mostra a reação de Cassilda; o recital começa no próximo aperto.
        /// </summary>
        private void ReceberFragmentos()
        {
            int recebidosAgora = 0;
            string ultimaFala = null;

            for (int i = 0; i < _quest.Total; i++)
            {
                if (_quest.FoiEntregue(i)) continue;
                if (!GerenciadorDeSave.JaAconteceu(ChaveDoFragmento(i))) continue;

                if (!_quest.Entregar(i)) continue;

                GerenciadorDeSave.MarcarAconteceu(ChaveDeEntrega(i));
                ultimaFala = FalaDoFragmento(i);
                recebidosAgora++;
            }

            if (_quest.TodosEntregues && _quest.Recital.Completo)
            {
                ConcluirQuest();
                return;
            }

            if (recebidosAgora > 0)
            {
                // Se essa entrega já é a última página, "faltam 0" seria estranho — a
                // rainha já tem tudo, só falta a canção.
                string mensagem = _quest.Estado == EstadoDaQuest.Recitando
                    ? ultimaFala
                    : $"{ultimaFala}\n\n{string.Format(falaDeEspera, _quest.Restantes)}";
                Falar(mensagem);
                return;
            }

            // Voltou de mãos vazias: repete o que falta, sem repetir o pedido inteiro.
            Falar(string.Format(falaDeEspera, _quest.Restantes));
        }

        /// <summary>
        /// Avança o recital uma fala por aperto, no mesmo ritmo da conversa com Abdul:
        /// abertura, depois a recapitulação, depois a pergunta da estrofe corrente.
        /// </summary>
        private void InteragirRecital()
        {
            if (!_introDoRecitalMostrada)
            {
                _introDoRecitalMostrada = true;
                Falar(falaDeAberturaDoRecital);
                return;
            }

            if (!_recapitulacaoMostrada)
            {
                _recapitulacaoMostrada = true;
                Falar(falaDeRecapitulacao);
                return;
            }

            AbrirPerguntaDaEstrofeAtual();
        }

        private void AbrirPerguntaDaEstrofeAtual()
        {
            string pergunta;
            string[] opcoes;

            switch (_quest.Recital.EstrofeAtual)
            {
                case 0:
                    pergunta = perguntaEstrofe3;
                    opcoes = opcoesEstrofe3;
                    break;
                case 1:
                    pergunta = perguntaEstrofe4;
                    opcoes = opcoesEstrofe4;
                    break;
                default:
                    // Não deveria acontecer (Estado sairia de Recitando ao completar), mas
                    // não trava o jogo por uma estrofe fora da faixa configurada.
                    ConcluirQuest();
                    return;
            }

            if (painelDeEscolha == null || opcoes == null || opcoes.Length == 0)
            {
                Debug.LogWarning("[CassildaNPC] Painel de Escolha ou opções da estrofe não " +
                                 "atribuídos — o recital não pode ser respondido.", this);
                return;
            }

            int estrofe = _quest.Recital.EstrofeAtual;
            var opcoesDeDialogo = new OpcaoDeDialogo[opcoes.Length];
            for (int i = 0; i < opcoes.Length; i++)
                opcoesDeDialogo[i] = new OpcaoDeDialogo(opcoes[i], i);

            Falar(pergunta);
            painelDeEscolha.Mostrar(opcoesDeDialogo, idOpcao => ResolverRespostaDoRecital(estrofe, idOpcao));
        }

        private void ResolverRespostaDoRecital(int estrofe, int opcaoEscolhida)
        {
            if (!_quest.Responder(opcaoEscolhida))
            {
                // Retry livre: a mesma estrofe reabre no próximo aperto, sem custo.
                Falar(falaDeErroNoRecital);
                return;
            }

            if (_quest.Recital.Completo)
            {
                ConcluirQuest();
                return;
            }

            // Só a 3ª estrofe acertada cai aqui — a pergunta da 4ª vem no próximo aperto.
            Falar(falaDeAcertoEstrofe3);
        }

        private void ConcluirQuest()
        {
            if (!_quest.Concluir()) return;

            GerenciadorDeSave.MarcarAconteceu(ChaveDaConclusao);

            string mensagem = string.IsNullOrWhiteSpace(falaDoLamentoFinal)
                ? falaDeConclusao
                : $"{falaDoLamentoFinal}\n\n{falaDeConclusao}";
            Falar(mensagem);

            if (prefabPatua != null)
                Instantiate(prefabPatua, transform.position + Vector3.down, Quaternion.identity);
            else
                Debug.LogWarning("[CassildaNPC] Prefab do Patuá não atribuído — a recompensa " +
                                 "da quest não foi entregue.", this);
        }

        private string FalaDoFragmento(int i)
            => falasPorFragmento != null && i < falasPorFragmento.Length
               && !string.IsNullOrWhiteSpace(falasPorFragmento[i])
                ? falasPorFragmento[i]
                : "Obrigada por trazer a letra dele de volta para mim.";

        /// <summary>
        /// Põe uma fala de Cassilda na caixa de diálogo.
        ///
        /// <para><b>O fallback que faltava (2026-08-28).</b> <c>caixaDeTexto</c> está vazio no
        /// prefab dela, e sem este <c>?? Instancia</c> a condição <c>!= null</c> simplesmente
        /// pulava tudo: <b>nenhuma fala de Cassilda aparecia em jogo</b>, em silêncio. São as
        /// falas mais longas escritas até agora — as três reações do primeiro encontro têm
        /// quase 300 caracteres cada — e nunca chegaram à tela.</para>
        ///
        /// <para>O <c>AbdulAlhazredAI</c> já fazia esta mesma queda para
        /// <c>TutorialHintUI.Instancia</c> desde que a caixa migrou para o prefab persistente
        /// da HUD, quando toda referência de cena para ela virou nula de uma vez. A Cassilda
        /// ficou de fora daquela passada.</para>
        /// </summary>
        private void Falar(string texto)
        {
            var caixa = caixaDeTexto != null ? caixaDeTexto : TutorialHintUI.Instancia;

            if (caixa != null) caixa.Mostrar(texto);
            else
                Debug.LogWarning($"[CassildaNPC] Sem caixa de diálogo: a fala \"{texto}\" não " +
                                 "vai aparecer para o jogador.", this);
        }

        private static string ChaveDoFragmento(int i) => $"Quest.Cassilda.Fragmento{i}";
        private static string ChaveDeEntrega(int i) => $"Quest.Cassilda.Entregue{i}";
        private const string ChaveDaConclusao = "Quest.Cassilda.Concluida";
    }
}
