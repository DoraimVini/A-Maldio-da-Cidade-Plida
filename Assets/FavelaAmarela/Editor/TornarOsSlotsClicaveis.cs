using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using FavelaAmarela.Runtime.UI;

namespace FavelaAmarela.EditorTools
{
    /// <summary>
    /// Põe um <see cref="Button"/> em cada casa do inventário e o liga ao
    /// <see cref="PainelDeInventario"/>.
    ///
    /// <para><b>O que isto conserta (2026-09-02).</b> O Vini relatou <i>"não dá para mexer nos
    /// itens dentro do inventário"</i>. Medido: as três camadas estavam faltando ao mesmo
    /// tempo — o prefab tinha <c>Slot_0..11</c> e <c>Corpo_0..6</c> como RectTransform +
    /// CanvasGroup + CanvasRenderer + <c>Image</c>, <b>sem <c>Button</c> e sem
    /// <c>EventTrigger</c></b>; o script não implementava nenhuma interface de ponteiro; e o
    /// modelo não tinha <c>Mover</c> nem <c>Descartar</c>.</para>
    ///
    /// <para>O <c>EventSystem</c> e o <c>GraphicRaycaster</c> sempre estiveram de pé — são eles
    /// que fazem os botões do menu responderem. O inventário não respondia porque <b>não havia
    /// nada para responder</b>.</para>
    ///
    /// <para><b>Deriva do campo <c>moldura</c></b>, que já está ligado às 19 casas, em vez de
    /// procurar por nome: existem três "Slot_1" no HUD, em painéis diferentes.</para>
    /// </summary>
    public static class TornarOsSlotsClicaveis
    {
        private const string Marcador = "[SlotsClicaveis]";
        private const string Hud = "Assets/FavelaAmarela/Resources/HUD_Gameplay.prefab";
        private const string Folha =
            "Assets/ThirdParty/DarkAgesUI/DarkAgesUi_v1.0/32x32-Tilesheet.png";

        [MenuItem("Tools/FavelaAmarela/UI: tornar os slots do inventário clicáveis")]
        public static void Executar()
        {
            var raiz = PrefabUtility.LoadPrefabContents(Hud);
            var resumo = new List<string>();

            try
            {
                var painel = raiz.GetComponentInChildren<PainelDeInventario>(true);
                if (painel == null)
                {
                    Debug.LogError($"{Marcador} PainelDeInventario não achado no HUD.");
                    return;
                }

                var realce = AssetDatabase.LoadAllAssetsAtPath(Folha).OfType<Sprite>()
                    .FirstOrDefault(s => s.name == "moldura_slot_cheia");

                var so = new SerializedObject(painel);
                int criados = 0, ligados = 0, total = 0;

                foreach (var nomeDoArray in new[] { "slotsDaMochila", "slotsDoCorpo" })
                {
                    var array = so.FindProperty(nomeDoArray);
                    if (array == null)
                    {
                        resumo.Add($"campo '{nomeDoArray}' não existe mais no PainelDeInventario");
                        continue;
                    }

                    for (int i = 0; i < array.arraySize; i++)
                    {
                        var entrada = array.GetArrayElementAtIndex(i);

                        var moldura = entrada.FindPropertyRelative("moldura")
                                             .objectReferenceValue as Image;
                        var campo = entrada.FindPropertyRelative("botao");

                        if (moldura == null || campo == null) continue;
                        total++;

                        var botao = moldura.GetComponent<Button>();

                        if (botao == null)
                        {
                            botao = moldura.gameObject.AddComponent<Button>();
                            criados++;
                        }

                        // O alvo gráfico é a própria moldura: é ela que o jogador vê e mira.
                        botao.targetGraphic = moldura;

                        // Sem sprite de realce a troca fica só na cor, que some por cima de arte
                        // escura. Com ele, passar o cursor acende a casa.
                        botao.transition = realce != null
                            ? Selectable.Transition.SpriteSwap
                            : Selectable.Transition.ColorTint;

                        if (realce != null)
                        {
                            var estado = botao.spriteState;
                            estado.highlightedSprite = realce;
                            estado.pressedSprite = realce;
                            botao.spriteState = estado;
                        }

                        // A Image PRECISA aceitar raycast, senão o clique atravessa a casa e o
                        // Button nunca dispara -- um botão que existe e não responde.
                        moldura.raycastTarget = true;

                        EditorUtility.SetDirty(botao);
                        EditorUtility.SetDirty(moldura);

                        if (campo.objectReferenceValue == botao) continue;

                        campo.objectReferenceValue = botao;
                        ligados++;
                    }
                }

                so.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(raiz, Hud, out bool gravou);

                resumo.Add(gravou
                    ? $"{total} casa(s): {criados} Button criado(s), {ligados} ligado(s) ao painel"
                    : "SaveAsPrefabAsset RECUSOU");
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
