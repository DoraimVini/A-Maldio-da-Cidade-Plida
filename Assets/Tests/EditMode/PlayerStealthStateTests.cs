using NUnit.Framework;
using FavelaAmarela.Player;

namespace FavelaAmarela.Tests
{
    /// <summary>
    /// EditMode unit tests for <see cref="PlayerStealthState"/> POCO.
    /// Validates speed/noise values per movement mode and storm dampening.
    /// </summary>
    [TestFixture]
    public class PlayerStealthStateTests
    {
        private PlayerStealthState state;

        // Default values from PlayerStealthState constructor
        private const float DefaultSneakSpeed = 2.0f;
        private const float DefaultSneakNoise = 2.0f;
        private const float DefaultWalkSpeed = 4.5f;
        private const float DefaultWalkNoise = 5.5f;
        private const float DefaultRunSpeed = 7.5f;
        private const float DefaultRunNoise = 8.5f;

        [SetUp]
        public void SetUp()
        {
            state = new PlayerStealthState();
        }

        // --- Mode: Walking (default) ---

        [Test]
        public void DefaultMode_IsWalking()
        {
            Assert.AreEqual(MovementMode.Walking, state.CurrentMode);
        }

        [Test]
        public void WalkingMode_HasCorrectSpeed()
        {
            state.SetMode(MovementMode.Walking);
            Assert.AreEqual(DefaultWalkSpeed, state.Speed, 0.001f);
        }

        [Test]
        public void WalkingMode_HasCorrectNoise()
        {
            state.SetMode(MovementMode.Walking);
            Assert.AreEqual(DefaultWalkNoise, state.NoiseRadius, 0.001f);
        }

        // --- Mode: Sneaking ---

        [Test]
        public void SneakingMode_HasCorrectSpeed()
        {
            state.SetMode(MovementMode.Sneaking);
            Assert.AreEqual(DefaultSneakSpeed, state.Speed, 0.001f);
        }

        [Test]
        public void SneakingMode_HasCorrectNoise()
        {
            state.SetMode(MovementMode.Sneaking);
            Assert.AreEqual(DefaultSneakNoise, state.NoiseRadius, 0.001f);
        }

        // --- Mode: Running ---

        [Test]
        public void RunningMode_HasCorrectSpeed()
        {
            state.SetMode(MovementMode.Running);
            Assert.AreEqual(DefaultRunSpeed, state.Speed, 0.001f);
        }

        [Test]
        public void RunningMode_HasCorrectNoise()
        {
            state.SetMode(MovementMode.Running);
            Assert.AreEqual(DefaultRunNoise, state.NoiseRadius, 0.001f);
        }

        // --- Noise Emission ---

        [Test]
        public void NoiseEmission_WhenNotMoving_IsZero()
        {
            state.SetMode(MovementMode.Running);
            float noise = state.GetCurrentNoiseEmission(isMoving: false, stormIntensity: 0f);
            Assert.AreEqual(0f, noise, 0.001f);
        }

        [Test]
        public void NoiseEmission_WhenMoving_NoStorm_EqualsNoiseRadius()
        {
            state.SetMode(MovementMode.Walking);
            float noise = state.GetCurrentNoiseEmission(isMoving: true, stormIntensity: 0f);
            Assert.AreEqual(DefaultWalkNoise, noise, 0.001f);
        }

        [Test]
        public void NoiseEmission_WhenMoving_FullStorm_IsReduced()
        {
            state.SetMode(MovementMode.Walking);
            float noise = state.GetCurrentNoiseEmission(isMoving: true, stormIntensity: 1.0f);
            // Dampening = 1.0 - Clamp01(1.0 * 0.6) = 1.0 - 0.6 = 0.4
            float expected = DefaultWalkNoise * 0.4f;
            Assert.AreEqual(expected, noise, 0.001f);
        }

        [Test]
        public void NoiseEmission_WhenMoving_HalfStorm_IsPartiallyReduced()
        {
            state.SetMode(MovementMode.Running);
            float noise = state.GetCurrentNoiseEmission(isMoving: true, stormIntensity: 0.5f);
            // Dampening = 1.0 - Clamp01(0.5 * 0.6) = 1.0 - 0.3 = 0.7
            float expected = DefaultRunNoise * 0.7f;
            Assert.AreEqual(expected, noise, 0.001f);
        }

        [Test]
        public void NoiseEmission_StormIntensityAboveOne_ClampedToMax()
        {
            state.SetMode(MovementMode.Sneaking);
            // Storm intensity above 1.0 should be clamped via Clamp01(2.0 * 0.6) = Clamp01(1.2) = 1.0
            // Dampening = 1.0 - 1.0 = 0.0
            float noise = state.GetCurrentNoiseEmission(isMoving: true, stormIntensity: 2.0f);
            Assert.AreEqual(0f, noise, 0.001f);
        }

        // --- Mode switching ---

        [Test]
        public void ModeSwitching_UpdatesSpeedAndNoise()
        {
            state.SetMode(MovementMode.Sneaking);
            Assert.AreEqual(DefaultSneakSpeed, state.Speed, 0.001f);

            state.SetMode(MovementMode.Running);
            Assert.AreEqual(DefaultRunSpeed, state.Speed, 0.001f);
            Assert.AreEqual(DefaultRunNoise, state.NoiseRadius, 0.001f);
        }

        // --- Custom constructor values ---

        [Test]
        public void CustomConstructor_UsesProvidedValues()
        {
            var custom = new PlayerStealthState(
                sneakSpeed: 1f, sneakNoise: 0.5f,
                walkSpeed: 3f, walkNoise: 2f,
                runSpeed: 6f, runNoise: 5f);

            custom.SetMode(MovementMode.Sneaking);
            Assert.AreEqual(1f, custom.Speed, 0.001f);
            Assert.AreEqual(0.5f, custom.NoiseRadius, 0.001f);

            custom.SetMode(MovementMode.Running);
            Assert.AreEqual(6f, custom.Speed, 0.001f);
            Assert.AreEqual(5f, custom.NoiseRadius, 0.001f);
        }
    }
}
