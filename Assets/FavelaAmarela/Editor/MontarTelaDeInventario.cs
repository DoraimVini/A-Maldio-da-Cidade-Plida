using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using FavelaAmarela.Runtime.UI;

namespace FavelaAmarela.EditorTools
{
    /// <summary>
    /// Constrói as casas da tela de inventário e as liga ao <see cref="PainelDeInventario"/>.
    ///
    /// <para><b>O bug (2026-08-19, relatado pelo Vini):</b> "o botão TAB serve basicamente como
    /// um botão de pause, pois nada aparece na tela". Conferido no YAML das 4 cenas:
    /// <c>raizDoPainel</c> <b>está</b> atribuído — por isso TAB pausa e a raiz aparece — mas
    /// <c>slotsDaMochila</c> e <c>slotsDoCorpo</c> estão com <b>zero entradas</b>. O painel abre
    /// como um retângulo sem nenhuma casa desenhada. O componente existe, o código está certo, e
    /// ninguém preencheu os arrays: o modo de falha assinatura deste projeto.</para>
    ///
    /// <para><b>Números medidos, não estimados:</b> 12 casas de mochila
    /// (<c>MainInventory.DefaultCapacidadeSurvivalHorror</c>) e 6 de corpo (o array
    /// <c>anatomia</c> do <c>InventoryManager</c>: Arma, Elmo, Peitoral, Grevas, Amuleto,
    /// Anel).</para>
    ///
    /// <para>Idempotente: se as casas já existirem, reaproveita em vez de duplicar.</para>
    /// </summary>
    public static class MontarTelaDeInventario
    {
        private const int CasasDaMochila = 12;
        private const int CasasDoCorpo = 6;

        /// <summary>4 colunas × 3 linhas para a mochila — cabe em tela larga sem rolagem.</summary>
        private const int ColunasDaMochila = 4;

        private static readonly string[] Cenas =
        {
            "Assets/Scenes/Deserto_Hali.unity",
            "Assets/Scenes/Playtest_RuinasPalidas.unity",
            "Assets/Scenes/Santuario_Yhtill.unity",
            "Assets/Scenes/Cena_ArenaDeTestes.unity",
        };

        [MenuItem("Tools/FavelaAmarela/Montar tela de inventario")]
        public static void Executar()
        {
            var resumo = new List<string>();

            foreach (var caminho in Cenas)
            {
                if (!System.IO.File.Exists(caminho))
                {
                    resumo.Add($"{System.IO.Path.GetFileName(caminho)}: ausente");
                    continue;
                }

                var cena = EditorSceneManager.OpenScene(caminho, OpenSceneMode.Single);

                var painel = Object.FindAnyObjectByType<PainelDeInventario>(FindObjectsInactive.Include);
                if (painel == null)
                {
                    resumo.Add($"{System.IO.Path.GetFileNameWithoutExtension(caminho)}: sem PainelDeInventario");
                    continue;
                }

                int mochila = MontarGrade(painel, "slotsDaMochila", CasasDaMochila, comRotulo: false);
                int corpo = MontarGrade(painel, "slotsDoCorpo", CasasDoCorpo, comRotulo: true);

                EditorSceneManager.MarkSceneDirty(cena);
                EditorSceneManager.SaveScene(cena);

                resumo.Add($"{System.IO.Path.GetFileNameWithoutExtension(caminho)}: " +
                           $"{mochila} casa(s) de mochila, {corpo} de corpo");
            }

            AssetDatabase.SaveAssets();
            Debug.Log("[TelaDeInventario] Concluído:\n  " + string.Join("\n  ", resumo));
        }

        /// <summary>
        /// Garante o contêiner da grade, cria as casas que faltarem e preenche o array
        /// serializado do painel.
        /// </summary>
        private static int MontarGrade(PainelDeInventario painel, string campo, int quantidade,
                                       bool comRotulo)
        {
            var raiz = ObterRaizDoPainel(painel);
            if (raiz == null)
            {
                Debug.LogError("[TelaDeInventario] 'raizDoPainel' não está atribuído — sem onde " +
                               "pendurar as casas.", painel);
                return 0;
            }

            string nomeDoGrupo = campo == "slotsDaMochila" ? "Grade_Mochila" : "Grade_Corpo";

            var grupo = raiz.transform.Find(nomeDoGrupo) as RectTransform;
            if (grupo == null) grupo = CriarGrupo(raiz.transform, nomeDoGrupo, campo == "slotsDaMochila");

            var visuais = new List<Object>();

            for (int i = 0; i < quantidade; i++)
            {
                string nomeDaCasa = $"Slot_{(comRotulo ? "Corpo_" : "")}{i}";
                var casa = grupo.Find(nomeDaCasa) as RectTransform ?? CriarCasa(grupo, nomeDaCasa, comRotulo);
                visuais.Add(casa.gameObject);
            }

            PreencherArray(painel, campo, quantidade, grupo, comRotulo);
            return quantidade;
        }

        private static GameObject ObterRaizDoPainel(PainelDeInventario painel)
        {
            var so = new SerializedObject(painel);
            return so.FindProperty("raizDoPainel")?.objectReferenceValue as GameObject;
        }

