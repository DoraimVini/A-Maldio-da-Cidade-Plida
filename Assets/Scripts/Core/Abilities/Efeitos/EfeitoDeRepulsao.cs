namespace FavelaAmarela.Core.Abilities.Efeitos
{
    /// <summary>
    /// Empurra o corpo atingido. A outra metade do Alfanje — o "espaço" que
    /// <c>armas_da_tumba.md</c> promete.
    ///
    /// <para>A força é modulada no impacto por <c>CorpoImpregnado</c>: em Carcosa, quanto mais
    /// uma coisa está impregnada, menos ela cede. Um Eco não se move por mais forte que seja
    /// o golpe.</para>
    /// </summary>
    public sealed class EfeitoDeRepulsao : IEfeitoDeHabilidade
    {
        private readonly float _forca;

        /// <inheritdoc cref="EfeitoDeDano.Nome"/>
        public string Nome => $"Repulsão {_forca:0.##}";

        /// <param name="forca">Velocidade do empurrão, antes da resistência do alvo.</param>
        public EfeitoDeRepulsao(float forca) => _forca = forca < 0f ? 0f : forca;

        /// <summary>Mantém a maior força; empurrões não somam pelo mesmo motivo do atordoamento.</summary>
        public void Aplicar(ConstrutorDeGolpe golpe)
        {
            if (_forca > golpe.ForcaRepulsao) golpe.ForcaRepulsao = _forca;
        }
    }
}
