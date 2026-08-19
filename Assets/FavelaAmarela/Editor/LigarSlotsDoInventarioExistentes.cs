using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using FavelaAmarela.Runtime.UI;

namespace FavelaAmarela.EditorTools
{
    /// <summary>
    /// Liga o <see cref="PainelDeInventario"/> às casas que <b>já existiam</b> na cena, e apaga
    /// as grades duplicadas que uma versão anterior desta sessão criou por engano.
    ///
    /// <para><b>O erro que isto corrige.</b> Ao investigar "TAB não mostra nada", encontrei os
    /// arrays <c>slotsDaMochila</c>/<c>slotsDoCorpo</c> vazios e concluí que as casas não
    /// existiam — então construí <c>Grade_Mochila</c> e <c>Grade_Corpo</c> do zero. Mas a
    /// <c>Janela</c> já tinha <c>Mochila/Slot_0..11</c> e <c>Corpo/Corpo_0..6</c>, diagramados
    /// com âncoras explícitas. O que faltava era só <b>preencher os arrays</b>. O resultado
    /// foram dois inventários sobrepostos na tela.</para>
    ///
    /// <para><b>E a minha grade estava errada, não só duplicada:</b> criei 6 casas de corpo
    /// lendo o array <c>anatomia</c> do <c>InventoryManager</c> — que tem <b>7</b>. A sétima é
    /// <c>MaoSecundaria</c>. As casas originais (<c>Corpo_0..6</c>) já eram 7 e batiam com a
    /// anatomia.</para>
    ///
    /// <para>Idempotente: rodar de novo religa os mesmos objetos e não recria nada.</para>
    /// </summary>
    public static class LigarSlotsDoInventarioExistentes
    {
        private static readonly string[] Cenas =
        {
            "Assets/Scenes/Deserto_Hali.unity",
            "Assets/Scenes/Playtest_RuinasPalidas.unity",
            "Assets/Scenes/Santuario_Yhtill.unity",
            "Assets/Scenes/Cena_ArenaDeTestes.unity",
        };

        /// <summary>Nomes das grades que eu criei por engano e que precisam sair.</summary>
        private static readonly string[] Duplicatas = { "Grade_Mochila", "Grade_Corpo" };

        [MenuItem("Tools/FavelaAmarela/Ligar slots do inventario existentes")]
        public static void Executar()
        {
            var resumo = new List<string>();

            foreach (var caminho in Cenas)
            {
                if (!System.IO.File.Exists(caminho)) { resumo.Add($"{caminho}: ausente"); continue; }

                var cena = EditorSceneManager.OpenScene(caminho, OpenSceneMode.Single);
                string nomeCena = System.IO.Path.GetFileNameWithoutExtension(caminho);

                int removidas = RemoverDuplicatas();
                string ligacao = LigarPainel();

                EditorSceneManager.MarkSceneDirty(cena);
                EditorSceneManager.SaveScene(cena);

                resumo.Add($"{nomeCena}: {removidas} grade(s) duplicada(s) removida(s); {ligacao}");
            }

            AssetDatabase.SaveAssets();
            Debug.Log("[SlotsDoInventario] Concluído:\n  " + string.Join("\n  ", resumo));
        }

        private static int RemoverDuplicatas()
        {
            int n = 0;

            // FindObjectsInactive.Include é obrigatório: a Janela vive desligada até o TAB,
            // então tudo que está dentro dela é inativo em edição.
            foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,
                                                                  FindObjectsSortMode.None))
            {
                if (t == null) continue;
                if (!Duplicatas.Contains(t.name)) continue;

                Object.DestroyImmediate(t.gameObject);
                n++;
            }

            return n;
        }

        private static string LigarPainel()
        {
            var painel = Object.FindAnyObjectByType<PainelDeInventario>(FindObjectsInactive.Include);
            if (painel == null) return "sem PainelDeInventario";

            var so = new SerializedObject(painel);

            var raiz = so.FindProperty("raizDoPainel").objectReferenceValue as GameObject;
            if (raiz == null) return "raizDoPainel vazio";

            // As casas originais: Mochila/Slot_N e Corpo/Corpo_N, na ordem numérica do nome.
            var mochila = AcharFilho(raiz.transform, "Mochila");
            var corpo = AcharFilho(raiz.transform, "Corpo");

            if (mochila == null || corpo == null)
                return $"Mochila={(mochila == null ? "ausente" : "ok")} Corpo={(corpo == null ? "ausente" : "ok")}";

            var casasMochila = OrdenarPorIndice(mochila, "Slot_");
            var casasCorpo = OrdenarPorIndice(corpo, "Corpo_");

            Preencher(so.FindProperty("slotsDaMochila"), casasMochila);
            Preencher(so.FindProperty("slotsDoCorpo"), casasCorpo);

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(painel);

            return $"mochila={casasMochila.Count} corpo={casasCorpo.Count}";
        }

        private static Transform AcharFilho(Transform pai, string nome)
        {
            foreach (Transform f in pai) if (f.name == nome) return f;
            return null;
        }

        /// <summary>
        /// Ordena pelo <b>índice numérico</b> do nome, não alfabeticamente: ordenar por string
        /// colocaria <c>Slot_10</c> antes de <c>Slot_2</c> e embaralharia a mochila inteira.
        /// </summary>
        private static List<Transform> OrdenarPorIndice(Transform pai, string prefixo)
        {
            var lista = new List<(int ordem, Transform t)>();

            foreach (Transform f in pai)
            {
                if (!f.name.StartsWith(prefixo)) continue;
                if (!int.TryParse(f.name.Substring(prefixo.Length), out int n)) continue;
                lista.Add((n, f));
            }

            return lista.OrderBy(p => p.ordem).Select(p => p.t).ToList();
        }

        private static void Preencher(SerializedProperty array, List<Transform> casas)
        {
            array.arraySize = casas.Count;

            for (int i = 0; i < casas.Count; i++)
            {
                var casa = casas[i];
                var entrada = array.GetArrayElementAtIndex(i);

                // CanvasGroup é o que o PainelDeInventario liga/desliga por casa; as casas
                // originais não vinham com um, então acrescenta sob demanda.
                var grupo = casa.GetComponent<CanvasGroup>() ?? casa.gameObject.AddComponent<CanvasGroup>();

                entrada.FindPropertyRelative("grupo").objectReferenceValue = grupo;
                entrada.FindPropertyRelative("icone").objectReferenceValue =
                    AcharFilho(casa, "Icone")?.GetComponent<Image>();
                entrada.FindPropertyRelative("quantidade").objectReferenceValue =
                    AcharFilho(casa, "Quantidade")?.GetComponent<Text>();
                entrada.FindPropertyRelative("rotulo").objectReferenceValue =
                    AcharFilho(casa, "Rotulo")?.GetComponent<Text>();
            }
        }
    }
}