        /// <summary>
        /// Contêiner com <c>GridLayoutGroup</c>: a Unity posiciona as casas sozinha, então não há
        /// coordenada chumbada aqui que quebre se alguém mudar a quantidade de slots.
        /// </summary>
        private static RectTransform CriarGrupo(Transform pai, string nome, bool ehMochila)
        {
            var go = new GameObject(nome, typeof(RectTransform), typeof(GridLayoutGroup));
            go.transform.SetParent(pai, false);

            var rt = (RectTransform)go.transform;

            // Mochila à esquerda, corpo à direita — leitura natural: o que você carrega, e o que
            // está vestindo.
            rt.anchorMin = ehMochila ? new Vector2(0.08f, 0.15f) : new Vector2(0.60f, 0.15f);
            rt.anchorMax = ehMochila ? new Vector2(0.52f, 0.85f) : new Vector2(0.92f, 0.85f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var grade = go.GetComponent<GridLayoutGroup>();
            grade.cellSize = new Vector2(96f, 96f);
            grade.spacing = new Vector2(12f, 12f);
            grade.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grade.constraintCount = ehMochila ? ColunasDaMochila : 2;
            grade.childAlignment = TextAnchor.UpperCenter;

            return rt;
        }

        /// <summary>
        /// Uma casa: <c>CanvasGroup</c> (o painel usa o alpha para marcar vazio/cheio), moldura,
        /// ícone, quantidade e — só no corpo — o rótulo da parte do corpo.
        /// </summary>
        private static RectTransform CriarCasa(Transform pai, string nome, bool comRotulo)
        {
            var go = new GameObject(nome, typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            go.transform.SetParent(pai, false);

            // A moldura de slot é a única do tilesheet desenhada para casa quadrada — é
            // exatamente o caso dela, ao contrário dos botões do menu.
            PaletaDaInterface.AplicarSlot(go.GetComponent<Image>());

            var icone = new GameObject("Icone", typeof(RectTransform), typeof(Image));
            icone.transform.SetParent(go.transform, false);
            var rtIcone = (RectTransform)icone.transform;
            rtIcone.anchorMin = new Vector2(0.15f, 0.15f);
            rtIcone.anchorMax = new Vector2(0.85f, 0.85f);
            rtIcone.offsetMin = Vector2.zero;
            rtIcone.offsetMax = Vector2.zero;
            icone.GetComponent<Image>().preserveAspect = true;
            icone.GetComponent<Image>().enabled = false;   // o painel liga quando há item

            var qtd = CriarTexto(go.transform, "Quantidade", TextAnchor.LowerRight, 22);
            var rtQtd = (RectTransform)qtd.transform;
            rtQtd.anchorMin = new Vector2(0.4f, 0f);
            rtQtd.anchorMax = new Vector2(0.95f, 0.4f);
            rtQtd.offsetMin = Vector2.zero;
            rtQtd.offsetMax = Vector2.zero;
            qtd.GetComponent<Text>().enabled = false;

            if (comRotulo)
            {
                var rotulo = CriarTexto(go.transform, "Rotulo", TextAnchor.UpperCenter, 18);
                var rtRotulo = (RectTransform)rotulo.transform;
                rtRotulo.anchorMin = new Vector2(0f, 0.98f);
                rtRotulo.anchorMax = new Vector2(1f, 1.28f);
                rtRotulo.offsetMin = Vector2.zero;
                rtRotulo.offsetMax = Vector2.zero;
                rotulo.GetComponent<Text>().color = PaletaDaInterface.TintaFraca;
            }

            return (RectTransform)go.transform;
        }

        private static GameObject CriarTexto(Transform pai, string nome, TextAnchor alinhamento,
                                             int tamanho)
        {
            var go = new GameObject(nome, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(pai, false);

            var txt = go.GetComponent<Text>();
            txt.font = PaletaDaInterface.Fonte;
            txt.fontSize = tamanho;
            txt.alignment = alinhamento;
            txt.color = PaletaDaInterface.Tinta;
            txt.horizontalOverflow = HorizontalWrapMode.Overflow;
            txt.verticalOverflow = VerticalWrapMode.Overflow;

            return go;
        }

        /// <summary>
        /// Preenche o array de <c>SlotVisual</c> por <see cref="SerializedProperty"/>: a classe é
        /// aninhada e privada no painel, então não dá para montá-la por código direto.
        /// </summary>
        private static void PreencherArray(PainelDeInventario painel, string campo, int quantidade,
                                           RectTransform grupo, bool comRotulo)
        {
            var so = new SerializedObject(painel);
            var prop = so.FindProperty(campo);
            prop.arraySize = quantidade;

            for (int i = 0; i < quantidade; i++)
            {
                string nomeDaCasa = $"Slot_{(comRotulo ? "Corpo_" : "")}{i}";
                var casa = grupo.Find(nomeDaCasa);
                if (casa == null) continue;

                var elemento = prop.GetArrayElementAtIndex(i);
                elemento.FindPropertyRelative("grupo").objectReferenceValue =
                    casa.GetComponent<CanvasGroup>();
                elemento.FindPropertyRelative("icone").objectReferenceValue =
                    casa.Find("Icone")?.GetComponent<Image>();
                elemento.FindPropertyRelative("quantidade").objectReferenceValue =
                    casa.Find("Quantidade")?.GetComponent<Text>();

                var rotulo = elemento.FindPropertyRelative("rotulo");
                rotulo.objectReferenceValue = comRotulo
                    ? casa.Find("Rotulo")?.GetComponent<Text>()
                    : null;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(painel);
        }
    }
}
