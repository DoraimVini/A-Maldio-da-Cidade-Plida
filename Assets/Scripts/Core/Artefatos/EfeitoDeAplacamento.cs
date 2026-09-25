namespace FavelaAmarela.Core.Artefatos
{
    /// <summary>
    /// "Sibilo de Yig": os serpentinos próximos hesitam, reconhecendo a autoridade do
    /// Sacerdote morto. Efeito ativo da Coroa de Ossos, tomada do Nagaraja.
    /// </summary>
    public sealed class EfeitoDeAplacamento : IEfeitoDeArtefato
    {
        private readonly float _raio;
        private readonly float _duracao;

        /// <summary>Monta o efeito com o raio e a duração autorados no <c>ArtefatoDef</c>.</summary>
        public EfeitoDeAplacamento(float raio, float duracao)
        {
            _raio = raio;
            _duracao = duracao;
        }

        /// <inheritdoc />
        public string Nome => "Sibilo de Yig";

        /// <inheritdoc />
        public void Aplicar(IContextoDeArtefato ctx) => ctx?.AplacarSerpentes(_raio, _duracao);
    }
}
