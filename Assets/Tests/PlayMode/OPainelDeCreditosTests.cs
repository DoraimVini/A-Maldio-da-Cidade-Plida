using System.Collections;
using System.Collections.Generic;
using FavelaAmarela.Runtime.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace FavelaAmarela.Tests.PlayMode
{
    /// <summary>
    /// A tela de créditos abre, mostra o texto, cabe na janela e fecha — no singleton real.
    ///
    /// <para>Mesmo desenho do guarda do painel de opções: layout só existe em PlayMode, e a
    /// asserção é relativa à janela para não depender da resolução do canvas de teste.</para>
    /// </summary>
    public sealed class OPainelDeCreditosTests
    {
        private PainelDeCreditos _painel;

        [UnitySetUp]
        public IEnumerator Montar()
        {
            PainelDeCreditos.GarantirInstancia();
            yield return null;
            _painel = PainelDeCreditos.Instancia;
            Assert.NotNull(_painel, "PainelDeCreditos.Instancia não subiu — falta Resources/Painel_Creditos?");
        }

        [UnityTearDown]
        public IEnumerator Desmontar()
        {
            if (_painel != null) _painel.Fechar();
            yield return null;
        }

        [UnityTest]
        public IEnumerator Abre_MostraOTexto_ETodaLinhaCabeNaJanela()
        {
            _painel.Abrir();
            Canvas.ForceUpdateCanvases();
            yield return null;
            yield return null;

            Assert.IsTrue(_painel.EstaAberta, "Abrir() não abriu.");
            Assert.IsTrue(_painel.Texto.Contains("Sucart"),
                "O texto mostrado não cita o Sucart — ou o TextAsset não carregou, ou o 'corpo' está solto.");

            var janela = _painel.transform.Find("Conteudo/Janela") as RectTransform;
            Assert.NotNull(janela, "Sem 'Conteudo/Janela' no painel de créditos.");

            var cj = new Vector3[4];
            janela.GetWorldCorners(cj);

            var fora = new List<string>();
            var cf = new Vector3[4];
            foreach (RectTransform filho in janela)
            {
                filho.GetWorldCorners(cf);
                if (cf[0].y < cj[0].y - 1f || cf[2].y > cj[2].y + 1f || cf[0].x < cj[0].x - 1f || cf[2].x > cj[2].x + 1f)
                    fora.Add($"{filho.name} [y {cf[0].y:F0}..{cf[2].y:F0}]");
            }

            Assert.IsEmpty(fora,
                "Linha(s) da janela de créditos fora dela: " + string.Join(", ", fora) +
                $" (janela y {cj[0].y:F0}..{cj[2].y:F0}).");

            var fechar = janela.Find("Botoes/Botao_Fechar")?.GetComponent<Button>();
            Assert.NotNull(fechar, "Sem 'Botoes/Botao_Fechar'.");
            Assert.IsTrue(fechar.gameObject.activeInHierarchy && fechar.interactable, "Fechar inativo.");

            // A rolagem existe para o texto poder ser maior que a janela: o conteúdo tem de ser
            // mais alto que o viewport, senão a rolagem é decoração e o texto está sendo cortado.
            var rolagem = janela.GetComponentInChildren<ScrollRect>();
            Assert.NotNull(rolagem, "Sem ScrollRect — texto longo não teria como ser lido.");
            Assert.Greater(rolagem.content.rect.height, rolagem.viewport.rect.height * 0.5f,
                "O conteúdo da rolagem está menor que metade do viewport — o ContentSizeFitter não " +
                "está medindo o texto.");
        }

        [UnityTest]
        public IEnumerator Fechar_Fecha()
        {
            _painel.Abrir();
            yield return null;
            _painel.Fechar();
            yield return null;

            Assert.IsFalse(_painel.EstaAberta, "Fechar() não fechou.");
        }
    }
}
