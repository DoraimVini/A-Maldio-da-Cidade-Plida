using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace FavelaAmarela.Tests.PlayMode
{
    /// <summary>
    /// Mede a arena dos Portões <b>na tela</b>, pelo caminho de render de verdade.
    ///
    /// <para><b>Por que PlayMode e não a captura por <c>Camera.Render()</c> (2026-09-10).</b> A
    /// ferramenta <c>CapturaDeCena</c> renderiza a câmera à mão, em EditMode. Sob URP isso
    /// mentiu duas vezes no mesmo dia: sprites recém-criados com material <i>lit</i> amostravam
    /// uma textura de luz que ninguém escreveu (rochas pálidas onde havia colunas negras), e um
    /// sprite novo dentro de um atlas não reempacotado desenhava a região de <b>outro</b> sprite
    /// (glifos dourados onde devia haver escuridão). Só o loop de render normal, com a cena
    /// carregada, diz o que o jogador vê.</para>
    ///
    /// <para>Exige GPU: em batch com <c>-nographics</c> o teste se declara ignorado, em vez de
    /// medir uma tela preta e passar. E <c>WaitForEndOfFrame</c> não existe em batch mode, então
    /// a tela vem de <c>Camera.Render()</c> numa <c>RenderTexture</c> — depois de quadros reais
    /// terem corrido, o que é o que separa isto da captura de EditMode.</para>
    ///
    /// <para><b>Este é o primeiro teste PlayMode que carrega uma cena de verdade</b>, e por isso
    /// limpa atrás de si: descarrega a cena e derruba o que tiver sobrado com a tag
    /// <c>Player</c>. Sem isso o rig do teste seguinte (<c>OAltarResponde</c>) achava o Damião
    /// real em vez do boneco dele, e uma expectativa de log deixava de bater — aconteceu.</para>
    /// </summary>
    public sealed class AArenaDosPortoesNaTelaTests
    {
        private const string Cena = "Portoes_Das_Ruinas";
        private const string Captura = "Capturas/playmode/Portoes_Das_Ruinas.png";

        /// <summary>
        /// Onde a escuridão começa: a base do sprite <c>Escuridao_AlemDosPortoes</c>, lida da
        /// cena. Era uma constante (3) — a linha do portão da MINHA composição; o Vini moveu a
        /// escuridão para y ≈ 11 ao recompor os Portões em 2026-09-10, e um teste que mede numa
        /// altura fixa passa a medir chão comum e reprova o que está certo.
        /// </summary>
        private static float BaseDaEscuridao()
        {
            var escuridao = GameObject.Find("Escuridao_AlemDosPortoes");
            Assert.NotNull(escuridao, "'Escuridao_AlemDosPortoes' não está na cena dos Portões.");
            return escuridao.transform.position.y;
        }

        private static bool SemGpu => SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null;

        [UnityTearDown]
        public IEnumerator DescarregarACena()
        {
            var portoes = SceneManager.GetSceneByName(Cena);
            if (!portoes.IsValid() || !portoes.isLoaded) yield break;

            var vazia = SceneManager.CreateScene("Vazia_DepoisDosPortoes");
            SceneManager.SetActiveScene(vazia);
            yield return SceneManager.UnloadSceneAsync(portoes);

            // O que a cena promoveu a DontDestroyOnLoad e carrega a tag Player não pode
            // sobreviver para o rig seguinte.
            foreach (var go in GameObject.FindGameObjectsWithTag("Player"))
                Object.Destroy(go);

            yield return null;
        }

        [UnityTest]
        public IEnumerator AlemDosPortoes_EhMaisEscuroQueAArena()
        {
            if (SemGpu) Assert.Ignore("Sem GPU (-nographics): não há tela para medir.");

            yield return SceneManager.LoadSceneAsync(Cena, LoadSceneMode.Single);
            yield return null;

            var cam = Camera.main;
            Assert.NotNull(cam, "Sem Camera.main na cena dos Portões.");

            // A câmera nasce no Damião, em (0, -10): de lá o portão está fora da tela. A medida
            // é do CENTRO da arena, que é de onde se luta -- então o seguidor sai e a câmera
            // vai para lá. É a mesma vista da CapturaDeCena, agora pelo render de verdade.
            var seguidor = cam.GetComponent<FavelaAmarela.CameraSystem.IsometricCameraController>();
            if (seguidor != null) seguidor.enabled = false;
            cam.transform.position = new Vector3(0f, 0f, -10f);

            // Deixa Start e alguns LateUpdate correrem: sombras nascem, animadores acertam o quadro.
            for (int i = 0; i < 5; i++) yield return null;

            // A câmera olha do centro da escuridão para baixo: a faixa escura fica no topo do
            // quadro e a arena logo abaixo da base dela, na mesma largura.
            float base_ = BaseDaEscuridao();
            cam.transform.position = new Vector3(0f, base_ - 2f, -10f);
            yield return null;

            var tela = Fotografar(cam);
            Directory.CreateDirectory(Path.GetDirectoryName(Captura));
            File.WriteAllBytes(Captura, tela.EncodeToPNG());

            float alemDosPortoes = LuminanciaMedia(tela, cam, yMin: base_ + 1.5f, yMax: base_ + 3f);
            float naArena = LuminanciaMedia(tela, cam, yMin: base_ - 6f, yMax: base_ - 4f);

            TestContext.WriteLine($"além dos portões: {alemDosPortoes:F1} | arena: {naArena:F1} | " +
                                  $"captura em {Captura}");

            Assert.Less(alemDosPortoes, naArena * 0.8f,
                $"Além dos portões a tela tem luminância {alemDosPortoes:F1} e a arena {naArena:F1}. " +
                "A escuridão além do portão não está escurecendo nada — ou o sprite não " +
                "renderiza, ou está atrás do chão.");

            Object.Destroy(tela);
        }

        /// <summary>
        /// Renderiza a câmera numa <c>RenderTexture</c> de 640 × 360 (a referência das arenas) e
        /// lê os pixels. <c>ScreenCapture</c> precisaria de <c>WaitForEndOfFrame</c>, que batch
        /// mode não evoca.
        /// </summary>
        private static Texture2D Fotografar(Camera cam)
        {
            var rt = new RenderTexture(640, 360, 24);
            var anterior = cam.targetTexture;

            cam.targetTexture = rt;
            cam.Render();
            cam.targetTexture = anterior;

            var ativa = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false);
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            tex.Apply();
            RenderTexture.active = ativa;

            rt.Release();
            Object.Destroy(rt);
            return tex;
        }

        /// <summary>
        /// Luminância média (0–255) dos pixels da tela entre duas alturas de mundo, na faixa
        /// central de largura da câmera (para não pegar borda de quadro).
        /// </summary>
        private static float LuminanciaMedia(Texture2D tela, Camera cam, float yMin, float yMax)
        {
            var px = tela.GetPixels32();
            int w = tela.width, h = tela.height;

            // A textura tem a resolução da RenderTexture, não da janela: converte pela
            // ortografia da câmera, e não por WorldToScreenPoint.
            float meiaAltura = cam.orthographicSize;
            float cy = cam.transform.position.y;
            int y0 = Mathf.Clamp(Mathf.RoundToInt((yMin - cy + meiaAltura) / (2f * meiaAltura) * h), 0, h - 1);
            int y1 = Mathf.Clamp(Mathf.RoundToInt((yMax - cy + meiaAltura) / (2f * meiaAltura) * h), 0, h - 1);
            if (y1 < y0) (y0, y1) = (y1, y0);

            int x0 = w / 4, x1 = 3 * w / 4;
            double soma = 0;
            int n = 0;

            for (int y = y0; y <= y1; y++)
            for (int x = x0; x < x1; x++)
            {
                var c = px[y * w + x];
                soma += 0.2126 * c.r + 0.7152 * c.g + 0.0722 * c.b;
                n++;
            }

            Assert.Greater(n, 0, $"Faixa y∈[{yMin},{yMax}] caiu fora da tela.");
            return (float)(soma / n);
        }
    }
}
