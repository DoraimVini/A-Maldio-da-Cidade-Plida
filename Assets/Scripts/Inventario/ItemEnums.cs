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
    /// <summary>
    /// Como a arma <b>entrega</b> o dano. Separado do dano em si de propósito: a matemática de
    /// <c>PerfilDeArma</c> e <c>ResolucaoDeGolpe</c> — faixa branca, crítico, precisão — é a
    /// mesma para uma lâmina e para um cano. <b>Uma espingarda rola igual a uma espada</b>; o
    /// que muda é como o golpe alcança o alvo.
    ///
    /// <para><b>Por que já existe, sem nada implementado (2026-09-01).</b> Decisão do Vini:
    /// abrir espaço para armas à distância e de fogo no futuro do jogo. Deixar a costura pronta
    /// custa um enum; descobrir depois que o modelo de dano presumia corpo-a-corpo custaria
    /// reescrever a resolução do golpe inteira.</para>
    ///
    /// <para><b>Só <c>CorpoACorpo</c> tem implementação hoje.</b> Os outros dois existem para
    /// serem autorados e para o código que os consumir falhar alto em vez de fingir que
    /// funciona.</para>
    /// </summary>
    public enum TipoDeEntrega
    {
        /// <summary>Varredura no alcance da arma. O único caminho implementado.</summary>
        CorpoACorpo,

        // SEMPRE NO FIM: TipoDeEntrega é serializado por ÍNDICE nos .asset, e inserir um valor
        // no meio remapearia silenciosamente toda arma já autorada.

        /// <summary>
        /// Projétil que viaja: arco, funda, lança. Precisa de velocidade de projétil e de o
        /// golpe resolver no impacto, não no gesto.
        /// </summary>
        Projetil,

        /// <summary>
        /// Arma de fogo. Além do projétil, traz <b>munição e recarga</b> — dois recursos que o
        /// jogo não tem, e é por isso que isto é planejamento e não implementação.
        /// </summary>
        Fogo
    }

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
        CustoCorridaVigor,// Custo de run

        // ── Combate a dado (2026-08-28) ───────────────────────────────────────
        // Acrescentados NO FIM: os índices são posicionais e já estão gravados em .asset.
        // Inserir no meio remapearia todo afixo autorado em silêncio -- um "de Irem" (Vigor)
        // viraria outra coisa sem ninguém notar.
        //
        // Os quatro são consumidos por ResolucaoDeGolpe, via MaoFisicaBridge. Todos entram
        // em PERCENTUAL no asset (5 = 5%), porque é assim que o gênero escreve e é como o
        // jogador lê num tooltip -- a conversão para fração acontece na bridge.

        ChanceCritica,       // Chance de o golpe sair crítico
        DanoCritico,         // Quanto o crítico multiplica, somado ao da arma
        Precisao,            // Chance de acertar (errar é dano zero)
        AumentoDeDanoFisico  // Multiplica o dano físico total do golpe
    }
}
