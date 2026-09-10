using System;
using System.Collections.Generic;

namespace FavelaAmarela.Core.Enemies
{
    /// <summary>
    /// POCO puro: a regra do confronto final contra o <b>Rei em Amarelo</b>, no Trono de
    /// Aldebaran. Ver <c>Docs/KnowledgeBundle/systems/level_design_castelo_carcosa.md</c> §Z5.
    ///
    /// <para><b>Duas metades, naturezas opostas.</b> Primeiro <see cref="AtivarReliquia"/>
    /// pela arena — sem pressão, sem relógio. Depois de todas ativas, o rito de selamento
    /// começa: o Rei se desvela em ciclos, e cada um é um teste de posição. Não existe dano
    /// nem barra — <see cref="Tick"/> pede um booleano só, "o jogador está abrigado agora?",
    /// e decide sobreviver ou colapsar com base nisso.</para>

    /// <para><b>Cada artefato ergue um escudo por vez</b> (pedido do Vini, 2026-09-10). A cada
    /// ciclo uma das relíquias ativadas acende o abrigo dela — <see cref="ReliquiaDoCiclo"/> —
    /// e Damião precisa estar dentro quando o Rei se desvelar. As relíquias se revezam na
    /// <b>ordem em que foram ativadas</b>, uma por ciclo, dando a volta se os ciclos passarem
    /// da conta.</para>

    /// <para><b>O que isto substituiu, e por quê.</b> A resposta era dar as costas ao Rei
    /// (<c>DetectorDeCostas</c>). O Vini jogou e relatou: <i>"Não tem como evitar o ataque do
    /// Rei, nem de costas."</i> Aquela mecânica lia <c>PlayerMovement.LookDirection</c>, que só
    /// é atualizada <b>enquanto o jogador anda</b>; quem parava para ler o aviso na tela ficava
    /// com o olhar preso no Rei e morria sem ter o que fazer. O abrigo não tem esse buraco:
    /// posição é posição, parado ou andando.</para>

    /// <para><b>E o escudo acende no começo da calmaria, não no desvelo.</b> É o que transforma
    /// os 6 s de espera em corrida — medido na cena do Trono, a travessia mais longa entre dois
    /// altares é 20 unidades, ou 4,44 s andando. Cabe, e sobra pouco.</para>

    /// <para><b>A quarta fase: o Confronto</b> (pedido do Vini, 2026-09-10 — <i>"depois dos três
    /// escudos, abre-se uma fase de combate contra ele"</i>). Sobrevividos os ciclos de
    /// selamento, o rito <b>não termina</b>: entra em <see cref="EmConfronto"/>. O ritmo é o
    /// mesmo — escudo, calmaria, desvelo — com uma diferença: <b>entre os desvelos, o Rei
    /// sangra</b> (<see cref="PodeReceberDano"/>). Damião tem de sair do abrigo, ferir e voltar
    /// antes do próximo desvelo. As três primeiras fases ensinam o relógio; a quarta cobra.</para>

    /// <para><b>Por que o mesmo ritmo, e não um chefe de espada.</b> O Rei tem cinco clipes de
    /// animação — idle, selar, desvelo, dano, queda — e <b>nenhum de ataque</b>. Um confronto em
    /// que ele avança e golpeia precisaria de arte que não existe. O desvelo já é o ataque dele,
    /// e o abrigo já é a resposta; a fase nova só dá ao jogador o que faltava: encostar nele.
    /// A vitória continua sendo <see cref="ReiEmAmareloState.Selado"/>, agora alcançada por
    /// <see cref="Abater"/> — quem decide que a Vitalidade zerou é o adaptador.</para>
    ///
    /// <para><b>A lista de relíquias exigidas é dado, não constante.</b> O design pede 4
    /// (Anel, Coroa, Patuá, Necronomicon), mas a Coroa de Ossos não tem fonte jogável ainda
    /// (Templo da Serpente sem cena) — ver `boss_rei_em_amarelo.md`. Receber a lista no
    /// construtor, como o `TabelaDeDrop` recebe entradas, deixa isso ser decisão de quem
    /// monta a luta, não do código.</para>
    ///
    /// <para>Sem Unity: testável com <c>new ReiEmAmareloFSM(...)</c> e chamadas de
    /// <see cref="Tick"/>.</para>
    /// </summary>
    public sealed class ReiEmAmareloFSM
    {
        private readonly HashSet<string> _reliquiasExigidas;
        private readonly HashSet<string> _reliquiasAtivas = new HashSet<string>();

