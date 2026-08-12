using System;

namespace FavelaAmarela.Core.Enemies
{
    /// <summary>
    /// POCO puro: a regra da luta contra o <b>Byakhee</b>, o cadeado vivo dos Portões das
    /// Ruínas. Ver <c>Docs/KnowledgeBundle/lore/cassilda_e_byakhee.md</c> §IV.
    ///
    /// <para><b>A inversão que define a luta:</b> ele é <b>imune no ar</b> e vulnerável só
    /// durante o pouso. O jogador não escolhe quando atacar — ele espera, esquiva e aproveita
    /// a janela. Aumentar a dificuldade aqui é <b>encurtar a janela</b>, não subir o dano.</para>
    ///
    /// <para><b>Grito infrassônico:</b> dreno passivo de Resiliência enquanto o Byakhee viver.
    /// É o relógio da luta — quem demora demais colapsa mesmo sem levar um golpe.</para>
    ///
    /// <para>Sem Unity: testável com <c>new ByakheeFSM()</c> e chamadas de <see cref="Tick"/>.</para>
    /// </summary>
    public sealed class ByakheeFSM
    {
        private readonly float fracaoFase2;
        private readonly float fracaoFase3;
        private readonly float fracaoFrenesi;

        private readonly float duracaoPousoFase1;
        private readonly float duracaoPousoFase2;
        private readonly float duracaoPousoDeDor;

        private readonly float duracaoRasante;
        private readonly float duracaoMergulho;
        private readonly float telegrafoDoGrito;
        private readonly float intervaloPousoEspontaneo;

        private float _fracaoDeVida = 1f;
        private float _timerCircundando;
        private bool _proximoAtaqueEhMergulho;

        /// <summary>Estado atual da luta.</summary>
        public ByakheeState CurrentState { get; private set; } = ByakheeState.Espreita;

        /// <summary>Segundos no estado atual.</summary>
        public float TimeInState { get; private set; }

        /// <summary>
        /// Se o Byakhee pode receber dano <b>agora</b>. Verdadeiro apenas em terra: no ar ele
        /// é intocável, e é isso que transforma a luta em leitura de padrão.
        /// </summary>
        public bool PodeReceberDano =>
            CurrentState == ByakheeState.Pousado || CurrentState == ByakheeState.Frenesi;

        /// <summary>Fase atual (1, 2 ou 3), derivada da vida. 0 antes de a luta começar.</summary>
        public int Fase
        {
            get
            {
                if (CurrentState == ByakheeState.Espreita) return 0;
                if (_fracaoDeVida > fracaoFase2) return 1;
                if (_fracaoDeVida > fracaoFase3) return 2;
                return 3;
            }
        }

        /// <summary>Dreno de Resiliência por segundo que o grito impõe no estado atual.</summary>
        public float DrenoDeResilienciaPorSegundo =>
            CurrentState switch
            {
                ByakheeState.Espreita => 0f,
                ByakheeState.Derrotado => 0f,
                ByakheeState.Frenesi => drenoFrenesi,
                _ => drenoPassivo
            };

        private readonly float drenoPassivo;
        private readonly float drenoFrenesi;

        /// <summary>Disparado a cada transição. (anterior, atual)</summary>
        public event Action<ByakheeState, ByakheeState> OnStateChanged;

        /// <summary>O Byakhee tocou o chão — a janela de dano abriu.</summary>
        public event Action OnPousou;

        /// <summary>Voltou a voar — a janela fechou.</summary>
        public event Action OnLevantouVoo;

        /// <summary>O cone de pressão sonora foi emitido (depois do telegrama).</summary>
        public event Action OnGritoEmitido;

        /// <summary>Abatido.</summary>
        public event Action OnDerrotado;

