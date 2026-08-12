using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace FavelaAmarela.EditorTools
{
    /// <summary>
    /// Ferramenta de Editor. Atribui um sprite às <see cref="Image"/> do HUD que estão sem
    /// nenhum — bug crítico achado em playtest (2026-07-31): <b>a barra de Vitalidade nunca
    /// diminuía</b>, mesmo com o dano sendo aplicado corretamente.
    ///
    /// <para><b>A causa:</b> uma <c>Image</c> sem sprite não respeita
    /// <c>Image.Type.Filled</c> nem <c>fillAmount</c>. A Unity cai num caminho de desenho
    /// alternativo (<c>Graphic.OnPopulateMesh</c>) que produz sempre um retângulo cheio,
    /// ignorando o preenchimento. O código da <c>VitalidadeBar</c> estava correto o tempo
    /// todo — só não tinha como aparecer.</para>
    ///
    /// <para>Usa o sprite embutido da Unity (<c>UI/Skin/UISprite.psd</c>) como
    /// <b>placeholder</b>: a arte de verdade das barras continua pendente. O ponto aqui é
    /// tornar o dano legível, não fechar a arte.</para>
    ///
    /// <para>Idempotente: só toca em Image com sprite nulo.</para>
    /// </summary>
    public static class CorrigirSpritesDoHUD
    {
        [MenuItem("Tools/FavelaAmarela/Corrigir sprites faltando no HUD")]
        public static void Executar()
        {
            var sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            if (sprite == null)
            {
                Debug.LogError("[CorrigirSpritesDoHUD] Não consegui carregar o sprite embutido " +
                               "'UI/Skin/UISprite.psd'. Nada alterado.");
                return;
            }

            var imagens = Object.FindObjectsByType<Image>(
                FindObjectsInactive.Include);

            int corrigidas = 0;
            foreach (var img in imagens)
            {
                if (img.sprite != null) continue;

                Undo.RecordObject(img, "Atribuir sprite ao HUD");
                img.sprite = sprite;

                // Filled só funciona com sprite; garantir o tipo evita que uma barra volte
                // a "não diminuir" caso alguém mexa no Inspector depois.
                if (img.type == Image.Type.Filled && img.fillMethod != Image.FillMethod.Horizontal)
                    img.fillMethod = Image.FillMethod.Horizontal;

                EditorUtility.SetDirty(img);
                corrigidas++;
                Debug.Log($"[CorrigirSpritesDoHUD] '{img.name}' ({img.type}) ganhou sprite.", img);
            }

            if (corrigidas == 0)
            {
                Debug.Log("[CorrigirSpritesDoHUD] Nenhuma Image sem sprite — nada a fazer.");
                return;
            }

            var cena = EditorSceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(cena);
            EditorSceneManager.SaveScene(cena);

            Debug.Log($"[CorrigirSpritesDoHUD] {corrigidas} Image(s) corrigidas e cena salva. " +
                      "A barra de Vitalidade volta a responder ao dano.");
        }

        // ── Estilo da barra de Vitalidade igual à de Resiliência (2026-08-02) ───
        //
        // Consolidado aqui, não num arquivo novo: um .cs recém-criado às vezes não é
        // indexado pela Unity a tempo (sem .meta gerado, o menu não registra) — juntar num
        // arquivo que a Unity já rastreia evita depender de reiniciar o Editor.

        private const string CaminhoPrefabHUD = "Assets/FavelaAmarela/Art/UI/HUD_ResilienciaBar.prefab";
        private const string CaminhoSpriteFundoBarra = "Assets/FavelaAmarela/Art/UI/Sprites/bar_background.png";
        private const string CaminhoSpriteFillBarra = "Assets/FavelaAmarela/Art/UI/Sprites/bar_fill.png";

        /// <summary>
        /// Faz a barra de Vitalidade usar os mesmos sprites de pixel art da barra de
        /// Resiliência — pedido do Vini (2026-08-02): "quero a barra de vida igual da barra
        /// de resiliência".
        ///
        /// <para><b>Causa do visual diferente:</b> <c>Trilho</c> e <c>Preenchimento</c>
        /// (filhos de <c>Barra_Vitalidade</c>) usavam o <c>UISprite</c> genérico embutido da
        /// Unity — o retângulo arredondado padrão — em vez dos sprites reais
        /// (<c>bar_background.png</c>/<c>bar_fill.png</c>) que a barra de Resiliência já
        /// usa. Cor continua diferenciando as duas (vermelho = corpo, amarelo = mente); só a
        /// moldura/forma passa a ser a mesma peça de arte.</para>
        ///
        /// <para>Edita o <b>asset</b> do HUD via <c>EditPrefabContentsScope</c>, não uma
        /// instância de cena — propaga para toda cena que usa o HUD.</para>
        /// </summary>
        [MenuItem("Tools/FavelaAmarela/Igualar estilo da barra de Vitalidade a Resiliencia")]
        public static void IgualarEstiloDaBarraDeVitalidade()
        {
            var spriteFundo = AssetDatabase.LoadAssetAtPath<Sprite>(CaminhoSpriteFundoBarra);
            var spriteFill = AssetDatabase.LoadAssetAtPath<Sprite>(CaminhoSpriteFillBarra);

            if (spriteFundo == null || spriteFill == null)
            {
                Debug.LogError("[BarraDeVitalidade] Sprites de referência não encontrados " +
                                $"({CaminhoSpriteFundoBarra} / {CaminhoSpriteFillBarra}). Nada alterado.");
                return;
            }

            using (var escopo = new PrefabUtility.EditPrefabContentsScope(CaminhoPrefabHUD))
            {
                var barra = escopo.prefabContentsRoot.transform.Find("Barra_Vitalidade");
                if (barra == null)
                {
                    Debug.LogError("[BarraDeVitalidade] 'Barra_Vitalidade' não encontrada no HUD.");
                    return;
                }

                var trilho = barra.Find("Trilho")?.GetComponent<Image>();
                var preenchimento = barra.Find("Preenchimento")?.GetComponent<Image>();

                if (trilho != null)
                {
                    trilho.sprite = spriteFundo;
                    trilho.type = Image.Type.Simple;
                    trilho.color = Color.white; // a cor do trilho já vem do sprite, sem tingimento
                }

                if (preenchimento != null)
                {
                    preenchimento.sprite = spriteFill;
                    preenchimento.type = Image.Type.Filled;
                    preenchimento.fillMethod = Image.FillMethod.Horizontal;
                    // Cor (vermelho) fica como está — é o que diferencia Vitalidade de
                    // Resiliência; só a moldura/forma muda.
                }

                Debug.Log("[BarraDeVitalidade] Trilho e Preenchimento agora usam os sprites " +
                          "reais da barra de Resiliência.");
            }
        }
    }
}

