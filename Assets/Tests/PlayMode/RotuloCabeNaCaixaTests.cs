using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace FavelaAmarela.Tests.PlayMode
{
    /// <summary>
    /// Guarda que todo rótulo com <c>Best Fit</c> tem <b>folga</b> entre o
    /// <c>resizeTextMinSize</c> e a caixa dele.
    ///
    /// <para><b>O defeito que chegou ao jogador (2026-09-09).</b> O Vini mandou a captura da
    /// tela de Colapso: a frase aparecia e os <b>dois botões saíam vazios</b>, só a moldura. Os
    /// rótulos existiam, com o texto certo, fonte atribuída, cor visível, componente ligado — e
    /// não desenhavam nada.</para>
    ///
    /// <para><b>A causa, medida:</b> a caixa do rótulo dava <b>39,3 px</b> contra um
    /// <c>MinSize</c> de <b>38</b>. Um vão de 1,3 px. Como o <c>Best Fit</c> se recusa a
    /// desenhar abaixo do mínimo, bastava a tela do jogador ser um pouco menor para o texto
    /// sumir — na resolução de referência (1080 de altura) a mesma caixa dá <b>34 px</b>.</para>
    ///
    /// <para><b>E era silencioso:</b> sem erro, sem aviso, sem exceção. O jogo simplesmente
    /// mostrava botões em branco — inclusive os <b>três do menu de pause</b>, que foi o que
    /// deixou o Vini sem conseguir sair das Opções.</para>
    ///
    /// <para><b>Por que 70% e não "cabe".</b> Empatar não é caber. A caixa muda com a resolução,
    /// então o mínimo tem de valer na menor tela plausível, não na que o teste calhou de medir.
    /// É a mesma lição do resto do projeto: um número que funciona por 1,3 px de margem não
    /// funciona, só ainda não falhou.</para>
    /// </summary>
    public sealed class RotuloCabeNaCaixaTests
    {
        /// <summary>Fração da caixa que o mínimo pode ocupar. O resto é margem de resolução.</summary>
        private const float Folga = 0.7f;

        private readonly List<GameObject> _lixo = new List<GameObject>();

        [TearDown]
        public void Limpar()
        {
            foreach (var go in _lixo) if (go != null) Object.DestroyImmediate(go);
            _lixo.Clear();
        }

        private static IEnumerable<string> Prefabs()
        {
            yield return "HUD_Gameplay";
            yield return "Painel_Opcoes";
        }

        [UnityTest]
        public IEnumerator TodoRotuloComBestFit_TemFolgaNaCaixa()
        {
            var apertados = new List<string>();

            foreach (var nome in Prefabs())
            {
                var prefab = Resources.Load<GameObject>(nome);
                Assert.NotNull(prefab, $"Prefab '{nome}' não está em Resources.");

                var vivo = Object.Instantiate(prefab);
                _lixo.Add(vivo);

                // O Destroy da Unity é adiado ao fim do quadro, então a guarda de singleton do
                // HUDController só derruba a cópia DEPOIS deste yield -- conferir antes não
                // pegaria nada.
                yield return null;

                vivo = Sobrevivente(vivo);
                if (vivo == null) continue;

                // As telas de fluxo (Colapso, Pause, Opções) nascem OCULTAS. Objeto desligado
                // não tem layout, mediria zero, e o defeito passaria batido — que é exatamente
                // como ele chegou ao jogador.
                Ligar(vivo.transform);

                yield return null;
                Canvas.ForceUpdateCanvases();
                yield return null;

                foreach (var t in vivo.GetComponentsInChildren<Text>(true))
                {
                    if (!t.resizeTextForBestFit) continue;

                    float altura = t.rectTransform.rect.height;
                    if (altura <= 0f) continue;

                    if (t.resizeTextMinSize > altura * Folga)
                        apertados.Add($"{nome} · {Caminho(t.transform, vivo.transform)}: " +
                                      $"caixa {altura:0.0} px, MinSize {t.resizeTextMinSize}");
                }
            }

            Assert.IsEmpty(apertados,
                "Rótulos sem folga — abaixo do mínimo o Best Fit não desenha NADA, e sem erro " +
                "nenhum:\n  " + string.Join("\n  ", apertados));
        }

        /// <summary>
        /// Os botões das telas de fluxo precisam ter <b>texto</b>. Um botão em branco é
        /// indistinguível de um botão quebrado, e foi assim que o menu de pause ficou sem saída.
        /// </summary>
        [UnityTest]
        public IEnumerator OsBotoesDeFluxo_TemRotuloEscrito()
        {
            var vivo = Object.Instantiate(Resources.Load<GameObject>("HUD_Gameplay"));
            _lixo.Add(vivo);

            yield return null;

            vivo = Sobrevivente(vivo);
            Assert.NotNull(vivo, "Nem a cópia nem o HUD vivo existem para medir.");

            Ligar(vivo.transform);
            yield return null;

            var mudos = vivo.GetComponentsInChildren<Button>(true)
                .Where(b => Caminho(b.transform, vivo.transform).StartsWith("Tela_"))
                .Where(b =>
                {
                    var t = b.GetComponentInChildren<Text>(true);
                    return t == null || string.IsNullOrWhiteSpace(t.text);
                })
                .Select(b => Caminho(b.transform, vivo.transform))
                .ToArray();

            Assert.IsEmpty(mudos,
                "Botões de tela de fluxo sem rótulo escrito: " + string.Join(", ", mudos));
        }

        /// <summary>
        /// A cópia, ou o HUD <b>vivo</b> quando ela foi destruída.
        ///
        /// <para>O <c>Awake</c> do <c>HUDController</c> tem guarda de singleton: se já existe
        /// instância, a nova se autodestrói. Medir a sobrevivente é igualmente válido — é ela
        /// que o jogador vê — e evita um <c>MissingReferenceException</c> que não diria nada
        /// sobre layout.</para>
        /// </summary>
        private static GameObject Sobrevivente(GameObject copia)
            => copia != null ? copia
                             : FavelaAmarela.Runtime.UI.HUDController.Instancia?.gameObject;

        private static void Ligar(Transform t)
        {
            t.gameObject.SetActive(true);
            foreach (Transform f in t) Ligar(f);
        }

        private static string Caminho(Transform t, Transform raiz)
        {
            var partes = new List<string>();
            while (t != null && t != raiz) { partes.Add(t.name); t = t.parent; }
            partes.Reverse();
            return string.Join("/", partes);
        }
    }
}
