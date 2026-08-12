using FavelaAmarela.Core.Abilities;

namespace FavelaAmarela.Core.Combat
{
    /// <summary>
    /// Contrato para qualquer entidade que pode receber um golpe de arma física
    /// (Cultista Amarelo, Aparição Primordial/boss, etc.). Desacopla o resolvedor de
    /// golpe (<c>MaoFisicaBridge</c>) de tipos concretos: antes ele só reconhecia o
    /// Cultista, o que tornava todo o resto imune "de graça" (é por isso que a Coisa
    /// do Cemitério é imortal — ela simplesmente não implementa isto).
    /// </summary>
    public interface IDanificavel
    {
        /// <summary>
        /// Se é uma Aparição Primordial (Vulto/boss). Aparições Primordiais são
        /// <b>imunes ao crítico de furtividade</b> — furtividade não resolve a luta
        /// contra bosses, só serve para chegar até ela.
        /// </summary>
        bool EhAparicaoPrimordial { get; }

        /// <summary>Aplica o resultado de um golpe físico (dano + efeitos do <see cref="ArmaResult"/>).</summary>
        void ReceberGolpe(ArmaResult resultado);
    }
}
