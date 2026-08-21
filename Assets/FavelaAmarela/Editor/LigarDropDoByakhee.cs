using UnityEditor;
using UnityEngine;
using FavelaAmarela.Inventario;
using FavelaAmarela.Runtime.Itens;

namespace FavelaAmarela.EditorTools
{
    /// <summary>
    /// Põe o <see cref="DropAoAbater"/> no <c>Byakhee.prefab</c>, apontando para
    /// <c>Drop_Byakhee</c>.
    ///
    /// <para><b>O buraco que isto fecha:</b> o Anel do Sinal Amarelo é uma das três relíquias
    /// que o Rei em Amarelo exige, e a própria descrição dele diz de onde vem — "gravação sacra
    /// <i>arrancada do Byakhee</i>". A tabela <c>Drop_Byakhee</c> estava autorada e correta
    /// (garantido, chance 1, nível mínimo 1), mas <b>nenhuma cena e nenhum prefab a
    /// referenciava</b>: o <c>DropAoAbater</c> só existia no <c>Cultista.prefab</c>. O Byakhee
    /// morria e não largava nada, e o rito final ficava impossível de completar sem o Carcosa
    /// Debugger. É o modo de falha assinatura deste projeto — o asset existe, o componente
    /// existe, e ninguém ligou os dois.</para>
    ///
    /// <para><b>O que isto ainda não resolve:</b> o Byakhee não está em cena nenhuma — falta a
    /// arena dos Portões (roadmap, item 9). Com esta ligação, no dia em que a arena existir o
    /// Anel cai sozinho; sem ela, o espólio continua inalcançável em jogo normal.</para>
    ///
    /// <para>Idempotente: reusa o componente se já estiver lá.</para>
    /// </summary>
    public static class LigarDropDoByakhee
    {
        private const string PrefabByakhee = "Assets/FavelaAmarela/Art/Enemies/Byakhee.prefab";
        private const string TabelaDropByakhee = "Assets/FavelaAmarela/Config/Drops/Drop_Byakhee.asset";

        [MenuItem("Tools/FavelaAmarela/Ligar espólio do Byakhee")]
        public static void Executar()
        {
            var tabela = AssetDatabase.LoadAssetAtPath<TabelaDeDrop>(TabelaDropByakhee);
            if (tabela == null)
            {
                Debug.LogError($"[Byakhee] Tabela ausente: {TabelaDropByakhee}");
                return;
            }

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabByakhee);
            if (prefab == null)
            {
                Debug.LogError($"[Byakhee] Prefab ausente: {PrefabByakhee}");
                return;
            }

            var raiz = PrefabUtility.LoadPrefabContents(PrefabByakhee);
            try
            {
                // Unity devolve um fake-null de GetComponent, que o operador ?? NÃO detecta —
                // 'GetComponent() ?? AddComponent()' já causou MissingComponentException neste
                // projeto. Comparação explícita com null é a forma que funciona.
                var drop = raiz.GetComponent<DropAoAbater>();
                if (drop == null) drop = raiz.AddComponent<DropAoAbater>();

                var so = new SerializedObject(drop);
                so.FindProperty("tabela").objectReferenceValue = tabela;

                // Nulo de propósito: o DropAoAbater monta um coletável mínimo quando não há
                // prefab, que é exatamente o que o Cultista já faz.
                so.FindProperty("prefabColetavel").objectReferenceValue = null;
                so.FindProperty("raioDeEspalhamento").floatValue = 0.4f;
                so.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(raiz, PrefabByakhee, out bool salvou);
                if (!salvou)
                {
                    Debug.LogError($"[Byakhee] SaveAsPrefabAsset recusou salvar {PrefabByakhee}.");
                    return;
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(raiz);
            }

            AssetDatabase.Refresh();

            // Confere no ASSET recarregado, não na cópia em memória que acabei de editar: é a
            // diferença entre "a API aceitou" e "está no disco".
            var conferido = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabByakhee)
                                         .GetComponent<DropAoAbater>();

            if (conferido == null)
            {
                Debug.LogError("[Byakhee] O DropAoAbater não sobreviveu ao salvamento.");
                return;
            }

            Debug.Log("[Byakhee] Espólio ligado: DropAoAbater → Drop_Byakhee (Anel do Sinal " +
                      "Amarelo garantido). Falta a arena dos Portões para ele ser alcançável.");
        }
    }
}