        /// <param name="fracaoFase2">Fração de vida que inicia a fase 2 (padrão 0,60).</param>
        /// <param name="fracaoFase3">Fração que inicia a fase 3 (padrão 0,30).</param>
        /// <param name="fracaoFrenesi">Fração que dispara o frenesi final (padrão 0,10).</param>
        /// <param name="duracaoPousoFase1">Janela de dano na fase 1, em segundos.</param>
        /// <param name="duracaoPousoFase2">Janela na fase 2 — menor de propósito.</param>
        /// <param name="duracaoPousoDeDor">Janela ampliada ao forçar o pouso na fase 3.</param>
        /// <param name="intervaloPousoEspontaneo">
        /// Sem a Lâmina do Sinal, quanto tempo circundando até pousar sozinho. É a válvula que
        /// impede a fase 3 de virar impasse para quem não tem a arma.
        /// </param>
        public ByakheeFSM(
            float fracaoFase2 = 0.60f,
            float fracaoFase3 = 0.30f,
            float fracaoFrenesi = 0.10f,
            float duracaoPousoFase1 = 2f,
            float duracaoPousoFase2 = 1.5f,
            float duracaoPousoDeDor = 3f,
            float duracaoRasante = 2f,
            float duracaoMergulho = 1.2f,
            float telegrafoDoGrito = 1f,
            float intervaloPousoEspontaneo = 15f,
            float drenoPassivo = 2f,
            float drenoFrenesi = 5f)
        {
            this.fracaoFase2 = fracaoFase2;
            this.fracaoFase3 = fracaoFase3;
            this.fracaoFrenesi = fracaoFrenesi;
            this.duracaoPousoFase1 = duracaoPousoFase1;
            this.duracaoPousoFase2 = duracaoPousoFase2;
            this.duracaoPousoDeDor = duracaoPousoDeDor;
            this.duracaoRasante = duracaoRasante;
            this.duracaoMergulho = duracaoMergulho;
            this.telegrafoDoGrito = telegrafoDoGrito;
            this.intervaloPousoEspontaneo = intervaloPousoEspontaneo;
            this.drenoPassivo = drenoPassivo;
            this.drenoFrenesi = drenoFrenesi;
        }

        /// <summary>Começa a luta: o Byakhee desce dos Portões no primeiro rasante.</summary>
        public void IniciarLuta()
        {
            if (CurrentState != ByakheeState.Espreita) return;
            Transicionar(ByakheeState.Rasante);
        }

        /// <summary>
        /// Informa a vida restante (0..1). É o que promove as fases e dispara o frenesi.
        /// </summary>
        public void AtualizarFracaoDeVida(float fracao)
        {
            int faseAnterior = Fase;
            _fracaoDeVida = fracao < 0f ? 0f : (fracao > 1f ? 1f : fracao);

            if (_fracaoDeVida <= 0f)
            {
                if (CurrentState != ByakheeState.Derrotado)
                {
                    Transicionar(ByakheeState.Derrotado);
                    OnDerrotado?.Invoke();
                }
                return;
            }

            // O frenesi interrompe qualquer padrão: é o último recurso da criatura.
            if (_fracaoDeVida <= fracaoFrenesi
                && CurrentState != ByakheeState.Frenesi
                && CurrentState != ByakheeState.Espreita)
            {
                Transicionar(ByakheeState.Frenesi);
                return;
            }

            // Entrar na fase 3 DECOLA na hora, mesmo no meio de um pouso.
            //
            // Sem isto a fase 3 quase não existia: cair para 30% durante uma janela apenas
            // ESTENDIA aquela janela (de 1,5 s para 3 s, via DuracaoDoPousoAtual), e o jogador
            // matava o Byakhee ali mesmo sem nunca vê-lo circundar. O design pede o oposto —
            // "começa a circundar a arena voando sem pousar" é a fase inteira, não um bônus
            // de tempo no pouso anterior.
            if (faseAnterior < 3 && Fase >= 3 && CurrentState == ByakheeState.Pousado)
            {
                OnLevantouVoo?.Invoke();
                Transicionar(ByakheeState.Circundando);
            }
        }

        /// <summary>
        /// Interrompe o frenesi com um golpe — mesma leitura do Nagaraja. Sem isso o dreno de
        /// 5 RM/s mata pela mente antes de a vida acabar.
        /// </summary>
        /// <returns>Se havia um frenesi para interromper.</returns>
        public bool InterromperFrenesi()
        {
            if (CurrentState != ByakheeState.Frenesi) return false;

            // Cai pousado: interromper o grito derruba a criatura, e a recompensa por acertar
            // a interrupção é justamente a janela de dano que vem junto.
            Pousar();
            return true;
        }

