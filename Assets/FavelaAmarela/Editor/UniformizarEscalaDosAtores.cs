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
    /// Uniformiza a escala dos atores nas cenas do Build Settings, <b>preservando a ALTURA</b>.
    ///
    /// <para><b>Por que existia o problema.</b> A auditoria de 2026-09-04 mediu que
    /// <b>nenhum</b> ator instanciado em cena tinha escala uniforme — Abdul em
    /// (1,162 × 2,671), os dez Cultistas em (0,630 × 0,804), Cassilda em (1,478 × 1,925). A
    /// medição anterior tinha olhado a raiz dos <b>prefabs</b>, e é na <b>instância</b> que a
    /// escala é sobrescrita.</para>
    ///
    /// <para><b>Por que altura, e não largura.</b> Decisão do Vini. É o eixo que ele ajustou à
    /// mão — o Abdul foi de 0,97 para 2,06 unidades de altura em 2026-09-03 —, e altura é o que
    /// lê como "o tamanho deste personagem". Igualar pela largura desfaria esse ajuste: o Abdul
    /// perderia mais da metade da altura.</para>
    ///
    /// <para><b>O que isto conserta além do visual.</b> Três coisas dependiam de escala
    /// uniforme e estavam silenciosamente erradas:</para>
    /// <list type="number">
    ///   <item>A razão <b>2:1</b> das pegadas só vale em mundo se os dois eixos escalarem
    ///   igual. Sob (0,63 × 0,80), uma pegada local de 2:1 chegava ao mundo em 1,57:1.</item>
    ///   <item>Um <c>CircleCollider2D</c> é escalado pela Unity <b>pelo maior eixo</b>, enquanto
    ///   o desenho vira elipse: física e gizmo discordavam.</item>
    ///   <item>Pixel art a 32 PPU esticada por fator não inteiro sai do grid de pixel — é o que
    ///   a skill <c>favela-pixelart-standards</c> existe para proteger.</item>
    /// </list>
    ///
    /// <para><b>Não mexe em prefabs.</b> A escala da instância sobrescreve a do prefab, então
    /// mexer nos dois arriscaria dobrar o efeito. O <c>YugNeth.prefab</c> tem a raiz em
    /// (4,1125 × 0,5625), o que é caso à parte e fica registrado no relatório.</para>
    /// </summary>
    public static class UniformizarEscalaDosAtores
    {
        /// <summary>Diferença entre eixos que já conta como uniforme.</summary>
        private const float Tolerancia = 0.001f;

        [MenuItem("Tools/FavelaAmarela/Cena: uniformizar a escala dos atores (pela altura)")]
        public static void Executar()
        {
            if (!Application.isBatchMode &&
                !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                Debug.Log("[EscalaDosAtores] Cancelado — havia cena modificada por salvar.");
                return;
            }

            var log = new StringBuilder("[EscalaDosAtores]\n");
            int mexidos = 0;

            foreach (var entrada in EditorBuildSettings.scenes)
            {
                if (!entrada.enabled || !File.Exists(entrada.path)) continue;

                Scene cena = EditorSceneManager.OpenScene(entrada.path, OpenSceneMode.Single);
                string nomeDaCena = Path.GetFileNameWithoutExtension(entrada.path);
                bool mexeu = false;

                foreach (var t in cena.GetRootGameObjects()
                             .SelectMany(r => r.GetComponentsInChildren<Transform>(true))
                             .Where(EhAtor))
                {
                    var e = t.localScale;
                    if (Mathf.Abs(e.x - e.y) <= Tolerancia) continue;

                    // A ALTURA VENCE: os dois eixos passam a valer o Y.
                    var novo = new Vector3(e.y, e.y, e.z);

                    log.AppendLine($"   {nomeDaCena} / {t.name}  " +
                                   $"{e.x:0.###} × {e.y:0.###}  ->  {novo.x:0.###} × {novo.y:0.###}  " +
                                   $"(largura {(novo.x / e.x - 1f) * 100f:+0;-0}%)");

                    t.localScale = novo;
                    EditorUtility.SetDirty(t);
                    mexidos++;
                    mexeu = true;
                }

                if (!mexeu) continue;

                EditorSceneManager.MarkSceneDirty(cena);
                EditorSceneManager.SaveScene(cena);
            }

            log.AppendLine($"   total: {mexidos} ator(es)");
            log.AppendLine("   a altura foi preservada em todos; só a largura mudou.");
            Debug.Log(log.ToString());
        }

        /// <summary>
        /// Ator = corpo não-estático mais sprite. Mesma classificação que a
        /// <c>AuditoriaDeColisores</c> usa para separar ator de cenário — parede esticada é
        /// legítima, e uniformizar uma seria estragar geometria de nível.
        /// </summary>
        private static bool EhAtor(Transform t)
        {
            // EXIGE Rigidbody2D não-estático, e isto NÃO é excesso de zelo.
            //
            // Em 2026-09-04 este filtro foi alargado para "qualquer coisa com SpriteRenderer",
            // com o argumento de que só restava um objeto não uniforme no projeto e portanto
            // nada mais poderia ser tocado. O argumento estava ERRADO: a varredura que o
            // sustentava tinha olhado apenas os overrides de <c>PrefabInstance</c> no YAML, e
            // não os GameObjects comuns de cena.
            //
            // O resultado, numa única execução:
            //   Limite_Norte e Limite_Sul     88 × 1  ->  1 × 1    (as paredes do Deserto)
            //   Limite_Leste e Limite_Oeste    1 × 64 -> 64 × 64
            //   Fundo e Preenchimento (barra de vida)      −87% de largura
            //   VisualDoEscudo do Abdul                   +152%
            //
            // E as duas suítes passaram VERDE por cima disso — 1052 + 50 testes, zero falhas.
            // Nada neste projeto guarda o tamanho das paredes do mapa.
            //
            // O corpo não-estático é o que separa ATOR de cenário, e cenário esticado é
            // legítimo: uma parede É uma caixa alongada. Quem não tem corpo e precisa de ajuste
            // vai na tabela por nome de <c>EscalaRelativaAoDamiao</c>, onde a mudança é
            // deliberada, revisável e não pega ninguém de carona.
            var corpo = t.GetComponent<Rigidbody2D>();
            return corpo != null
                   && corpo.bodyType != RigidbodyType2D.Static
                   && t.GetComponentInChildren<SpriteRenderer>(true) != null;
        }
    }
}
