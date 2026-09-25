using System;

namespace FavelaAmarela.Core.Combat
{
    /// <summary>
    /// Acúmulo do frio dos <b>Cones de Gelo</b> de Abdul Alhazred. Cada cone que acerta
    /// Damião adiciona um acúmulo; ao atingir o limite (3 por padrão), ele <b>congela</b>
    /// — fica atordoado por um tempo curto e o acúmulo zera.
    ///
    /// <para>Os acúmulos <b>expiram</b> se ele passar tempo suficiente sem tomar cone: é o
    /// que transforma a mecânica em "não leve três seguidos" em vez de "morra por acúmulo
    /// inevitável". POCO puro — testável sem Unity.</para>
    /// </summary>
    public sealed class AcumuloDeCongelamento
    {
        private readonly int limite;
        private readonly float duracaoDoAcumulo;
        private readonly float duracaoDoCongelamento;

        private int _acumulos;
        private float _tempoDesdeUltimoAcumulo;
        private float _tempoCongeladoRestante;

        /// <summary>Acúmulos correntes (0 … limite − 1; ao atingir o limite, zera e congela).</summary>
        public int Acumulos => _acumulos;

        /// <summary>Quantos acúmulos são necessários para congelar.</summary>
        public int Limite => limite;

        /// <summary>Se Damião está congelado (atordoado pelo frio) neste instante.</summary>
        public bool EstaCongelado => _tempoCongeladoRestante > 0f;

        /// <summary>Segundos restantes de congelamento (0 se não estiver congelado).</summary>
        public float TempoCongeladoRestante => _tempoCongeladoRestante > 0f ? _tempoCongeladoRestante : 0f;

        /// <summary>Disparado quando o acúmulo atinge o limite e Damião congela.</summary>
        public event Action OnCongelou;

        /// <summary>Disparado quando o congelamento termina e ele volta a se mover.</summary>
        public event Action OnDescongelou;

        /// <summary>Disparado quando a contagem de acúmulos muda (para a UI de debuff).</summary>
        public event Action<int> OnAcumulosMudaram;

        /// <param name="limite">Acúmulos necessários para congelar (default 3).</param>
        /// <param name="duracaoDoAcumulo">Segundos que um acúmulo dura sem novo cone (default 6).</param>
        /// <param name="duracaoDoCongelamento">Segundos de atordoamento ao congelar (default 1,5).</param>
        public AcumuloDeCongelamento(
            int limite = 3,
            float duracaoDoAcumulo = 6f,
            float duracaoDoCongelamento = 1.5f)
        {
            this.limite = limite > 0 ? limite : 3;
            this.duracaoDoAcumulo = duracaoDoAcumulo > 0f ? duracaoDoAcumulo : 6f;
            this.duracaoDoCongelamento = duracaoDoCongelamento > 0f ? duracaoDoCongelamento : 1.5f;
        }

        /// <summary>
        /// Damião tomou dano de um Cone de Gelo. Adiciona um acúmulo; ao bater no limite,
        /// congela e zera. Ignorado enquanto ele já está congelado (não empilha punição).
        /// </summary>
        public void AplicarAcumulo()
        {
            if (EstaCongelado) return;

            _acumulos++;
            _tempoDesdeUltimoAcumulo = 0f;

            if (_acumulos >= limite)
            {
                _acumulos = 0;
                _tempoCongeladoRestante = duracaoDoCongelamento;
                OnAcumulosMudaram?.Invoke(_acumulos);
                OnCongelou?.Invoke();
                return;
            }

            OnAcumulosMudaram?.Invoke(_acumulos);
        }

        /// <summary>Avança o relógio: expira acúmulos antigos e o congelamento.</summary>
        public void Tick(float dt)
        {
            if (_tempoCongeladoRestante > 0f)
            {
                _tempoCongeladoRestante -= dt;
                if (_tempoCongeladoRestante <= 0f)
                {
                    _tempoCongeladoRestante = 0f;
                    OnDescongelou?.Invoke();
                }
                return; // congelado não perde acúmulos (já estão zerados)
            }

            if (_acumulos <= 0) return;

            _tempoDesdeUltimoAcumulo += dt;
            if (_tempoDesdeUltimoAcumulo >= duracaoDoAcumulo)
            {
                _tempoDesdeUltimoAcumulo = 0f;
                _acumulos--;
                OnAcumulosMudaram?.Invoke(_acumulos);
            }
        }

        /// <summary>Limpa acúmulos e congelamento (ex.: fim da luta, morte, Refúgio).</summary>
        public void Limpar()
        {
            bool estavaCongelado = EstaCongelado;
            _acumulos = 0;
            _tempoDesdeUltimoAcumulo = 0f;
            _tempoCongeladoRestante = 0f;

            OnAcumulosMudaram?.Invoke(0);
            if (estavaCongelado) OnDescongelou?.Invoke();
        }
    }
}
