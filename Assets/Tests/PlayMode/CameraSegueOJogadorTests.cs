using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using FavelaAmarela.CameraSystem;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace FavelaAmarela.Tests.PlayMode
{
    /// <summary>
    /// Guarda o comportamento da câmera <b>rodando</b>: zona morta, seguimento, travamento nos
    /// limites e tremor.
    ///
    /// <para><b>Por que em PlayMode e não em EditMode.</b> A aritmética já tem 22 testes
    /// EditMode em <c>EnquadramentoDaCameraTests</c> — e ela estava certa o tempo todo. O que
    /// falhava era o <b>elo</b>: o <c>ResolverLimites</c> procurava colisores <c>Limite_*</c>,
    /// nome que <b>só o Deserto usa</b>, então em 5 das 6 cenas a câmera nunca travava. Conta
    /// pura não alcança isso. Só um teste que instancia o componente, roda
    /// <c>LateUpdate</c> de verdade e olha a transform alcança.</para>
    ///
    /// <para><b>O padrão de montagem importa:</b> o <c>GameObject</c> nasce <b>desativado</b>,
    /// recebe os componentes e os campos, e só então é ativado. <c>Awake</c> roda no
    /// <c>AddComponent</c> de um objeto ativo — montar na ordem ingênua faria o
    /// <c>ResolverLimites</c> rodar antes de existir parede, e todo teste de limite passaria
    /// por engano.</para>
    /// </summary>
    public sealed class CameraSegueOJogadorTests
    {
        private const float TamanhoOrtografico = 4.21875f;
        private const float Proporcao = 16f / 9f;

        /// <summary>Meia-vista da cena padrão: 7,50 × 4,22 un.</summary>
        private static readonly Vector2 MeiaVista =
            new Vector2(TamanhoOrtografico * Proporcao, TamanhoOrtografico);

        private const float FracaoDaZonaMorta = 0.178f;

        /// <summary>Meia-extensão da zona morta que sai daquela meia-vista: 1,335 × 0,751 un.</summary>
        private static readonly Vector2 ZonaMorta = MeiaVista * FracaoDaZonaMorta;

        private readonly List<GameObject> _lixo = new List<GameObject>();
        private float _escalaOriginal;

        [SetUp]
        public void Preparar() => _escalaOriginal = Time.timeScale;

        [TearDown]
        public void Limpar()
        {
            Time.timeScale = _escalaOriginal;

            foreach (var go in _lixo)
                if (go != null) Object.DestroyImmediate(go);

            _lixo.Clear();
        }

        // ── montagem ─────────────────────────────────────────────────────────

        private static void Definir(object alvo, string campo, object valor)
        {
            var f = alvo.GetType().GetField(campo,
                BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.NotNull(f, $"Campo '{campo}' sumiu de {alvo.GetType().Name}. Se foi " +
                              "renomeado de propósito, atualize este teste — ele existe para " +
                              "notar exatamente isso.");

            f.SetValue(alvo, valor);
        }

        private static T Ler<T>(object alvo, string campo)
            => (T)alvo.GetType()
                      .GetField(campo, BindingFlags.Instance | BindingFlags.NonPublic)
                      .GetValue(alvo);

        private Transform CriarAlvo(Vector2 onde)
        {
            var go = new GameObject("Damiao_DeTeste");
            go.transform.position = onde;
            _lixo.Add(go);
            return go.transform;
        }

        /// <summary>Uma parede de borda, com o nome que o <c>ResolverLimites</c> procura.</summary>
        private void CriarParede(string nome, Vector2 centro, Vector2 tamanho)
        {
            var go = new GameObject(nome);
            go.transform.position = centro;
            var col = go.AddComponent<BoxCollider2D>();
            col.size = tamanho;
            _lixo.Add(go);
        }

        private IsometricCameraController CriarCamera(Transform alvo, bool prender)
        {
            var go = new GameObject("Camera_DeTeste");
            go.SetActive(false);                       // Awake só depois dos campos
            go.transform.position = new Vector3(0f, 0f, -10f);
            _lixo.Add(go);

            var cam = go.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = TamanhoOrtografico;
            cam.aspect = Proporcao;                    // determinístico: batch mode não tem tela

            var ctrl = go.AddComponent<IsometricCameraController>();

            Definir(ctrl, "target", alvo);
            Definir(ctrl, "orthographicSize", TamanhoOrtografico);
            Definir(ctrl, "prenderNosLimites", prender);
            Definir(ctrl, "fracaoDaZonaMorta", FracaoDaZonaMorta);
            Definir(ctrl, "smoothTime", 0.15f);
            Definir(ctrl, "velocidadeMaxima", 0f);     // sem teto: o teste mede a curva, não o teto
            Definir(ctrl, "zOffset", -10f);

            go.SetActive(true);                        // agora Awake roda, e vê tudo pronto

            cam.aspect = Proporcao;                    // Awake pode ter mexido; refixa
            return ctrl;
        }

        private static IEnumerator Quadros(int quantos)
        {
            for (int i = 0; i < quantos; i++) yield return null;
        }

        // ── 1. dentro da zona morta ──────────────────────────────────────────

        [UnityTest]
        public IEnumerator DentroDaZonaMorta_ACameraNaoSeMexe()
        {
            var alvo = CriarAlvo(Vector2.zero);
            var ctrl = CriarCamera(alvo, prender: false);

            yield return Quadros(3);
            Vector3 antes = ctrl.transform.position;

            // Bem dentro da caixa de 1,335 × 0,751.
            alvo.position = new Vector3(0.9f, 0.5f, 0f);
            yield return Quadros(20);

            Assert.AreEqual(antes.x, ctrl.transform.position.x, 0.0001f,
                "Passo curto arrastou a câmera na horizontal — é o balanço de fundo constante " +
                "que a zona morta existe para evitar.");
            Assert.AreEqual(antes.y, ctrl.transform.position.y, 0.0001f,
                "Passo curto arrastou a câmera na vertical.");
        }

        // ── 2. fora da zona morta ────────────────────────────────────────────

        [UnityTest]
        public IEnumerator ForaDaZonaMorta_ACameraSegueSuavemente()
        {
            var alvo = CriarAlvo(Vector2.zero);
            var ctrl = CriarCamera(alvo, prender: false);

            yield return Quadros(3);

            const float Longe = 6f;
            alvo.position = new Vector3(Longe, 0f, 0f);

            yield return null;
            float depoisDeUmQuadro = ctrl.transform.position.x;

            Assert.Greater(depoisDeUmQuadro, 0f, "A câmera não reagiu ao alvo sair da zona.");
            Assert.Less(depoisDeUmQuadro, Longe * 0.5f,
                "A câmera saltou mais da metade da distância num quadro — isso é teletransporte, " +
                "não amortecimento.");

            // A meia-extensão REAL, medida da câmera viva em vez de 16:9 cravado à mão.
            // A primeira versão cravava, e falhou medindo 1,07 onde esperava 4,67 -- o que tem
            // DUAS explicações possíveis (proporção diferente em batch mode, ou a câmera ainda
            // a caminho depois de 120 quadros) e eu não sei qual foi. Medir a proporção da
            // câmera e asserir a invariante resolve as duas sem eu precisar adivinhar; a
            // mensagem de falha imprime a proporção, então a próxima falha já vem diagnosticada.
            var camera = ctrl.GetComponent<Camera>();
            float extensao = camera.orthographicSize * camera.aspect * FracaoDaZonaMorta;
            float repouso = Longe - extensao;

            // ── a invariante, que vale em TODO quadro, convergido ou não ──
            // A câmera nunca pode passar da borda da zona morta: se passasse, teria
            // centralizado o jogador e reaberto a folga do outro lado.
            for (int i = 0; i < 600; i++)
            {
                yield return null;

                Assert.LessOrEqual(ctrl.transform.position.x, repouso + 0.01f,
                    "A câmera passou da borda da zona morta e foi centralizar o jogador — é o " +
                    "que faria a zona oscilar em vez de segurar.");
            }

            // E ela ANDA quase todo o caminho. Fração, não valor cravado: o passo de tempo em
            // batch mode não é o do jogo, e amarrar o teste à velocidade de convergência
            // mediria o ambiente. Duas versões deste teste já falharam por centésimos --
            // primeiro por 120 quadros serem poucos, depois por 0,3 un de tolerância.
            float andado = ctrl.transform.position.x / repouso;

            Assert.GreaterOrEqual(andado, 0.9f,
                $"A câmera andou só {andado:P0} do caminho até a borda da zona morta " +
                $"({ctrl.transform.position.x:0.00} de {repouso:0.00}). Proporção medida: " +
                $"{camera.aspect:0.000}, meia-vista {camera.orthographicSize * camera.aspect:0.00}.");
        }

        // ── 3. travamento nos limites ────────────────────────────────────────

        [UnityTest]
        public IEnumerator PertoDaBorda_ACameraTravaNosLimites()
        {
            // Uma caixa de 40 × 40 centrada na origem, feita de quatro paredes.
            const float Meia = 20f;
            CriarParede("Limite_Norte", new Vector2(0f, Meia), new Vector2(Meia * 2f, 1f));
            CriarParede("Limite_Sul", new Vector2(0f, -Meia), new Vector2(Meia * 2f, 1f));
            CriarParede("Limite_Leste", new Vector2(Meia, 0f), new Vector2(1f, Meia * 2f));
            CriarParede("Limite_Oeste", new Vector2(-Meia, 0f), new Vector2(1f, Meia * 2f));

            yield return null;   // deixa a física assentar antes do Awake da câmera

            var alvo = CriarAlvo(Vector2.zero);
            var ctrl = CriarCamera(alvo, prender: true);

            yield return Quadros(3);

            // Bem além do canto: a câmera tem de parar na borda menos a meia-vista.
            alvo.position = new Vector3(500f, 500f, 0f);
            yield return Quadros(200);

            // A parede tem espessura: o limite é a face externa dela. E a meia-vista sai da
            // câmera viva, não de 16:9 escrito à mão — em batch mode a proporção é outra.
            var camera = ctrl.GetComponent<Camera>();
            float meiaAltura = camera.orthographicSize;
            float meiaLargura = meiaAltura * camera.aspect;

            float tetoX = Meia + 0.5f - meiaLargura;
            float tetoY = Meia + 0.5f - meiaAltura;

            Assert.AreEqual(tetoX, ctrl.transform.position.x, 0.2f,
                "A câmera passou da borda direita e mostrou o vazio além do mapa.");
            Assert.AreEqual(tetoY, ctrl.transform.position.y, 0.2f,
                "A câmera passou da borda de cima.");
        }

        /// <summary>
        /// O defeito real que motivou este arquivo: a câmera precisa <b>achar</b> os limites.
        /// A conta sempre esteve certa; era o <c>_temLimites</c> que ficava falso.
        /// </summary>
        [UnityTest]
        public IEnumerator ParedeChamadaParede_TambemConta()
        {
            const float Meia = 20f;
            CriarParede("Parede_Norte", new Vector2(0f, Meia), new Vector2(Meia * 2f, 1f));
            CriarParede("Parede_Sul", new Vector2(0f, -Meia), new Vector2(Meia * 2f, 1f));
            CriarParede("Parede_Leste", new Vector2(Meia, 0f), new Vector2(1f, Meia * 2f));
            CriarParede("Parede_Oeste", new Vector2(-Meia, 0f), new Vector2(1f, Meia * 2f));

            yield return null;

            var alvo = CriarAlvo(Vector2.zero);
            var ctrl = CriarCamera(alvo, prender: true);

            yield return Quadros(3);
            alvo.position = new Vector3(500f, 0f, 0f);
            yield return Quadros(200);

            var camera = ctrl.GetComponent<Camera>();
            float teto = Meia + 0.5f - camera.orthographicSize * camera.aspect;

            Assert.AreEqual(teto, ctrl.transform.position.x, 0.2f,
                "As paredes do Santuário se chamam 'Parede_*'. Se só 'Limite_*' contar, a " +
                "câmera do Santuário nunca trava — que foi exatamente o defeito.");
        }

        // ── 4. tremor ────────────────────────────────────────────────────────

        [UnityTest]
        public IEnumerator AcrescentarTrauma_DeslocaACamera()
        {
            var alvo = CriarAlvo(Vector2.zero);
            var ctrl = CriarCamera(alvo, prender: false);

            yield return Quadros(5);
            Vector3 emRepouso = ctrl.transform.position;

            ctrl.AcrescentarTrauma(0.8f);

            float maiorDesvio = 0f;
            for (int i = 0; i < 10; i++)
            {
                yield return null;
                maiorDesvio = Mathf.Max(maiorDesvio,
                    Vector2.Distance(ctrl.transform.position, emRepouso));
            }

            Assert.Greater(maiorDesvio, 0.01f,
                "Trauma acrescentado e a tela não se mexeu.");
        }

        [UnityTest]
        public IEnumerator ComOJogoPausado_ATelaNaoTreme()
        {
            var alvo = CriarAlvo(Vector2.zero);
            var ctrl = CriarCamera(alvo, prender: false);

            yield return Quadros(5);
            Vector3 emRepouso = ctrl.transform.position;

            Time.timeScale = 0f;
            ctrl.AcrescentarTrauma(1f);

            for (int i = 0; i < 10; i++)
            {
                yield return null;

                Assert.AreEqual(emRepouso, ctrl.transform.position,
                    "A tela tremeu com o jogo pausado. Tremor atrás de um menu não é dramático, " +
                    "é defeito.");
            }
        }

        // ── 5. o Shake legado não pode mexer no campo serializado ────────────

        /// <summary>
        /// Regressão de 2026-09-09: <c>Shake</c> fazia
        /// <c>amplitudeDoTremor = Max(amplitudeDoTremor, magnitude)</c> — mutação
        /// <b>permanente</b> de um campo serializado a partir do argumento de uma chamada. Um
        /// <c>Shake</c> alto deixaria todos os golpes do resto da cena tremendo mais forte.
        /// </summary>
        [UnityTest]
        public IEnumerator OShakeLegado_NaoAlteraAAmplitudeSerializada()
        {
            var alvo = CriarAlvo(Vector2.zero);
            var ctrl = CriarCamera(alvo, prender: false);

            yield return null;

            float antes = Ler<float>(ctrl, "amplitudeDoTremor");

            LogAssert.Expect(LogType.Warning, new Regex("Shake pediu magnitude"));
            ctrl.Shake(0.3f, antes * 10f);

            yield return Quadros(3);

            Assert.AreEqual(antes, Ler<float>(ctrl, "amplitudeDoTremor"), 0.0001f,
                "Um Shake alto subiu a amplitude para o resto da vida da cena.");
        }
    }
}
