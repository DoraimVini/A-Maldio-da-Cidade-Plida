using NUnit.Framework;
using FavelaAmarela.Core.Environment;

namespace FavelaAmarela.Tests.EditMode
{
    public class EnvironmentStateTests
    {
        [Test]
        public void EnvironmentState_InitializaComStubDe0_3()
        {
            var env = new EnvironmentState();
            Assert.AreEqual(0.3f, env.StormIntensity);
        }

        [Test]
        public void SetStormIntensity_ValorDentroDoRange_DefineCorretamente()
        {
            var env = new EnvironmentState();
            env.SetStormIntensity(0.7f);
            Assert.AreEqual(0.7f, env.StormIntensity);
        }

        [Test]
        public void SetStormIntensity_ValorAbaixoDeZero_FazClampParaZero()
        {
            var env = new EnvironmentState();
            env.SetStormIntensity(-0.5f);
            Assert.AreEqual(0f, env.StormIntensity);
        }

        [Test]
        public void SetStormIntensity_ValorAcimaDeUm_FazClampParaUm()
        {
            var env = new EnvironmentState();
            env.SetStormIntensity(1.5f);
            Assert.AreEqual(1f, env.StormIntensity);
        }
    }
}
