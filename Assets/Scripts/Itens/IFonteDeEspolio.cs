using System;

namespace FavelaAmarela.Runtime.Itens
{
    /// <summary>
    /// Quem pode largar espólio ao ser derrotado.
    ///
    /// <para><b>Por que existe (2026-08-28).</b> O <c>DropAoAbater</c> exigia
    /// <c>EnemyBase</c> por construção: <c>GetComponent&lt;EnemyBase&gt;()</c>, e
    /// <c>LogError</c> se não achasse. Isso deixava de fora exatamente quem mais importa —
    /// <b>o Abdul não é um <c>EnemyBase</c></b> (implementa <c>IDanificavel</c> direto), então
    /// pôr o componente nele não faria nada. Abater o primeiro chefe do jogo nunca largou uma
    /// peça de equipamento sequer.</para>
    ///
    /// <para>Com a interface, "quem larga espólio" deixa de ser "quem herda de uma classe
    /// específica" e passa a ser "quem sabe avisar que foi derrotado". Chefe novo entra sem
    /// tocar no <c>DropAoAbater</c>.</para>
    /// </summary>
    public interface IFonteDeEspolio
    {
        /// <summary>
        /// Disparado uma vez, quando o ator é derrotado. Quem escuta materializa o espólio.
        /// </summary>
        event Action OnAbatido;
    }
}
