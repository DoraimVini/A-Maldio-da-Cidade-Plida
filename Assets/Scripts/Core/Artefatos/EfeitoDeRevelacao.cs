namespace FavelaAmarela.Core.Artefatos
{
    /// <summary>
    /// "Recitar o Aklo": revela entidades através da parede por um tempo — quem lê o Aklo
    /// vê demais. Efeito ativo do Necronomicon.
    /// </summary>
    public sealed class EfeitoDeRevelacao : IEfeitoDeArtefato
    {
        private readonly float _raio;
        private readonly float _duracao;

        /// <summary>Monta o efeito com o raio e a duração autorados no <c>ArtefatoDef</c>.</summary>
        public EfeitoDeRevelacao(float raio, float duracao)
        {
            _raio = raio;
            _duracao = duracao;
        }

        /// <inheritdoc />
        public string Nome => "Recitar o Aklo";

        /// <inheritdoc />
        public void Aplicar(IContextoDeArtefato ctx) => ctx?.RevelarEntidades(_raio, _duracao);
    }
}
