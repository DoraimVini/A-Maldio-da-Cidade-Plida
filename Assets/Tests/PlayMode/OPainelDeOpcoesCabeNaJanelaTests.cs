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
    /// Toda linha do painel de Opções — inclusive a dos botões — fica <b>dentro da janela</b>.
    ///
    /// <para><b>O defeito (2026-09-10).</b> O Vini: <i>"não tem como voltar do menu de
    /// opções"</i>. O botão Fechar existia, ligado ao <c>Fechar()</c>, e estava em
    /// y = −176 … −76 a 1920 × 1080: <b>abaixo do monitor</b>. A coluna da janela tinha
    /// <c>childControlHeight = false</c>, então os <c>LayoutElement</c> com altura preferida
    /// (54, 34, 30 …) não valiam nada e cada linha ficava com os 100 px padrão de um
    /// RectTransform novo — oito linhas de 100 numa janela de 520.</para>
    ///
    /// <para>Medido em PlayMode, no prefab real de <c>Resources</c>, porque layout não existe
    /// no YAML: só depois de um quadro de <c>Canvas</c> as posições são as que o jogador vê.
    /// A asserção é relativa à janela (filho dentro do pai), então não depende da resolução
    /// do canvas de teste.</para>
    /// </summary>
    public sealed class OPainelDeOpcoesCabeNaJanelaTests
    {
        private GameObject _painel;
        private bool _instanciadoAqui;

        /// <summary>
        /// O painel é singleton e nasce sozinho no início do Play (<c>GarantirInstancia</c>);
        /// um segundo Instantiate é destruído no Awake dele. Usa-se o que existe, e só se
        /// instancia quando não há nenhum.
        /// </summary>
        private PainelDeOpcoes ObterPainel()
        {
            if (PainelDeOpcoes.Instancia != null)
            {
                _painel = PainelDeOpcoes.Instancia.gameObject;
                return PainelDeOpcoes.Instancia;
            }

            var prefab = Resources.Load<GameObject>("Painel_Opcoes");
            Assert.NotNull(prefab, "Resources/Painel_Opcoes não existe — a tela de opções não nasce.");
            _painel = Object.Instantiate(prefab);
            _instanciadoAqui = true;
            return _painel.GetComponent<PainelDeOpcoes>();
        }

        [UnityTearDown]
        public IEnumerator Desmontar()
        {
            if (_painel != null)
            {
                var p = _painel.GetComponent<PainelDeOpcoes>();
                if (p != null) p.Fechar();
                if (_instanciadoAqui) Object.Destroy(_painel);
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator TodaLinhaDaJanela_FicaDentroDela_EOsBotoesTambem()
        {
            var painel = ObterPainel();
            Assert.NotNull(painel, "O prefab não tem PainelDeOpcoes.");

            painel.Abrir();
            Canvas.ForceUpdateCanvases();
            yield return null;
            yield return null;

            var janela = _painel.transform.Find("Conteudo/Janela") as RectTransform;
            Assert.NotNull(janela, "Sem 'Conteudo/Janela' no painel — a hierarquia mudou; adapte o teste.");

            var cj = new Vector3[4];
            janela.GetWorldCorners(cj);
            float yMin = cj[0].y, yMax = cj[2].y, xMin = cj[0].x, xMax = cj[2].x;

            var fora = new List<string>();
            var cf = new Vector3[4];
            foreach (RectTransform filho in janela)
            {
                filho.GetWorldCorners(cf);
                if (cf[0].y < yMin - 1f || cf[2].y > yMax + 1f || cf[0].x < xMin - 1f || cf[2].x > xMax + 1f)
                    fora.Add($"{filho.name} [y {cf[0].y:F0}..{cf[2].y:F0}]");
            }

            TestContext.WriteLine($"janela y {yMin:F0}..{yMax:F0}; {janela.childCount} linhas");

            Assert.IsEmpty(fora,
                "Linha(s) do painel de Opções FORA da janela: " + string.Join(", ", fora) +
                $". Janela y {yMin:F0}..{yMax:F0}. Se a última é 'Botoes', o jogador não tem como " +
                "fechar a tela — é o defeito de 2026-09-10. Confira childControlHeight na coluna " +
                "e a altura da janela em MontarPainelDeOpcoes.");

            var fechar = janela.Find("Botoes/Botao_Fechar")?.GetComponent<Button>();
            Assert.NotNull(fechar, "Sem 'Botoes/Botao_Fechar' na janela.");
            Assert.IsTrue(fechar.gameObject.activeInHierarchy && fechar.interactable,
                "O botão Fechar existe mas está inativo ou não interativo.");
        }

        [UnityTest]
        public IEnumerator Esc_FechaATela()
        {
            var painel = ObterPainel();

            painel.Abrir();
            yield return null;
            Assert.IsTrue(painel.EstaAberta, "Abrir() não abriu.");

            // Sem teclado físico no runner: o caminho do Esc é o Fechar() que o Update chama.
            // O que se guarda aqui é a semântica pública que o PausaInputHandler consome.
            painel.Fechar();
            yield return null;

            Assert.IsFalse(painel.EstaAberta, "Fechar() não fechou.");
            Assert.IsFalse(painel.ConsumiuEscNesteQuadro,
                "ConsumiuEscNesteQuadro acusa Esc num quadro em que nenhum Esc foi lido.");
        }
    }
}
