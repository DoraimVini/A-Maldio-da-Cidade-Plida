using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using FavelaAmarela.Runtime.UI;
using FavelaAmarela.Runtime.Combat;

namespace FavelaAmarela.EditorTools
{
    /// <summary>
    /// Utilitário de Editor: monta o <see cref="PainelDeFicha"/> dentro do painel de inventário
    /// da <b>cena aberta</b>, e liga as duas referências que ele precisa.
    ///
    /// <para>Existe pelo mesmo motivo de <c>BuildHUDCompleto</c>: montar hierarquia de UI à mão
    /// no YAML da cena exige <c>fileID</c> inéditos e entradas em <c>m_Component</c>; errar
    /// corrompe a cena de um jeito que só aparece ao abri-la. A Unity gera tudo certo.</para>
    ///
    /// <para>A ficha é filha de <c>raizDoPainel</c> de propósito — liga e desliga junto com o
    /// inventário, sem precisar de tecla própria nem de acoplamento entre os dois scripts.</para>
    ///
    /// <para>Idempotente: se a ficha já existir na cena, só reata as referências.</para>
    /// </summary>
    public static class BuildPainelDeFicha
    {
        private const string NomeDoObjeto = "PainelDeFicha";

        /// <summary>
        /// Cenas jogáveis, para execução em lote. Mesma lista de
        /// <c>PainelDeFichaNoMundoTests</c> — se divergirem, o guarda acusa.
        ///
        /// <para><c>Cena_ArenaDeTestes</c> está fora por ora: é uma decisão de escopo pendente
        /// com o Vini (a Arena é onde os chefes são calibrados, então é onde a ficha visível
        /// serviria mais). Entrando ela aqui, tem de entrar no guarda junto.</para>
        /// </summary>
        private static readonly string[] CenasJogaveis =
        {
            "Assets/Scenes/Deserto_Hali.unity",
            "Assets/Scenes/Playtest_RuinasPalidas.unity",
            "Assets/Scenes/Santuario_Yhtill.unity",
        };

