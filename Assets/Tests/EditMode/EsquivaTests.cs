using NUnit.Framework;
using FavelaAmarela.Core.Abilities;

namespace FavelaAmarela.Tests.EditMode
{
    public class EsquivaTests
    {
        [Test]
        public void Esquiva_CanActivate_ReturnsTrue_WhenCooldownElapsed()
        {
            var esquiva = new Esquiva(duration: 0.15f, cooldown: 0.8f, speedMultiplier: 2.5f);

            bool canActivate = esquiva.CanActivate(timeSinceLastUse: 1.0f);

            Assert.IsTrue(canActivate, "Esquiva deve ativar se o cooldown já passou.");
        }

        [Test]
        public void Esquiva_CanActivate_ReturnsFalse_WhenOnCooldown()
        {
            var esquiva = new Esquiva(duration: 0.15f, cooldown: 0.8f, speedMultiplier: 2.5f);

            bool canActivate = esquiva.CanActivate(timeSinceLastUse: 0.3f); // 0.3 < 0.8

            Assert.IsFalse(canActivate, "Esquiva NÃO deve ativar se o cooldown não passou.");
        }

        [Test]
        public void Esquiva_Execute_ReturnsCorrectResult()
        {
            var esquiva = new Esquiva(duration: 0.15f, cooldown: 0.8f, speedMultiplier: 2.5f);

            var result = esquiva.Execute();

            Assert.IsTrue(result.Success);
            Assert.AreEqual(0.15f, result.DurationSeconds);
            Assert.AreEqual(2.5f, result.SpeedMultiplier);
        }
    }
}
