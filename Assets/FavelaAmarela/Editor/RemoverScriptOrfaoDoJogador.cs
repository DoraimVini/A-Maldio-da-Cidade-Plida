using UnityEditor;
using UnityEngine;

namespace FavelaAmarela.EditorTools
{
    /// <summary>
    /// Ferramenta de Editor. Remove componentes com script perdido do prefab do Damião —
    /// achado em playtest (2026-08-02): "The referenced script on this Behaviour (Game
    /// Object 'Player_Damiao') is missing!".
    ///
    /// <para><b>Causa:</b> `FavelaAmarela.Player.AnomalyPowerBridge` — resto da remoção do
    /// Salto Dimensional (a habilidade saiu, mas o componente ficou preso no prefab do
    /// jogador). Sem script correspondente em lugar nenhum do projeto (confirmado por
    /// busca). Afeta <b>toda</b> cena que usa <c>Player_Damiao.prefab</c> — Deserto, Tumba
    /// e Santuário —, não é específico de nenhuma delas.</para>
    ///
    /// <para>Idempotente: sem componente órfão, não faz nada.</para>
    /// </summary>
    public static class RemoverScriptOrfaoDoJogador
    {
        private const string CaminhoPrefab = "Assets/FavelaAmarela/Art/Characters/Damiao/Player_Damiao.prefab";

        [MenuItem("Tools/FavelaAmarela/Remover script orfao do prefab do jogador")]
        public static void Executar()
        {
            using (var escopo = new PrefabUtility.EditPrefabContentsScope(CaminhoPrefab))
            {
                int removidos = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(escopo.prefabContentsRoot);
                Debug.Log(removidos > 0
                    ? $"[Player_Damiao] {removidos} componente(s) com script perdido removido(s)."
                    : "[Player_Damiao] Nenhum componente com script perdido — nada a fazer.");
            }
        }
    }
}
