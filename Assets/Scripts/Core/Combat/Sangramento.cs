using System;

namespace FavelaAmarela.Core.Combat
{
    /// <summary>
    /// Resultado de um intervalo de sangramento: quanto dano contínuo saiu e se os
    /// acúmulos estouraram neste tick. <c>readonly struct</c> para não alocar por frame
    /// (Regra de Ouro 1) — o escoamento roda em todo <c>FixedUpdate</c> de quem sangra.
    /// </summary>
    public readonly struct TickDeSangramento
    {
        /// <summary>Dano contínuo acumulado neste intervalo (0 se não há ferida).</summary>
        public readonly float DanoContinuo;

        /// <summary>True apenas no tick em que os acúmulos atingiram o teto e estouraram.</summary>
        public readonly bool Explodiu;

        public TickDeSangramento(float danoContinuo, bool explodiu)
        {
            DanoContinuo = danoContinuo;
            Explodiu = explodiu;
        }
    }

    /// <summary>
    /// Ferida sangrante do <b>Estilete de Irem</b>, em acúmulos. Cada golpe abre mais uma
    /// ferida; ao chegar ao teto (10), todas <b>estouram de uma vez</b> num dano baseado em
    /// percentual de vida (ver <see cref="ExplosaoDeSangramento"/>) e a contagem zera.
    ///
    /// <para>Isso resolve o problema de identidade da arma: o Estilete tem o menor dano do
    /// baú e perderia feio a disputa por dano-por-janela contra o Alfanje. Com acúmulo +
    /// estouro, ele **converte permanência em burst** — quem fica em cima do alvo é
    /// recompensado, e o dano percentual ignora o fato de a arma bater fraco.</para>
    ///
    /// <para>O dano contínuo enquanto acumula é pequeno de propósito: serve de feedback
    /// (a barra do alvo se move, o jogador vê que algo está acontecendo) sem ser a fonte
    /// principal de dano — essa é o estouro.</para>
    ///
    /// <para>POCO puro. <see cref="Tick"/> devolve quanto sair no intervalo; quem chama
    /// decide em que recurso aplicar e quanto vale o estouro.</para>
    /// </summary>
    public sealed class Sangramento
    {
        /// <summary>Teto padrão de acúmulos antes do estouro.</summary>
        public const int LimitePadrao = 10;

        private readonly int limiteDeAcumulos;

        private int _acumulos;
        private float _danoPorSegundoPorAcumulo;
        private float _tempoRestante;

        /// <summary>Acúmulos correntes (0 … limite − 1; ao atingir o limite, estoura e zera).</summary>
        public int Acumulos => _acumulos;

        /// <summary>Quantos acúmulos são necessários para estourar.</summary>
        public int LimiteDeAcumulos => limiteDeAcumulos;

        /// <summary>Se há ferida sangrando agora.</summary>
        public bool Ativo => _acumulos > 0 && _tempoRestante > 0f;

        /// <summary>Dano por segundo corrente — escala com a quantidade de acúmulos.</summary>
        public float DanoPorSegundo => Ativo ? _acumulos * _danoPorSegundoPorAcumulo : 0f;

        /// <summary>Segundos restantes até as feridas estancarem sozinhas.</summary>
        public float TempoRestante => _tempoRestante > 0f ? _tempoRestante : 0f;

        /// <summary>Disparado quando a contagem de acúmulos muda (para a UI de debuff).</summary>
        public event Action<int> OnAcumulosMudaram;

        /// <summary>Disparado no instante do estouro (para VFX/som), antes da contagem zerar.</summary>
        public event Action OnExplodiu;

        /// <summary>Disparado quando as feridas estancam por tempo ou <see cref="Limpar"/>.</summary>
        public event Action OnTerminou;

        /// <param name="limiteDeAcumulos">Acúmulos necessários para estourar (default 10).</param>
        public Sangramento(int limiteDeAcumulos = LimitePadrao)
        {
            this.limiteDeAcumulos = limiteDeAcumulos > 0 ? limiteDeAcumulos : LimitePadrao;
        }

