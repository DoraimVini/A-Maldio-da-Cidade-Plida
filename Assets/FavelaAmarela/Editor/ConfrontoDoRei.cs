using System.Linq;
using UnityEditor;
using UnityEngine;
using FavelaAmarela.Core.Combat;
using FavelaAmarela.Runtime.Enemies;
using FavelaAmarela.Runtime.UI;

namespace FavelaAmarela.EditorTools
{
    /// <summary>
    /// Dá carne ao prefab do Rei em Amarelo: a ficha e a barra de vida flutuante que a quarta
    /// fase do rito precisa.
    ///
    /// <para><b>A quarta fase (pedido do Vini, 2026-09-10):</b> <i>"depois dos três escudos,
    /// abre-se uma fase de combate contra ele"</i>. O <c>ReiEmAmareloAI</c> passou a implementar
    /// <c>IDanificavel</c>, mas dois pedaços não se resolvem em código: a <b>ficha</b> é um
    /// asset (<c>Ficha_Rei</c>), e a <b>barra</b> são dois <c>SpriteRenderer</c> com referências
    /// cruzadas — as duas coisas precisam estar <b>gravadas no prefab</b>, que é onde se vai
    /// procurar quando algo não aparecer.</para>
    ///
    /// <para><b>A barra é a mesma do Yug-Neth</b> (<c>BarraDeVidaFlutuante</c>): fundo escuro,
    /// preenchimento claro, some quando cheia e reaparece a cada golpe. Para um chefe isso
    /// funciona melhor do que parece — ela aparece exatamente quando o jogador o feriu, que é
    /// quando ele quer ver o progresso, e some enquanto ele corre de volta ao abrigo.</para>
    ///
    /// <para>Idempotente: reatribui a ficha e refaz a barra.</para>
    /// </summary>
    public static class ConfrontoDoRei
    {
        private const string Marcador = "[ConfrontoDoRei]";
        private const string Prefab = "Assets/FavelaAmarela/Art/Enemies/ReiEmAmarelo.prefab";
        private const string Ficha = "Assets/FavelaAmarela/Config/Ficha_Rei.asset";
        private const string NomeDaBarra = "BarraDeVida";

        /// <summary>
        /// Altura da barra acima dos pés. O sprite <c>rei_idle</c> tem 129 px a PPU 32 =
        /// 4,03 un com pivô no pé; 4,4 deixa a barra um passo acima da coroa.
        /// </summary>
        private const float AlturaDaBarra = 4.4f;

        /// <summary>Largura da barra do Yug-Neth. O Rei é maior; a barra também.</summary>
        private const float LarguraDaBarra = 6f;
        private const float AlturaDoTrilho = 0.5625f;
        private const float RecuoDoPreenchimento = 0.25f;

        [MenuItem("Tools/FavelaAmarela/Trono: dar carne ao Rei (ficha e barra)")]
        public static void Executar()
        {
            var ficha = AssetDatabase.LoadAssetAtPath<FichaAtributosConfig>(Ficha);
            if (ficha == null)
            {
                Debug.LogError($"{Marcador} '{Ficha}' não encontrada — sem ficha, o Confronto " +
                               "roda na de emergência.");
                return;
            }

            var raiz = PrefabUtility.LoadPrefabContents(Prefab);

            try
            {
                var rei = raiz.GetComponent<ReiEmAmareloAI>();
                if (rei == null)
                {
                    Debug.LogError($"{Marcador} o prefab não tem ReiEmAmareloAI.");
                    return;
                }

                var barra = MontarBarra(raiz.transform);

                var so = new SerializedObject(rei);
                so.FindProperty("ficha").objectReferenceValue = ficha;
                so.FindProperty("barraDeVida").objectReferenceValue = barra;
                so.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(raiz, Prefab);

                Debug.Log($"{Marcador} ficha '{ficha.name}' (Vitalidade {ficha.VitalidadeMax}, " +
                          $"Defesa {ficha.Defesa}, Res. Anômala {ficha.ResistenciaAnomala}) " +
                          $"e barra de {LarguraDaBarra} un a {AlturaDaBarra} un gravadas em " +
                          $"{System.IO.Path.GetFileName(Prefab)}.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(raiz);
            }
        }

        private static BarraDeVidaFlutuante MontarBarra(Transform rei)
        {
            var antiga = rei.Find(NomeDaBarra);
            if (antiga != null) Object.DestroyImmediate(antiga.gameObject);

            var go = new GameObject(NomeDaBarra);
            go.transform.SetParent(rei, false);
            go.transform.localPosition = new Vector3(0f, AlturaDaBarra, 0f);

            var branco = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

            var fundo = Renderer(go.transform, "Fundo", branco,
                new Vector3(LarguraDaBarra, AlturaDoTrilho, 1f),
                new Color(0.05f, 0.04f, 0.03f, 0.7f), 32000);

            var preenchimento = Renderer(go.transform, "Preenchimento", branco,
                new Vector3(LarguraDaBarra - RecuoDoPreenchimento, AlturaDoTrilho, 1f),
                new Color(0.75f, 0.72f, 0.45f, 0.85f), 32001);

            var barra = go.AddComponent<BarraDeVidaFlutuante>();
            var so = new SerializedObject(barra);
            so.FindProperty("fundo").objectReferenceValue = fundo;
            so.FindProperty("preenchimento").objectReferenceValue = preenchimento;
            so.FindProperty("alturaAcimaDaCabeca").floatValue = AlturaDaBarra;
            so.ApplyModifiedPropertiesWithoutUndo();

            return barra;
        }

        private static SpriteRenderer Renderer(Transform pai, string nome, Sprite sprite,
                                               Vector3 escala, Color cor, int ordem)
        {
            var go = new GameObject(nome);
            go.transform.SetParent(pai, false);
            go.transform.localScale = escala;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = cor;
            sr.sortingLayerName = "Frente";
            sr.sortingOrder = ordem;

            return sr;
        }
    }
}
