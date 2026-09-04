using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using FavelaAmarela.Runtime.Combat;

namespace FavelaAmarela.EditorTools
{
    /// <summary>
    /// Reconstrói, nas cenas do Build Settings, os dois colisores de ator que a auditoria de
    /// 2026-09-04 acusou fora do esperado.
    ///
    /// <list type="number">
    ///   <item><b>Hurtbox 43% estreita.</b> As dez do Cultista no Deserto saíam em 0,63 de
    ///   largura contra 1,11 que <c>Hurtbox.GarantirPara</c> produziria. O valor está gravado na
    ///   cena, e <c>GarantirPara</c> <b>não corrige o que já existe</b> — ele faz
    ///   <c>if (existente != null) return existente;</c>. Então o conserto tem de ser aqui.</item>
    ///
    ///   <item><b>Pegada 1:1.</b> A dos dois Cortesãos é quadrada, e chão isométrico é 2:1 —
    ///   a célula é 1,0 × 0,5. Uma pegada quadrada barra movimento numa profundidade que o
    ///   jogador não vê.</item>
    /// </list>
    ///
    /// <para><b>Os números não são escolhidos aqui.</b> A hurtbox sai dos MESMOS fatores que
    /// <c>GarantirPara</c> usa (0,72 × 0,86 da silhueta), e a pegada da razão da célula. Uma
    /// constante nova nesta ferramenta seria uma segunda fonte da verdade para geometria que já
    /// tem dona.</para>
    /// </summary>
    public static class RecalibrarColisoresDeAtor
    {
        /// <inheritdoc cref="Hurtbox"/>
        private const float FatorLargura = 0.72f;
        private const float FatorAltura = 0.86f;

        [MenuItem("Tools/FavelaAmarela/Colisores: recalibrar hurtbox e pegada dos atores")]
        public static void Executar()
        {
            if (!Application.isBatchMode &&
                !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                Debug.Log("[RecalibrarColisores] Cancelado — havia cena modificada por salvar.");
                return;
            }

            var log = new StringBuilder("[RecalibrarColisores]\n");
            int hurtboxes = 0, pegadas = 0;

            foreach (var entrada in EditorBuildSettings.scenes)
            {
                if (!entrada.enabled || !File.Exists(entrada.path)) continue;

                Scene cena = EditorSceneManager.OpenScene(entrada.path, OpenSceneMode.Single);
                string nome = Path.GetFileNameWithoutExtension(entrada.path);
                bool mexeu = false;

                foreach (var hb in cena.GetRootGameObjects()
                             .SelectMany(r => r.GetComponentsInChildren<Hurtbox>(true)))
                {
                    if (Recalibrar(hb, nome, log)) { hurtboxes++; mexeu = true; }
                }

                foreach (var pegada in cena.GetRootGameObjects()
                             .SelectMany(r => r.GetComponentsInChildren<Collider2D>(true))
                             .Where(EhPegadaDeAtor))
                {
                    if (Achatar(pegada, nome, log)) { pegadas++; mexeu = true; }
                }

                if (!mexeu) continue;

                EditorSceneManager.MarkSceneDirty(cena);
                EditorSceneManager.SaveScene(cena);
            }

            log.AppendLine($"   total: {hurtboxes} hurtbox(es), {pegadas} pegada(s)");
            Debug.Log(log.ToString());
        }

        /// <summary>
        /// Refaz a caixa da hurtbox a partir do sprite, como o <c>GarantirPara</c> faria numa
        /// hurtbox nova.
        /// </summary>
        private static bool Recalibrar(Hurtbox hb, string cena, StringBuilder log)
        {
            var caixa = hb.GetComponent<BoxCollider2D>();
            if (caixa == null) return false;

            // O sprite do DONO, e não do objeto da hurtbox: ela é um filho sem renderer.
            var sr = hb.transform.parent != null
                ? hb.transform.parent.GetComponentInChildren<SpriteRenderer>(true)
                : null;

            if (sr == null || sr.sprite == null) return false;

            var b = sr.sprite.bounds;
            var esperado = new Vector2(b.size.x * FatorLargura, b.size.y * FatorAltura);
            var esperadoOffset = new Vector2(b.center.x, b.center.y);

            // 2% de folga: recalibrar o que ja esta certo suja seis cenas por arredondamento.
            if (Vector2.Distance(caixa.size, esperado) < esperado.magnitude * 0.02f &&
                Vector2.Distance(caixa.offset, esperadoOffset) < 0.02f)
                return false;

            log.AppendLine($"   {cena} / {Caminho(hb.transform)} hurtbox " +
                           $"{caixa.size.x:0.##}×{caixa.size.y:0.##} -> " +
                           $"{esperado.x:0.##}×{esperado.y:0.##}");

            caixa.size = esperado;
            caixa.offset = esperadoOffset;
            EditorUtility.SetDirty(caixa);
            return true;
        }

        /// <summary>
        /// Pegada de ator: colisor sólido num objeto com corpo não-estático e sprite. Mesma
        /// classificação que a <c>AuditoriaDeColisores</c> usa.
        /// </summary>
        private static bool EhPegadaDeAtor(Collider2D col)
        {
            if (col.isTrigger) return false;
            if (col.GetComponent<Hurtbox>() != null) return false;

            var corpo = col.GetComponentInParent<Rigidbody2D>();
            return corpo != null
                   && corpo.bodyType != RigidbodyType2D.Static
                   && col.GetComponentInParent<SpriteRenderer>() != null;
        }

        /// <summary>Deita a pegada na razão 2:1 da célula, preservando a LARGURA.</summary>
        private static bool Achatar(Collider2D col, string cena, StringBuilder log)
        {
            if (!(col is BoxCollider2D caixa)) return false;

            float razao = caixa.size.y <= 0.0001f ? 0f : caixa.size.x / caixa.size.y;
            if (Mathf.Abs(razao - 2f) <= 0.5f) return false;

            // Preserva a LARGURA e ajusta a altura. O contrario alargaria o ator, mudando o
            // quanto ele ocupa do corredor -- e largura de pegada e o que o jogador sente ao
            // tentar passar por alguem.
            var novo = new Vector2(caixa.size.x, caixa.size.x * 0.5f);

            log.AppendLine($"   {cena} / {Caminho(col.transform)} pegada " +
                           $"{caixa.size.x:0.##}×{caixa.size.y:0.##} -> " +
                           $"{novo.x:0.##}×{novo.y:0.##} (razão {razao:0.##}:1 -> 2:1)");

            caixa.size = novo;
            EditorUtility.SetDirty(caixa);
            return true;
        }

        private static string Caminho(Transform t)
        {
            var partes = new System.Collections.Generic.List<string>();
            for (var a = t; a != null; a = a.parent) partes.Add(a.name);
            partes.Reverse();
            return string.Join("/", partes);
        }
    }
}
