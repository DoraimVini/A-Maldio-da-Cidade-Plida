using System.IO;
using UnityEditor;
using UnityEngine;
using FavelaAmarela.Runtime.Combat;
using FavelaAmarela.Runtime.Enemies;

namespace FavelaAmarela.EditorTools
{
    /// <summary>
    /// Monta a separação hitbox/hurtbox nos prefabs que já têm combate.
    ///
    /// <para><b>Por que (2026-08-21):</b> as quatro camadas —<c>PlayerHitbox</c> (11),
    /// <c>EnemyHitbox</c> (12), <c>PlayerHurtbox</c> (13), <c>EnemyHurtbox</c> (14) — estavam
    /// declaradas em <c>TagManager.asset</c> com <b>zero</b> prefabs, cenas ou código as usando.
    /// Alguém planejou a separação e não a construiu. Até aqui, um colisor por personagem fazia
    /// três trabalhos: barrar movimento, receber dano e ser detectado.</para>
    ///
    /// <para><b>Escopo desta primeira passada:</b> a <b>hurtbox do Damião</b> e a <b>hitbox das
    /// garras do Byakhee</b> — a luta que o Vini relatou como "sem feel". O ganho real não é
    /// arquitetural, é de <i>sensação</i>: o golpe do Byakhee deixa de ser um teste de distância
    /// de um quadro só e passa a ter <b>janela ativa</b>, o que torna a esquiva uma decisão de
    /// tempo. Os outros inimigos entram numa passada seguinte.</para>
    /// </summary>
    public static class MontarHitboxEHurtbox
    {
        private const string PrefabDamiao =
            "Assets/FavelaAmarela/Art/Characters/Damiao/Player_Damiao.prefab";

        private const string PrefabByakhee = "Assets/FavelaAmarela/Art/Enemies/Byakhee.prefab";

        /// <summary>
        /// Corpo do Damião em unidades de mundo: a figura tem ~2,12 un de altura e ~0,84 de
        /// largura. A hurtbox cobre o <b>corpo desenhado</b>, não a pegada no chão — é o que o
        /// jogador vê e espera que seja atingível.
        /// </summary>
        private static readonly Vector2 CorpoDoDamiao = new Vector2(0.70f, 1.90f);

        /// <summary>Centro do corpo, medido a partir dos pés (o pivô está no rodapé).</summary>
        private const float AlturaDoCentro = 1.05f;

        /// <summary>
        /// Raio do baque de pouso do Byakhee, em unidades de mundo. Herda o
        /// <c>alcanceDasGarras</c> antigo (1,5) para não mudar duas variáveis de uma vez: o que
        /// muda de verdade nesta passada é a <b>janela</b>, não o alcance.
        /// </summary>
        private const float RaioDasGarras = 1.5f;

        [MenuItem("Tools/FavelaAmarela/Combate: montar hitbox e hurtbox")]
        public static void Executar()
        {
            int camadaHurtboxDoJogador = LayerMask.NameToLayer("PlayerHurtbox");

            if (camadaHurtboxDoJogador < 0)
            {
                Debug.LogError("[HitboxHurtbox] A camada 'PlayerHurtbox' não existe em " +
                               "TagManager.asset. Abortado.");
                return;
            }

            MontarHurtboxDoDamiao(camadaHurtboxDoJogador);
            MontarHitboxDoByakhee(camadaHurtboxDoJogador);
        }

        private static void MontarHurtboxDoDamiao(int camada)
        {
            if (!File.Exists(PrefabDamiao))
            {
                Debug.LogError($"[HitboxHurtbox] {PrefabDamiao} não existe.");
                return;
            }

            var raiz = PrefabUtility.LoadPrefabContents(PrefabDamiao);
            if (raiz == null) return;

            try
            {
                var t = raiz.transform.Find("Hurtbox");
                var go = t != null ? t.gameObject : new GameObject("Hurtbox");
                if (t == null) go.transform.SetParent(raiz.transform, false);

                go.layer = camada;
                go.transform.localPosition = Vector3.zero;

                // O colisor é filho de um objeto com localScale != 1, então o tamanho local
                // precisa ser dividido pela escala para dar o tamanho pretendido em MUNDO —
                // mesma conta de RevisarColisores. Sem isso a hurtbox sai 19% menor.
                float escala = raiz.transform.localScale.x;
                if (Mathf.Approximately(escala, 0f)) escala = 1f;

                var capsula = go.GetComponent<CapsuleCollider2D>();
                if (capsula == null) capsula = go.AddComponent<CapsuleCollider2D>();

                capsula.direction = CapsuleDirection2D.Vertical;
                capsula.isTrigger = true;
                capsula.size = new Vector2(CorpoDoDamiao.x / escala, CorpoDoDamiao.y / escala);
                capsula.offset = new Vector2(0f, AlturaDoCentro / escala);

                if (go.GetComponent<Hurtbox>() == null) go.AddComponent<Hurtbox>();

                PrefabUtility.SaveAsPrefabAsset(raiz, PrefabDamiao, out bool salvou);

                Debug.Log(salvou
                    ? $"[HitboxHurtbox] Damião: hurtbox {CorpoDoDamiao.x:0.00}×{CorpoDoDamiao.y:0.00} " +
                      $"un (local {capsula.size.x:0.00}×{capsula.size.y:0.00}, escala {escala:0.000}), " +
                      $"camada {LayerMask.LayerToName(camada)}."
                    : "[HitboxHurtbox] Damião: SaveAsPrefabAsset recusou.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(raiz);
            }
        }

        private static void MontarHitboxDoByakhee(int camadaAlvo)
        {
            if (!File.Exists(PrefabByakhee))
            {
                Debug.LogError($"[HitboxHurtbox] {PrefabByakhee} não existe.");
                return;
            }

            var raiz = PrefabUtility.LoadPrefabContents(PrefabByakhee);
            if (raiz == null) return;

            try
            {
                var t = raiz.transform.Find("Hitbox_Garras");
                var go = t != null ? t.gameObject : new GameObject("Hitbox_Garras");
                if (t == null) go.transform.SetParent(raiz.transform, false);

                go.transform.localPosition = Vector3.zero;

                var hitbox = go.GetComponent<Hitbox>();
                if (hitbox == null) hitbox = go.AddComponent<Hitbox>();

                // Campos privados serializados: SerializedObject é o caminho suportado.
                var so = new SerializedObject(hitbox);
                so.FindProperty("raio").floatValue = RaioDasGarras;
                so.FindProperty("deslocamento").vector2Value = Vector2.zero;
                so.FindProperty("camadasAlvo").intValue = 1 << camadaAlvo;
                so.ApplyModifiedPropertiesWithoutUndo();

                var ia = raiz.GetComponent<ByakheeAI>();
                if (ia == null)
                {
                    Debug.LogError("[HitboxHurtbox] Byakhee.prefab sem ByakheeAI — não dá para " +
                                   "ligar a hitbox.");
                    return;
                }

                var soIa = new SerializedObject(ia);
                soIa.FindProperty("hitboxDasGarras").objectReferenceValue = hitbox;
                soIa.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(raiz, PrefabByakhee, out bool salvou);

                Debug.Log(salvou
                    ? $"[HitboxHurtbox] Byakhee: hitbox raio {RaioDasGarras:0.00} un, alvo " +
                      $"{LayerMask.LayerToName(camadaAlvo)}, ligada no ByakheeAI."
                    : "[HitboxHurtbox] Byakhee: SaveAsPrefabAsset recusou.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(raiz);
            }
        }
    }
}
