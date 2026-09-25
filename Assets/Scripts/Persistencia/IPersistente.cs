namespace FavelaAmarela.Runtime.Persistencia
{
    /// <summary>
    /// Contrato de quem tem estado a salvar. Implementado por <c>MonoBehaviour</c> que
    /// acompanham um <see cref="ObjetoPersistente"/> (para objetos de cena) ou que usam uma
    /// chave global de <c>ChavesDeSave</c> (para estado do jogador e flags).
    ///
    /// <para><b>Padrão Observer, conforme decidido:</b> nenhum objeto salva o próprio
    /// arquivo. Cada um só sabe <b>ler e escrever o próprio estado</b>; quem junta tudo e
    /// grava é o <see cref="GerenciadorDeSave"/>. Isso mantém um arquivo só, um formato só
    /// e um lugar só para depurar.</para>
    /// </summary>
    public interface IPersistente
    {
        /// <summary>
        /// Chave sob a qual este estado é gravado. Para objetos de cena, venha do
        /// <see cref="ObjetoPersistente"/>; para estado global, use uma constante de
        /// <c>ChavesDeSave</c>. <b>Nunca</b> derive de nome ou hierarquia.
        /// </summary>
        string ChaveDePersistencia { get; }

        /// <summary>
        /// Devolve o estado atual serializado como string (JSON ou valor simples). Chamado
        /// pelo gerenciador na hora de salvar.
        /// </summary>
        string CapturarEstado();

        /// <summary>
        /// Reaplica um estado lido do save. <b>Só é chamado se a chave existir</b> — objeto
        /// novo, sem entrada no save, simplesmente mantém seu estado padrão (degradação
        /// graciosa).
        /// </summary>
        void AplicarEstado(string estado);
    }
}
