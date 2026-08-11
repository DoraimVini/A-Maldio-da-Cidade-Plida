namespace FavelaAmarela.Core.Artefatos
{
    /// <summary>
    /// "Canção de Cassilda": devolve Resiliência Mental de uma vez. Efeito ativo do Patuá
    /// das Luas Gêmeas.
    /// </summary>
    public sealed class EfeitoDeAncoragem : IEfeitoDeArtefato
    {
        private readonly float _valor;

        /// <summary>Monta o efeito com o valor de Ancoragem autorado no <c>ArtefatoDef</c>.</summary>
        public EfeitoDeAncoragem(float valor) => _valor = valor;

        /// <inheritdoc />
        public string Nome => "Canção de Cassilda";

        /// <inheritdoc />
        public void Aplicar(IContextoDeArtefato ctx) => ctx?.AncorarJogador(_valor);
    }
}
