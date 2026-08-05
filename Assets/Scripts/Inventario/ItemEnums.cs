// Assets/Scripts/Inventario/ItemEnums.cs
namespace FavelaAmarela.Inventario
{
    public enum ItemType
    {
        Arma,
        Armadura,
        Amuleto,
        Consumivel,
        Chave
    }

    public enum EquipmentSlot
    {
        Nenhum,      // Para itens não equipáveis
        Arma,
        Elmo,
        Peitoral,
        Grevas,
        Amuleto,
        Anel
    }

    public enum StatType
    {
        // Atributos primários
        VitMaxima,        // Vitalidade Corpórea máxima
        RMMaxima,         // Resiliência Mental máxima
        RCMaxima,         // Resiliência do Companheiro máxima (Yug-Neth)

        // Dano causado
        TraumaFisico,     // Dano físico base (armas)
        TraumaAnomalia,   // Dano de estática/cósmico

        // Outros modificadores
        Velocidade,       // Multiplicador de movimento
        Furtividade,      // Redução de ruído (0-1, somado)
        DefesaFisica,     // Redução de Trauma físico
        DefesaAnomalia,   // Redução de Trauma de anomalia
        
        // Relíquias e Sanidade
        RegenRM,          // Acelera a regeneração de RM
        DrenoRM,          // Perda/Custo de RM por segundo

        // Vigor
        VigorMaximo,      // Stamina total
        RegeneracaoVigor, // Regen. passiva
        CustoEsquivaVigor,// Custo de dash
        CustoCorridaVigor // Custo de run
    }
}
