namespace FavelaAmarela.Core.Abilities.Efeitos
{
    /// <summary>
    /// Corta a conjuração de quem estiver conjurando. É o que faz o Maça de Aklo ser a arma
    /// anti-mago do arsenal.
    /// </summary>
    public sealed class EfeitoDeInterrupcao : IEfeitoDeHabilidade
    {
        /// <inheritdoc cref="EfeitoDeDano.Nome"/>
        public string Nome => "Interrompe conjuração";

        /// <inheritdoc/>
        public void Aplicar(ConstrutorDeGolpe golpe) => golpe.InterrompeConjuracao = true;
    }
}
