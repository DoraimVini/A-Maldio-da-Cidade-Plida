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
    /// começa: o Rei se desvela em ciclos, e cada um é um teste de reação puro. Não existe
    /// dano nem barra — <see cref="Tick"/> pede um booleano só, "o jogador está de costas
    /// agora?", e decide sobreviver ou colapsar com base nisso.</para>
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

        private readonly int _ciclosDeSelamento;
        private readonly float _duracaoDaJanela;
        private readonly float _intervaloEntreCiclos;

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

        /// <summary>Quantos ciclos de desvelar já foram sobrevividos.</summary>
        public int CiclosSobrevividos => _ciclosSobrevividos;

        /// <summary>Quantos ciclos o rito de selamento exige.</summary>
        public int TotalDeCiclos => _ciclosDeSelamento;

        /// <summary>Disparado a cada transição. (anterior, atual)</summary>
        public event Action<ReiEmAmareloState, ReiEmAmareloState> OnStateChanged;

        /// <summary>Uma relíquia foi ativada com sucesso. (id, quantas faltam)</summary>
        public event Action<string, int> OnReliquiaAtivada;

        /// <summary>O Rei começou a se desvelar — a janela de reação abriu agora.</summary>
        public event Action OnComecouADesvelar;

        /// <summary>Um ciclo de desvelar foi sobrevivido (o jogador deu as costas a tempo).</summary>
        public event Action OnCicloSobrevivido;

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
        public ReiEmAmareloFSM(
            IEnumerable<string> reliquiasExigidas,
            int ciclosDeSelamento = 3,
            float duracaoDaJanela = 1.5f,
            float intervaloEntreCiclos = 6f)
        {
            _reliquiasExigidas = new HashSet<string>(reliquiasExigidas ?? Array.Empty<string>());
            if (_reliquiasExigidas.Count == 0)
                throw new ArgumentException("O rito precisa de ao menos uma relíquia exigida.",
                    nameof(reliquiasExigidas));

            _ciclosDeSelamento = Math.Max(1, ciclosDeSelamento);
            _duracaoDaJanela = duracaoDaJanela;
            _intervaloEntreCiclos = intervaloEntreCiclos;
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
        /// <param name="jogadorEstaDeCostas">
        /// Se o jogador está de costas para o Rei <b>agora</b>. Só importa durante
        /// <see cref="ReiEmAmareloState.Desvelado"/> — fora dessa janela, é ignorado.
        /// </param>
        public void Tick(float deltaTime, bool jogadorEstaDeCostas)
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
                    if (_timerDoEstado >= _intervaloEntreCiclos)
                        Transicionar(ReiEmAmareloState.Desvelado);
                    break;

                case ReiEmAmareloState.Desvelado:
                    // De costas SALVA assim que acontecer — não precisa estar de costas do
                    // início ao fim da janela. É um reflexo pontual, não um estado a manter,
                    // o que casa com "1,5 s para reagir" em vez de "1,5 s parado de costas".
                    if (jogadorEstaDeCostas) _sobreviveuOCicloAtual = true;

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

            if (_ciclosSobrevividos >= _ciclosDeSelamento)
            {
                Transicionar(ReiEmAmareloState.Selado);
                OnSelado?.Invoke();
            }
            else
            {
                Transicionar(ReiEmAmareloState.Selando);
            }
        }

        private void Transicionar(ReiEmAmareloState novo)
        {
            if (novo == CurrentState) return;

            var anterior = CurrentState;
            CurrentState = novo;
            _timerDoEstado = 0f;

            if (novo == ReiEmAmareloState.Desvelado)
            {
                _sobreviveuOCicloAtual = false;
                OnComecouADesvelar?.Invoke();
            }

            OnStateChanged?.Invoke(anterior, novo);
        }
    }
}
