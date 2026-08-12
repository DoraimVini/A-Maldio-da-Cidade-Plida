namespace FavelaAmarela.Core.Player
{
    /// <summary>
    /// Estados de ação exclusiva de Damião. O modo furtivo (Sneaking/Walking/Running)
    /// é um eixo ortogonal e vive em <c>PlayerStealthState</c> — não aqui. O Colapso
    /// também não entra: é responsabilidade do <c>GameLoopStateMachine</c> via
    /// <c>ResilienciaMental.IsColapso</c>; duplicá-lo aqui reintroduziria a redundância
    /// que esta FSM existe para eliminar.
    /// </summary>
    public enum PlayerState
    {
        /// <summary>Sem ação exclusiva ativa — pode andar e iniciar Esquiva/Ataque.</summary>
        Livre,

        /// <summary>Executando uma Esquiva (dash curto).</summary>
        Esquivando,

        /// <summary>Executando um golpe da Mão Física (travado no lugar).</summary>
        Atacando,

        /// <summary>
        /// Congelado pelos Cones de Gelo de Abdul (3 acúmulos de frio). Diferente dos
        /// outros estados, <b>não é uma ação escolhida</b> — é imposto pelo inimigo, então
        /// entra por <c>ForcarEstado</c> e interrompe o que estiver em curso.
        /// </summary>
        Congelado
    }
}
