using NUnit.Framework;
using FavelaAmarela.Core.Abilities;

namespace FavelaAmarela.Tests.EditMode
{
    public class DimensionalLeapTests
    {
        [Test]
        public void DimensionalLeap_CanActivate_ReturnsTrue_WhenConditionsMet()
        {
            // Arrange
            var leap = new DimensionalLeap(duration: 0.2f, cooldown: 1.0f, resilienceCost: 10f);
            
            // Act
            bool canActivate = leap.CanActivate(currentResilience: 20f, timeSinceLastUse: 1.5f);
            
            // Assert
            Assert.IsTrue(canActivate, "Leap should activate if resilience is sufficient and cooldown has passed.");
        }

        [Test]
        public void DimensionalLeap_CanActivate_ReturnsFalse_WhenOnCooldown()
        {
            // Arrange
            var leap = new DimensionalLeap(duration: 0.2f, cooldown: 1.0f, resilienceCost: 10f);
            
            // Act
            bool canActivate = leap.CanActivate(currentResilience: 20f, timeSinceLastUse: 0.5f); // 0.5 < 1.0
            
            // Assert
            Assert.IsFalse(canActivate, "Leap should NOT activate if cooldown has not passed.");
        }

        [Test]
        public void DimensionalLeap_CanActivate_ReturnsFalse_WhenInsufficientResilience()
        {
            // Arrange
            var leap = new DimensionalLeap(duration: 0.2f, cooldown: 1.0f, resilienceCost: 10f);
            
            // Act
            bool canActivate = leap.CanActivate(currentResilience: 5f, timeSinceLastUse: 1.5f); // 5 < 10
            
            // Assert
            Assert.IsFalse(canActivate, "Leap should NOT activate if resilience is lower than the cost.");
        }

        [Test]
        public void DimensionalLeap_Execute_ReturnsCorrectResult()
        {
            // Arrange
            var leap = new DimensionalLeap(duration: 0.2f, cooldown: 1.0f, resilienceCost: 10f);
            
            // Act
            var result = leap.Execute(currentResilience: 20f);
            
            // Assert
            Assert.IsTrue(result.Success);
            Assert.AreEqual(0.2f, result.DurationSeconds);
            Assert.AreEqual(1.0f, result.CooldownSeconds);
            Assert.AreEqual(10f, result.ResilienceCost);
        }
    }
}
