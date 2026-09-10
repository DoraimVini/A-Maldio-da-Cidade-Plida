using UnityEngine;

namespace FavelaAmarela.Core.Enemies
{
    /// <summary>
    /// Geometria pura: se Damião está <b>dentro</b> do escudo que uma relíquia ergueu. É a
    /// resposta do rito no Trono de Aldebaran a partir de 2026-09-10 — antes era
    /// <see cref="DetectorDeCostas"/>.
    ///
    /// <para><b>Por que a troca (pedido do Vini).</b> Ele jogou e relatou: <i>"Não tem como
    /// evitar o ataque do Rei, nem de costas."</i> A mecânica antiga dependia de
    /// <c>PlayerMovement.LookDirection</c>, que <b>só é atualizada enquanto o jogador se
    /// move</b> — quem parava para ler o aviso ficava com o olhar preso no Rei e morria sem ter
    /// o que fazer. Um teste de reação cuja resposta certa é "não olhar", num jogo cujo único
    /// aviso era visual, era invencível por construção pelo terceiro caminho.</para>
    ///
    /// <para><b>O abrigo é uma ELIPSE, não um círculo</b> — a mesma lição da arena da Byakhee e
    /// da zona morta da câmera, no mesmo trabalho: a vista deste jogo é 20 × 11,25 unidades, e
    /// espaço de jogo redondo num quadro largo lê torto. Os semi-eixos padrão (2,0 × 1,0) saem
    /// da arte: o <c>Escudo_Magico</c> tem 64 px de largura a PPU 32 com a bolha ocupando 51
    /// deles; à escala 2,5 isso dá <b>4,0 unidades</b> de largura desenhada, e a meia-largura
    /// testada é exatamente a meia-largura que o jogador vê.</para>
    ///
    /// <para><b>A zona testada nunca pode ser menor que a bolha desenhada.</b> "Eu estava dentro
    /// e morri" é o pior desfecho possível numa mecânica de abrigo — pior que abrigo generoso
    /// demais, porque destrói a confiança do jogador no que a tela mostra.</para>
    /// </summary>
    public static class AbrigoDeReliquia
    {
        /// <summary>Meia-largura padrão do abrigo, em unidades. Ver o resumo da classe.</summary>
        public const float SemiEixoXPadrao = 2f;

        /// <summary>Meia-altura padrão do abrigo, em unidades (o achatamento isométrico).</summary>
        public const float SemiEixoYPadrao = 1f;

        /// <summary>
        /// Se <paramref name="posicaoDoJogador"/> está dentro da elipse de abrigo centrada em
        /// <paramref name="centroDoAbrigo"/>.
        ///
        /// <para>Semi-eixo não-positivo devolve <c>false</c>: um abrigo sem tamanho não abriga,
        /// e é melhor falhar visível do que abrigar o mundo inteiro por causa de um campo
        /// zerado no Inspector.</para>
        /// </summary>
        public static bool EstaAbrigado(
            Vector2 posicaoDoJogador,
            Vector2 centroDoAbrigo,
            float semiEixoX = SemiEixoXPadrao,
            float semiEixoY = SemiEixoYPadrao)
        {
            if (semiEixoX <= 0f || semiEixoY <= 0f) return false;

            Vector2 doCentro = posicaoDoJogador - centroDoAbrigo;

            float nx = doCentro.x / semiEixoX;
            float ny = doCentro.y / semiEixoY;

            return nx * nx + ny * ny <= 1f;
        }

        /// <summary>
        /// Segundos para atravessar de <paramref name="origem"/> até a borda do abrigo em
        /// <paramref name="centroDoAbrigo"/>, na <paramref name="velocidade"/> dada.
        ///
        /// <para>Existe para que a <b>geometria da arena e o relógio do rito se mantenham
        /// responsáveis um pelo outro</b>. O escudo acende no começo da calmaria, e a calmaria
        /// é o tempo de corrida: se alguém afastar um altar, o teste que usa isto falha em vez
        /// de o jogador descobrir sozinho que não dava para chegar.</para>
        ///
        /// <para>Mede até a <b>borda</b>, não até o centro — parar em cima do altar não é
        /// exigido, entrar é.</para>
        /// </summary>
        public static float SegundosParaAlcancar(
            Vector2 origem,
            Vector2 centroDoAbrigo,
            float velocidade,
            float semiEixoX = SemiEixoXPadrao,
            float semiEixoY = SemiEixoYPadrao)
        {
            if (velocidade <= 0f) return float.PositiveInfinity;
            if (EstaAbrigado(origem, centroDoAbrigo, semiEixoX, semiEixoY)) return 0f;

            Vector2 doCentro = origem - centroDoAbrigo;
            float distancia = doCentro.magnitude;

            // Raio da elipse NA DIREÇÃO da aproximação — não o semi-eixo maior (otimista) nem o
            // menor (pessimista). Para o ângulo t da direção, r(t) = ab / sqrt((b·cos)² + (a·sin)²).
            Vector2 direcao = doCentro / distancia;
            float b = semiEixoY * direcao.x;
            float a = semiEixoX * direcao.y;
            float raioNaDirecao = semiEixoX * semiEixoY / Mathf.Sqrt(b * b + a * a);

            return Mathf.Max(0f, distancia - raioNaDirecao) / velocidade;
        }
    }
}
