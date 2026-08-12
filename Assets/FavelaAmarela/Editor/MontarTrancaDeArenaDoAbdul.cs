using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using FavelaAmarela.Runtime.Enemies;
using FavelaAmarela.Runtime.GameLoop;

namespace FavelaAmarela.EditorTools
{
    /// <summary>
    /// Ferramenta de Editor. Monta a <see cref="TrancaDeArena"/> da luta do Abdul: cria o
    /// objeto da tranca, aponta-o para o portal de saída que fica <b>dentro da arena</b>, e
    /// liga o campo <c>trancaDaArena</c> do Abdul a ela.
    ///
    /// <para><b>Contexto:</b> a Tumba tem dois portais de saída — um na entrada da dungeon e
    /// outro colocado à mão dentro da arena, para o jogador não ter que refazer o caminho
    /// depois da luta. O da arena precisa ficar inerte durante o combate: nenhum chefe do
    /// jogo pode ser abandonado antes do desfecho.</para>
    ///
    /// <para><b>Acha o portal por distância ao Abdul, não por nome.</b> O objeto se chama
    /// <c>"Saida_TumbaAlhazred (1)"</c> — sufixo automático da Unity ao duplicar, exatamente
    /// o tipo de identificador frágil que renomear quebraria em silêncio.</para>
    ///
    /// <para>Idempotente: reaproveita a tranca existente e só preenche o que estiver vazio.</para>
    /// </summary>
    public static class MontarTrancaDeArenaDoAbdul
    {
        private const string NomeDoObjeto = "TrancaDeArena_Abdul";

        [MenuItem("Tools/FavelaAmarela/Montar Tranca de Arena do Abdul")]
        public static void Executar()
        {
            var abdul = Object.FindAnyObjectByType<AbdulAlhazredAI>(FindObjectsInactive.Include);
            if (abdul == null)
            {
                Debug.LogError("[MontarTrancaDeArena] Nenhum AbdulAlhazredAI nesta cena.");
                return;
            }

            var portalDaArena = AcharPortalMaisProximo(abdul.transform.position);
            if (portalDaArena == null)
            {
                Debug.LogError("[MontarTrancaDeArena] Nenhum PortalDeCena nesta cena — " +
                               "não há saída para trancar.");
                return;
            }

            var colisor = portalDaArena.GetComponent<Collider2D>();
            if (colisor == null)
            {
                Debug.LogError($"[MontarTrancaDeArena] '{portalDaArena.name}' não tem Collider2D.",
                               portalDaArena);
                return;
            }

            float distancia = Vector3.Distance(abdul.transform.position, portalDaArena.transform.position);
            Debug.Log($"[MontarTrancaDeArena] Portal da arena identificado: '{portalDaArena.name}' " +
                      $"a {distancia:F1} unidades do Abdul.", portalDaArena);

            var tranca = ObterOuCriarTranca(portalDaArena.transform.position);
            LigarSaida(tranca, colisor);
            LigarNoAbdul(abdul, tranca);

            var cena = EditorSceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(cena);
            EditorSceneManager.SaveScene(cena);

            Debug.Log("[MontarTrancaDeArena] Pronto — a saída da arena fecha ao começar a luta " +
                      "e reabre ao resolvê-la.", tranca);
        }

        private static PortalDeCena AcharPortalMaisProximo(Vector3 origem)
        {
            var portais = Object.FindObjectsByType<PortalDeCena>(
                FindObjectsInactive.Include);

            PortalDeCena maisProximo = null;
            float menorDistancia = float.MaxValue;

            foreach (var portal in portais)
            {
                float d = Vector3.Distance(origem, portal.transform.position);
                if (d >= menorDistancia) continue;

                menorDistancia = d;
                maisProximo = portal;
            }

            return maisProximo;
        }

        private static TrancaDeArena ObterOuCriarTranca(Vector3 posicao)
        {
            var existente = Object.FindAnyObjectByType<TrancaDeArena>(FindObjectsInactive.Include);
            if (existente != null) return existente;

            var go = new GameObject(NomeDoObjeto);
            go.transform.position = posicao;
            Undo.RegisterCreatedObjectUndo(go, "Criar Tranca de Arena");

            return go.AddComponent<TrancaDeArena>();
        }

        private static void LigarSaida(TrancaDeArena tranca, Collider2D colisor)
        {
            var so = new SerializedObject(tranca);
            var saidas = so.FindProperty("saidas");

            for (int i = 0; i < saidas.arraySize; i++)
            {
                if (saidas.GetArrayElementAtIndex(i).objectReferenceValue == colisor)
                {
                    Debug.Log("[MontarTrancaDeArena] Saída já estava ligada.");
                    return;
                }
            }

            saidas.arraySize++;
            saidas.GetArrayElementAtIndex(saidas.arraySize - 1).objectReferenceValue = colisor;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(tranca);
        }

        private static void LigarNoAbdul(AbdulAlhazredAI abdul, TrancaDeArena tranca)
        {
            var so = new SerializedObject(abdul);
            var campo = so.FindProperty("trancaDaArena");

            if (campo == null)
            {
                Debug.LogError("[MontarTrancaDeArena] Campo 'trancaDaArena' não existe no AbdulAlhazredAI.");
                return;
            }

            if (campo.objectReferenceValue == tranca) return;

            campo.objectReferenceValue = tranca;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(abdul);
        }
    }
}

