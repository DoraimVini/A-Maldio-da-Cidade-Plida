using UnityEngine;

namespace FavelaAmarela.Core.Enemies
{
    /// <summary>
    /// A pegada de uma cobertura no chão: um retângulo alinhado aos eixos, em coordenadas de
    /// mundo. É o colisor de um Nobre Fossilizado visto de cima.
    /// </summary>
    public readonly struct CaixaDeCobertura
    {
        /// <summary>Canto inferior esquerdo.</summary>
        public readonly Vector2 Min;

        /// <summary>Canto superior direito.</summary>
        public readonly Vector2 Max;

        /// <summary>Cria a caixa a partir de dois cantos opostos (em qualquer ordem).</summary>
        public CaixaDeCobertura(Vector2 a, Vector2 b)
        {
            Min = Vector2.Min(a, b);
            Max = Vector2.Max(a, b);
        }

        /// <summary>Cria a caixa a partir do centro e do tamanho.</summary>
        public static CaixaDeCobertura DoCentro(Vector2 centro, Vector2 tamanho)
            => new CaixaDeCobertura(centro - tamanho * 0.5f, centro + tamanho * 0.5f);

        /// <summary>Se um ponto está dentro da caixa (bordas incluídas).</summary>
        public bool Contem(Vector2 p) => p.x >= Min.x && p.x <= Max.x && p.y >= Min.y && p.y <= Max.y;
    }

    /// <summary>
    /// Geometria pura da <b>linha de visão do Rei em Amarelo</b>: se o segmento do Rei até o
    /// Damião atravessa alguma cobertura.
    ///
    /// <para><b>Por que geometria e não <c>Physics2D.Linecast</c></b>, como o plano previa. O
    /// jogo só precisa de um teste de segmento contra meia dúzia de retângulos parados — e o
    /// Linecast traria junto tudo o que a física sabe: parede, o colisor do próprio Rei, o do
    /// Damião, a ordem dos <c>FixedUpdate</c> e uma camada nova na <c>TagManager</c> só para
    /// filtrar. Aqui a resposta é a mesma em PlayMode, em EditMode e no guarda que lê a cena
    /// sem rodá-la (<c>OTronoDoOlharTests</c>), e o teste unitário não precisa de cena.</para>
    ///
    /// <para>O algoritmo é o de Liang–Barsky (recorte de segmento por "slabs"): o segmento
    /// cruza a caixa se o intervalo de <c>t</c> em que ele está dentro das duas faixas, x e y,
    /// não for vazio. Sem alocação — roda por quadro.</para>
    /// </summary>
    public static class LinhaDeVisao
    {
        /// <summary>Se o segmento <paramref name="de"/>→<paramref name="ate"/> toca a caixa.</summary>
        public static bool Cruza(Vector2 de, Vector2 ate, in CaixaDeCobertura caixa)
        {
            float dx = ate.x - de.x;
            float dy = ate.y - de.y;
            float t0 = 0f, t1 = 1f;

            return Recorta(-dx, de.x - caixa.Min.x, ref t0, ref t1)
                && Recorta(dx, caixa.Max.x - de.x, ref t0, ref t1)
                && Recorta(-dy, de.y - caixa.Min.y, ref t0, ref t1)
                && Recorta(dy, caixa.Max.y - de.y, ref t0, ref t1);
        }

        /// <summary>
        /// O ângulo, em graus, da direção Rei→Damião medido a partir de "para baixo" (o Rei
        /// fica ao fundo da sala e olha para a entrada). Positivo à direita da tela. É a
        /// convenção do <see cref="OlharDoRei"/>: o farol varre de −Amplitude a +Amplitude.
        /// </summary>
        public static float AnguloAPartirDeBaixo(Vector2 de, Vector2 ate)
        {
            Vector2 d = ate - de;
            if (d.sqrMagnitude < 1e-8f) return 0f;
            return Mathf.Atan2(d.x, -d.y) * Mathf.Rad2Deg;
        }

        private static bool Recorta(float p, float q, ref float t0, ref float t1)
        {
            if (Mathf.Abs(p) < 1e-9f) return q >= 0f;   // paralelo: dentro da faixa ou fora dela

            float r = q / p;
            if (p < 0f)
            {
                if (r > t1) return false;
                if (r > t0) t0 = r;
            }
            else
            {
                if (r < t0) return false;
                if (r < t1) t1 = r;
            }
            return true;
        }
    }
}
