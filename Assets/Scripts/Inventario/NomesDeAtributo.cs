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
        /// <para><c>DefesaAnomalia</c> é o pior caso — o <c>PainelDeFicha</c> <b>exibe</b> a
        /// linha e o combate não aplica. <c>RCMaxima</c> e <c>Velocidade</c> não têm uma única
        /// menção em <c>Assets/Scripts</c>; <c>Furtividade</c> é autorada no Anel do Sinal
        /// Amarelo e nenhum sistema de stealth a lê.</para>
        ///
        /// <para><b>Mora aqui, e não em três lugares.</b> Ela estava duplicada na validação da
        /// forja, no guarda do pool de afixos e implicitamente no <c>PainelDeFicha</c> — três
        /// cópias que divergiriam na primeira vez que alguém implementasse um dos quatro. A
        /// fonte da verdade sobre o que é consumido continua sendo
        /// <c>PainelDeFicha.AtributoConsomeBonus</c>, cruzada com o código por
        /// <c>AtributosConsumidosTests</c>; esta é a lista do lado de quem AUTORA.</para>
        /// </summary>
        public static readonly System.Collections.Generic.IReadOnlyList<StatType> SemEfeito =
            new[]
            {
                StatType.RCMaxima,
                StatType.Velocidade,
                StatType.Furtividade,
                StatType.DefesaAnomalia,
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
