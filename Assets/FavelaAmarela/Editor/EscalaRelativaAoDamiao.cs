using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FavelaAmarela.EditorTools
{
    /// <summary>
    /// Ajusta a escala de atores para uma <b>altura-alvo relativa ao Damião</b>.
    ///
    /// <para><b>Por que relativa e não absoluta.</b> O Vini relatou jogando: <i>"a Byakhee está
    /// muito menor do que estava, ela tem que ser maior que o Damião"</i>. Medido, ele estava
    /// certo e o problema era mais antigo que o ajuste de escala: o corpo desenhado do Byakhee
    /// dava <b>2,35 unidades</b> contra <b>2,12</b> do Damião — 11% maior, o que lê como "do
    /// mesmo tamanho" e não como chefe. Escala absoluta não diz nada; o que o jogador enxerga é
    /// a razão entre os dois corpos na tela.</para>
    ///
    /// <para><b>A conta usa o corpo DESENHADO, não a célula do sprite.</b> A folha do Byakhee
    /// tem quadros de 164 px (5,12 unidades a PPU 32), mas o bicho ocupa 2,50 delas — o resto é
    /// margem transparente. Escalar pela célula daria um chefe com metade do tamanho pedido.</para>
    /// </summary>
    public static class EscalaRelativaAoDamiao
    {
        /// <summary>
        /// Altura do corpo desenhado do Damião, em unidades de mundo. Medida do maior quadro de
        /// <c>Damiao_idle.png</c> (2,53) vezes a escala do prefab (0,8375).
        /// </summary>
        private const float AlturaDoDamiao = 2.12f;

        /// <summary>
        /// Ator → quantas vezes o Damião ele deve medir, e a altura do corpo desenhado dele
        /// numa escala de 1. As duas coisas juntas dão a escala.
        /// </summary>
        private static readonly Dictionary<string, (float Vezes, float CorpoEmEscala1)> Alvos =
            new Dictionary<string, (float, float)>
            {
                // 2,7x, escolhido pelo Vini: "aumente mais ainda", partindo do 1,8x que ela
                // ja tinha. Ela e a Aparicao da Fase 1 e chega voando -- se nao domina a tela
                // na entrada, a luta perde o susto antes de comecar. A 2,7x ela fica em 5,72
                // unidades, praticamente do tamanho do Abdul (5,50), que e o outro chefe.
                ["Byakhee"] = (2.7f, 2.50f),

                // 2,2x: acima do Byakhee (1,8x) e abaixo do Abdul (2,59x). Ele estava em
                // 1,03x -- o tamanho do Damiao, e MENOR que a Cassilda, que e NPC de dialogo.
                // A hierarquia estava invertida: quanto mais tarde o chefe, menor ele ficava.
                //
                // Ele nao leva dano por design ("um rito que se sobrevive"), mas presenca de
                // tela e outra coisa: a ultima luta precisa dominar a sala como a penultima ja
                // domina.
                // 3,0x: o chefe FINAL tem de ser o maior. A 2,2x ele ficava menor que o
                // Abdul (2,59x) e que a Byakhee (2,7x) -- a mesma inversao que o Vini apontou.
                // Ele nao leva dano por design ("um rito que se sobrevive"), e por isso a
                // presenca de tela e o unico canal que ele tem para pesar.
                ["ReiEmAmarelo"] = (3.0f, 2.19f),

                // 1,2x: "um pouco mais alto que o Damiao, so, nao precisa virar uma torre".
                // O Abdul e HUMANO -- um feiticeiro, nao uma aparicao. Ele estava em 2,59x por
                // acidente: a escala dele passou de 0,85 x 0,90 para 1,16 x 2,67 de carona no
                // commit 6b8d9e07 (2026-08-26), que era sobre o sprite do DAMIAO. Ninguem pediu
                // aquilo, e o esticamento em Y foi o que sobreviveu a uniformizacao.
                ["Abdul_Alhazred"] = (1.2f, 2.06f),

                // A Cassilda entra aqui, e nao na uniformizacao automatica, porque ela e
                // PrefabInstance SEM Rigidbody2D -- e alargar aquele filtro para alcanca-la foi
                // o que esmagou as paredes do Deserto. 1,44x preserva a altura que ela ja
                // tinha; o efeito e so tirar o esticamento (largura +30%).
                ["Cassilda"] = (1.44f, 1.59f),
            };

        [MenuItem("Tools/FavelaAmarela/Cena: escala dos chefes relativa ao Damião")]
        public static void Executar()
        {
            if (!Application.isBatchMode &&
                !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                Debug.Log("[EscalaRelativa] Cancelado — havia cena modificada por salvar.");
                return;
            }

            var log = new StringBuilder("[EscalaRelativa]\n");
            log.AppendLine($"   referência: corpo do Damião = {AlturaDoDamiao:0.##} unidades");
            int mexidos = 0;

            foreach (var entrada in EditorBuildSettings.scenes)
            {
                if (!entrada.enabled || !File.Exists(entrada.path)) continue;

                Scene cena = EditorSceneManager.OpenScene(entrada.path, OpenSceneMode.Single);
                string nomeDaCena = Path.GetFileNameWithoutExtension(entrada.path);
                bool mexeu = false;

                foreach (var t in cena.GetRootGameObjects()
                             .SelectMany(r => r.GetComponentsInChildren<Transform>(true)))
                {
                    if (!Alvos.TryGetValue(t.name, out var alvo)) continue;

                    float alturaDesejada = AlturaDoDamiao * alvo.Vezes;
                    float escala = alturaDesejada / alvo.CorpoEmEscala1;

                    float antes = t.localScale.y;
                    if (Mathf.Abs(antes - escala) < 0.005f) continue;

                    t.localScale = new Vector3(escala, escala, t.localScale.z);
                    EditorUtility.SetDirty(t);
                    mexidos++;
                    mexeu = true;

                    log.AppendLine($"   {nomeDaCena} / {t.name}  escala {antes:0.###} -> " +
                                   $"{escala:0.###}   corpo {antes * alvo.CorpoEmEscala1:0.##} -> " +
                                   $"{alturaDesejada:0.##} un  " +
                                   $"({antes * alvo.CorpoEmEscala1 / AlturaDoDamiao:0.##}× -> " +
                                   $"{alvo.Vezes:0.##}× o Damião)");
                }

                if (!mexeu) continue;

                EditorSceneManager.MarkSceneDirty(cena);
                EditorSceneManager.SaveScene(cena);
            }

            log.AppendLine($"   total: {mexidos} ator(es)");
            Debug.Log(log.ToString());
        }
    }
}
