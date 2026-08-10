namespace FavelaAmarela.Core.Enemies
{
    /// <summary>
    /// Estados da Coisa do Cemitério (bestiário, item 5). Diferente do Cultista,
    /// ela nunca "descansa" — não existe um estado parado/inconsciente.
    /// </summary>
    public enum CoisaDoCemiterioState
    {
        /// <summary>Se aproxima devagar da última posição aproximada conhecida (fareja).</summary>
        Farejando,

        /// <summary>Um estímulo sonoro recente revelou a posição exata — avança direto.</summary>
        AlvoPreciso
    }
}
