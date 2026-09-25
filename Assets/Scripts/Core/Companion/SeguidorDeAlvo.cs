using UnityEngine;

namespace FavelaAmarela.Core.Companion
{
    /// <summary>
    /// Regra de movimento de um companheiro que segue Damião (o filhote Mi-Go) — fica
    /// parado dentro de uma distância de conforto e anda em direção ao jogador quando
    /// fica pra trás. POCO puro, sem <c>Rigidbody2D</c>: o Runtime aplica o vetor
    /// devolvido em <c>FixedUpdate</c>, mesma convenção de movimentação do projeto.
    /// </summary>
    public sealed class SeguidorDeAlvo
    {
        /// <summary>Distância dentro da qual o companheiro não se move (perto o suficiente).</summary>
        public float DistanciaDeConforto { get; }

        /// <summary>Velocidade de perseguição quando está além da distância de conforto.</summary>
        public float Velocidade { get; }

        /// <param name="distanciaDeConforto">Deve ser &gt;= 0.</param>
        /// <param name="velocidade">Deve ser &gt; 0.</param>
        public SeguidorDeAlvo(float distanciaDeConforto, float velocidade)
        {
            DistanciaDeConforto = distanciaDeConforto >= 0f ? distanciaDeConforto : 0f;
            Velocidade = velocidade > 0f ? velocidade : 3f;
        }

        /// <summary>
        /// Velocidade a aplicar neste frame: zero se já está perto o bastante, ou o
        /// vetor na direção do alvo com magnitude <see cref="Velocidade"/> caso contrário.
        /// </summary>
        public Vector2 CalcularVelocidade(Vector2 posicaoPropria, Vector2 posicaoAlvo)
        {
            Vector2 delta = posicaoAlvo - posicaoPropria;
            float distancia = delta.magnitude;

            if (distancia <= DistanciaDeConforto) return Vector2.zero;

            return (delta / distancia) * Velocidade; // delta/distancia = normalizado, sem dividir por zero (já tratado acima)
        }
    }
}
