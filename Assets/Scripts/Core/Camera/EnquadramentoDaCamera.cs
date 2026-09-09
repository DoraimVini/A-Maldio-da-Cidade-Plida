using UnityEngine;

namespace FavelaAmarela.Core.Camera
{
    /// <summary>
    /// A aritmética do enquadramento: onde a câmera pode parar, e quanto ela treme.
    ///
    /// <para>POCO de propósito (regra de ouro §2 e §4.6): as duas regras aqui são contas puras,
    /// e conta pura testada num teste EditMode vale mais que a mesma conta escondida dentro de
    /// um <c>LateUpdate</c> que nenhum teste consegue rodar.</para>
    /// </summary>
    public static class EnquadramentoDaCamera
    {
        /// <summary>
        /// Prende a posição da câmera dentro dos limites do mapa, descontando o que ela enxerga.
        ///
        /// <para><b>O defeito que isto fecha (2026-09-09).</b> Nada limitava a câmera. Com
        /// <c>orthographicSize</c> 4,21875 a 16:9 ela enxerga <b>7,50 unidades</b> para cada
        /// lado — e no Deserto de Hali o jogador <i>alcança</i> as paredes de limite. A câmera
        /// seguia e revelava até 7,5 unidades de nada além da borda.</para>
        ///
        /// <para><b>Quando a vista é maior que o mapa, ela centraliza</b> em vez de prender.
        /// Prender com <c>min &gt; max</c> daria um <c>Clamp</c> invertido e a câmera saltaria
        /// entre as duas bordas a cada quadro — pior que mostrar o vazio, porque pisca.</para>
        /// </summary>
        /// <param name="desejada">Onde a câmera quer estar (já suavizada).</param>
        /// <param name="minimo">Canto inferior-esquerdo da área jogável.</param>
        /// <param name="maximo">Canto superior-direito da área jogável.</param>
        /// <param name="meiaLargura">Metade do que a câmera enxerga na horizontal.</param>
        /// <param name="meiaAltura">Metade do que ela enxerga na vertical.</param>
        /// <remarks>
        /// Recebe dois <c>Vector2</c> e não um <c>Rect</c> porque o <c>CLAUDE.md</c> desta
        /// camada limita a dependência de UnityEngine a <c>Vector2</c>, <c>Vector3</c> e
        /// <c>Mathf</c> — e <c>Rect</c> não está na lista.
        /// </remarks>
        public static Vector2 Prender(Vector2 desejada, Vector2 minimo, Vector2 maximo,
                                      float meiaLargura, float meiaAltura)
        {
            float x = Prender1D(desejada.x, minimo.x, maximo.x, meiaLargura);
            float y = Prender1D(desejada.y, minimo.y, maximo.y, meiaAltura);

            return new Vector2(x, y);
        }

        private static float Prender1D(float valor, float minimo, float maximo, float meia)
        {
            float piso = minimo + meia;
            float teto = maximo - meia;

            // A vista não cabe no mapa neste eixo: centraliza.
            if (piso > teto) return (minimo + maximo) * 0.5f;

            return Mathf.Clamp(valor, piso, teto);
        }

        // ── tremor por trauma ────────────────────────────────────────────────

        /// <summary>Trauma máximo. Acima disto o tremor não cresce mais.</summary>
        public const float TraumaMaximo = 1f;

        /// <summary>
        /// Acrescenta trauma, com teto.
        ///
        /// <para><b>Por que trauma e não "duração + magnitude".</b> A versão anterior guardava
        /// os dois e sorteava um deslocamento constante até o tempo acabar — dois golpes
        /// seguidos <b>reiniciavam</b> o tremor em vez de somar, e o tremor tinha a mesma
        /// violência do primeiro ao último quadro. Trauma acumula e decai sozinho: uma pancada
        /// grande sacode forte, duas pequenas somam, e o fim é sempre suave.</para>
        /// </summary>
        public static float Acumular(float trauma, float acrescimo)
            => Mathf.Clamp(trauma + Mathf.Max(0f, acrescimo), 0f, TraumaMaximo);

        /// <summary>Decaimento linear. Zero é chão.</summary>
        public static float Decair(float trauma, float deltaTempo, float porSegundo)
            => Mathf.Max(0f, trauma - Mathf.Max(0f, deltaTempo) * Mathf.Max(0f, porSegundo));

        /// <summary>
        /// O deslocamento do tremor neste quadro.
        ///
        /// <para><b>O trauma entra ao QUADRADO</b>, e é o que separa um tremor legível de um
        /// chiado. Linear, um golpe fraco (trauma 0,2) ainda sacode 20% do máximo, e a tela
        /// treme o tempo todo numa luta; ao quadrado ele sacode 4%, que some. O golpe forte
        /// (trauma 1) continua no máximo. É a curva do "juice" clássico, e ela existe para o
        /// tremor dizer <i>quanto</i> doeu.</para>
        ///
        /// <para><b>E escala com o zoom.</b> As cenas deste projeto rodam em dois
        /// <c>orthographicSize</c> — 4,21875 e 5,625, 33% de diferença. Um deslocamento fixo em
        /// unidades de mundo apareceria menor na cena mais afastada. O fator normaliza para que
        /// o tremor ocupe a mesma fração da tela nas duas.</para>
        /// </summary>
        /// <param name="ruido">
        /// Dois valores em [-1, 1]. Vem de fora para a conta ser testável — mesmo motivo pelo
        /// qual o sorteio de itens recebe uma <c>IFonteDeAleatoriedade</c>.
        /// </param>
        public static Vector2 Deslocamento(float trauma, float amplitudeMaxima,
                                           Vector2 ruido, float fatorDeZoom = 1f)
        {
            if (trauma <= 0f) return Vector2.zero;

            float forca = trauma * trauma * Mathf.Max(0f, amplitudeMaxima) * fatorDeZoom;

            return new Vector2(Mathf.Clamp(ruido.x, -1f, 1f),
                               Mathf.Clamp(ruido.y, -1f, 1f)) * forca;
        }

