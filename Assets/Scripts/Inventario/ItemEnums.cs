// Assets/Scripts/Inventario/ItemEnums.cs
namespace FavelaAmarela.Inventario
{
    public enum ItemType
    {
        Arma,
        Armadura,
        Amuleto,
        Consumivel,
        Chave,

        // Sempre no FIM: ItemType é serializado por índice nos .asset, e inserir um valor no
        // meio remapearia silenciosamente todo item já autorado.
        /// <summary>Relíquia com passiva e habilidade próprias. Não ocupa slot de corpo —
        /// vive no inventário de Artefatos, de 4 slots.</summary>
        Artefato
    }

    public enum EquipmentSlot
    {
        Nenhum,      // Para itens não equipáveis
        Arma,
        Elmo,
        Peitoral,
        Grevas,
        Amuleto,
        Anel,

        // Sempre no FIM, pelo mesmo motivo do ItemType: EquipmentSlot é serializado por
        // índice nos .asset, e inserir um valor no meio remapearia silenciosamente todo
        // item já autorado (um elmo viraria grevas sem ninguém notar).
        /// <summary>Mão secundária — escudos, focos arcanos e a segunda lâmina.
        /// Fica bloqueada enquanto a Mão principal empunha uma arma de
        /// <see cref="Empunhadura.DuasMaos"/>.</summary>
        MaoSecundaria
    }

    /// <summary>
    /// Como uma arma ocupa as mãos de Damião. É a escolha tática central do combate:
    /// arma leve + foco/escudo na off-hand, ou uma lâmina colossal que toma as duas mãos
    /// e não deixa espaço para defesa.
    /// </summary>
    public enum Empunhadura
    {
        /// <summary>Ocupa só a mão principal — deixa a secundária livre. Default.</summary>
        UmaMao,

        /// <summary>Toma as duas mãos: bloqueia o slot de Mão Secundária enquanto equipada.</summary>
        DuasMaos
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
