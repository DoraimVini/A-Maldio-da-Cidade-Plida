using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using FavelaAmarela.Player;
using FavelaAmarela.Runtime.Enemies;

namespace FavelaAmarela.EditorTools
{
    /// <summary>
    /// Utilitário de Editor: fecha as duas últimas pontas soltas da luta do Abdul na cena
    /// aberta — o <b>sprite do Abdul</b> (que pode ter ficado órfão) e o
    /// <see cref="CongelamentoBridge"/> no Damião (sem ele os Cones de Gelo não congelam
    /// ninguém).
    ///
    /// <para>Idempotente: reatribui o sprite só se estiver faltando e não duplica componente.</para>
    /// </summary>
    public static class FecharLutaDoAbdul
    {
        private const string CaminhoSpritesheet =
            "Assets/Sprites/Bosses/Alhazred/abdul_alhazred_spritesheet.png";
        private const string SpriteIdleDoAbdul = "abdul_transe_0";

        [MenuItem("Tools/FavelaAmarela/Fechar Luta do Abdul (sprite + congelamento)")]
        public static void Fechar()
        {
            int mudancas = 0;
            mudancas += ReconectarSpriteDoAbdul();
            mudancas += AdicionarCongelamentoNoDamiao();

            if (mudancas > 0)
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

            Debug.Log($"[FecharLuta] {mudancas} ajuste(s) aplicado(s). " +
                      "Cena NÃO foi salva — confira antes.");
        }

        /// <summary>
        /// Reatribui o sprite de idle do Abdul. A referência pode ficar órfã se a folha for
        /// refatiada — os `spriteID` mudavam a cada execução do slicer (corrigido depois,
        /// com IDs determinísticos), e uma referência quebrada deixa o boss **invisível**
        /// sem nenhum erro no console.
        /// </summary>
        private static int ReconectarSpriteDoAbdul()
        {
            var abdul = Object.FindAnyObjectByType<AbdulAlhazredAI>(FindObjectsInactive.Include);
            if (abdul == null)
            {
                Debug.LogWarning("[FecharLuta] Nenhum AbdulAlhazredAI na cena.");
                return 0;
            }

            var sr = abdul.GetComponent<SpriteRenderer>();
            if (sr == null) return 0;
            if (sr.sprite != null) return 0; // já tem sprite válido

            var sprite = AssetDatabase.LoadAllAssetsAtPath(CaminhoSpritesheet)
                .OfType<Sprite>()
                .FirstOrDefault(s => s.name == SpriteIdleDoAbdul);

            if (sprite == null)
            {
                Debug.LogError($"[FecharLuta] Sprite '{SpriteIdleDoAbdul}' não encontrado — " +
                               "rode 'Slice Spritesheet do Abdul' antes.");
                return 0;
            }

            var so = new SerializedObject(sr);
            so.FindProperty("m_Sprite").objectReferenceValue = sprite;
            so.ApplyModifiedPropertiesWithoutUndo();

            Debug.Log($"[FecharLuta] Sprite do Abdul reconectado ('{SpriteIdleDoAbdul}').");
            return 1;
        }

        /// <summary>
        /// Garante o <see cref="CongelamentoBridge"/> no Damião. Sem ele, o Cone de Gelo
        /// acerta mas não acumula frio — a mecânica de congelamento da Fase 2 não existe.
        /// </summary>
        private static int AdicionarCongelamentoNoDamiao()
        {
            var player = Object.FindAnyObjectByType<PlayerMovement>(FindObjectsInactive.Include);
            if (player == null)
            {
                Debug.LogWarning("[FecharLuta] Nenhum PlayerMovement na cena.");
                return 0;
            }

            if (player.GetComponent<CongelamentoBridge>() != null) return 0;

            var bridge = player.gameObject.AddComponent<CongelamentoBridge>();

            // Liga o sprite do Damião para o tingimento azul enquanto congelado.
            var sr = player.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                var so = new SerializedObject(bridge);
                var prop = so.FindProperty("spriteDoDamiao");
                if (prop != null)
                {
                    prop.objectReferenceValue = sr;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
            }

            Debug.Log("[FecharLuta] CongelamentoBridge adicionado ao Damião.");
            return 1;
        }
    }
}