        /// <summary>
        /// As relíquias na ordem em que o jogador as ativou — é essa ordem que o revezamento
        /// dos escudos segue. <c>HashSet</c> não guarda ordem, e "qual escudo acende agora"
        /// precisa ser determinístico para o jogador aprender a luta e para o teste medir.
        /// </summary>
        private readonly List<string> _ordemDeAtivacao = new List<string>();

        private readonly int _ciclosDeSelamento;
        private readonly float _duracaoDaJanela;
        private readonly float _intervaloEntreCiclos;
        private readonly float _intervaloNoConfronto;

        private int _ciclosSobrevividos;
        private float _timerDoEstado;
        private bool _sobreviveuOCicloAtual;

        /// <summary>Estado atual do confronto.</summary>
        public ReiEmAmareloState CurrentState { get; private set; } = ReiEmAmareloState.Aguardando;

        /// <summary>Segundos no estado atual.</summary>
        public float TimeInState => _timerDoEstado;

        /// <summary>Quantas relíquias já foram ativadas, das exigidas.</summary>
        public int ReliquiasAtivas => _reliquiasAtivas.Count;

        /// <summary>Quantas relíquias o rito exige ao todo.</summary>
        public int TotalDeReliquiasExigidas => _reliquiasExigidas.Count;

        /// <summary>Se todas as relíquias exigidas já foram ativadas.</summary>
        public bool TodasAsReliquiasAtivas => _reliquiasAtivas.Count >= _reliquiasExigidas.Count;

        /// <summary>
        /// A relíquia que ergue o escudo <b>neste</b> ciclo, ou <c>null</c> fora do rito de
        /// selamento. É onde Damião precisa estar quando o Rei se desvelar.
        /// </summary>
        public string ReliquiaDoCiclo { get; private set; }

        /// <summary>Quantos ciclos de desvelar já foram sobrevividos.</summary>
        public int CiclosSobrevividos => _ciclosSobrevividos;

        /// <summary>Quantos ciclos o rito de selamento exige antes do Confronto.</summary>
        public int TotalDeCiclos => _ciclosDeSelamento;

        /// <summary>
        /// Se o rito já entrou na quarta fase: os escudos continuam se revezando, mas agora o
        /// Rei pode ser ferido entre os desvelos.
        /// </summary>
        public bool EmConfronto { get; private set; }

        /// <summary>
        /// Se um golpe entregue <b>agora</b> fere o Rei: só no Confronto, e só na calmaria.
        ///
        /// <para>Durante o desvelo ele é imune — quem está batendo nele nesse instante está
        /// fora do abrigo, e o rito já cobra isso. A imunidade existe para a leitura ser uma
        /// só: <i>"ele sangra entre os desvelos"</i>, como o Byakhee sangra pousado.</para>
        /// </summary>
        public bool PodeReceberDano =>
            EmConfronto && CurrentState == ReiEmAmareloState.Selando;

        /// <summary>Disparado a cada transição. (anterior, atual)</summary>
        public event Action<ReiEmAmareloState, ReiEmAmareloState> OnStateChanged;

        /// <summary>Uma relíquia foi ativada com sucesso. (id, quantas faltam)</summary>
        public event Action<string, int> OnReliquiaAtivada;

        /// <summary>
        /// Um escudo acendeu: esta relíquia abriga o ciclo que começa. Disparado na entrada de
        /// <see cref="ReiEmAmareloState.Selando"/>, ou seja, no <b>começo da calmaria</b> — a
        /// corrida até lá é a calmaria inteira, não a janela do desvelo.
        /// </summary>
        public event Action<string> OnEscudoAceso;

        /// <summary>O Rei começou a se desvelar — a janela de reação abriu agora.</summary>
        public event Action OnComecouADesvelar;

        /// <summary>Um ciclo de desvelar foi sobrevivido (o jogador deu as costas a tempo).</summary>
        public event Action OnCicloSobrevivido;