        /// <summary>
        /// Ponto de entrada em lote (batch mode). <see cref="Montar"/> age na cena <b>aberta</b>,
        /// e em <c>-executeMethod</c> não há cena aberta — sem isto a ferramenta não faria nada e
        /// pareceria ter funcionado.
        /// </summary>
        public static void MontarEmTodasAsCenas()
        {
            int ok = 0;

            foreach (var caminho in CenasJogaveis)
            {
                var cena = EditorSceneManager.OpenScene(caminho, OpenSceneMode.Single);
                Montar();
                EditorSceneManager.SaveScene(cena);
                ok++;
                Debug.Log($"[BuildPainelDeFicha] {caminho}: pronto e salvo.");
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[BuildPainelDeFicha] Lote concluído — {ok}/{CenasJogaveis.Length} cenas.");
        }

        [MenuItem("Tools/FavelaAmarela/Montar Painel de Ficha na cena")]
        public static void Montar()
        {
            var painelInv = Object.FindAnyObjectByType<PainelDeInventario>(
                FindObjectsInactive.Include);

            if (painelInv == null)
            {
                Debug.LogError("[BuildPainelDeFicha] Sem PainelDeInventario na cena aberta — a " +
                               "ficha vive dentro dele. Abra uma cena jogável.");
                return;
            }

            var raiz = RaizDoPainel(painelInv);
            if (raiz == null)
            {
                Debug.LogError("[BuildPainelDeFicha] O PainelDeInventario está sem 'raizDoPainel'. " +
                               "Ligue esse campo antes de montar a ficha.");
                return;
            }

            var ficha = EncontrarOuCriar(raiz.transform);

            // As duas referências. Sem elas o painel abre em branco — e um painel de diagnóstico
            // em branco é pior que nenhum, porque parece que os atributos estão zerados.
            var so = new SerializedObject(ficha);

            var pCorpo = so.FindProperty("corpo");
            var pVit = so.FindProperty("vitalidadeDoJogador");

            if (pCorpo == null || pVit == null)
            {
                Debug.LogError("[BuildPainelDeFicha] Campo serializado não encontrado em " +
                               "PainelDeFicha ('corpo' e/ou 'vitalidadeDoJogador'). Se foram " +
                               "renomeados, atualize esta ferramenta.", ficha);
                return;
            }

            if (pCorpo.objectReferenceValue == null)
                pCorpo.objectReferenceValue = ficha.GetComponentInChildren<Text>(true);

            if (pVit.objectReferenceValue == null)
                pVit.objectReferenceValue = AcharVitalidadeDoJogador();

            so.ApplyModifiedPropertiesWithoutUndo();

            if (pVit.objectReferenceValue == null)
                Debug.LogWarning("[BuildPainelDeFicha] Nenhuma VitalidadeBridge marcada 'Player' " +
                                 "na cena — a ficha vai dizer que está indisponível. Ligue à mão " +
                                 "se Damião vier de outro caminho.", ficha);

            EditorUtility.SetDirty(ficha);
            EditorSceneManager.MarkSceneDirty(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene());

            Debug.Log("[BuildPainelDeFicha] Ficha montada em " +
                      $"'{raiz.name}'. SALVE A CENA (Ctrl+S).", ficha);
        }

        private static GameObject RaizDoPainel(PainelDeInventario painel)
        {
            var so = new SerializedObject(painel);
            var p = so.FindProperty("raizDoPainel");
            return p?.objectReferenceValue as GameObject;
        }

        private static PainelDeFicha EncontrarOuCriar(Transform pai)
        {
            var existente = pai.GetComponentInChildren<PainelDeFicha>(true);
            if (existente != null) return existente;

            var go = new GameObject(NomeDoObjeto, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(go, "Montar Painel de Ficha");
            go.transform.SetParent(pai, false);

            // Coluna à direita da mochila. Âncora no canto direito para não brigar com o grid de
            // slots, que cresce a partir da esquerda.
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1f, 0f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 0.5f);
            rt.sizeDelta = new Vector2(260f, 0f);
            rt.anchoredPosition = new Vector2(-24f, 0f);

            var fundo = go.AddComponent<Image>();
            fundo.color = new Color(0.05f, 0.04f, 0.03f, 0.72f);

            var textoGO = new GameObject("Corpo", typeof(RectTransform));
            textoGO.transform.SetParent(go.transform, false);
            var trt = textoGO.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = new Vector2(12f, 12f);
            trt.offsetMax = new Vector2(-12f, -12f);

            var texto = textoGO.AddComponent<Text>();
            texto.font = ObterFontePadrao();
            texto.fontSize = 39;
            texto.color = new Color(0.93f, 0.90f, 0.75f);
            texto.alignment = TextAnchor.UpperLeft;
            texto.horizontalOverflow = HorizontalWrapMode.Wrap;
            texto.verticalOverflow = VerticalWrapMode.Overflow;
            texto.text = "—";

            return go.AddComponent<PainelDeFicha>();
        }

        /// <summary>
        /// A <c>VitalidadeBridge</c> de Damião. O critério é a tag "Player" — o mesmo que a
        /// própria bridge usa para decidir se assina os efeitos passivos.
        /// </summary>
        private static VitalidadeBridge AcharVitalidadeDoJogador()
        {
            foreach (var v in Object.FindObjectsByType<VitalidadeBridge>(
                         FindObjectsInactive.Include))
            {
                if (v.CompareTag("Player")) return v;
            }
            return null;
        }

        /// <summary>
        /// Fonte embutida da Unity 6. O nome antigo (<c>Arial.ttf</c>) foi removido e
        /// <b>lança</b> ArgumentException; ver <c>FonteBuiltinTests</c>.
        /// </summary>
        private static Font ObterFontePadrao()
        {
            try
            {
                return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[BuildPainelDeFicha] Fonte built-in indisponível: {e.Message}");
                return null;
            }
        }
    }
}
