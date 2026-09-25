using System;

namespace FavelaAmarela.Core.Enemies
{
    /// <summary>
    /// Máquina de estados pura da luta contra <b>Abdul Alhazred</b>, a Aparição Primordial
    /// da Tumba (ver <c>Docs/KnowledgeBundle/lore/abdul_alhazred.md</c>). POCO — nenhuma
    /// dependência de Unity.
    ///
    /// <para>O escudo é o coração do combate: <b>o dano só entra quando o escudo está
    /// baixo</b>. Na Fase 1 isso depende de quebrar Pedras de Poder na arena; na Fase 2 o
    /// escudo é permanente e só cai quando a mana dele acaba (após o ciclo de conjurações),
    /// criando a janela do golpe de misericórdia.</para>
    ///
    /// <para>A FSM <b>não</b> guarda a vida — quem tem a <c>Vitalidade</c> é o adaptador
    /// Runtime, que informa a fração restante por <see cref="AtualizarFracaoDeVida"/> e
    /// consulta <see cref="PodeReceberDano"/> antes de aplicar um golpe. Mesma divisão de
    /// responsabilidade de <c>CultistaFSM</c>/<c>CultistaAI</c>.</para>
    /// </summary>
    public sealed class AbdulFSM
    {
        private readonly float fracaoParaFase2;
        private readonly float duracaoEscudoQuebrado;
        private readonly int magiasPorCiclo;
        private readonly float duracaoExaustao;
        private readonly float intervaloDeConjuracao;

        private float _timerEscudoQuebrado;
        private float _timerExaustao;
        private float _timerConjuracao;
        private bool _escudoAtivo = true;
        private int _magiasNoCiclo;
        private bool _proximaMagiaEhCone = true;

        /// <summary>Estado atual da luta.</summary>
        public AbdulState CurrentState { get; private set; } = AbdulState.Transe;

        /// <summary>Tempo decorrido no estado atual, em segundos.</summary>
        public float TimeInState { get; private set; }

        /// <summary>
        /// Se o Escudo Mágico está de pé. Enquanto estiver, Abdul é impenetrável —
        /// é isto que impede a luta de ser um simples "bata até cair".
        /// </summary>
        public bool EscudoAtivo => _escudoAtivo;

        /// <summary>
        /// Se um golpe pode ferir Abdul agora: escudo baixo e a luta em andamento
        /// (em Transe ele é intocável; Derrotado já acabou).
        /// </summary>
        public bool PodeReceberDano =>
            !_escudoAtivo && CurrentState != AbdulState.Transe && CurrentState != AbdulState.Derrotado;

        /// <summary>Quantas Pedras de Poder foram quebradas nesta luta (telemetria/UI).</summary>
        public int PedrasQuebradas { get; private set; }

        /// <summary>
        /// Quantas Pedras sustentam o escudo nesta luta. Informado pelo adaptador ao
        /// invocá-las — a FSM não decide quantas nascem, mas precisa saber para reconhecer
        /// quando a última cai.
        /// </summary>
        public int TotalDePedras { get; private set; }

        /// <summary>
        /// Se todas as Pedras já foram quebradas. A partir daqui o escudo <b>não volta
        /// mais</b> na Fase 1 — não há mais o que o sustente.
        /// </summary>
        public bool EscudoDestruido => TotalDePedras > 0 && PedrasQuebradas >= TotalDePedras;

        /// <summary>Magias já conjuradas no ciclo de mana corrente (Fase 2).</summary>
        public int MagiasNoCiclo => _magiasNoCiclo;

        /// <summary>Transição de estado: (anterior, atual).</summary>
        public event Action<AbdulState, AbdulState> OnStateChanged;

        /// <summary>Escudo Mágico subiu (true) ou caiu (false) — a UI/VFX reage a isto.</summary>
        public event Action<bool> OnEscudoMudou;

        /// <summary>Abdul invocou esqueletos para flanquear Damião.</summary>
        public event Action OnInvocarEsqueletos;

        /// <summary>Abdul conjurou um Cone de Gelo.</summary>
        public event Action OnConjurarConeDeGelo;

        /// <summary>Abdul foi abatido — o adaptador dropa o Necronomicon.</summary>
        public event Action OnDerrotado;

        /// <param name="fracaoParaFase2">Fração de vida que dispara a Fase 2 (default 0,35 = 35%).</param>
        /// <param name="duracaoEscudoQuebrado">Segundos que o escudo fica baixo ao quebrar uma Pedra de Poder.</param>
        /// <param name="magiasPorCiclo">Quantas magias ele conjura antes de esgotar a mana (Fase 2).</param>
        /// <param name="duracaoExaustao">Segundos de exaustão (escudo baixo) após esgotar a mana.</param>
        /// <param name="intervaloDeConjuracao">Segundos entre conjurações.</param>
        public AbdulFSM(
            float fracaoParaFase2 = 0.35f,
            float duracaoEscudoQuebrado = 6f,
            int magiasPorCiclo = 3,
            float duracaoExaustao = 5f,
            float intervaloDeConjuracao = 3f)
        {
            this.fracaoParaFase2 = fracaoParaFase2;
            this.duracaoEscudoQuebrado = duracaoEscudoQuebrado > 0f ? duracaoEscudoQuebrado : 6f;
            this.magiasPorCiclo = magiasPorCiclo > 0 ? magiasPorCiclo : 3;
            this.duracaoExaustao = duracaoExaustao > 0f ? duracaoExaustao : 5f;
            this.intervaloDeConjuracao = intervaloDeConjuracao > 0f ? intervaloDeConjuracao : 3f;
        }