        /// <summary>
        /// Os selos fecharam e a Máscara caiu: começa o Confronto. Dispara uma vez.
        /// </summary>
        public event Action OnComecouOConfronto;

        /// <summary>O rito se completou — vitória.</summary>
        public event Action OnSelado;

        /// <summary>O jogador foi visto de frente — derrota instantânea.</summary>
        public event Action OnColapso;

        /// <param name="reliquiasExigidas">
        /// Ids das relíquias que o rito exige. Duplicatas são ignoradas (mesmo id ativado duas
        /// vezes não conta duas vezes).
        /// </param>
        /// <param name="ciclosDeSelamento">
        /// Quantas vezes o Rei se desvela até o rito se completar. Não está no design doc —
        /// default de 3, pensado para ser calibrado na Arena de Testes, não em simulação: é
        /// mecânica de reação, sem "jogo perfeito" simulável como o DPS do Byakhee.
        /// </param>
        /// <param name="duracaoDaJanela">
        /// Segundos de reação por desvelar. O design doc é explícito: 1,5 s. Não é estimativa
        /// minha — é o único número que a doc realmente especifica para este chefe.
        /// </param>
        /// <param name="intervaloEntreCiclos">Segundos de calmaria entre um desvelar e o próximo.</param>
        /// <param name="intervaloNoConfronto">
        /// Segundos de calmaria <b>no Confronto</b> — o tempo de ir, ferir e voltar. Zero ou
        /// negativo usa o mesmo <paramref name="intervaloEntreCiclos"/>. É um número à parte
        /// porque a fase pede mais do jogador: sair do abrigo, e não só chegar nele.
        /// </param>
        public ReiEmAmareloFSM(
            IEnumerable<string> reliquiasExigidas,
            int ciclosDeSelamento = 3,
            float duracaoDaJanela = 1.5f,
            float intervaloEntreCiclos = 6f,
            float intervaloNoConfronto = 0f)
        {
            _reliquiasExigidas = new HashSet<string>(reliquiasExigidas ?? Array.Empty<string>());
            if (_reliquiasExigidas.Count == 0)
                throw new ArgumentException("O rito precisa de ao menos uma relíquia exigida.",
                    nameof(reliquiasExigidas));

            _ciclosDeSelamento = Math.Max(1, ciclosDeSelamento);
            _duracaoDaJanela = duracaoDaJanela;
            _intervaloEntreCiclos = intervaloEntreCiclos;
            _intervaloNoConfronto = intervaloNoConfronto > 0f ? intervaloNoConfronto
                                                              : intervaloEntreCiclos;
        }

        /// <summary>
        /// O Rei caiu: a Vitalidade dele zerou. Só tem efeito no Confronto — antes dele o Rei
        /// não pode ser ferido, e depois de Selado ou Colapso não há mais o que abater.
        /// </summary>
        /// <returns>Se o abate teve efeito.</returns>
        public bool Abater()
        {
            if (!EmConfronto) return false;
            if (CurrentState == ReiEmAmareloState.Selado
                || CurrentState == ReiEmAmareloState.Colapso) return false;

            Transicionar(ReiEmAmareloState.Selado);
            OnSelado?.Invoke();
            return true;
        }

        /// <summary>Começa o confronto: a arena libera os pontos focais.</summary>
        public void Iniciar()
        {
            if (CurrentState != ReiEmAmareloState.Aguardando) return;
            Transicionar(ReiEmAmareloState.AtivandoReliquias);
        }

        /// <summary>
        /// Ativa uma relíquia num ponto focal. Só tem efeito durante
        /// <see cref="ReiEmAmareloState.AtivandoReliquias"/> — o design não prevê ativar
        /// relíquia com o rito já em curso.
        /// </summary>
        /// <returns>Se a ativação teve efeito.</returns>
        public bool AtivarReliquia(string id)
        {
            if (CurrentState != ReiEmAmareloState.AtivandoReliquias) return false;
            if (string.IsNullOrWhiteSpace(id)) return false;
            if (!_reliquiasExigidas.Contains(id)) return false;
            if (!_reliquiasAtivas.Add(id)) return false; // já estava ativa
            _ordemDeAtivacao.Add(id);

            int faltam = _reliquiasExigidas.Count - _reliquiasAtivas.Count;
            OnReliquiaAtivada?.Invoke(id, faltam);

            if (TodasAsReliquiasAtivas)
                Transicionar(ReiEmAmareloState.Selando);

            return true;
        }

