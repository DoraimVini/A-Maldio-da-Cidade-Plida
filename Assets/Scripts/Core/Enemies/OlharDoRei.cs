using System;

namespace FavelaAmarela.Core.Enemies
{
    /// <summary>Configuração do farol da Fase 2 do Rito do Olhar.</summary>
    public sealed class ParametrosDoOlhar
    {
        /// <summary>Largura do cone, em graus, com a Máscara fechada.</summary>
        public float LarguraDoCone { get; set; } = 70f;

        /// <summary>Quanto o centro do cone se afasta da frente do Rei para cada lado, em graus.</summary>
        public float Amplitude { get; set; } = 60f;

        /// <summary>Segundos de uma varredura (de um lado ao outro).</summary>
        public float DuracaoDaVarredura { get; set; } = 8f;

        /// <summary>A Máscara abre na primeira parte de uma a cada tantas varreduras.</summary>
        public int VarredurasEntreAberturas { get; set; } = 3;

        /// <summary>Segundos que a Máscara fica aberta.</summary>
        public float DuracaoDaAbertura { get; set; } = 2f;

        /// <summary>Segundos de aviso antes de a Máscara abrir.</summary>
        public float AvisoAntesDaAbertura { get; set; } = 0.5f;
    }

    /// <summary>
    /// O farol da Fase 2: o olhar do Rei deixa de ver a sala inteira e passa a <b>varrer</b>. Um
    /// cone oscila de um lado ao outro; fora dele o Damião não é visto, com ou sem cobertura.
    /// A cada <see cref="ParametrosDoOlhar.VarredurasEntreAberturas"/> varreduras a Máscara
    /// abre: o cone dobra e o dreno triplica (ver <see cref="ExposicaoAoRei"/>).
    ///
    /// <para>POCO puro, determinístico no tempo: o mesmo <c>t</c> dá sempre o mesmo cone.</para>
    /// </summary>
    public sealed class OlharDoRei
    {
        private readonly ParametrosDoOlhar _p;
        private float _t;
        private bool _avisouEstaAbertura;

        /// <summary>Disparado <see cref="ParametrosDoOlhar.AvisoAntesDaAbertura"/> segundos antes da Máscara abrir.</summary>
        public event Action OnMascaraVaiAbrir;

        /// <summary>Cria o farol.</summary>
        public OlharDoRei(ParametrosDoOlhar parametros)
        {
            _p = parametros ?? new ParametrosDoOlhar();
        }

        /// <summary>Segundos desde que o farol começou.</summary>
        public float Tempo => _t;

        /// <summary>Qual varredura está acontecendo (0, 1, 2…).</summary>
        public int Varredura => (int)Math.Floor(_t / Math.Max(0.01f, _p.DuracaoDaVarredura));

        private float TempoNaVarredura => _t - Varredura * _p.DuracaoDaVarredura;

        private bool VarreduraDeAbertura(int k) =>
            _p.VarredurasEntreAberturas > 0 && k % _p.VarredurasEntreAberturas == _p.VarredurasEntreAberturas - 1;

        /// <summary>A Máscara está aberta agora.</summary>
        public bool MascaraAberta => VarreduraDeAbertura(Varredura) && TempoNaVarredura < _p.DuracaoDaAbertura;

        /// <summary>
        /// Centro do cone, em graus a partir da frente do Rei. Um seno completa meio ciclo por
        /// varredura: de −Amplitude a +Amplitude, e de volta na seguinte.
        /// </summary>
        public float Centro => _p.Amplitude * (float)Math.Sin(Math.PI * _t / Math.Max(0.01f, _p.DuracaoDaVarredura) - Math.PI / 2);

        /// <summary>Largura atual do cone, em graus — dobra com a Máscara aberta.</summary>
        public float Largura => MascaraAberta ? _p.LarguraDoCone * 2f : _p.LarguraDoCone;

        /// <summary>Se o cone, agora, cobre um ângulo medido a partir da frente do Rei.</summary>
        public bool Ve(float anguloDoJogador) => Math.Abs(anguloDoJogador - Centro) <= Largura / 2f;

        /// <summary>Avança o relógio do farol.</summary>
        public void Avancar(float dt)
        {
            if (dt <= 0f) return;

            int antes = Varredura;
            _t += dt;
            if (Varredura != antes) _avisouEstaAbertura = false;

            // O aviso vem no fim da varredura ANTERIOR à de abertura.
            if (!_avisouEstaAbertura && VarreduraDeAbertura(Varredura + 1)
                && TempoNaVarredura >= _p.DuracaoDaVarredura - _p.AvisoAntesDaAbertura)
            {
                _avisouEstaAbertura = true;
                OnMascaraVaiAbrir?.Invoke();
            }
        }
    }
}
