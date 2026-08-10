namespace FavelaAmarela.Core.Persistencia
{
    /// <summary>
    /// Chaves de persistência <b>globais</b> (flags e estado que não pertencem a um objeto
    /// de cena específico). Objetos físicos usam o GUID do <c>ObjetoPersistente</c>; aqui
    /// ficam as invisíveis.
    ///
    /// <para><b>Convenção hierárquica, nunca "magic string" solta.</b> <c>Quest.Main.X</c>,
    /// não <c>chefe_morto</c>. O prefixo agrupa por domínio e torna o save legível na hora
    /// de depurar — abrir o JSON e entender o que cada linha significa.</para>
    ///
    /// <para>Constantes, não literais espalhados: um erro de digitação num literal cria uma
    /// chave nova silenciosamente, e o progresso associado some sem nenhum erro.</para>
    /// </summary>
    public static class ChavesDeSave
    {
        /// <summary>Arma da Tumba empunhada por Damião (identificador da arma, ou vazio se desarmado).</summary>
        public const string ArmaEquipada = "Jogador.Equipamento.Arma";

        /// <summary>Vitalidade corpórea corrente de Damião.</summary>
        public const string VitalidadeAtual = "Jogador.Vitalidade.Atual";

        /// <summary>Resiliência Mental corrente de Damião.</summary>
        public const string ResilienciaAtual = "Jogador.Resiliencia.Atual";

        /// <summary>
        /// Como a conversa com Abdul terminou. Guarda um <b>valor</b>, não um simples "sim":
        /// <see cref="ValorAbdulDerrotado"/> ou <see cref="ValorAbdulPoupado"/> — os dois
        /// desfechos levam a estados de mundo diferentes ao recarregar a cena.
        /// </summary>
        public const string AbdulResolvido = "Quest.Tumba.AbdulResolvido";

        /// <summary>Valor de <see cref="AbdulResolvido"/> quando Abdul foi vencido em combate.</summary>
        public const string ValorAbdulDerrotado = "derrotado";

        /// <summary>Valor de <see cref="AbdulResolvido"/> quando Abdul foi poupado na conversa.</summary>
        public const string ValorAbdulPoupado = "poupado";

        /// <summary>
        /// Yug-Neth já foi libertado e acompanha Damião.
        ///
        /// <para><b>Não é gravada por ninguém hoje, de propósito.</b> A libertação é
        /// <b>derivada</b> de <see cref="AbdulResolvido"/>: os dois caminhos da conversa
        /// (vencer ou poupar) chamam <c>LibertarYugNeth()</c>, e não existe nenhum outro
        /// gatilho. Gravar uma segunda chave criaria uma segunda fonte da verdade que pode
        /// dessincronizar da primeira. Mantida para quando a libertação ganhar um gatilho
        /// independente de Abdul.</para>
        /// </summary>
        public const string YugNethLibertado = "Quest.Tumba.YugNethLibertado";

        /// <summary>
        /// Vitalidade corpórea corrente de Yug-Neth, uma vez livre. Sem isto, cada vez que
        /// ele atravessa de cena (ver <c>TravessiaDoCompanheiro</c>) nascia com Vitalidade
        /// cheia, mesmo tendo levado dano ou estando incapacitado — a instância antiga era
        /// destruída junto com a cena de origem e a nova não sabia de nada.
        ///
        /// <para>Cativo (antes de libertado) ele é intocável (<c>IgnorarDano</c>), então o
        /// valor gravado nessa fase é sempre o máximo — inofensivo.</para>
        /// </summary>
        public const string YugNethVitalidadeAtual = "Companheiro.YugNeth.Vitalidade.Atual";

        /// <summary>O Necronomicon já foi recolhido.</summary>
        public const string NecronomiconColetado = "Quest.Tumba.NecronomiconColetado";

        /// <summary>O baú da Tumba já foi aberto (a arma já foi sorteada e entregue).</summary>
        public const string BauDaTumbaAberto = "Quest.Tumba.BauAberto";

        /// <summary>
        /// O patuá já foi recolhido.
        ///
        /// <para>Domínio próprio (<c>Quest.Patua</c>), não <c>Quest.Tumba</c>: o patuá não
        /// está posicionado em nenhuma cena hoje e o efeito dele ainda não foi decidido —
        /// prefixá-lo como se pertencesse à Tumba fixaria uma decisão que está em aberto.</para>
        /// </summary>
        public const string PatuaColetado = "Quest.Patua.Coletado";

        /// <summary>
        /// Prefixo das chaves de "esta criatura já foi abatida". Diferente das constantes
        /// acima, não é uma chave única: cada inimigo tem a sua, formada com o GUID imutável
        /// do próprio objeto (ver <c>ObjetoPersistente</c>).
        /// </summary>
        public const string PrefixoAbatido = "Mundo.Abatido.";

        /// <summary>
        /// Monta a chave de abate de uma criatura a partir do GUID dela.
        ///
        /// <para>Usa GUID, e não nome nem posição, pelo mesmo motivo de sempre: mover ou
        /// renomear um Cultista no Editor não pode ressuscitá-lo no save de quem já o
        /// matou.</para>
        /// </summary>
        /// <param name="guid">Chave imutável vinda de <c>ObjetoPersistente.Chave</c>.</param>
        /// <returns>A chave completa, ou <c>null</c> se o GUID for vazio (objeto sem chave).</returns>
        public static string ChaveDeAbatido(string guid)
            => string.IsNullOrWhiteSpace(guid) ? null : PrefixoAbatido + guid;
    }
}
