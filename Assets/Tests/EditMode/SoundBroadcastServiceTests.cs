using NUnit.Framework;
using FavelaAmarela.Core.Stealth;
using UnityEngine;

namespace FavelaAmarela.Tests.EditMode
{
    public class SoundBroadcastServiceTests
    {
        [Test]
        public void Emitir_SemAssinantes_NaoQuebra()
        {
            var service = new SoundBroadcastService();
            var som = new SomEmitido(Vector2.zero, 10f);
            
            // Não deve lançar exceção
            Assert.DoesNotThrow(() => service.Emitir(som));
        }

        [Test]
        public void Emitir_DisparaEvento_ComPayloadCorreto()
        {
            var service = new SoundBroadcastService();
            var somRecebido = default(SomEmitido);
            bool eventoDisparado = false;

            service.OnSomEmitido += (som) => {
                eventoDisparado = true;
                somRecebido = som;
            };

            var origem = new Vector2(5f, 5f);
            var somEmitido = new SomEmitido(origem, 8f);
            service.Emitir(somEmitido);

            Assert.IsTrue(eventoDisparado);
            Assert.AreEqual(origem, somRecebido.Origem);
            Assert.AreEqual(8f, somRecebido.RaioEfetivo);
        }

        [Test]
        public void Emitir_ComMultiplosAssinantes_DisparaParaTodos()
        {
            var service = new SoundBroadcastService();
            int disparos = 0;

            service.OnSomEmitido += (_) => disparos++;
            service.OnSomEmitido += (_) => disparos++;
            service.OnSomEmitido += (_) => disparos++;

            service.Emitir(new SomEmitido(Vector2.zero, 5f));

            Assert.AreEqual(3, disparos);
        }
    }
}
