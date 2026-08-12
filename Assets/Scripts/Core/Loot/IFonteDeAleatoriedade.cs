namespace FavelaAmarela.Core.Loot
{
    /// <summary>
    /// Fonte de aleatoriedade injetável. Existe para que o sorteio de drop seja
    /// determinístico sob teste: <c>UnityEngine.Random</c> é estático e global, e com ele
    /// nenhuma tabela de drop poderia ser afirmada numa suíte EditMode.
    /// </summary>
    public interface IFonteDeAleatoriedade
    {
        /// <summary>Devolve um valor em [0, 1).</summary>
        float ProximoValor();

        /// <summary>Devolve um inteiro em [<paramref name="minInclusivo"/>, <paramref name="maxExclusivo"/>).</summary>
        int ProximoInteiro(int minInclusivo, int maxExclusivo);
    }
}