        /// <summary>
        /// Tira Abdul do transe e começa a luta (Damião interagiu com o grimório).
        /// Ignorado se a luta já começou.
        /// </summary>
        public void IniciarLuta()
        {
            if (CurrentState != AbdulState.Transe) return;
            DefinirEscudo(true);
            ChangeState(AbdulState.Fase1);
        }

        /// <summary>
        /// Informa quantas Pedras sustentam o escudo. Chamado pelo adaptador ao invocá-las
        /// no início da Fase 1.
        /// </summary>
        public void DefinirTotalDePedras(int total)
        {
            TotalDePedras = total > 0 ? total : 0;
        }

        /// <summary>
        /// Damião quebrou uma Pedra de Poder. <b>Só tem efeito na Fase 1</b> — na Fase 2 o
        /// escudo é permanente e não depende mais das Pedras (é o que muda o plano do
        /// jogador na virada de fase).
        ///
        /// <para><b>A última Pedra derruba o escudo de vez.</b> Antes, cada Pedra abria só
        /// uma janela temporária e o escudo sempre voltava — o que tornava a luta
        /// <b>invencível</b> depois da última Pedra, se Damião não tivesse conseguido levar
        /// Abdul abaixo do limiar da Fase 2 nas janelas anteriores. Não havia mais o que
        /// quebrar e o escudo nunca mais caía: softlock. As Pedras são as âncoras do escudo;
        /// sem nenhuma de pé, não há escudo.</para>
        /// </summary>
        public void QuebrarPedraDePoder()
        {
            if (CurrentState != AbdulState.Fase1) return;

            PedrasQuebradas++;
            DefinirEscudo(false);

            // Última Pedra: escudo permanentemente baixo (o timer deixa de valer).
            _timerEscudoQuebrado = EscudoDestruido ? 0f : duracaoEscudoQuebrado;
        }

        /// <summary>
        /// Informa a fração de vida restante (0..1). Dispara a virada para a Fase 2 e a
        /// derrota. Chamado pelo adaptador após cada golpe aplicado.
        /// </summary>
        public void AtualizarFracaoDeVida(float fracao)
        {
            if (CurrentState == AbdulState.Transe || CurrentState == AbdulState.Derrotado) return;

            if (fracao <= 0f)
            {
                DefinirEscudo(false);
                ChangeState(AbdulState.Derrotado);
                OnDerrotado?.Invoke();
                return;
            }

            // Entra na Fase 2 vindo da Fase 1 (ou da exaustão da Fase 1).
            if (fracao <= fracaoParaFase2 && CurrentState == AbdulState.Fase1)
            {
                ReiniciarCicloDeMana();
                DefinirEscudo(true); // escudo permanente a partir daqui
                ChangeState(AbdulState.Fase2);
            }
        }

        /// <summary>Avança o relógio da luta.</summary>
        public void Tick(float dt)
        {
            TimeInState += dt;

            switch (CurrentState)
            {
                case AbdulState.Fase1:
                    TickFase1(dt);
                    break;
                case AbdulState.Fase2:
                    TickFase2(dt);
                    break;
                case AbdulState.Exausto:
                    TickExausto(dt);
                    break;
            }
        }

        private void TickFase1(float dt)
        {
            // Escudo baixo por Pedra quebrada: conta o tempo e reconjura o escudo.
            // Com todas as Pedras destruídas ele não volta — não há mais o que o sustente.
            if (!_escudoAtivo && !EscudoDestruido)
            {
                _timerEscudoQuebrado -= dt;
                if (_timerEscudoQuebrado <= 0f)
                    DefinirEscudo(true);
            }

            // Invoca esqueletos em cadência, independente do escudo.
            _timerConjuracao += dt;
            if (_timerConjuracao >= intervaloDeConjuracao)
            {
                _timerConjuracao -= intervaloDeConjuracao;
                OnInvocarEsqueletos?.Invoke();
            }
        }

        private void TickFase2(float dt)
        {
            _timerConjuracao += dt;
            if (_timerConjuracao < intervaloDeConjuracao) return;

            _timerConjuracao -= intervaloDeConjuracao;

            // Alterna Cone de Gelo e esqueletos; ambos custam mana.
            if (_proximaMagiaEhCone) OnConjurarConeDeGelo?.Invoke();
            else OnInvocarEsqueletos?.Invoke();
            _proximaMagiaEhCone = !_proximaMagiaEhCone;

            _magiasNoCiclo++;
            if (_magiasNoCiclo >= magiasPorCiclo)
            {
                // Mana esgotada: escudo cai e abre a janela de burst.
                _timerExaustao = duracaoExaustao;
                DefinirEscudo(false);
                ChangeState(AbdulState.Exausto);
            }
        }

        private void TickExausto(float dt)
        {
            _timerExaustao -= dt;
            if (_timerExaustao > 0f) return;

            // Recupera a mana: escudo permanente volta e o ciclo recomeça.
            ReiniciarCicloDeMana();
            DefinirEscudo(true);
            ChangeState(AbdulState.Fase2);
        }

        private void ReiniciarCicloDeMana()
        {
            _magiasNoCiclo = 0;
            _timerConjuracao = 0f;
        }

        private void DefinirEscudo(bool ativo)
        {
            if (_escudoAtivo == ativo) return;
            _escudoAtivo = ativo;
            OnEscudoMudou?.Invoke(ativo);
        }

        private void ChangeState(AbdulState novo)
        {
            if (CurrentState == novo) return;
            var anterior = CurrentState;
            CurrentState = novo;
            TimeInState = 0f;
            OnStateChanged?.Invoke(anterior, novo);
        }
    }
}
