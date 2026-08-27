using FavelaAmarela.Core.Loot;

namespace FavelaAmarela.Inventario
{
    /// <summary>
    /// Quantos afixos cada grau de impregnação concede.
    ///
    /// <para><b>Sem teto, o item ganha modificadores sem limite</b> — é o defeito mais comum de
    /// um sistema de afixos caseiro, e o que separa "raridade" de "ruído". Aqui o teto é a
    /// própria definição do grau, como em D2 (mágico = 1 prefixo + 1 sufixo; raro = até 3+3).</para>
    ///
    /// <para><b>Os números traduzem o design escrito</b> em <c>loot_e_drop.md</c>:
    /// Inerte é "matéria comum, que Carcosa ainda não tocou"; Marcado "carrega o Sinal em algum
    /// canto" e recebe <i>um modificador pequeno</i>; Impregnado está "saturado" e recebe
    /// <i>modificador relevante, com contrapartida</i>; Relíquia é "peça única e nomeada",
    /// autorada individualmente e <b>nunca sorteada em tabela genérica</b>.</para>
    /// </summary>
    public static class RegrasDeGrau
    {
        /// <summary>Quantos prefixos este grau concede.</summary>
        public static int Prefixos(GrauDeImpregnacao grau) => grau switch
        {
            GrauDeImpregnacao.Marcado => 1,
            GrauDeImpregnacao.Impregnado => 1,
            _ => 0,
        };

        /// <summary>Quantos sufixos este grau concede.</summary>
        public static int Sufixos(GrauDeImpregnacao grau) => grau switch
        {
            GrauDeImpregnacao.Impregnado => 1,
            _ => 0,
        };

        /// <summary>
        /// Total de afixos do grau. Serve de leitura rápida e de guarda: um grau que conceda
        /// mais que isto está gerando item fora do que o design prevê.
        /// </summary>
        public static int Total(GrauDeImpregnacao grau) => Prefixos(grau) + Sufixos(grau);

        /// <summary>
        /// Relíquia é autorada à mão, nunca gerada. O gerador recusa — e recusar em voz alta é
        /// melhor que produzir uma relíquia aleatória, que quebraria a promessa de que cada uma
        /// tem história própria.
        /// </summary>
        public static bool PodeSerGerado(GrauDeImpregnacao grau) =>
            grau != GrauDeImpregnacao.Reliquia;
    }
}
