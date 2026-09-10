using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace FavelaAmarela.EditorTools
{
    /// <summary>
    /// Acha e conserta os rótulos de UI que <b>não desenham nada</b> porque a caixa deles é
    /// menor do que o <c>Best Fit</c> aceita.
    ///
    /// <para><b>O defeito, medido (2026-09-09).</b> O Vini mandou a tela de Colapso: a frase
    /// aparecia e os <b>dois botões saíam vazios</b>, só a moldura. Os rótulos existiam, com
    /// texto certo ("Despertar no último refúgio", "Menu principal"), fonte atribuída, cor
    /// visível e componente ligado. A conta explica:</para>
    ///
    /// <list type="bullet">
    ///   <item>Canvas de referência 1920 × 1080</item>
    ///   <item>O botão ocupa 7% da altura → <b>75,6 px</b></item>
    ///   <item>O rótulo ancora 0,1–0,9 do botão e tem <c>sizeDelta −56</c> → 0,8 × 75,6 − 56 =
    ///         <b>4,5 px</b> de altura</item>
    ///   <item><c>Best Fit</c> com <c>MinSize 38</c> em 4,5 px: a Unity <b>não desenha</b></item>
    /// </list>
    ///
    /// <para>O <c>−56</c> é um respiro pensado para painel grande e come um botão inteiro. E o
    /// modo de falha é <b>silencioso</b>: sem erro, sem aviso, sem exceção — o texto
    /// simplesmente não existe na tela.</para>
    ///
    /// <para><b>Por que instancia em vez de ler o YAML.</b> A altura de um rótulo depende da
    /// cadeia inteira de âncoras até o Canvas, e resolver isso à mão em regex é a receita para
    /// consertar o número errado. Instanciado, é a própria Unity quem responde
    /// <c>rect.height</c>.</para>
    /// </summary>
    public static class RotulosQueNaoCabem
    {
        private const string Marcador = "[Rotulos]";

        /// <summary>Fração da caixa acima da qual o <c>MinSize</c> é apertado demais.</summary>
        /// <remarks>Igual à do <c>RotuloCabeNaCaixaTests</c>: é o mesmo critério.</remarks>
        private const float Folga = 0.7f;

        /// <summary>
        /// Fração usada ao <b>escrever</b> o novo mínimo — mais rigorosa que a de detecção.
        ///
        /// <para>Consertar exatamente no limite deixa o valor empatado com o critério, e a caixa
        /// medida em PlayMode não é idêntica à medida em batch: a primeira tentativa consertou
        /// para 21 numa caixa de 30 px e o teste reprovou os mesmos rótulos por centésimos.
        /// Quem conserta precisa de mais margem que quem confere.</para>
        /// </remarks>
        private const float FolgaAoEscrever = 0.6f;

        private static readonly string[] Prefabs =
        {
            "Assets/FavelaAmarela/Resources/HUD_Gameplay.prefab",
            "Assets/FavelaAmarela/Resources/Painel_Opcoes.prefab",
        };

        [MenuItem("Tools/FavelaAmarela/UI: achar rótulos que não cabem")]
        public static void Auditar() => Rodar(consertar: false);

        [MenuItem("Tools/FavelaAmarela/UI: consertar rótulos que não cabem")]
        public static void Consertar() => Rodar(consertar: true);

        private static void Rodar(bool consertar)
        {
            int achados = 0, consertados = 0;

            foreach (var caminho in Prefabs)
            {
                var raiz = PrefabUtility.LoadPrefabContents(caminho);

                try
                {
                    // Um prefab-asset não tem layout resolvido. Instanciar na cena corrente e
                    // forçar o Canvas é o que dá números reais. Cena nova aditiva não serve:
                    // em batch mode a cena inicial é sem título e a Unity recusa.
                    var vivo = Object.Instantiate(raiz);

                    LigarTudo(vivo.transform);
                    ForcarResolucaoDeReferencia(vivo);
                    Canvas.ForceUpdateCanvases();

                    var cv = vivo.GetComponentInChildren<Canvas>(true);
                    if (cv != null)
                        Debug.Log($"{Marcador} canvas medido de {caminho.Split('/').Last()}: " +
                                  $"{cv.GetComponent<RectTransform>().rect.width:0} x " +
                                  $"{cv.GetComponent<RectTransform>().rect.height:0}");


                    var mudancas = new List<(string, float, int, int)>();

                    foreach (var t in vivo.GetComponentsInChildren<Text>(true))
                    {
                        if (!t.resizeTextForBestFit) continue;

                        float altura = t.rectTransform.rect.height;
                        if (altura <= 0f) continue;

                        // FOLGA, e não empate. Medido em batch o canvas dá 1663 x 1247, e os
                        // rótulos dos botões de fluxo saíram com 39,3 px contra MinSize 38 --
                        // 1,3 px de margem. Na resolução de referência (1080 de altura) a mesma
                        // caixa dá 34 px, abaixo do mínimo, e a Unity não desenha. Foi assim que
                        // a tela de Colapso chegou ao jogador com dois botões vazios.
                        //
                        // 70% da caixa medida deixa a margem que a variação de resolução exige.
                        if (t.resizeTextMinSize <= altura * Folga) continue;

                        achados++;
                        int novo = Mathf.Max(8, Mathf.FloorToInt(altura * FolgaAoEscrever));
                        mudancas.Add((Caminho(t.transform, vivo.transform), altura,
                                      t.resizeTextMinSize, novo));
                    }

                    foreach (var (caminhoDoRotulo, altura, antes, depois) in mudancas)
                        Debug.Log($"{Marcador} {System.IO.Path.GetFileNameWithoutExtension(caminho)}" +
                                  $" · {caminhoDoRotulo}: caixa de {altura:0.0} px com MinSize " +
                                  $"{antes} — margem insuficiente. " +
                                  (consertar ? $"MinSize -> {depois}" : ""));

                    if (consertar && mudancas.Count > 0)
                    {
                        foreach (var t in raiz.GetComponentsInChildren<Text>(true))
                        {
                            var achado = mudancas.FirstOrDefault(
                                m => m.Item1 == Caminho(t.transform, raiz.transform));

                            if (achado.Item1 == null) continue;

                            t.resizeTextMinSize = achado.Item4;
                            consertados++;
                        }

                        PrefabUtility.SaveAsPrefabAsset(raiz, caminho);
                    }

                    Object.DestroyImmediate(vivo);
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(raiz);
                }
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"{Marcador} {achados} rótulo(s) que não cabem" +
                      (consertar ? $", {consertados} consertado(s)." : "."));
        }

        /// <summary>
        /// Liga tudo antes de medir: telas de fluxo (Colapso, Opções) nascem <b>inativas</b>, e
        /// objeto inativo não tem layout resolvido — mediria zero e passaria batido. Nada é
        /// salvo com o estado ativo: quem é salvo é o prefab original, intocado nisso.
        /// </summary>
        /// <summary>
        /// Põe o canvas na <b>resolução de referência do próprio CanvasScaler</b> antes de
        /// medir.
        ///
        /// <para><b>Este método existe por causa de um erro meu, medido em 2026-09-10.</b> Em
        /// batch mode o canvas do Editor dá <b>1663 × 1247</b> — 4:3. O jogo roda 16:9 com
        /// referência 1920 × 1080 e <c>match 0,5</c>, o que fixa o canvas lógico em 1080 de
        /// altura. Os botões de fluxo têm 0,07 de altura ancorada:</para>
        ///
        /// <list type="bullet">
        ///   <item>no batch: 0,07 × 1247 = <b>87 px</b> de botão, <b>39 px</b> de rótulo</item>
        ///   <item>no jogo: 0,07 × 1080 = <b>75,6 px</b> de botão, <b>27,6 px</b> de rótulo</item>
        /// </list>
        ///
        /// <para>A primeira passada desta ferramenta escreveu <c>minSize = floor(39 × 0,6) =
        /// 27</c> achando que deixava 40% de folga. Contra os 27,6 px reais, a folga era de
        /// <b>0,6 px</b> — e a tela de Colapso continuou chegando ao jogador com os botões
        /// vazios. O Vini relatou duas vezes.</para>
        ///
        /// <para><c>WorldSpace</c> desliga o <c>CanvasScaler</c> e faz o <c>RectTransform</c>
        /// mandar no tamanho, que é a única forma de escolher a resolução em batch mode —
        /// <c>Screen.SetResolution</c> não tem efeito sem janela.</para>
        /// </summary>
        private static void ForcarResolucaoDeReferencia(GameObject vivo)
        {
            foreach (var canvas in vivo.GetComponentsInChildren<Canvas>(true))
            {
                var escala = canvas.GetComponent<UnityEngine.UI.CanvasScaler>();
                Vector2 alvo = escala != null && escala.uiScaleMode ==
                               UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize
                    ? escala.referenceResolution
                    : new Vector2(1920f, 1080f);

                canvas.renderMode = RenderMode.WorldSpace;
                canvas.GetComponent<RectTransform>().sizeDelta = alvo;

                Debug.Log($"{Marcador} {canvas.name}: medindo a {alvo.x} x {alvo.y} " +
                          "(a resolução de referência, não a do batch).");
            }
        }

        private static void LigarTudo(Transform t)
        {
            t.gameObject.SetActive(true);
            foreach (Transform f in t) LigarTudo(f);
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
