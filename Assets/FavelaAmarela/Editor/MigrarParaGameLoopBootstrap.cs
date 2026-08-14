using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace FavelaAmarela.EditorTools
{
    /// <summary>
    /// Utilitário de Editor (uso único, Fase 2 da refatoração de managers): acrescenta os
    /// componentes focados ao GameObject que hoje carrega o <c>GameManager</c> e <b>copia para
    /// eles os valores serializados</b> que estavam na casca.
    ///
    /// <para><b>Por que uma ferramenta, e não edição de YAML à mão:</b> acrescentar um
    /// <c>MonoBehaviour</c> a uma cena exige um bloco novo com <c>fileID</c> inédito <b>e</b> a
    /// entrada correspondente em <c>m_Component</c> do GameObject. Errar um dos dois corrompe a
    /// cena de um jeito que só aparece ao abri-la. A Unity gera os dois corretamente; este script
    /// só pede.</para>
    ///
    /// <para><b>Ordem que importa:</b> roda <b>enquanto</b> o <c>GameManager</c> ainda declara
    /// <c>telaPause</c> e <c>sequenciaColapso</c>. A Unity descarta o valor serializado de um
    /// campo removido da classe — migrar depois de limpar a casca perderia as referências.</para>
    ///
    /// <para>Idempotente: componente que já existe não é duplicado, e campo já preenchido no
    /// destino não é sobrescrito.</para>
    ///
    /// <para><b>⚠ FERRAMENTA GASTA (rodou em 2026-08-14, 4/4 cenas).</b> Os campos de origem já
    /// saíram do <c>GameManager</c>, então <see cref="CopiarCampo"/> não os acha mais e uma nova
    /// execução só registra erro — os valores <b>não</b> se perderam, estão nos componentes de
    /// destino. Fica no repositório pelo mesmo motivo que <c>WireConfigAssets</c> e
    /// <c>WireStormTriggers</c>: é o registro de <b>como</b> a migração foi feita. Para acrescentar
    /// o bootstrap a uma cena nova, adicione os componentes pelo Inspector — não há mais o que
    /// copiar.</para>
    /// </summary>
    public static class MigrarParaGameLoopBootstrap
    {
        private static readonly string[] Cenas =
        {
            "Assets/Scenes/Deserto_Hali.unity",
            "Assets/Scenes/Playtest_RuinasPalidas.unity",
            "Assets/Scenes/Santuario_Yhtill.unity",
            "Assets/Scenes/Cena_ArenaDeTestes.unity",
            // cena_1 fica de fora: legado abandonado, fora do Build Settings, com serialização
            // anterior à criação de sequenciaColapso. Ver BootstrapDeCenaTests.
        };

        [MenuItem("Tools/FavelaAmarela/Migrar para GameLoopBootstrap")]
        public static void Migrar()
        {
            int cenasTocadas = 0;

            foreach (var caminho in Cenas)
            {
                var cena = EditorSceneManager.OpenScene(caminho, OpenSceneMode.Single);

                var manager = Object.FindAnyObjectByType<Runtime.GameLoop.GameManager>(
                    FindObjectsInactive.Include);

                if (manager == null)
                {
                    Debug.LogError($"[Migrar] {caminho}: sem GameManager na cena — nada a migrar.");
                    continue;
                }

                var alvo = manager.gameObject;
                var origem = new SerializedObject(manager);

                // 1. Bootstrap: recebe a configuração numérica de Resiliência.
                var bootstrap = Garantir<Runtime.GameLoop.GameLoopBootstrap>(alvo);
                CopiarCampo(origem, "maxResiliencia", bootstrap, "maxResiliencia");
                CopiarCampo(origem, "fracaoPanico", bootstrap, "fracaoPanico");

                // 2. Presenter: recebe a tela de pausa.
                var presenter = Garantir<Runtime.GameLoop.GameStatePresenter>(alvo);
                CopiarCampo(origem, "telaPause", presenter, "telaPause");

                // 3. Controlador de morte: recebe a sequência de Colapso.
                var morte = Garantir<Runtime.GameLoop.PlayerDeathController>(alvo);
                CopiarCampo(origem, "sequenciaColapso", morte, "sequenciaColapso");

                // 4. Componentes sem campo serializado.
                Garantir<Runtime.GameLoop.CutsceneController>(alvo);
                Garantir<Runtime.GameLoop.PausaInputHandler>(alvo);
                Garantir<Player.CompanionManager>(alvo);

                EditorUtility.SetDirty(alvo);
                EditorSceneManager.MarkSceneDirty(cena);
                EditorSceneManager.SaveScene(cena);
                cenasTocadas++;

                Debug.Log($"[Migrar] {caminho}: componentes focados no GameObject " +
                          $"'{alvo.name}', valores copiados.");
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[Migrar] Concluído — {cenasTocadas}/{Cenas.Length} cenas migradas.");
        }

        /// <summary>Devolve o componente do tipo pedido, criando-o se ainda não existir.</summary>
        private static T Garantir<T>(GameObject alvo) where T : Component
        {
            var existente = alvo.GetComponent<T>();
            if (existente != null) return existente;

            return Undo.AddComponent<T>(alvo);
        }

        /// <summary>
        /// Copia um campo serializado da casca para o componente de destino. Não sobrescreve
        /// destino já preenchido — é o que torna a ferramenta segura de rodar duas vezes.
        /// </summary>
        private static void CopiarCampo(SerializedObject origem, string campoOrigem,
                                        Component destino, string campoDestino)
        {
            var pOrigem = origem.FindProperty(campoOrigem);
            if (pOrigem == null)
            {
                Debug.LogError($"[Migrar] Campo de origem '{campoOrigem}' não existe mais no " +
                               "GameManager. Se ele já foi removido, os valores de cena se " +
                               "perderam — restaure o campo e rode de novo.");
                return;
            }

            var destinoSO = new SerializedObject(destino);
            var pDestino = destinoSO.FindProperty(campoDestino);
            if (pDestino == null)
            {
                Debug.LogError($"[Migrar] Campo de destino '{campoDestino}' não existe em " +
                               $"{destino.GetType().Name}.");
                return;
            }

            if (JaPreenchido(pDestino)) return;

            switch (pOrigem.propertyType)
            {
                case SerializedPropertyType.Float:
                    pDestino.floatValue = pOrigem.floatValue;
                    break;
                case SerializedPropertyType.ObjectReference:
                    pDestino.objectReferenceValue = pOrigem.objectReferenceValue;
                    break;
                default:
                    Debug.LogError($"[Migrar] Tipo não tratado em '{campoOrigem}': " +
                                   $"{pOrigem.propertyType}.");
                    return;
            }

            destinoSO.ApplyModifiedPropertiesWithoutUndo();
        }

        private static bool JaPreenchido(SerializedProperty p)
        {
            return p.propertyType == SerializedPropertyType.ObjectReference
                ? p.objectReferenceValue != null
                : false; // campo numérico sempre tem valor: copiar é o comportamento certo
        }
    }
}
