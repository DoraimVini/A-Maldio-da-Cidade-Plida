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
    /// <para><b>Escopo: só o que é específico do Byakhee.</b> As <b>hurtboxes não são montadas
    /// aqui</b> — elas se constroem sozinhas em runtime, via
    /// <c>Hurtbox.GarantirPara</c>, chamado do <c>Awake</c> de quem implementa
    /// <c>IDanificavel</c>. Uma versão anterior desta ferramenta trazia uma lista de prefabs
    /// escrita à mão; o Vini apontou que isso contraria a ideia de contrato por camada, e ele
    /// estava certo — <b>seis</b> listas escritas à mão já envelheceram neste projeto. A hitbox
    /// das garras continua aqui porque é <i>de fato</i> particular deste chefe: alcance, janela
    /// e o momento do pouso.</para>
    /// </summary>
    public static class MontarHitboxEHurtbox
    {
        private const string PrefabDamiao =
            "Assets/FavelaAmarela/Art/Characters/Damiao/Player_Damiao.prefab";

        private const string PrefabByakhee = "Assets/FavelaAmarela/Art/Enemies/Byakhee.prefab";



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

            MontarHitboxDoByakhee(camadaHurtboxDoJogador);

            ApontarGolpeDoJogadorParaAsHurtboxes();
        }

        /// <summary>
        /// Grava explicitamente no prefab do Damião a máscara <c>Enemy | EnemyHurtbox</c>. O
        /// fallback em <c>MaoFisicaBridge.Awake</c> só age quando o campo está zerado — num
        /// prefab que já tem "Enemy" salvo, ele nunca rodaria, e o golpe continuaria mirando
        /// só a pegada dos pés.
        /// </summary>
        private static void ApontarGolpeDoJogadorParaAsHurtboxes()
        {
            int enemy = LayerMask.NameToLayer("Enemy");
            int hurtbox = LayerMask.NameToLayer("EnemyHurtbox");
            if (enemy < 0 || hurtbox < 0) return;

            var raiz = PrefabUtility.LoadPrefabContents(PrefabDamiao);
            if (raiz == null) return;

            try
            {
                var mao = raiz.GetComponent<FavelaAmarela.Player.MaoFisicaBridge>();
                if (mao == null)
                {
                    Debug.LogWarning("[HitboxHurtbox] Damião sem MaoFisicaBridge.");
                    return;
                }

                int mascara = (1 << enemy) | (1 << hurtbox);

                var so = new SerializedObject(mao);
                var campo = so.FindProperty("camadaInimigos");
                if (campo == null)
                {
                    Debug.LogWarning("[HitboxHurtbox] MaoFisicaBridge sem 'camadaInimigos'.");
                    return;
                }

                campo.intValue = mascara;
                so.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(raiz, PrefabDamiao, out bool salvou);

                Debug.Log(salvou
                    ? $"[HitboxHurtbox] Golpe do Damião mira Enemy|EnemyHurtbox (máscara {mascara})."
                    : "[HitboxHurtbox] Damião: SaveAsPrefabAsset recusou ao gravar a máscara.");
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
