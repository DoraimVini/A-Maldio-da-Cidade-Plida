using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using FavelaAmarela.Runtime.UI;

namespace FavelaAmarela.EditorTools
{
    /// <summary>
    /// Põe o texto de diálogo do <c>HUD_Gameplay</c> no padrão — <b>escrevendo pela Unity</b>,
    /// e não no arquivo.
    ///
    /// <para><b>Por que uma ferramenta só para isto (2026-08-29).</b> O texto do
    /// <c>TutorialHintUI</c> dentro do HUD estava com <c>máximo 60</c> contra o padrão
    /// <b>44</b> — é a caixa de que o Vini reclamou (<i>"as letras da Cassilda grandes demais e
    /// não cabem na caixa"</i>). Consertá-lo editando o YAML funcionou <b>três vezes</b>, e
    /// <b>reverteu as três</b>.</para>
    ///
    /// <para><b>A causa.</b> A <c>AssetDatabase</c> serve um artefato em cache que, para este
    /// prefab, ainda diz 60. Qualquer ferramenta que abra o prefab —
    /// <c>EditPrefabContentsScope</c>, <c>LoadPrefabContents</c> — lê o <b>cache</b>, não o
    /// arquivo; ao salvar, grava o valor velho por cima da correção. Aconteceu ao restaurar os
    /// sprites das barras e de novo ao ligar o botão de Opções: duas ferramentas que não têm
    /// nada a ver com tipografia desfizeram a tipografia.</para>
    ///
    /// <para><b>Por isso a correção passa pela Unity.</b> Quando é ela quem escreve, o cache
    /// passa a valer 44 também — e a próxima ferramenta que abrir o prefab reserializa o valor
    /// certo. É o que quebra o ciclo, em vez de vencer mais uma rodada dele.</para>
    ///
    /// <para><c>TipografiaDeDialogoTests</c> guarda o resultado, lendo o arquivo no disco.</para>
    /// </summary>
    public static class PadronizarTextoDoHud
    {
        private const string Marcador = "[TextoDoHud]";
        private const string Hud = "Assets/FavelaAmarela/Resources/HUD_Gameplay.prefab";

        [MenuItem("Tools/FavelaAmarela/UI: padronizar o texto do HUD (pela Unity)")]
        public static void Executar()
        {
            var resumo = new List<string>();

            // LoadPrefabContents + SaveAsPrefabAsset, e NÃO EditPrefabContentsScope: o escopo
            // relatou a mudança e não a gravou -- o par explícito devolve um booleano que diz
            // se a gravação aconteceu, e é isso que se pode afirmar.
            var raiz = PrefabUtility.LoadPrefabContents(Hud);
            bool gravou;

            try
            {
                foreach (var hint in raiz.GetComponentsInChildren<TutorialHintUI>(
                             includeInactive: true))
                {
                    foreach (var texto in hint.GetComponentsInChildren<Text>(includeInactive: true))
                    {
                        string antes = $"melhor ajuste {texto.resizeTextForBestFit}, " +
                                       $"{texto.resizeTextMinSize}–{texto.resizeTextMaxSize}";

                        texto.resizeTextForBestFit = true;
                        texto.resizeTextMinSize = PadraoDeTextoDeDialogo.TamanhoMinimo;
                        texto.resizeTextMaxSize = PadraoDeTextoDeDialogo.TamanhoMaximo;

                        EditorUtility.SetDirty(texto);

                        // RELÊ do componente, em vez de imprimir a constante que acabei de
                        // pedir. A primeira versão desta ferramenta imprimia o alvo e não o
                        // resultado -- ou seja, relatava sucesso mesmo quando a atribuição não
                        // pegava. É o Corolário 4 do COMMANDMENT aplicado a um campo.
                        string depois = $"melhor ajuste {texto.resizeTextForBestFit}, " +
                                        $"{texto.resizeTextMinSize}–{texto.resizeTextMaxSize}";

                        resumo.Add($"{hint.name}/{texto.name}: {antes} → {depois}");
                    }
                }

                PrefabUtility.SaveAsPrefabAsset(raiz, Hud, out gravou);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(raiz);
            }

            if (!gravou)
            {
                Debug.LogError($"{Marcador} SaveAsPrefabAsset RECUSOU.");
                return;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (resumo.Count == 0)
            {
                Debug.LogError($"{Marcador} Nenhum TutorialHintUI com Text no HUD — a ferramenta " +
                               "não tem o que padronizar, e o guarda vai continuar vermelho.");
                return;
            }

            // Conferir NO DISCO: o log desta ferramenta descreve o que ela mandou a Unity fazer,
            // e é exatamente esse relato que já enganou três vezes.
            string yaml = System.IO.File.ReadAllText(Hud);
            bool ficou = yaml.Contains($"m_MaxSize: {PadraoDeTextoDeDialogo.TamanhoMaximo}");

            string quebra = System.Environment.NewLine + "  ";
            string texto2 = $"{Marcador} " + (ficou ? "Concluído" : "GRAVOU ERRADO") +
                            ":" + quebra + string.Join(quebra, resumo) + quebra +
                            $"Conferido no disco: máximo {PadraoDeTextoDeDialogo.TamanhoMaximo} " +
                            (ficou
                                ? "presente."
                                : "AUSENTE. O valor FOI para 44 em memória (veja acima) e o " +
                                  "SaveAsPrefabAsset não o levou ao disco: o artefato em cache " +
                                  "da Library vence a gravação deste campo. CONSERTO: apague " +
                                  "Library/ e reabra o projeto, ou edite o m_MaxSize no YAML " +
                                  "DEPOIS de rodar toda ferramenta que abra este prefab.");

            if (ficou) Debug.Log(texto2);
            else Debug.LogError(texto2);
        }
    }
}
