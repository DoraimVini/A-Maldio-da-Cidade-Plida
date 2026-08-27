namespace FavelaAmarela.Core.Abilities.Efeitos
{
    /// <summary>
    /// Abre acúmulos de sangramento. É o que o Estilete de Irem faz — dano por permanência
    /// em vez de dano de pico.
    ///
    /// <para>Reaproveita os POCOs que já existem: <c>Sangramento</c> e
    /// <c>ExplosaoDeSangramento</c> continuam donos da regra; este efeito só autora os números.</para>
    /// </summary>
    public sealed class EfeitoDeSangramento : IEfeitoDeHabilidade
    {
        private readonly int _acumulos;
        private readonly float _porSegundo;
        private readonly float _duracao;

        /// <inheritdoc cref="EfeitoDeDano.Nome"/>
        public string Nome => $"Sangramento {_acumulos}× ({_porSegundo:0.##}/s por {_duracao:0.##}s)";

        /// <param name="acumulos">Quantos acúmulos este golpe abre.</param>
        /// <param name="porSegundo">Dano contínuo por segundo.</param>
        /// <param name="duracao">Quanto tempo cada acúmulo dura.</param>
        public EfeitoDeSangramento(int acumulos, float porSegundo, float duracao)
        {
            _acumulos = acumulos < 0 ? 0 : acumulos;
            _porSegundo = porSegundo < 0f ? 0f : porSegundo;
            _duracao = duracao < 0f ? 0f : duracao;
        }

        /// <summary>
        /// Acúmulos SOMAM (dois efeitos de sangramento no mesmo golpe abrem mais feridas), mas
        /// intensidade e duração ficam na maior — é a leitura natural de "sangrar mais", e é
        /// o que a mecânica de estouro por acúmulo espera.
        /// </summary>
        public void Aplicar(ConstrutorDeGolpe golpe)
        {
            if (_acumulos <= 0) return;

            golpe.AcumulosDeSangramento += _acumulos;
            if (_porSegundo > golpe.SangramentoPorSegundo) golpe.SangramentoPorSegundo = _porSegundo;
            if (_duracao > golpe.DuracaoSangramento) golpe.DuracaoSangramento = _duracao;
        }
    }
}
