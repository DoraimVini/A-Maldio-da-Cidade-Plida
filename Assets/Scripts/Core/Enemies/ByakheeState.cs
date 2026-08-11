namespace FavelaAmarela.Core.Enemies
{
    /// <summary>
    /// Estados da luta contra o Byakhee, o cadeado vivo dos Portões das Ruínas.
    /// Ver <c>Docs/KnowledgeBundle/lore/cassilda_e_byakhee.md</c> §IV.
    ///
    /// <para>A luta inteira gira em torno de uma inversão: o Byakhee passa a maior parte do
    /// tempo <b>no ar, imune</b>. Atacar não é a ação principal — <b>esperar o pouso</b> é.</para>
    /// </summary>
    public enum ByakheeState
    {
        /// <summary>
        /// Pousado no topo do arco, antes do grito de abertura. Não ataca nem pode ser ferido.
        /// </summary>
        Espreita,

        /// <summary>
        /// No ar, cruzando a arena em linha reta (zigue-zague a partir da fase 2).
        /// <b>Imune.</b>
        /// </summary>
        Rasante,

        /// <summary>
        /// No ar, descendo na vertical sobre Damião. <b>Imune</b> — a resposta é a Esquiva.
        /// </summary>
        MergulhoDeGarras,

        /// <summary>
        /// Aponta o bico para Damião por 1 s antes de emitir o cone de pressão sonora.
        /// Só existe a partir da fase 2. <b>Imune</b>, mas o telegrama dá tempo de sair.
        /// </summary>
        GritoDirecionado,

        /// <summary>
        /// <b>A única janela de dano.</b> Pousa, ataca com as garras e volta a voar. A duração
        /// encurta da fase 1 para a 2 — é o que aperta a luta sem aumentar dano.
        /// </summary>
        Pousado,

        /// <summary>
        /// Fase 3: circula a arena sem pousar. Sem a Lâmina do Sinal, o pouso só vem
        /// espontaneamente — é o trecho mais longo de paciência da luta.
        /// </summary>
        Circundando,

        /// <summary>
        /// Abaixo de 10%: grito longo que drena RM depressa. Interrompível com um golpe,
        /// como o Nagaraja — mas para golpear é preciso que ele esteja ao alcance.
        /// </summary>
        Frenesi,

        /// <summary>Abatido — dropa o Anel do Sinal Amarelo e os Portões se abrem.</summary>
        Derrotado
    }
}