        /// <summary>
        /// Corta a asa durante um rasante (exige a Lâmina do Sinal). Força o pouso de dor,
        /// que é a janela ampliada da fase 3.
        /// </summary>
        /// <returns>Se o corte teve efeito (só vale em voo, na fase 3).</returns>
        public bool CortarAsa()
        {
            if (Fase < 3) return false;
            if (CurrentState != ByakheeState.Rasante && CurrentState != ByakheeState.Circundando)
                return false;

            Pousar();
            return true;
        }

        /// <summary>Avança o relógio da luta.</summary>
        public void Tick(float deltaTime)
        {
            if (deltaTime <= 0f) return;
            if (CurrentState == ByakheeState.Espreita || CurrentState == ByakheeState.Derrotado)
                return;

            TimeInState += deltaTime;

            switch (CurrentState)
            {
                case ByakheeState.Rasante:
                    if (TimeInState >= duracaoRasante) DepoisDoVoo();
                    break;

                case ByakheeState.MergulhoDeGarras:
                    if (TimeInState >= duracaoMergulho) Pousar();
                    break;

                case ByakheeState.GritoDirecionado:
                    if (TimeInState >= telegrafoDoGrito)
                    {
                        OnGritoEmitido?.Invoke();
                        Transicionar(ByakheeState.Rasante);
                    }
                    break;

                case ByakheeState.Pousado:
                    if (TimeInState >= DuracaoDoPousoAtual())
                    {
                        OnLevantouVoo?.Invoke();
                        Transicionar(Fase >= 3 ? ByakheeState.Circundando : ByakheeState.Rasante);
                    }
                    break;

                case ByakheeState.Circundando:
                    _timerCircundando += deltaTime;
                    if (_timerCircundando >= intervaloPousoEspontaneo) Pousar();
                    break;

                case ByakheeState.Frenesi:
                    // Não sai sozinho: só um golpe interrompe. O relógio corre contra o jogador.
                    break;
            }
        }

        /// <summary>
        /// Escolhe o próximo padrão ao fim de um rasante. Alterna mergulho e pouso para o
        /// jogador não decorar uma resposta só; na fase 2+ o grito entra na rotação.
        /// </summary>
        private void DepoisDoVoo()
        {
            if (Fase >= 3)
            {
                Transicionar(ByakheeState.Circundando);
                return;
            }

            if (Fase >= 2 && _proximoAtaqueEhMergulho)
            {
                _proximoAtaqueEhMergulho = false;
                Transicionar(ByakheeState.GritoDirecionado);
                return;
            }

            if (_proximoAtaqueEhMergulho)
            {
                _proximoAtaqueEhMergulho = false;
                Transicionar(ByakheeState.MergulhoDeGarras);
                return;
            }

            _proximoAtaqueEhMergulho = true;
            Pousar();
        }

        /// <summary>
        /// A janela encurta da fase 1 para a 2. O pouso de dor da fase 3 é o mais longo —
        /// é a recompensa por forçar a descida em vez de esperar.
        /// </summary>
        private float DuracaoDoPousoAtual() => Fase switch
        {
            1 => duracaoPousoFase1,
            2 => duracaoPousoFase2,
            _ => duracaoPousoDeDor
        };

        /// <summary>
        /// Leva ao chão e anuncia a janela. Concentrado num método só porque o pouso vem de
        /// quatro caminhos diferentes (fim de rasante, fim de mergulho, corte de asa e
        /// interrupção do frenesi) — e esquecer o evento em um deles deixaria a janela de dano
        /// aberta sem ninguém saber.
        /// </summary>
        private void Pousar()
        {
            Transicionar(ByakheeState.Pousado);
            OnPousou?.Invoke();
        }

        private void Transicionar(ByakheeState novo)
        {
            if (novo == CurrentState) return;

            var anterior = CurrentState;
            CurrentState = novo;
            TimeInState = 0f;

            // Zera aqui, e não ao sair: entrar em Circundando é o marco que reinicia a
            // contagem para o pouso espontâneo.
            if (novo == ByakheeState.Circundando) _timerCircundando = 0f;

            OnStateChanged?.Invoke(anterior, novo);
        }
    }
}
