namespace FavelaAmarela.Core.Artefatos
{
    /// <summary>
    /// "Resguardo do Sinal": os passos de Damião deixam de emitir ruído por um tempo.
    /// Efeito ativo do Anel do Sinal Amarelo — e a razão de ele existir, já que os
    /// Cultistas caçam por som.
    /// </summary>
    public sealed class EfeitoDeSilencio : IEfeitoDeArtefato
    {
        private readonly float _duracao;

        /// <summary>Monta o efeito com a duração autorada no <c>ArtefatoDef</c>.</summary>
        public EfeitoDeSilencio(float duracao) => _duracao = duracao;

        /// <inheritdoc />
        public string Nome => "Resguardo do Sinal";

        /// <inheritdoc />
        public void Aplicar(IContextoDeArtefato ctx) => ctx?.SilenciarPassos(_duracao);
    }
}
