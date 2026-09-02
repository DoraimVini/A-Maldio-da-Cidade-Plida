using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using FavelaAmarela.Runtime.UI;

namespace FavelaAmarela.EditorTools
{
    /// <summary>
    /// Conserta a <b>caixa de diálogo</b>: três defeitos independentes que juntos produziram o
    /// print que o Vini mandou em 2026-09-02, com o poema da Cassilda transbordando a tela.
    ///
    /// <list type="number">
    ///   <item><b>A moldura estava pintada de preto.</b> A <c>Image</c> tinha
    ///   <c>m_Color (0.05, 0.04, 0.02)</c>, e a cor <b>multiplica</b> a textura: o dourado do
    ///   <c>painel_ornado</c> (~220,180,90) virava (11,7,2). O sprite estava lá o tempo todo,
    ///   invisível — daí o relato de "está sem o asset".</item>
    ///
    ///   <item><b><c>verticalOverflow = Overflow</c> desliga o BestFit por altura.</b> Com ele,
    ///   a Unity deixa o texto vazar em vez de encolher, e o <c>resizeTextForBestFit</c> nunca
    ///   é acionado — a caixa nunca tentou caber.</item>
    ///
    ///   <item><b>A caixa não comporta a fala mais longa.</b> Medido: a
    ///   <c>falaDeRecapitulacao</c> do <c>CassildaNPC</c> tem <b>11 quebras de linha
    ///   explícitas</b>, e 11 linhas no piso legível (fonte 24) pedem <b>304 unidades</b> — a
    ///   caixa tinha 259. Não cabia nem encolhendo ao máximo.</item>
    /// </list>
    ///
    /// <para><b>O que NÃO era o problema:</b> largura. Por contagem de caracteres a pior fala
    /// cabia com folga; o gargalo eram as quebras de linha do poema, que nenhuma conta por
    /// caractere revela. Medi errado na primeira tentativa e o número me corrigiu.</para>
    /// </summary>
    public static class ConsertarACaixaDeDialogo
    {
        private const string Marcador = "[CaixaDeDialogo]";
        private const string Hud = "Assets/FavelaAmarela/Resources/HUD_Gameplay.prefab";

        /// <summary>
        /// Altura da caixa, em fração da tela. <b>34%</b> comporta as 11 linhas do poema no
        /// piso legível (304 unidades) com margem para o recuo interno do texto.
        ///
        /// <para>Parece muito para uma caixa de fala, e é uma escolha: este jogo mostra
        /// <b>poemas</b>. Com BestFit, uma fala curta continua sendo desenhada grande — a caixa
        /// grande não a encolhe, só lhe dá espaço.</para>
        /// </summary>
        private const float TopoDaCaixa = 0.46f;
        private const float BaseDaCaixa = 0.12f;

        [MenuItem("Tools/FavelaAmarela/UI: consertar a caixa de diálogo")]
        public static void Executar()
        {
            var raiz = PrefabUtility.LoadPrefabContents(Hud);
            var resumo = new List<string>();

            try
            {
                var caixa = raiz.GetComponentInChildren<TutorialHintUI>(true);
                if (caixa == null)
                {
                    Debug.LogError($"{Marcador} TutorialHintUI não achado no HUD.");
                    return;
                }

                var rt = (RectTransform)caixa.transform;

                // 1. A altura.
                var antesMin = rt.anchorMin;
                var antesMax = rt.anchorMax;

                rt.anchorMin = new Vector2(antesMin.x, BaseDaCaixa);
                rt.anchorMax = new Vector2(antesMax.x, TopoDaCaixa);

                float antesAlt = (antesMax.y - antesMin.y) * 1080f;
                float depoisAlt = (TopoDaCaixa - BaseDaCaixa) * 1080f;

                resumo.Add($"altura: {antesAlt:0} → {depoisAlt:0} unidades — as 11 linhas do " +
                           "poema da Cassilda pedem 304 no piso legível");

                // 2. A cor da moldura.
                var img = caixa.GetComponent<Image>();
                if (img != null)
                {
                    var antesCor = img.color;

                    // Alpha preservado: a translucidez é decisão de quem desenhou a tela. O que
                    // muda é o RGB, que estava apagando o sprite.
                    img.color = new Color(1f, 1f, 1f, antesCor.a);

                    resumo.Add($"cor: RGB({antesCor.r:0.00}, {antesCor.g:0.00}, {antesCor.b:0.00}) " +
                               $"→ branco (alpha {antesCor.a:0.00} preservado) — a cor multiplica " +
                               "a textura, e o dourado do painel virava preto");

                    EditorUtility.SetDirty(img);
                }

                // 3. O overflow.
                var texto = new SerializedObject(caixa).FindProperty("texto")
                                .objectReferenceValue as Text;

                if (texto == null) resumo.Add("texto: campo 'texto' não está ligado");
                else
                {
                    var antesOv = texto.verticalOverflow;

                    texto.verticalOverflow = VerticalWrapMode.Truncate;
                    texto.horizontalOverflow = HorizontalWrapMode.Wrap;

                    resumo.Add($"verticalOverflow: {antesOv} → Truncate — com Overflow o BestFit " +
                               "não encolhe por altura, e o texto só vaza");

                    EditorUtility.SetDirty(texto);
                }

                EditorUtility.SetDirty(rt);
                PrefabUtility.SaveAsPrefabAsset(raiz, Hud, out bool gravou);

                if (!gravou) resumo.Add("PREFAB: SaveAsPrefabAsset RECUSOU");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(raiz);
            }

            AssetDatabase.SaveAssets();

            string quebra = System.Environment.NewLine + "  ";
            Debug.Log($"{Marcador} Concluído:" + quebra + string.Join(quebra, resumo));
        }
    }
}
