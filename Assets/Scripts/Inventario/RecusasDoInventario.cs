namespace FavelaAmarela.Inventario
{
    /// <summary>
    /// Por que um item da mochila não pôde ir para o corpo.
    ///
    /// <para><b>Por que existe (2026-09-28).</b> O <see cref="InventoryManager.Equipar"/> só
    /// devolvia <c>false</c>, e a tela de inventário não dizia nada: o jogador clicava no escudo,
    /// clicava no corpo, e o escudo continuava na mochila. Recusa muda é indistinguível de
    /// clique que não pegou — e a regra das duas mãos, que é justamente a mais fina, ficava
    /// invisível.</para>
    /// </summary>
    public enum RecusaAoEquipar
    {
        /// <summary>Nada impede.</summary>
        Nenhuma,

        /// <summary>Não há item naquela casa.</summary>
        CasaVazia,

        /// <summary>Consumível, chave, Artefato — não se veste nem se empunha.</summary>
        NaoSeVeste,

        /// <summary>O corpo não tem slot do tipo do item.</summary>
        SemLugarNoCorpo,

        /// <summary>Uma arma de duas mãos toma a Mão Secundária.</summary>
        MaosTomadasPorArmaDeDuasMaos,

        /// <summary>Empunhar duas mãos exige guardar a Mão Secundária, e a mochila está cheia.</summary>
        SemEspacoParaGuardarAMaoSecundaria,
    }

    /// <summary>Por que um item do corpo não pôde voltar para a mochila.</summary>
    public enum RecusaAoDesequipar
    {
        /// <summary>Nada impede.</summary>
        Nenhuma,

        /// <summary>Não há item naquele slot.</summary>
        CasaVazia,

        /// <summary>
        /// A mochila está cheia. <b>Recusa, e não perda:</b> até 2026-09-28 o item saía do corpo,
        /// não cabia, e sumia com um aviso de console ("dropado no chão" — não havia chão).
        /// </summary>
        MochilaCheia,
    }
}
