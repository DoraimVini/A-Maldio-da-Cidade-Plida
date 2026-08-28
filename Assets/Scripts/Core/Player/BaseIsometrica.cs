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

        /// <summary>
        /// A direção que uma <b>ação</b> (golpe, habilidade, esquiva) deve usar: a do input
        /// quando há input, e a <b>última encarada</b> quando não há.
        ///
        /// <para><b>O defeito que isto conserta (playtest de 2026-08-28).</b> O Vini: <i>"o
        /// boneco só está atacando enquanto está se movimentando; ele não bate parado"</i>. As
        /// três ações recebiam a direção do <b>input</b>, e as três começam com
        /// <c>if (direcao == Vector2.zero) return;</c> — que é guarda correta, porque golpe sem
        /// direção não tem para onde apontar a hitbox. Parado, o input é zero, então todo golpe
        /// era descartado <b>na primeira linha, sem um log</b>.</para>
        ///
        /// <para><b>Não foi a unificação de espaço que causou:</b> antes dela o código passava
        /// <c>inputDirection</c> cru, que é zero parado do mesmo jeito. O defeito é mais velho —
        /// mas vivia exatamente nas linhas que aquela mudança tocou, e continuou de pé porque
        /// nenhum teste EditMode consegue apertar um botão com o personagem parado.</para>
        ///
        /// <para>A regra certa é simples: <b>ataca-se para onde se encara</b>. Em movimento,
        /// encarar e andar são a mesma direção, então nada muda; parado, a última encarada é a
        /// única resposta que o jogador espera.</para>
        /// </summary>
        /// <param name="input">Direção lida do controle, em espaço de input.</param>
        /// <param name="ultimaDirecaoEncarada">
        /// Para onde o personagem encara, <b>já em espaço de mundo</b>. Nunca deve ser
        /// <c>Vector2.zero</c> — quem a mantém a inicializa com um valor válido.
        /// </param>
        /// <param name="alinhadoAoGrid">
        /// Se o input é remapeado para o grid isométrico. É o <c>useIsometricGridAlignment</c>
        /// do <c>PlayerMovement</c>, e mora aqui para que os dois caminhos sejam testáveis.
        /// </param>
        /// <param name="alturaDaCelula">O <c>cellSize.y</c> do Grid. Ver <see cref="ParaMundo"/>.</param>
        public static Vector2 DirecaoDeAcao(Vector2 input, Vector2 ultimaDirecaoEncarada,
                                            bool alinhadoAoGrid = true,
                                            float alturaDaCelula = AlturaDeCelulaPadrao)
        {
            Vector2 mundo = DirecaoDeMundo(input, alinhadoAoGrid, alturaDaCelula);
            return mundo == Vector2.zero ? ultimaDirecaoEncarada : mundo;
        }

        /// <summary>
        /// A direção de mundo correspondente ao input, com ou sem o remapeamento isométrico.
        /// Devolve <c>Vector2.zero</c> para input nulo nos dois casos.
        /// </summary>
        public static Vector2 DirecaoDeMundo(Vector2 input, bool alinhadoAoGrid = true,
                                             float alturaDaCelula = AlturaDeCelulaPadrao)
        {
            if (alinhadoAoGrid) return ParaMundo(input, alturaDaCelula);

            return input.sqrMagnitude < 0.000001f ? Vector2.zero : input.normalized;
        }
    }
}
