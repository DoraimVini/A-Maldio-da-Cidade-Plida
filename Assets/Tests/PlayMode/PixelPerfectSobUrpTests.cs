using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace FavelaAmarela.Tests.PlayMode
{
    /// <summary>
    /// A pergunta que o spike da URP existe para responder, e que <b>nenhum outro teste da
    /// suíte responde</b>.
    ///
    /// <para><b>Por que os outros não bastam.</b> O <c>CameraPixelPerfectTests</c> é EditMode e
    /// lê o YAML da cena: PPU, resolução de referência, flags. Esses campos <b>não mudam</b> com
    /// a migração — o arquivo continua igual byte a byte. Ele passaria mesmo se o
    /// <c>PixelPerfectCamera</c> tivesse parado de funcionar por completo. Suíte verde ali não é
    /// evidência de nada sobre a URP.</para>
    ///
    /// <para><b>O que este mede.</b> Roda a cena de verdade e lê o
    /// <c>orthographicSize</c> <i>resultante</i>. Esse número não está escrito em lugar nenhum:
    /// ele é <b>derivado</b> pelo <c>PixelPerfectCamera</c> a cada <c>OnPreCull</c>, a partir da
    /// resolução de referência e da tela. Se a migração quebrou a corrente, o número muda — e
    /// com ele o enquadramento de todas as cenas.</para>
    ///
    /// <para><b>E há um motivo concreto para desconfiar:</b> depois de instalar a URP existem
    /// <b>dois</b> <c>PixelPerfectCamera</c> no projeto — o de <c>Unity.2D.PixelPerfect</c>,
    /// que é o que as cenas referenciam por GUID, e o de
    /// <c>Unity.RenderPipelines.Universal.2D.Runtime</c>, que a URP traz. Qual deles governa é
    /// exatamente o que precisa ser medido, não deduzido.</para>
    /// </summary>
    public sealed class PixelPerfectSobUrpTests
    {
        /// <summary>O tamanho derivado nas 4 cenas de referência de 480×270.</summary>
        private const float TamanhoEsperado = 4.21875f;

        /// <summary>
        /// Carregar a cena de verdade acende um <c>LogError</c> do <c>GameLoopBootstrap</c>
        /// ("Nenhum HUDController na cena"), e o framework de teste trata log de erro não
        /// esperado como falha. Isso é um defeito <b>real e separado</b> — está registrado —,
        /// mas mascarar este teste com ele mediria o HUD em vez do pixel-perfect.
        /// </summary>
        [SetUp]
        public void Preparar() => LogAssert.ignoreFailingMessages = true;

        [TearDown]
        public void Limpar() => LogAssert.ignoreFailingMessages = false;

        [UnityTest]
        public IEnumerator ODesertoMantemOTamanhoDerivado()
        {
            yield return CarregarEConferir("Deserto_Hali", TamanhoEsperado);
        }

        [UnityTest]
        public IEnumerator ATumbaMantemOTamanhoDerivado()
        {
            yield return CarregarEConferir("Tumba_De_Alhazred", TamanhoEsperado);
        }

        private static IEnumerator CarregarEConferir(string cena, float esperado)
        {
            // Dentro da corrotina, e não no [SetUp]: o erro do GameLoopBootstrap sai durante o
            // carregamento da cena, já fora do escopo em que o SetUp vale.
            LogAssert.ignoreFailingMessages = true;

            SceneManager.LoadScene(cena, LoadSceneMode.Single);

            // Dois quadros: um para a cena subir, outro para o PixelPerfectCamera rodar o
            // OnPreCull que DERIVA o tamanho. Um só mediria o valor serializado, que é o que
            // este teste justamente não quer medir.
            yield return null;
            yield return null;

            var cam = Camera.main;
            Assert.NotNull(cam, $"{cena} não tem câmera marcada como MainCamera.");

            Assert.IsTrue(cam.orthographic, $"A câmera de {cena} deixou de ser ortográfica.");

            Assert.AreEqual(esperado, cam.orthographicSize, 0.001f,
                $"O orthographicSize derivado de {cena} mudou: {cam.orthographicSize} em vez de " +
                $"{esperado}. Isso muda o enquadramento da cena inteira — é o risco central da " +
                "migração para a URP, e ele não aparece em nenhum teste que só leia o YAML.");
        }
    }
}
