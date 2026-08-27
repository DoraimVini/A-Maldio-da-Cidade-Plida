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
