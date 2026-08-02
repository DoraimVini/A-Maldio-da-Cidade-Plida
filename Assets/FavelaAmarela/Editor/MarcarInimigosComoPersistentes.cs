using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using FavelaAmarela.Runtime.Enemies;
using FavelaAmarela.Runtime.Persistencia;

namespace FavelaAmarela.EditorTools
{
    /// <summary>
    /// Ferramenta de Editor. Dá a cada inimigo da cena um <see cref="ObjetoPersistente"/>
    /// com chave imutável, para que <b>abates sejam lembrados</b> entre trocas de cena.
    ///
    /// <para><b>Bug que motivou (playtest 2026-07-31):</b> os Cultistas ressuscitavam ao
    /// sair da Tumba e voltar — a cena era recriada do zero e nada registrava quem já tinha
    /// sido morto, então o jogador reencontrava tudo o que já havia limpado.</para>
    ///
    /// <para>Idempotente e <b>nunca regenera uma chave existente</b>: trocar o GUID de um
    /// inimigo já salvo o ressuscitaria, que é exatamente o bug que a chave existe para
    /// evitar.</para>
    /// </summary>
    public static class MarcarInimigosComoPersistentes
    {
        [MenuItem("Tools/FavelaAmarela/Marcar inimigos como persistentes")]
        public static void Executar()
        {
            // Sem o parâmetro de ordenação: a sobrecarga com FindObjectsSortMode está
            // obsoleta na Unity 6.4, e aqui a ordem não importa.
            var cultistas = Object.FindObjectsByType<CultistaAI>(FindObjectsInactive.Include);

            if (cultistas.Length == 0)
            {
                Debug.Log("[MarcarInimigos] Nenhum Cultista nesta cena.");
                return;
            }

            int componentesAdicionados = 0;
            int semChave = 0;

            foreach (var cultista in cultistas)
            {
                var persistencia = cultista.GetComponent<ObjetoPersistente>();
                if (persistencia == null)
                {
                    // Nota: `AddComponent` no Editor dispara o `Reset()` do componente, que
                    // já carimba a chave. Por isso não dá para contar chaves pelo retorno do
                    // `GarantirChave()` abaixo — ele quase sempre devolve false aqui.
                    persistencia = Undo.AddComponent<ObjetoPersistente>(cultista.gameObject);
                    componentesAdicionados++;
                }

                persistencia.GarantirChave(); // rede de segurança para quem veio sem Reset
                EditorUtility.SetDirty(persistencia);

                if (!persistencia.TemChave) semChave++;
            }

            var cena = EditorSceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(cena);
            EditorSceneManager.SaveScene(cena);

            Debug.Log($"[MarcarInimigos] {cultistas.Length} Cultista(s) na cena '{cena.name}': " +
                      $"{componentesAdicionados} componente(s) adicionados, " +
                      $"{cultistas.Length - semChave} com chave válida.");

            if (semChave > 0)
                Debug.LogError($"[MarcarInimigos] {semChave} Cultista(s) ficaram SEM chave — " +
                               "o abate deles não será lembrado.");
        }
    }
}
