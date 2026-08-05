using UnityEditor;
using UnityEngine;
using FavelaAmarela.Runtime.Combat;
using FavelaAmarela.Runtime.Enemies;

namespace FavelaAmarela.EditorTools
{
    /// <summary>
    /// Ferramenta de Editor. Garante que Yug-Neth carregue o marcador <see cref="Aliado"/>,
    /// que é o que impede o golpe de Damião de atingi-lo (ver <c>MaoFisicaBridge</c>).
    ///
    /// <para><b>Por que existe:</b> o bug foi achado em playtest — o golpe do jogador feria
    /// o companheiro obrigatório, inclusive durante a luta do Abdul. O código já foi
    /// corrigido, mas o prefab que já existia no disco precisa ganhar o componente novo;
    /// <c>[RequireComponent]</c> só age em objetos criados depois.</para>
    ///
    /// <para>Idempotente: rodar de novo não duplica nada.</para>
    /// </summary>
    public static class ProtegerYugNethDoJogador
    {
        private const string CaminhoPrefab =
            "Assets/FavelaAmarela/Art/Characters/MiGo/YugNeth.prefab";

        [MenuItem("Tools/FavelaAmarela/Proteger Yug-Neth do golpe do jogador")]
        public static void Executar()
        {
            int alterados = 0;

            alterados += CorrigirPrefab() ? 1 : 0;
            alterados += CorrigirInstanciasDaCena();

            if (alterados == 0)
                Debug.Log("[ProtegerYugNeth] Nada a fazer — Yug-Neth já está marcado como Aliado.");
            else
                Debug.Log($"[ProtegerYugNeth] Pronto: {alterados} objeto(s) marcados como Aliado. " +
                          "O golpe de Damião não o atinge mais.");
        }

        /// <remarks>
        /// <b>Grava sempre, sem checar antes.</b> Parece redundante, mas não é: com
        /// <c>[RequireComponent(typeof(Aliado))]</c> no <c>YugNethAI</c>, a Unity adiciona o
        /// componente <b>em memória</b> assim que carrega o prefab — então um
        /// <c>GetComponent&lt;Aliado&gt;() != null</c> retorna <c>true</c> e faz a ferramenta
        /// pular o salvamento, deixando o arquivo em disco sem o componente. Foi exatamente
        /// isso que aconteceu na primeira execução: log de "nada a fazer" e YAML intacto.
        /// Salvar incondicionalmente persiste o que a Unity só tinha na memória.
        /// </remarks>
        private static bool CorrigirPrefab()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(CaminhoPrefab) == null)
            {
                Debug.LogWarning($"[ProtegerYugNeth] Prefab não encontrado em '{CaminhoPrefab}'.");
                return false;
            }

            var raiz = PrefabUtility.LoadPrefabContents(CaminhoPrefab);
            if (raiz.GetComponent<Aliado>() == null) raiz.AddComponent<Aliado>();

            PrefabUtility.SaveAsPrefabAsset(raiz, CaminhoPrefab);
            PrefabUtility.UnloadPrefabContents(raiz);
            AssetDatabase.SaveAssets();

            Debug.Log("[ProtegerYugNeth] Marcador Aliado gravado no prefab (em disco).");
            return true;
        }

        private static int CorrigirInstanciasDaCena()
        {
            // Instâncias já colocadas em cena antes do marcador existir não herdam o
            // componente automaticamente se estiverem desconectadas do prefab.
            var encontrados = Object.FindObjectsByType<YugNethAI>(
                FindObjectsInactive.Include);

            int corrigidos = 0;
            foreach (var yugNeth in encontrados)
            {
                if (yugNeth.GetComponent<Aliado>() != null) continue;

                Undo.AddComponent<Aliado>(yugNeth.gameObject);
                EditorUtility.SetDirty(yugNeth.gameObject);
                corrigidos++;
            }

            return corrigidos;
        }
    }
}

