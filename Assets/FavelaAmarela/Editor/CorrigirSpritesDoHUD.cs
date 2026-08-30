using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace FavelaAmarela.EditorTools
{
    /// <summary>
    /// Devolve os sprites às barras do HUD — sem eles, <b>uma barra nunca diminui</b>.
    ///
    /// <para><b>A causa, na fonte do uGUI</b> (<c>Image.cs:883</c>, pacote
    /// <c>com.unity.ugui</c> desta Unity):</para>
    /// <code>
    /// protected override void OnPopulateMesh(VertexHelper toFill)
    /// {
    ///     if (activeSprite == null)
    ///     {
    ///         base.OnPopulateMesh(toFill);   // quad INTEIRO
    ///         return;                        // o 'type' nunca é consultado
    ///     }
    ///     switch (type) { ... case Type.Filled: ...
    /// </code>
    ///
    /// <para>Com sprite nulo o <c>Image</c> retorna <b>antes</b> de olhar o tipo, e
    /// <c>Graphic.OnPopulateMesh</c> monta o retângulo inteiro a partir de
    /// <c>GetPixelAdjustedRect()</c>. O <c>fillAmount</c> continua mudando no código — a lógica
    /// da barra está correta — mas a malha desenhada é sempre a mesma. O sintoma é
    /// indistinguível de "o dano não está sendo aplicado".</para>
    ///
    /// <para><b>Por que regrediu (medido em 2026-08-29).</b> Duas causas independentes, e as
    /// duas silenciosas:</para>
    ///
    /// <list type="number">
    ///   <item><b>Esta ferramenta varria a CENA ATIVA</b>
    ///   (<c>FindObjectsByType&lt;Image&gt;</c> + <c>SaveScene</c>). Desde a migração para HUD
    ///   persistente, o HUD não está em cena nenhuma — ele é
    ///   <c>Resources/HUD_Gameplay.prefab</c>, instanciado em runtime. A ferramenta passou a
    ///   relatar <i>"nenhuma Image sem sprite — nada a fazer"</i> e a sair, o que lida como
    ///   sucesso.</item>
    ///
    ///   <item><b>Os sprites nunca carregavam.</b> <c>bar_background.png</c> e
    ///   <c>bar_fill.png</c> estão em <c>spriteMode: 2</c> (Multiple), e para textura em modo
    ///   Multiple o <c>Sprite</c> é <b>sub-asset</b>: o asset principal é a <c>Texture2D</c>, e
    ///   <c>LoadAssetAtPath&lt;Sprite&gt;</c> devolve <c>null</c>. O outro menu deste arquivo
    ///   caía no guarda de nulo e abortava toda vez.</item>
    /// </list>
    ///
    /// <para><c>HudComSpritesTests</c> guarda o resultado: nenhuma <c>Image</c> do tipo
    /// <c>Filled</c> pode ficar sem sprite.</para>
    /// </summary>
    public static class CorrigirSpritesDoHUD
    {
        /// <summary>O HUD que o jogo realmente carrega, em runtime, via <c>Resources.Load</c>.</summary>
        private const string HudVivo = "Assets/FavelaAmarela/Resources/HUD_Gameplay.prefab";

        private const string SpriteDoTrilho = "Assets/FavelaAmarela/Art/UI/Sprites/bar_background.png";
        private const string SpriteDoFill = "Assets/FavelaAmarela/Art/UI/Sprites/bar_fill.png";

        /// <summary>
        /// Carrega o <see cref="Sprite"/> de um PNG, <b>inclusive em modo Multiple</b>, onde ele
        /// é sub-asset e <c>LoadAssetAtPath&lt;Sprite&gt;</c> devolve nulo.
        /// </summary>
        private static Sprite CarregarSprite(string caminho)
        {
            var direto = AssetDatabase.LoadAssetAtPath<Sprite>(caminho);
            if (direto != null) return direto;

            return AssetDatabase.LoadAllAssetsAtPath(caminho).OfType<Sprite>().FirstOrDefault();
        }

        [MenuItem("Tools/FavelaAmarela/HUD: restaurar os sprites das barras")]
        public static void Executar()
        {
            var trilho = CarregarSprite(SpriteDoTrilho);
            var fill = CarregarSprite(SpriteDoFill);

            if (trilho == null || fill == null)
            {
                Debug.LogError($"[SpritesDoHUD] Sprite não carregou — trilho: {trilho != null}, " +
                               $"fill: {fill != null}. Confira {SpriteDoTrilho} e {SpriteDoFill}.");
                return;
            }

            var resumo = new List<string>();

            using (var escopo = new PrefabUtility.EditPrefabContentsScope(HudVivo))
            {
                foreach (var img in escopo.prefabContentsRoot
                             .GetComponentsInChildren<Image>(includeInactive: true))
                {
                    string caminho = Caminho(img.transform);

                    // Ícone de item recebe sprite em runtime (ColetavelDeItem/BarraDeItens):
                    // nulo aqui é o estado correto, e forçar um sprite mostraria um trilho de
                    // barra no lugar do ícone.
                    if (img.name == "Icone") continue;

                    if (img.sprite != null) continue;

                    if (img.name == "Trilho")
                    {
                        img.sprite = trilho;
                        img.type = Image.Type.Simple;
                        img.color = Color.white;   // a cor vem do sprite; tingir escureceria
                        resumo.Add($"{caminho}: trilho");
                    }
                    else if (img.type == Image.Type.Filled)
                    {
                        // O que preenche: barra de recurso ou o giro de recarga da habilidade.
                        // O fillMethod autorado é preservado — Radial360 na Recarga, Horizontal
                        // nas barras.
                        img.sprite = fill;
                        resumo.Add($"{caminho}: preenchimento ({img.fillMethod})");
                    }
                    else
                    {
                        resumo.Add($"{caminho}: SEM SPRITE e não é Filled — deixado como está " +
                                   $"(tipo {img.type}); confira se é intencional");
                        continue;
                    }

                    EditorUtility.SetDirty(img);
                }
            }

            if (resumo.Count == 0)
            {
                Debug.Log("[SpritesDoHUD] Nenhuma Image sem sprite no HUD vivo — nada a fazer.");
                return;
            }

            AssetDatabase.SaveAssets();

            Debug.Log($"[SpritesDoHUD] {resumo.Count} Image(s) no HUD vivo:\n  " +
                      string.Join("\n  ", resumo) +
                      "\n[SpritesDoHUD] As barras voltam a encolher com o dano.");
        }

        /// <summary>Caminho legível na hierarquia, para o resumo dizer QUAL barra mudou.</summary>
        private static string Caminho(Transform t)
        {
            var partes = new List<string>();
            for (var atual = t; atual != null; atual = atual.parent) partes.Add(atual.name);
            partes.Reverse();
            return string.Join("/", partes);
        }
    }
}
