using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using FavelaAmarela.Player;

namespace FavelaAmarela.EditorTools
{
    /// <summary>
    /// Utilitário de Editor: garante o <see cref="CongelamentoBridge"/> no Damião da cena
    /// aberta — sem ele os Cones de Gelo do Abdul não congelam ninguém.
    ///
    /// <para><b>Ela fazia duas coisas até 2026-08-28.</b> A outra era reatribuir o sprite de
    /// idle do Abdul a partir de <c>abdul_alhazred_spritesheet.png</c>. Essa folha era arte de
    /// IA <b>totalmente opaca</b> (o xadrez de transparência foi achatado na exportação, e em
    /// jogo o boss virava um quadrado claro de 4×4 unidades); foi substituída pelo Mage do
    /// Horror Enemy Pack em <c>LigarAnimacaoDoAbdul</c>, e o Vini mandou apagar os órfãos. Com a
    /// folha fora do projeto aquele passo só saberia emitir erro, então saiu junto — <b>reatribuir
    /// o sprite hoje seria restaurar a arte quebrada</b>.</para>
    ///
    /// <para>Idempotente: não duplica componente.</para>
    /// </summary>
    public static class FecharLutaDoAbdul
    {
        [MenuItem("Tools/FavelaAmarela/Fechar Luta do Abdul (congelamento no Damião)")]
        public static void Fechar()
        {
            int mudancas = AdicionarCongelamentoNoDamiao();

            if (mudancas > 0)
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

            Debug.Log($"[FecharLuta] {mudancas} ajuste(s) aplicado(s). " +
                      "Cena NÃO foi salva — confira antes.");
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
