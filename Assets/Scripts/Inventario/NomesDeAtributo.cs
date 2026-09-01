namespace FavelaAmarela.Inventario
{
    /// <summary>
    /// Traduz <see cref="StatType"/> para o nome que o jogador lê.
    ///
    /// <para><b>Por que num arquivo só.</b> A skill <c>favela-lore-enforcer</c> proíbe
    /// vocabulário genérico de RPG em texto visível — nada de "HP", "Mana" ou o nome cru do
    /// enum. Com o tooltip de item nascendo, passaram a existir dois lugares querendo nomear
    /// atributo (a ficha e o item), e duas tabelas de tradução divergiriam na primeira vez que
    /// alguém renomeasse um atributo em só uma delas.</para>
    ///
    /// <para><b>Este arquivo NÃO consome atributo</b> — ele só os nomeia. É por isso que
    /// <c>AtributosConsumidosTests</c> o exclui da varredura, junto de <c>ItemEnums.cs</c> e
    /// <c>PainelDeFicha.cs</c>: citar um <c>StatType</c> para escrever o rótulo dele não é o
    /// mesmo que ler o bônus e aplicá-lo.</para>
    /// </summary>
    public static class NomesDeAtributo
    {
        /// <summary>
        /// Os atributos <b>sem consumidor no jogo</b>. Autorar um item ou afixo em cima de um
        /// deles produz algo que <b>mente</b>: o jogador lê o número, ocupa o slot e não recebe
        /// nada.
        ///
        /// <para><b>Esta é a FONTE ÚNICA</b>, e passou a ser em 2026-09-01. A versão anterior
        /// dizia que a verdade morava em <c>PainelDeFicha.AtributoConsomeBonus</c> e que esta era
        /// "a lista do lado de quem autora" — duas cópias do mesmo fato, com o aviso escrito de
        /// que elas <i>"divergiriam na primeira vez que alguém implementasse um dos quatro"</i>.
        /// Foi exatamente o que aconteceu: <c>Furtividade</c> e <c>DefesaAnomalia</c> ganharam
        /// consumidor em 2026-08-28, só uma lista foi atualizada, e um afixo legítimo
        /// (<c>afixo_couracado</c>) foi barrado por um guarda lendo a lista velha.</para>
        ///
        /// <para>Agora <c>PainelDeFicha</c> <b>delega para cá</b>. A dependência aponta na
        /// direção certa — dado não pergunta à interface —, e não há segunda cópia para
        /// envelhecer.</para>
        ///
        /// <para><b>Quem sobrou, e por quê:</b> <c>RMMaxima</c> existe no enum e nenhum sistema
        /// a soma ao teto de Resiliência (que vem da ficha); <c>RCMaxima</c> e
        /// <c>Velocidade</c> não têm uma única menção em <c>Assets/Scripts</c>.</para>
        /// </summary>
        public static readonly System.Collections.Generic.IReadOnlyList<StatType> SemEfeito =
            new[]
            {
                StatType.RMMaxima,
                StatType.RCMaxima,
                StatType.Velocidade,
            };

        /// <summary>Se este atributo não faz nada em jogo.</summary>
        public static bool NaoTemEfeito(StatType stat)
        {
            for (int i = 0; i < SemEfeito.Count; i++)
                if (SemEfeito[i] == stat) return true;

            return false;
        }

        /// <summary>Nome diegético do atributo.</summary>
        public static string De(StatType stat) => stat switch
        {
            StatType.VitMaxima => "Vitalidade",
            StatType.RMMaxima => "Resiliência Mental",
            StatType.RCMaxima => "Resiliência do Companheiro",
            StatType.TraumaFisico => "Trauma Físico",
            StatType.TraumaAnomalia => "Trauma de Anomalia",
            StatType.Velocidade => "Velocidade",
            StatType.Furtividade => "Furtividade",
            StatType.DefesaFisica => "Defesa Física",
            StatType.DefesaAnomalia => "Resistência Anômala",
            StatType.RegenRM => "Ancoragem",
            StatType.DrenoRM => "Dreno de Resiliência",
            StatType.VigorMaximo => "Vigor",
            StatType.RegeneracaoVigor => "Recuperação de Vigor",
            _ => stat.ToString(),
        };
    }
}