        /// <summary>
        /// O alvo efetivo depois da <b>zona morta</b>: enquanto o jogador estiver dentro do
        /// raio, a câmera não persegue.
        ///
        /// <para><b>Por quê.</b> Sem zona morta, cada passo do jogador move a câmera, e num
        /// isométrico onde andar em diagonal é o normal isso vira um balanço constante de
        /// fundo. Com ela, a câmera fica parada enquanto o jogador circula perto e só volta a
        /// seguir quando ele de fato se desloca.</para>
        ///
        /// <para>Devolve o ponto <b>na borda</b> da zona, e não o jogador: assim a câmera para
        /// assim que o alcança, em vez de continuar até centralizá-lo e reabrir a folga do
        /// outro lado — que faria a zona morta oscilar em vez de segurar.</para>
        /// </summary>
        public static Vector2 AlvoComZonaMorta(Vector2 alvo, Vector2 camera, float raio)
        {
            if (raio <= 0f) return alvo;

            Vector2 daCameraAoAlvo = alvo - camera;
            float distancia = daCameraAoAlvo.magnitude;

            if (distancia <= raio) return camera;

            return alvo - daCameraAoAlvo / distancia * raio;
        }

        /// <summary>
        /// A antecipação: para onde a câmera olha além do jogador, dada a velocidade dele.
        ///
        /// <para>Ela é <b>suavizada à parte</b> do seguimento. Aplicada crua, uma troca de
        /// direção jogaria a câmera de um lado ao outro no mesmo quadro — e trocar de direção é
        /// o que mais se faz num ARPG.</para>
        /// </summary>
        /// <param name="velocidade">Velocidade do alvo, em unidades por segundo.</param>
        /// <param name="velocidadeDeReferencia">A velocidade em que a antecipação é cheia.</param>
        public static Vector2 AntecipacaoDesejada(Vector2 velocidade, float distancia,
                                                  float velocidadeDeReferencia)
        {
            if (distancia <= 0f || velocidadeDeReferencia <= 0f) return Vector2.zero;

            float rapidez = velocidade.magnitude;
            if (rapidez < 0.01f) return Vector2.zero;

            float fracao = Mathf.Min(1f, rapidez / velocidadeDeReferencia);

            return velocidade / rapidez * (distancia * fracao);
        }

        /// <summary>
        /// Ruído <b>contínuo</b> para o tremor: duas amostras de Perlin em faixas diferentes.
        ///
        /// <para><b>O defeito que isto conserta, e ele era meu (2026-09-09).</b> A primeira
        /// versão do tremor por trauma sorteava <c>Random.insideUnitCircle</c> <b>a cada
        /// quadro</b>. Sorteio independente por quadro não é tremor, é chiado: a câmera salta
        /// para um ponto sem relação com o anterior, e a 60 fps isso lê como ruído de vídeo em
        /// vez de impacto. Perlin é contínuo — quadros seguidos ficam perto —, então o
        /// movimento tem direção e a tela parece sacudida por uma força.</para>
        ///
        /// <para>As duas amostras usam deslocamentos grandes e diferentes para X e Y. Sem isso
        /// as duas leem quase o mesmo valor e o tremor sai <b>na diagonal</b>, sempre.</para>
        /// </summary>
        /// <param name="tempo">Relógio do tremor. Determinístico: o mesmo tempo dá o mesmo ruído.</param>
        /// <param name="frequencia">Quantas oscilações por segundo. Alto demais volta a ser chiado.</param>
        public static Vector2 Ruido(float tempo, float frequencia)
        {
            float t = tempo * frequencia;

            return new Vector2(
                Mathf.PerlinNoise(t, 0f) * 2f - 1f,
                Mathf.PerlinNoise(0f, t + 137.13f) * 2f - 1f);
        }

        /// <summary>
        /// Quanto trauma um golpe de <paramref name="dano"/> gera.
        ///
        /// <para>Normalizado por <paramref name="danoDeReferencia"/> — o golpe que deve sacudir
        /// "forte". Acima dele o trauma satura, então um crítico não arranca a tela do lugar.</para>
        /// </summary>
        public static float TraumaDeUmGolpe(float dano, float danoDeReferencia, float teto)
        {
            if (dano <= 0f || danoDeReferencia <= 0f) return 0f;

            return Mathf.Min(teto, dano / danoDeReferencia * teto);
        }
    }
}
