using System.Collections;
using FavelaAmarela.Core.Combat;
using FavelaAmarela.Runtime.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace FavelaAmarela.Tests.PlayMode
{
    /// <summary>
    /// Testes PlayMode (integração de cena) para <see cref="ResilienciaBar"/>.
    /// Diferente dos testes EditMode do Core, estes precisam de GameObject,
    /// Image e do loop de Update — por isso vivem em PlayMode.
    ///
    /// Verificam o contrato de integração:
    ///   • A barra reage ao evento OnChanged (não faz polling).
    ///   • fillAmount converge para o Percentual da POCO.
    ///   • Unbind/OnDisable remove o handler (sem vazamento).
    /// </summary>
    public class ResilienciaBarPlayTests
    {
        private GameObject _go;
        private ResilienciaBar _bar;
        private Image _fill;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("ResilienciaBar_Test");
            var canvasGo = new GameObject("Canvas", typeof(Canvas));
            _go.transform.SetParent(canvasGo.transform);

            var fillGo = new GameObject("Fill", typeof(Image));
            fillGo.transform.SetParent(_go.transform);
            _fill = fillGo.GetComponent<Image>();
            _fill.type = Image.Type.Filled;
            _fill.fillMethod = Image.FillMethod.Horizontal;

            _bar = _go.AddComponent<ResilienciaBar>();

            // Injeta a referência de fill via reflection (campo serializado privado).
            var field = typeof(ResilienciaBar).GetField("fillImage",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field.SetValue(_bar, _fill);

            // Lerp instantâneo para os testes não dependerem de tempo.
            var velField = typeof(ResilienciaBar).GetField("velocidadeLerp",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            velField.SetValue(_bar, 0f);
        }

        [TearDown]
        public void TearDown()
        {
            if (_go != null) Object.Destroy(_go.transform.parent.gameObject);
        }

        [UnityTest]
        public IEnumerator Bind_SincronizaFillComEstadoInicial()
        {
            var rm = new ResilienciaMental(100f, 25f);
            _bar.Bind(rm);
            yield return null; // um frame para Update rodar

            Assert.AreEqual(1f, _fill.fillAmount, 1e-3f, "Fill deveria começar cheio.");
        }

        [UnityTest]
        public IEnumerator SofrerTrauma_AtualizaFill()
        {
            var rm = new ResilienciaMental(100f, 25f);
            _bar.Bind(rm);
            yield return null;

            rm.SofrerTrauma(40f); // 100 → 60, Percentual = 0.6
            yield return null;

            Assert.AreEqual(0.6f, _fill.fillAmount, 1e-3f);
        }

        [UnityTest]
        public IEnumerator EntrarPanico_MudaCorDoFill()
        {
            var rm = new ResilienciaMental(100f, 25f);
            _bar.Bind(rm);
            yield return null;

            Color corNormal = _fill.color;
            rm.SofrerTrauma(80f); // atual = 20, entra em pânico
            yield return null;

            Assert.AreNotEqual(corNormal, _fill.color, "Cor deveria mudar ao entrar em pânico.");
        }

        [UnityTest]
        public IEnumerator Unbind_ParaDeReagirAoEvento()
        {
            var rm = new ResilienciaMental(100f, 25f);
            _bar.Bind(rm);
            yield return null;

            _bar.Unbind();
            rm.SofrerTrauma(50f); // não deveria mais afetar a barra
            yield return null;

            Assert.AreEqual(1f, _fill.fillAmount, 1e-3f,
                "Fill não deveria mudar após Unbind.");
        }

        [UnityTest]
        public IEnumerator OnDisable_RemoveHandler()
        {
            var rm = new ResilienciaMental(100f, 25f);
            _bar.Bind(rm);
            yield return null;

            _bar.enabled = false;
            _go.SetActive(false); // dispara OnDisable
            yield return null;

            // Se o handler ainda estivesse pendurado, isto lançaria ou mudaria estado.
            Assert.DoesNotThrow(() => rm.SofrerTrauma(50f));
        }
    }
}
