namespace FavelaAmarela.Core.Abilities.Efeitos
{
    /// <summary>Dano físico direto. O efeito que quase toda arma tem.</summary>
    public sealed class EfeitoDeDano : IEfeitoDeHabilidade
    {
        private readonly float _dano;

        /// <summary>Nome legível, usado em diagnóstico e no tooltip do item.</summary>
        public string Nome => $"Dano {_dano:0.##}";

        /// <param name="dano">Trauma físico aplicado. Negativo é tratado como zero.</param>
        public EfeitoDeDano(float dano) => _dano = dano < 0f ? 0f : dano;

        /// <inheritdoc/>
        public void Aplicar(ConstrutorDeGolpe golpe) => golpe.Dano += _dano;
    }
}
