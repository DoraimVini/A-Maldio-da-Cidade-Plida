using NUnit.Framework;
using UnityEngine;
using FavelaAmarela.Core.Persistence;

namespace FavelaAmarela.Tests.EditMode
{
    public class SaveDataTests
    {
        [Test]
        public void RoundTripJson_PreservaTodosOsCampos()
        {
            var original = new SaveData
            {
                versao = 1,
                resilienciaAtual = 42.5f,
                saltoDesbloqueado = true,
                armaDesbloqueada = false,
                posX = 7f,
                posY = -16.5f
            };

            string json = JsonUtility.ToJson(original);
            var restaurado = JsonUtility.FromJson<SaveData>(json);

            Assert.AreEqual(original.versao, restaurado.versao);
            Assert.AreEqual(original.resilienciaAtual, restaurado.resilienciaAtual, 0.0001f);
            Assert.AreEqual(original.saltoDesbloqueado, restaurado.saltoDesbloqueado);
            Assert.AreEqual(original.armaDesbloqueada, restaurado.armaDesbloqueada);
            Assert.AreEqual(original.posX, restaurado.posX, 0.0001f);
            Assert.AreEqual(original.posY, restaurado.posY, 0.0001f);
        }

        [Test]
        public void Versao_TemDefaultUm()
        {
            var dados = new SaveData();

            Assert.AreEqual(1, dados.versao);
        }
    }
}
