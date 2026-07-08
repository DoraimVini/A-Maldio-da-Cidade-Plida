using NUnit.Framework;
using FavelaAmarela.Core.Abilities;

namespace FavelaAmarela.Tests.EditMode
{
    public class BarraEnferrujadaTests
    {
        [Test]
        public void CanActivate_ReturnsTrue_WhenCooldownElapsed()
        {
            var arma = new BarraEnferrujada(cooldown: 0.6f);
            Assert.IsTrue(arma.CanActivate(timeSinceLastUse: 1f));
        }

        [Test]
        public void CanActivate_ReturnsFalse_WhenOnCooldown()
        {
            var arma = new BarraEnferrujada(cooldown: 0.6f);
            Assert.IsFalse(arma.CanActivate(timeSinceLastUse: 0.2f));
        }

        [Test]
        public void Execute_AmostraBaixa_Atordoa()
        {
            var arma = new BarraEnferrujada(probabilidadeAtordoar: 0.35f, duracaoAtordoamento: 2f, amostraAleatoria: () => 0.0);

            var resultado = arma.Execute();

            Assert.IsTrue(resultado.Success);
            Assert.IsTrue(resultado.Atordoou);
            Assert.AreEqual(2f, resultado.DuracaoAtordoamento);
        }

        [Test]
        public void Execute_AmostraAlta_NaoAtordoa()
        {
            var arma = new BarraEnferrujada(probabilidadeAtordoar: 0.35f, amostraAleatoria: () => 0.99);

            var resultado = arma.Execute();

            Assert.IsTrue(resultado.Success);
            Assert.IsFalse(resultado.Atordoou);
            Assert.AreEqual(0f, resultado.DuracaoAtordoamento);
        }

        [Test]
        public void Execute_RetornaDuracaoECooldownConfigurados()
        {
            var arma = new BarraEnferrujada(duration: 0.3f, cooldown: 0.6f, amostraAleatoria: () => 0.99);

            var resultado = arma.Execute();

            Assert.AreEqual(0.3f, resultado.DurationSeconds);
            Assert.AreEqual(0.6f, resultado.CooldownSeconds);
        }
    }
}
