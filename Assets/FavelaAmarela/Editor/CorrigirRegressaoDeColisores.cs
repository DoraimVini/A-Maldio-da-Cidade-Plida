using UnityEditor;
using UnityEngine;

namespace FavelaAmarela.EditorTools
{
    /// <summary>
    /// Corrige uma regressão que <c>RevisarColisores</c> introduziu em 2026-08-21: ele tratou
    /// <c>YugNeth.prefab</c> e <c>EsqueletoInvocado.prefab</c> como "humanos genéricos" e
    /// flattenou os dois para a pegada de combate uniforme (0,60 × 0,30), sobrescrevendo
    /// calibragem de colisor <b>já testada e derivada da arte real</b>
    /// (<c>ArteDosPlaceholdersTests.Escala_E_Colisor_PreservamOVolumeDeMundo</c>), sem eu ter
    /// checado se um guarda já protegia esses valores.
    ///
    /// <para>Este script é <b>de uso único</b>: restaura os dois prefabs ao tamanho local que a
    /// suíte já validava antes de hoje, e deve ser apagado depois de rodar — não é ferramenta
    /// permanente do projeto.</para>
    /// </summary>
    public static class CorrigirRegressaoDeColisores
    {
        [MenuItem("Tools/FavelaAmarela/[Uso único] Corrigir regressão de colisores")]
        public static void Executar()
        {
            Restaurar("Assets/FavelaAmarela/Art/Characters/MiGo/YugNeth.prefab",
                      new Vector2(1.2f, 1.2f));
            Restaurar("Assets/FavelaAmarela/Art/Enemies/EsqueletoInvocado.prefab",
                      new Vector2(0.832f, 1.088f));
        }

        private static void Restaurar(string caminho, Vector2 tamanhoLocal)
        {
            var raiz = PrefabUtility.LoadPrefabContents(caminho);
            if (raiz == null)
            {
                Debug.LogError($"[CorrigirRegressao] {caminho}: não carregou.");
                return;
            }

            try
            {
                var box = raiz.GetComponent<BoxCollider2D>();
                if (box == null)
                {
                    Debug.LogError($"[CorrigirRegressao] {caminho}: sem BoxCollider2D.");
                    return;
                }

                var antes = box.size;
                box.size = tamanhoLocal;

                PrefabUtility.SaveAsPrefabAsset(raiz, caminho, out bool salvou);

                Debug.Log(salvou
                    ? $"[CorrigirRegressao] {System.IO.Path.GetFileName(caminho)}: " +
                      $"{antes.x:0.###}×{antes.y:0.###} → {tamanhoLocal.x:0.###}×{tamanhoLocal.y:0.###} (local)"
                    : $"[CorrigirRegressao] {caminho}: SaveAsPrefabAsset recusou.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(raiz);
            }
        }
    }
}
