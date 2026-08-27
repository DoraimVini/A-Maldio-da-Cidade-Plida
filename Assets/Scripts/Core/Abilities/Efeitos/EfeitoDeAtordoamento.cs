namespace FavelaAmarela.Core.Abilities.Efeitos
{
    /// <summary>
    /// Atordoa o alvo por um tempo. É metade do que o Alfanje de Alhazred faz hoje em
    /// código — e, como dado, passa a valer para qualquer arma futura sem uma linha nova.
    /// </summary>
    public sealed class EfeitoDeAtordoamento : IEfeitoDeHabilidade
    {
        private readonly float _duracao;

        /// <inheritdoc cref="EfeitoDeDano.Nome"/>
        public string Nome => $"Atordoamento {_duracao:0.##}s";

        /// <param name="duracao">Segundos de atordoamento. Zero ou menos não atordoa.</param>
        public EfeitoDeAtordoamento(float duracao) => _duracao = duracao;

        /// <summary>
        /// Mantém a MAIOR duração quando dois efeitos atordoam no mesmo golpe. Somar faria
        /// uma habilidade com dois atordoamentos travar o alvo pelo dobro, o que nunca é a
        /// intenção de quem autora — e é o tipo de composição que um Item Creator produz por
        /// acidente.
        /// </summary>
        public void Aplicar(ConstrutorDeGolpe golpe)
        {
            if (_duracao <= 0f) return;

            golpe.Atordoou = true;
            if (_duracao > golpe.DuracaoAtordoamento) golpe.DuracaoAtordoamento = _duracao;
        }
    }
}
