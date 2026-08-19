using UnityEditor;
using UnityEngine;
using FavelaAmarela.Runtime.Combat;

namespace FavelaAmarela.EditorTools
{
    /// <summary>
    /// Utilitário de Editor (uso único): põe a <see cref="ResilienciaBridge"/> no
    /// <c>Player_Damiao.prefab</c>.
    ///
    /// <para><b>Por que a bridge existe (2026-08-18):</b> a Vitalidade sempre teve uma bridge no
    /// Damião, então tudo que fere a carne resolve o alvo com
    /// <c>GetComponentInParent&lt;VitalidadeBridge&gt;()</c>. A Resiliência <b>não tinha
    /// nenhuma</b> — e por isso 19 call-sites em 11 arquivos alcançavam
    /// <c>GameManager.Instance.Resiliencia</c>. Não era descuido de quem escreveu: era a única
    /// porta existente.</para>
    ///
    /// <para><b>Sem este componente no prefab, nada que fere a mente funciona</b> — Cone de Gelo,
    /// Coisa do Cemitério, grito do Byakhee, zonas de pressão psíquica e o Colapso do Rei em
    /// Amarelo. Todos resolvem a bridge pelo alvo; sem ela, o <c>null</c> faz a ação virar
    /// no-op silencioso. Mesmo modo de falha do Vigor, que só existia na Arena.</para>
    ///
    /// <para>O guarda <c>ResilienciaSemGlobalTests</c> confere o resultado.</para>
    /// </summary>
    public static class LigarResilienciaBridge
    {
        private const string CaminhoDoPrefab =
            "Assets/FavelaAmarela/Art/Characters/Damiao/Player_Damiao.prefab";

        [MenuItem("Tools/FavelaAmarela/Ligar Resiliência Bridge no Damião")]
        public static void Ligar()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CaminhoDoPrefab);
            if (prefab == null)
            {
                Debug.LogError($"[LigarResilienciaBridge] Prefab não encontrado: {CaminhoDoPrefab}");
                return;
            }

            var raiz = PrefabUtility.LoadPrefabContents(CaminhoDoPrefab);
            try
            {
                if (raiz.GetComponent<ResilienciaBridge>() != null)
                {
                    Debug.Log("[LigarResilienciaBridge] O prefab já tem ResilienciaBridge — intocado.");
                    return;
                }

                raiz.AddComponent<ResilienciaBridge>();
                PrefabUtility.SaveAsPrefabAsset(raiz, CaminhoDoPrefab);

                Debug.Log("[LigarResilienciaBridge] ResilienciaBridge acrescentada ao " +
                          "Player_Damiao.prefab. O GameLoopBootstrap injeta a ResilienciaMental " +
                          "nela no Awake.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(raiz);
            }
        }
    }
}
