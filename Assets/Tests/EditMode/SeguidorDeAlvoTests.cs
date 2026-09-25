using NUnit.Framework;
using UnityEngine;
using FavelaAmarela.Core.Companion;

namespace FavelaAmarela.Tests.EditMode
{
    /// <summary>
    /// Suite EditMode do <see cref="SeguidorDeAlvo"/> — a regra de movimento do
    /// companheiro Mi-Go que segue Damião. POCO puro, sem cena.
    /// </summary>
    public class SeguidorDeAlvoTests
    {
        [Test]
        public void DentroDaDistanciaDeConforto_FicaParado()
        {
            var seguidor = new SeguidorDeAlvo(distanciaDeConforto: 2f, velocidade: 5f);
            var v = seguidor.CalcularVelocidade(Vector2.zero, new Vector2(1f, 0f));
            Assert.AreEqual(Vector2.zero, v);
        }

        [Test]
        public void ExatamenteNaBorda_FicaParado()
        {
            var seguidor = new SeguidorDeAlvo(distanciaDeConforto: 2f, velocidade: 5f);
            var v = seguidor.CalcularVelocidade(Vector2.zero, new Vector2(2f, 0f));
            Assert.AreEqual(Vector2.zero, v);
        }

        [Test]
        public void AlemDaDistancia_AndaEmDirecaoAoAlvoComAVelocidadeConfigurada()
        {
            var seguidor = new SeguidorDeAlvo(distanciaDeConforto: 1f, velocidade: 4f);
            var v = seguidor.CalcularVelocidade(Vector2.zero, new Vector2(10f, 0f));

            Assert.AreEqual(4f, v.magnitude, 0.0001f);
            Assert.AreEqual(new Vector2(4f, 0f), v);
        }

        [Test]
        public void DirecaoDiagonal_EhNormalizadaAntesDeEscalar()
        {
            var seguidor = new SeguidorDeAlvo(distanciaDeConforto: 0f, velocidade: 2f);
            var v = seguidor.CalcularVelocidade(Vector2.zero, new Vector2(3f, 4f)); // dist=5

            Assert.AreEqual(2f, v.magnitude, 0.0001f);
        }

        [Test]
        public void ValoresInvalidos_CaemNoPadraoSeguro()
        {
            var seguidor = new SeguidorDeAlvo(distanciaDeConforto: -5f, velocidade: -1f);
            Assert.AreEqual(0f, seguidor.DistanciaDeConforto);
            Assert.AreEqual(3f, seguidor.Velocidade);
        }
    }
}
