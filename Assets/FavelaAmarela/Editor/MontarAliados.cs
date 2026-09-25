using UnityEditor;
using UnityEngine;
using FavelaAmarela.Runtime.Enemies;
using FavelaAmarela.Runtime.UI;

namespace FavelaAmarela.EditorTools
{
    /// <summary>
    /// Ferramenta de Editor. Põe Yug-Neth na layer <b>Aliados</b> e monta a barra de vida
    /// flutuante sobre a cabeça dele.
    ///
    /// <para><b>O que motivou (auditoria 2026-08-11):</b> o companheiro estava na layer
    /// <c>Enemy</c> e o <c>EnemyCombat</c> só procurava alvos em <c>Player</c> — ou seja, os
    /// inimigos <b>nunca o atacavam</b>. Toda a mecânica de incapacitação (cair, bloquear os
    /// Portões, ser reanimado num Refúgio) estava implementada e nunca disparava.</para>
    ///
    /// <para>A layer é própria, e não "põe o aliado em Player", porque virão outros aliados e
    /// NPCs ao longo da campanha (decisão do Vini) — e porque o que mira só em Damião
    /// (câmera, gatilhos de quest) não pode passar a mirar neles junto.</para>
    ///
    /// <para>Idempotente: refaz a barra do zero e não duplica nada.</para>
    /// </summary>
    public static class MontarAliados
    {
        private const string PrefabYugNeth = "Assets/FavelaAmarela/Art/Characters/MiGo/YugNeth.prefab";
        private const string NomeDaBarra = "BarraDeVida";
        private const string LayerAliados = "Aliados";

        // Discreta de propósito: o pedido foi "sem poluir a tela". Fina e estreita, ela é
        // legível de relance sem competir com o que importa.
        private const float Largura = 0.7f;
        private const float Altura = 0.09f;

        [MenuItem("Tools/FavelaAmarela/Montar aliados (layer + barra de vida)")]
        public static void Executar()
        {
            int layer = LayerMask.NameToLayer(LayerAliados);
            if (layer < 0)
            {
                Debug.LogError($"[Aliados] A layer '{LayerAliados}' não existe. " +
                               "Ela deveria ter sido criada no TagManager — reimporte o projeto.");
                return;
            }

            var raiz = PrefabUtility.LoadPrefabContents(PrefabYugNeth);
            if (raiz == null)
            {
                Debug.LogError($"[Aliados] Prefab não encontrado: '{PrefabYugNeth}'.");
                return;
            }

            AplicarLayer(raiz, layer);
            MontarBarra(raiz);

            PrefabUtility.SaveAsPrefabAsset(raiz, PrefabYugNeth);
            PrefabUtility.UnloadPrefabContents(raiz);

            AssetDatabase.SaveAssets();
            Debug.Log($"[Aliados] Yug-Neth agora está na layer '{LayerAliados}' e tem barra de " +
                      "vida. Os inimigos passam a enxergá-lo como alvo.");
        }

        /// <summary>Aplica a layer na raiz e em toda a descendência — colisor filho também conta.</summary>
        private static void AplicarLayer(GameObject raiz, int layer)
        {
            foreach (var t in raiz.GetComponentsInChildren<Transform>(includeInactive: true))
                t.gameObject.layer = layer;
        }

        private static void MontarBarra(GameObject raiz)
        {
            var antiga = raiz.transform.Find(NomeDaBarra);
            if (antiga != null) Object.DestroyImmediate(antiga.gameObject);

            var barraGo = new GameObject(NomeDaBarra);
            barraGo.transform.SetParent(raiz.transform, false);

            var sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

            var fundo = CriarPeca(barraGo.transform, "Fundo", sprite,
                new Color(0.05f, 0.04f, 0.03f, 0.7f), ordem: 0, largura: Largura);

            // Levemente menor que o fundo, para sobrar uma moldura escura de contorno.
            var preenchimento = CriarPeca(barraGo.transform, "Preenchimento", sprite,
                new Color(0.75f, 0.72f, 0.45f, 0.85f), ordem: 1, largura: Largura * 0.94f);

            var comp = barraGo.AddComponent<BarraDeVidaFlutuante>();
            var so = new SerializedObject(comp);
            so.FindProperty("fundo").objectReferenceValue = fundo;
            so.FindProperty("preenchimento").objectReferenceValue = preenchimento;
            so.ApplyModifiedPropertiesWithoutUndo();

            // O YugNethAI liga a barra à Vitalidade no Awake; aqui só montamos as peças.
            var ai = raiz.GetComponent<YugNethAI>();
            if (ai == null)
                Debug.LogWarning("[Aliados] Prefab sem YugNethAI — a barra não terá o que observar.");
        }

        private static SpriteRenderer CriarPeca(Transform pai, string nome, Sprite sprite,
            Color cor, int ordem, float largura)
        {
            var go = new GameObject(nome, typeof(SpriteRenderer));
            go.transform.SetParent(pai, false);

            var sr = go.GetComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = cor;
            sr.sortingLayerName = "Default";

            // Ordem alta: a barra fica acima de qualquer ator, inclusive de quem passar na
            // frente. Ela é informação, não parte do cenário.
            sr.sortingOrder = 32000 + ordem;

            float escalaX = sprite != null ? largura / sprite.bounds.size.x : largura;
            float escalaY = sprite != null ? Altura / sprite.bounds.size.y : Altura;
            go.transform.localScale = new Vector3(escalaX, escalaY, 1f);

            return sr;
        }
    }
}
