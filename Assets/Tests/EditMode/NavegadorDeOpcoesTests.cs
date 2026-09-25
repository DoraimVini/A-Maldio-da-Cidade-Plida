using System;
using NUnit.Framework;
using FavelaAmarela.Core.Dialogo;

namespace FavelaAmarela.Tests.EditMode
{
    /// <summary>
    /// Suite EditMode do <see cref="NavegadorDeOpcoes"/> — o cursor de uma escolha de
    /// diálogo ramificado (ex.: Lutar × Concordar com o Abdul). POCO puro, sem cena.
    /// </summary>
    public class NavegadorDeOpcoesTests
    {
        [Test]
        public void ComecaNoIndiceZero()
        {
            var nav = new NavegadorDeOpcoes(2);
            Assert.AreEqual(0, nav.IndiceAtual);
        }

        [Test]
        public void Avancar_MoveParaAProxima()
        {
            var nav = new NavegadorDeOpcoes(3);
            nav.Avancar();
            Assert.AreEqual(1, nav.IndiceAtual);
        }

        [Test]
        public void Avancar_DaAVoltaDaUltimaParaAPrimeira()
        {
            var nav = new NavegadorDeOpcoes(2);
            nav.Avancar(); // 1
            nav.Avancar(); // volta pra 0
            Assert.AreEqual(0, nav.IndiceAtual);
        }

        [Test]
        public void Retroceder_DaAVoltaDaPrimeiraParaAUltima()
        {
            var nav = new NavegadorDeOpcoes(3);
            nav.Retroceder();
            Assert.AreEqual(2, nav.IndiceAtual);
        }

        [Test]
        public void AvancarERetroceder_SaoInversos()
        {
            var nav = new NavegadorDeOpcoes(4);
            nav.Avancar();
            nav.Avancar();
            nav.Retroceder();
            nav.Retroceder();
            Assert.AreEqual(0, nav.IndiceAtual);
        }

        [Test]
        public void UnicaOpcao_AvancarNaoSaiDoLugar()
        {
            var nav = new NavegadorDeOpcoes(1);
            nav.Avancar();
            Assert.AreEqual(0, nav.IndiceAtual);
            nav.Retroceder();
            Assert.AreEqual(0, nav.IndiceAtual);
        }

        [Test]
        public void QuantidadeNaoPositiva_Lanca()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new NavegadorDeOpcoes(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new NavegadorDeOpcoes(-1));
        }
    }
}
