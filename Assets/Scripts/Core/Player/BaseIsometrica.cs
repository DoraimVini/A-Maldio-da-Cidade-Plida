using UnityEngine;

namespace FavelaAmarela.Core.Player
{
    /// <summary>
    /// Converte uma direção do <b>espaço de input</b> para o <b>espaço de mundo</b> do grid
    /// isométrico.
    ///
    /// <para><b>O bug que motivou extrair isto (2026-08-27).</b> Esta conversão vivia como um
    /// <c>private static</c> dentro do <c>PlayerMovement</c> — intestável — e <b>só o movimento
    /// a usava</b>. <c>LookDirection</c> e a direção do golpe recebiam o input cru. O resultado
    /// é que o corpo ia para um lado e a mira, o sprite e toda geometria de "costas" apontavam
    /// para outro, com desvio de <b>26,6° na horizontal e 63,4° na vertical</b>.</para>
    ///
    /// <para>Sintoma que o Vini relatou jogando: <i>"as 8 direções... tudo parece meio fora"</i>.
    /// Não era um sistema quebrado; eram <b>dois espaços de coordenada que ninguém
    /// reconciliou</b>.</para>
    ///
    /// <para><b>Por que a altura da célula é PARÂMETRO e não constante.</b> O manual da Unity
    /// 6.4, em <i>Isometric tilemap grid cells</i>, diz: <i>"Changing the Cell Size of the Grid
    /// component changes the size of angles that make up each Cell, which affects the type of
    /// projection being simulated. By default, Cell Size of the Isometric Cell Layout is
    /// (1, 0.5, 1) which simulates dimetric projection angles. True isometric projection instead
    /// uses a Y value of 0.57735."</i></para>
    ///
    /// <para>Ou seja: o <c>0,5</c> desta conta <b>é</b> o <c>cellSize.y</c> do Grid. Deixá-lo
    /// como literal aqui criaria mais uma constante mantida à mão, que divergiria em silêncio
    /// no dia em que alguém ajustasse o Grid. Um guarda de teste confere as duas contra as
    /// cenas do build.</para>
    ///
    /// <para><b>Nota de nomenclatura:</b> pela definição da própria Unity, <c>(1, 0.5)</c> é
    /// projeção <b>dimétrica</b>, não isométrica verdadeira (que seria <c>0,57735</c>). O
    /// projeto chama de "isométrico" por convenção; o nome fica, a precisão está registrada.</para>
    /// </summary>
    public static class BaseIsometrica
    {
        /// <summary>
        /// Altura de célula do grid do projeto. Dimétrica 2:1 — confirmado nas cenas
        /// (<c>m_CellSize: {x: 1, y: 0.5}</c>, <c>m_CellLayout: 2</c>).
        /// </summary>
        public const float AlturaDeCelulaPadrao = 0.5f;

        /// <summary>
        /// Leva uma direção de input para a direção correspondente no mundo.
        ///
        /// <para>Consequência que surpreende e é <b>correta</b>: as diagonais do teclado viram
        /// as cardinais da tela, e as cardinais do teclado viram as diagonais. Apertar W move
        /// para cima-e-para-a-esquerda; W+D é que sobe reto.</para>
        /// </summary>
        /// <param name="input">Direção lida do controle. Não precisa estar normalizada.</param>
        /// <param name="alturaDaCelula">
        /// O <c>cellSize.y</c> do Grid. 0,5 = dimétrico (padrão do projeto e da Unity);
        /// 0,57735 = isométrico verdadeiro.
        /// </param>
        /// <returns>
        /// A direção no mundo, normalizada. <c>Vector2.zero</c> entra, <c>Vector2.zero</c> sai —
        /// normalizar um vetor nulo devolveria lixo, e "parado" precisa continuar parado.
        /// </returns>
        public static Vector2 ParaMundo(Vector2 input, float alturaDaCelula = AlturaDeCelulaPadrao)
        {
            if (input.sqrMagnitude < 0.000001f) return Vector2.zero;

            float x = input.x - input.y;
            float y = (input.x + input.y) * alturaDaCelula;

            return new Vector2(x, y).normalized;
        }
    }
}
