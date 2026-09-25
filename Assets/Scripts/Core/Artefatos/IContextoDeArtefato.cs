namespace FavelaAmarela.Core.Artefatos
{
    /// <summary>
    /// O que um efeito de Artefato consegue fazer no mundo. O Core <b>declara</b> o
    /// vocabulário; quem <b>executa</b> é o adaptador Runtime.
    ///
    /// <para>Essa inversão é o que mantém os efeitos testáveis: uma suíte EditMode passa um
    /// contexto falso e afirma que o efeito pediu a coisa certa, sem cena nem Unity rodando.</para>
    /// </summary>
    public interface IContextoDeArtefato
    {
        /// <summary>
        /// Revela as entidades num raio, atravessando parede, pelo tempo dado.
        /// O Aklo lido em voz alta mostra o que estava do outro lado.
        /// </summary>
        void RevelarEntidades(float raio, float duracao);

        /// <summary>Devolve Resiliência Mental a Damião — Ancoragem imediata.</summary>
        void AncorarJogador(float valor);

        /// <summary>Cala os passos de Damião pelo tempo dado: ele deixa de emitir ruído.</summary>
        void SilenciarPassos(float duracao);

        /// <summary>Aplaca os serpentinos num raio, que hesitam pelo tempo dado.</summary>
        void AplacarSerpentes(float raio, float duracao);
    }
}
