namespace FavelaAmarela.Core.Artefatos
{
    /// <summary>
    /// Um efeito atômico de habilidade de Artefato. Composição sobre herança
    /// (<c>CLAUDE.md</c> §4.3): uma habilidade é uma <b>lista</b> de efeitos, não uma classe
    /// nova por artefato — é a primeira aplicação real do desenho de
    /// <c>systems/habilidades_de_item.md</c>.
    /// </summary>
    public interface IEfeitoDeArtefato
    {
        /// <summary>Nome curto do efeito, para diagnóstico e teste.</summary>
        string Nome { get; }

        /// <summary>Aplica o efeito através do contexto fornecido pelo Runtime.</summary>
        void Aplicar(IContextoDeArtefato ctx);
    }
}
