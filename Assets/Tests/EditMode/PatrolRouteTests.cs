using System;
using NUnit.Framework;
using UnityEngine;
using FavelaAmarela.Core.Enemies;

namespace FavelaAmarela.Tests.EditMode
{
    public class PatrolRouteTests
    {
        [Test]
        public void Construtor_ArrayVazioOuNulo_LancaArgumentException()
        {
            Assert.Throws<ArgumentException>(() => new PatrolRoute(null));
            Assert.Throws<ArgumentException>(() => new PatrolRoute(new Vector2[0]));
        }

        [Test]
        public void AtualizarChegada_SoAvanca_QuandoDentroDoRaio()
        {
            var route = new PatrolRoute(new[] { Vector2.zero, Vector2.one });
            
            // Longe, não deve avançar
            bool result = route.AtualizarChegada(new Vector2(10f, 10f), 1f);
            Assert.IsFalse(result);
            Assert.AreEqual(Vector2.zero, route.AlvoAtual);

            // Perto, deve avançar
            result = route.AtualizarChegada(new Vector2(0.5f, 0f), 1f);
            Assert.IsTrue(result);
            Assert.AreEqual(Vector2.one, route.AlvoAtual);
        }

        [Test]
        public void RotaDe1Waypoint_NaoQuebraEFicaSempreNele()
        {
            var route = new PatrolRoute(new[] { Vector2.zero });
            
            bool result = route.AtualizarChegada(Vector2.zero, 1f);
            Assert.IsTrue(result);
            Assert.AreEqual(Vector2.zero, route.AlvoAtual); // Continua no 0
        }

        [Test]
        public void Loop_AposOUltimoWaypoint_VoltaAoPrimeiro()
        {
            var route = new PatrolRoute(new[] { Vector2.zero, Vector2.one }, loop: true);
            
            // Alcança índice 0, vai pro índice 1
            route.AtualizarChegada(Vector2.zero, 1f);
            Assert.AreEqual(Vector2.one, route.AlvoAtual);

            // Alcança índice 1, como é loop, volta pro índice 0
            route.AtualizarChegada(Vector2.one, 1f);
            Assert.AreEqual(Vector2.zero, route.AlvoAtual);
        }

        [Test]
        public void PingPong_AposOUltimo_InverteDirecaoEPercorreDeTrasPraFrente()
        {
            var wps = new[] { Vector2.zero, Vector2.one, new Vector2(2, 2) };
            var route = new PatrolRoute(wps, loop: false);

            // 0 -> 1
            route.AtualizarChegada(Vector2.zero, 1f);
            Assert.AreEqual(Vector2.one, route.AlvoAtual);

            // 1 -> 2
            route.AtualizarChegada(Vector2.one, 1f);
            Assert.AreEqual(new Vector2(2, 2), route.AlvoAtual);

            // 2 -> inverte -> 1
            route.AtualizarChegada(new Vector2(2, 2), 1f);
            Assert.AreEqual(Vector2.one, route.AlvoAtual);

            // 1 -> 0
            route.AtualizarChegada(Vector2.one, 1f);
            Assert.AreEqual(Vector2.zero, route.AlvoAtual);
            
            // 0 -> inverte -> 1
            route.AtualizarChegada(Vector2.zero, 1f);
            Assert.AreEqual(Vector2.one, route.AlvoAtual);
        }
    }
}
