using UnityEngine;
using UnityEngine.UI;

namespace FavelaAmarela.Runtime.Enemies
{
    /// <summary>
    /// O que o Rito do Olhar põe na tela: a <b>barra do selo</b> no topo, com os marcos das fases,
    /// e a <b>vinheta amarela</b> que cresce enquanto o Damião é visto.
    ///
    /// <para><b>Montada em código pelo Rei, quando o rito começa</b> — e não autorada no HUD: a
    /// tela só existe no Trono, e uma peça de HUD persistente que só serve a uma sala seria mais
    /// uma coisa ligada em seis cenas para funcionar em uma. A textura da vinheta é gerada uma
    /// vez (64 × 64, gradiente radial): nenhum asset novo.</para>
    ///
    /// <para><b>Por que não o <c>TempestadeVisualOverlay</c></b>, que o plano sugeria "a
    /// confirmar": ele pinta a tela inteira de uma cor, por intensidade — é névoa, não borda. A
    /// vinheta precisa deixar o centro limpo, onde o jogador está olhando.</para>
    /// </summary>
    internal sealed class TelaDoRito
    {
        private static readonly Color CorDoSelo = new Color(0.86f, 0.74f, 0.25f, 1f);
        private static readonly Color CorDaVinheta = new Color(0.95f, 0.82f, 0.25f, 1f);
        private const float VinhetaMaxima = 0.55f;
        private const float SegundosAteOMaximo = 4f;

        private readonly GameObject _raiz;
        private readonly Image _preenchimento;
        private readonly RawImage _vinheta;
        private float _exposicao;

        /// <summary>Monta a tela, escondida, sob <paramref name="pai"/>.</summary>
        /// <param name="fonte">Fonte do rótulo; nula deixa a barra sem rótulo.</param>
        /// <param name="marcos">Os marcos das fases, de 0 a 100, desenhados na barra.</param>
        public TelaDoRito(Transform pai, Font fonte, float[] marcos)
        {
            _raiz = new GameObject("Rito_Tela", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            _raiz.transform.SetParent(pai, false);

            var canvas = _raiz.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 40;   // por cima do mundo e do HUD de jogo, abaixo das telas de fluxo

            var escala = _raiz.GetComponent<CanvasScaler>();
            escala.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            escala.referenceResolution = new Vector2(1920f, 1080f);
            escala.matchWidthOrHeight = 0.5f;

            _vinheta = Filho<RawImage>(_raiz.transform, "Vinheta", Vector2.zero, Vector2.one);
            _vinheta.texture = TexturaDaVinheta();
            _vinheta.color = new Color(1f, 1f, 1f, 0f);

            var fundo = Filho<Image>(_raiz.transform, "Selo_Fundo", new Vector2(0.32f, 0.925f), new Vector2(0.68f, 0.955f));
            fundo.color = new Color(0.06f, 0.06f, 0.05f, 0.85f);

            _preenchimento = Filho<Image>(fundo.transform, "Selo_Preenchimento", Vector2.zero, Vector2.one);
            _preenchimento.sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f));
            _preenchimento.type = Image.Type.Filled;
            _preenchimento.fillMethod = Image.FillMethod.Horizontal;
            _preenchimento.fillAmount = 0f;
            _preenchimento.color = CorDoSelo;
            _preenchimento.rectTransform.offsetMin = new Vector2(4f, 4f);
            _preenchimento.rectTransform.offsetMax = new Vector2(-4f, -4f);

            if (marcos != null)
                foreach (float m in marcos)
                {
                    float x = Mathf.Clamp01(m / 100f);
                    var traco = Filho<Image>(fundo.transform, "Marco", new Vector2(x, 0f), new Vector2(x, 1f));
                    traco.rectTransform.offsetMin = new Vector2(-1.5f, -6f);
                    traco.rectTransform.offsetMax = new Vector2(1.5f, 6f);
                    traco.color = new Color(0.95f, 0.93f, 0.85f, 0.9f);
                }

            if (fonte != null)
            {
                var rotulo = Filho<Text>(_raiz.transform, "Selo_Rotulo", new Vector2(0.32f, 0.955f), new Vector2(0.68f, 0.99f));
                rotulo.font = fonte;
                rotulo.text = "O SELO";
                rotulo.fontSize = 30;
                rotulo.alignment = TextAnchor.MiddleCenter;
                rotulo.color = CorDoSelo;
            }

            _raiz.SetActive(false);
        }

        /// <summary>Mostra ou esconde a tela inteira.</summary>
        public void Mostrar(bool visivel) => _raiz.SetActive(visivel);

        /// <summary>Atualiza a barra (0 a 100) e a vinheta de exposição.</summary>
        public void Atualizar(float selo, bool exposto, float dt)
        {
            _preenchimento.fillAmount = Mathf.Clamp01(selo / 100f);

            _exposicao = Mathf.Clamp(_exposicao + (exposto ? dt : -dt * 2f), 0f, SegundosAteOMaximo);
            var c = CorDaVinheta;
            c.a = VinhetaMaxima * (_exposicao / SegundosAteOMaximo);
            _vinheta.color = c;
        }

        private static T Filho<T>(Transform pai, string nome, Vector2 min, Vector2 max) where T : Graphic
        {
            var go = new GameObject(nome, typeof(RectTransform), typeof(T));
            go.transform.SetParent(pai, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var g = go.GetComponent<T>();
            g.raycastTarget = false;
            return g;
        }

        /// <summary>Gradiente radial: transparente no centro, opaco nas bordas.</summary>
        private static Texture2D TexturaDaVinheta()
        {
            const int n = 64;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[n * n];
            for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float dx = (x + 0.5f) / n * 2f - 1f;
                float dy = (y + 0.5f) / n * 2f - 1f;
                float d = Mathf.Sqrt(dx * dx + dy * dy) / 1.4142f;
                float a = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.45f, 1f, d));
                px[y * n + x] = new Color32(255, 255, 255, (byte)(a * 255));
            }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            return tex;
        }
    }
}
