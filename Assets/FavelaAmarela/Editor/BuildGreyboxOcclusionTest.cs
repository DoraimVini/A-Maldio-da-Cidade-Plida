using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace FavelaAmarela.EditorTools
{
    /// <summary>
    /// Utilitário de Editor (grey-box da cidade isométrica): monta um teste da oclusão
    /// dither na cena ativa — adiciona <c>DynamicYSort</c> ao Player_Damiao, cria um
    /// material do shader <c>FavelaAmarela/SpriteDitherOcclusion</c>, e planta uma
    /// parede alta placeholder (com trigger + <c>OcclusaoDitherFade</c>) perto do spawn,
    /// pra provar em Play que o boneco vira silhueta ao passar atrás dela.
    ///
    /// Componentes resolvidos por nome de tipo (sem depender de referência de assembly),
    /// mesmo padrão dos outros builders (WireConfigAssets / WireStormTriggers).
    /// </summary>
    public static class BuildGreyboxOcclusionTest
    {
        private const string MatDir = "Assets/FavelaAmarela/Art/Materials";
        private const string MatPath = MatDir + "/OcclusionDither.mat";

        [MenuItem("Tools/FavelaAmarela/Build Greybox Occlusion Test")]
        public static void Build()
        {
            var player = GameObject.Find("Player_Damiao");
            if (player == null)
            {
                Debug.LogError("[Greybox] 'Player_Damiao' não encontrado na cena ativa.");
                return;
            }

            var shader = Shader.Find("FavelaAmarela/SpriteDitherOcclusion");
            if (shader == null)
            {
                Debug.LogError("[Greybox] Shader 'FavelaAmarela/SpriteDitherOcclusion' não encontrado.");
                return;
            }

            // 1. DynamicYSort no player (base da profundidade isométrica).
            if (!GarantirComponente(player, "FavelaAmarela.Runtime.Rendering.DynamicYSort")) return;

            // 2. Material do shader de oclusão.
            var mat = AssetDatabase.LoadAssetAtPath<Material>(MatPath);
            if (mat == null)
            {
                Directory.CreateDirectory(MatDir);
                mat = new Material(shader) { name = "OcclusionDither" };
                AssetDatabase.CreateAsset(mat, MatPath);
            }

            // 3. Parede ALTA placeholder logo ao sul do spawn (0,0), no chão aberto.
            // Centro em y=-0.5 com escala Y=5 → sprite ocupa ~y[-3, 2], cobrindo o
            // player (y=0) que está ATRÁS dela (y do player > y da parede) → silhueta.
            // (Antes estava em (0,2), atrás da Wall_North_0 sólida — inalcançável; e
            // depois baixa demais pra cobrir o boneco.)
            const float wallBaseY = -0.5f;
            var wall = GameObject.Find("GreyboxWall_Teste") ?? new GameObject("GreyboxWall_Teste");
            wall.transform.position = new Vector3(0f, wallBaseY, 0f);
            wall.transform.localScale = new Vector3(1.5f, 2.5f, 1f); // parede alta, mas sem encher a tela

            var sr = wall.GetComponent<SpriteRenderer>();
            if (sr == null) sr = wall.AddComponent<SpriteRenderer>();
            sr.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            sr.color = new Color(0.5f, 0.35f, 0.2f); // marrom deserto placeholder
            sr.sharedMaterial = mat;
            sr.sortingOrder = Mathf.RoundToInt(-wallBaseY * 10f); // mesma convenção do gerador (-y*10)

            var col = wall.GetComponent<BoxCollider2D>();
            if (col == null) col = wall.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(1.2f, 1.4f);   // cobre a parede + área logo atrás
            col.offset = new Vector2(0f, 0.2f);

            if (!GarantirComponente(wall, "FavelaAmarela.Runtime.Rendering.OcclusaoDitherFade")) return;

            EditorSceneManager.MarkSceneDirty(player.scene);
            EditorSceneManager.SaveOpenScenes();
            Debug.Log("[Greybox] OK: DynamicYSort no player + parede de teste (0,2) com oclusão. Entre em Play e ande pra cima, atrás da parede.");
        }

        private static bool GarantirComponente(GameObject go, string fullName)
        {
            foreach (var c in go.GetComponents<Component>())
                if (c != null && c.GetType().FullName == fullName) return true; // já tem

            var type = ResolverTipo(fullName);
            if (type == null)
            {
                Debug.LogError($"[Greybox] Tipo '{fullName}' não encontrado (recompile pendente?).");
                return false;
            }

            go.AddComponent(type);
            return true;
        }

        private static Type ResolverTipo(string fullName)
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                var t = asm.GetType(fullName);
                if (t != null) return t;
            }
            return null;
        }
    }
}