        /// <summary>
        /// Avança o relógio do confronto.
        /// </summary>
        /// <param name="deltaTime">Segundos desde o último Tick.</param>
        /// <param name="jogadorEstaAbrigado">
        /// Se o jogador está dentro do escudo da <see cref="ReliquiaDoCiclo"/> <b>agora</b>. Só
        /// importa durante <see cref="ReiEmAmareloState.Desvelado"/> — fora dessa janela, é
        /// ignorado.
        /// </param>
        public void Tick(float deltaTime, bool jogadorEstaAbrigado)
        {
            if (deltaTime <= 0f) return;

            var estadosQueNaoAvancam = CurrentState == ReiEmAmareloState.Aguardando
                                       || CurrentState == ReiEmAmareloState.AtivandoReliquias
                                       || CurrentState == ReiEmAmareloState.Selado
                                       || CurrentState == ReiEmAmareloState.Colapso;
            if (estadosQueNaoAvancam) return;

            _timerDoEstado += deltaTime;

            switch (CurrentState)
            {
                case ReiEmAmareloState.Selando:
                    if (_timerDoEstado >= (EmConfronto ? _intervaloNoConfronto
                                                        : _intervaloEntreCiclos))
                        Transicionar(ReiEmAmareloState.Desvelado);
                    break;

                case ReiEmAmareloState.Desvelado:
                    // Estar abrigado SALVA assim que acontecer — não precisa se manter do
                    // início ao fim da janela. Quem chegou correndo e pisou dentro no último
                    // instante sobrevive, e quem já estava lá sobrevive no primeiro quadro.
                    if (jogadorEstaAbrigado) _sobreviveuOCicloAtual = true;

                    if (_sobreviveuOCicloAtual)
                    {
                        SobreviverCiclo();
                    }
                    else if (_timerDoEstado >= _duracaoDaJanela)
                    {
                        Transicionar(ReiEmAmareloState.Colapso);
                        OnColapso?.Invoke();
                    }
                    break;
            }
        }

        private void SobreviverCiclo()
        {
            _ciclosSobrevividos++;
            OnCicloSobrevivido?.Invoke();

            // O último selo não sela: DESMASCARA. A partir daqui os ciclos continuam, o
            // revezamento dos escudos continua (dá a volta na lista), e o Rei sangra entre os
            // desvelos. A vitória vem por Abater(), quando a Vitalidade dele zerar.
            if (!EmConfronto && _ciclosSobrevividos >= _ciclosDeSelamento)
            {
                EmConfronto = true;
                OnComecouOConfronto?.Invoke();
            }

            Transicionar(ReiEmAmareloState.Selando);
        }

        private void Transicionar(ReiEmAmareloState novo)
        {
            if (novo == CurrentState) return;

            var anterior = CurrentState;
            CurrentState = novo;
            _timerDoEstado = 0f;

            if (novo == ReiEmAmareloState.Selando)
            {
                // O ciclo que COMEÇA é o de índice _ciclosSobrevividos. Dá a volta na lista se
                // o rito pedir mais ciclos do que há relíquias — o padrão é um por relíquia.
                ReliquiaDoCiclo = _ordemDeAtivacao.Count == 0
                    ? null
                    : _ordemDeAtivacao[_ciclosSobrevividos % _ordemDeAtivacao.Count];

                if (ReliquiaDoCiclo != null) OnEscudoAceso?.Invoke(ReliquiaDoCiclo);
            }

            if (novo == ReiEmAmareloState.Selado || novo == ReiEmAmareloState.Colapso)
                ReliquiaDoCiclo = null;

            if (novo == ReiEmAmareloState.Desvelado)
            {
                _sobreviveuOCicloAtual = false;
                OnComecouADesvelar?.Invoke();
            }

            OnStateChanged?.Invoke(anterior, novo);
        }
    }
}