        /// <summary>
        /// Aplica acúmulos de sangramento. O ataque básico do Estilete traz 1; a habilidade
        /// (Ferida de Aklo) traz vários de uma vez.
        ///
        /// <para>Cada aplicação <b>renova a duração</b> — parar de bater deixa a ferida
        /// estancar, então acumular exige manter a pressão em vez de aplicar uma vez e
        /// esperar.</para>
        ///
        /// <para>Não estoura aqui: o estouro é resolvido no <see cref="Tick"/>, para que
        /// quem chama receba um único ponto de decisão por frame.</para>
        /// </summary>
        /// <param name="acumulos">Quantos acúmulos somar (≥ 1).</param>
        /// <param name="danoPorSegundoPorAcumulo">Dano contínuo que cada acúmulo contribui.</param>
        /// <param name="duracaoSegundos">Quanto tempo as feridas duram sem novo golpe.</param>
        public void Aplicar(int acumulos, float danoPorSegundoPorAcumulo, float duracaoSegundos)
        {
            if (acumulos <= 0 || danoPorSegundoPorAcumulo <= 0f || duracaoSegundos <= 0f) return;

            // Fica com o sangramento mais forte: um golpe fraco não rebaixa uma ferida grave.
            _danoPorSegundoPorAcumulo = Math.Max(_danoPorSegundoPorAcumulo, danoPorSegundoPorAcumulo);
            _tempoRestante = Math.Max(_tempoRestante, duracaoSegundos);

            int antes = _acumulos;
            _acumulos = Math.Min(_acumulos + acumulos, limiteDeAcumulos);

            if (_acumulos != antes) OnAcumulosMudaram?.Invoke(_acumulos);
        }

        /// <summary>
        /// Avança o relógio e devolve o que saiu neste intervalo: o dano contínuo e se as
        /// feridas estouraram. O dano do último tick é proporcional ao tempo que realmente
        /// sobrou, para a ferida não entregar mais do que a duração previa.
        /// </summary>
        public TickDeSangramento Tick(float dt)
        {
            if (!Ativo || dt <= 0f) return default;

            float intervalo = Math.Min(dt, _tempoRestante);
            float dano = _acumulos * _danoPorSegundoPorAcumulo * intervalo;

            // Estouro tem prioridade sobre a expiração: chegar ao teto resolve a ferida
            // inteira, mesmo que a duração fosse acabar neste mesmo tick.
            if (_acumulos >= limiteDeAcumulos)
            {
                OnExplodiu?.Invoke();
                Estancar();
                return new TickDeSangramento(dano, explodiu: true);
            }

            _tempoRestante -= dt;
            if (_tempoRestante <= 0f) Estancar();

            return new TickDeSangramento(dano, explodiu: false);
        }

        /// <summary>Estanca as feridas imediatamente (fim de luta, morte, Refúgio).</summary>
        public void Limpar()
        {
            if (!Ativo) return;
            Estancar();
        }

        private void Estancar()
        {
            _acumulos = 0;
            _danoPorSegundoPorAcumulo = 0f;
            _tempoRestante = 0f;
            OnAcumulosMudaram?.Invoke(0);
            OnTerminou?.Invoke();
        }
    }

    /// <summary>
    /// Quanto dano o estouro do <see cref="Sangramento"/> causa. Função pura, isolada aqui
    /// pelo mesmo motivo da <see cref="MitigacaoDeDano"/>: é a "conta" do efeito, e três
    /// adaptadores diferentes precisam dela.
    ///
    /// <para><b>Percentual contra Aparições Primordiais, fixo contra o resto.</b> Dano
    /// percentual é o que faz o Estilete valer contra um boss de muita vida — mas contra um
    /// Cultista (100 de Vitalidade) 10% seriam 10 de dano, e o jogador concluiria que a
    /// mecânica "não funciona". O valor fixo mantém o estouro relevante fora da luta de boss.</para>
    ///
    /// <para>O <b>teto</b> existe para o efeito não virar um "delete boss" quando houver um
    /// chefe com vida muito alta.</para>
    /// </summary>
    public static class ExplosaoDeSangramento
    {
        /// <summary>Fração da vida máxima levada pelo estouro contra Aparições Primordiais.</summary>
        public const float FracaoDaVidaMaxima = 0.10f;

        /// <summary>Teto absoluto do estouro percentual (evita "delete boss" em chefes muito grandes).</summary>
        public const float TetoDeDano = 60f;

        /// <summary>Dano do estouro contra inimigos comuns (percentual seria irrelevante).</summary>
        public const float DanoContraComuns = 40f;

        /// <summary>
        /// Calcula o dano do estouro para o alvo dado.
        /// </summary>
        /// <param name="vitalidadeMaxima">Vitalidade máxima do alvo.</param>
        /// <param name="ehAparicaoPrimordial">Se o alvo é boss (usa percentual) ou comum (usa fixo).</param>
        public static float Calcular(float vitalidadeMaxima, bool ehAparicaoPrimordial)
        {
            if (!ehAparicaoPrimordial) return DanoContraComuns;
            if (vitalidadeMaxima <= 0f) return 0f;

            return Math.Min(vitalidadeMaxima * FracaoDaVidaMaxima, TetoDeDano);
        }
    }
}
