namespace FavelaAmarela.Core.Abilities.Efeitos
{
    /// <summary>
    /// Dano de anomalia — a estática cósmica de Carcosa, que não é ferimento de corpo.
    /// Mitigado por Resistência Anômala, não por Defesa Física.
    /// </summary>
    public sealed class EfeitoDeTraumaAnomalia : IEfeitoDeHabilidade
    {
        private readonly float _trauma;

        /// <inheritdoc cref="EfeitoDeDano.Nome"/>
        public string Nome => $"Anomalia {_trauma:0.##}";

        /// <param name="trauma">Trauma de anomalia aplicado.</param>
        public EfeitoDeTraumaAnomalia(float trauma) => _trauma = trauma < 0f ? 0f : trauma;

        /// <inheritdoc/>
        public void Aplicar(ConstrutorDeGolpe golpe) => golpe.TraumaAnomalia += _trauma;
    }
}
