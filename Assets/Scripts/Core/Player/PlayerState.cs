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
        /// <summary>Sem ação exclusiva ativa — pode andar e iniciar Esquiva/Salto/Ataque.</summary>
        Livre,

        /// <summary>Executando uma Esquiva (dash curto).</summary>
        Esquivando,

        /// <summary>Executando o Salto Dimensional (dash intangível).</summary>
        Saltando,

        /// <summary>Executando um golpe da Mão Física (travado no lugar).</summary>
        Atacando
    }
}
