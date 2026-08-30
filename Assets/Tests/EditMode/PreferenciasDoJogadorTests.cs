using System;
using System.IO;
using NUnit.Framework;
using FavelaAmarela.Core.Preferencias;

namespace FavelaAmarela.Tests.EditMode
{
    /// <summary>
    /// Guarda as <b>opções do jogador</b> — volume, janela e sincronização de quadros.
    ///
    /// <para><b>Por que existem (2026-08-29).</b> O projeto não tinha controle de volume nem
    /// opção de vídeo nenhuma. Para um jogo que vai ser vendido isso não é polimento: é a
    /// primeira coisa que alguém procura quando o som está alto demais, e a ausência vira
    /// análise negativa antes de qualquer julgamento sobre o jogo.</para>
    /// </summary>
    public sealed class PreferenciasDoJogadorTests
    {
        // ── Os limites ────────────────────────────────────────────────────────

        [Test]
        public void OVolume_FicaEntreZeroEUm()
        {
            var p = new PreferenciasDoJogador();

            p.VolumeGeral = 5f;
            Assert.AreEqual(1f, p.VolumeGeral, 1e-4f, "Volume acima de 1 estouraria o som.");

            p.VolumeGeral = -3f;
            Assert.AreEqual(0f, p.VolumeGeral, 1e-4f, "Volume negativo não significa nada.");
        }

        /// <summary>
        /// Arquivo de preferências corrompido não pode travar a partida. Zero quadros por
        /// segundo é um jogo congelado; qualquer valor inválido vira "sem limite".
        /// </summary>
        [Test]
        public void UmLimiteDeQuadrosInvalido_ViraSemLimite()
        {
            var p = new PreferenciasDoJogador();

            foreach (int ruim in new[] { 0, -7, -1 })
            {
                p.LimiteDeQuadros = ruim;
                Assert.AreEqual(PreferenciasDoJogador.SemLimiteDeQuadros, p.LimiteDeQuadros,
                    $"{ruim} quadros por segundo não é um estado jogável.");
            }

            p.LimiteDeQuadros = 60;
            Assert.AreEqual(60, p.LimiteDeQuadros, "Um limite válido tem de passar.");
        }

        // ── A regra que a documentação da Unity impõe ─────────────────────────

        /// <summary>
        /// <b>A doc da Unity 6.4 é explícita:</b> <i>"If QualitySettings.vSyncCount is set to 0,
        /// then Application.targetFrameRate chooses a target frame rate. If vSyncCount != 0,
        /// then targetFrameRate is ignored."</i>
        ///
        /// <para>Uma tela que mostrasse "60 fps" com VSync ligada estaria mentindo — o motor
        /// ignora aquele número. <c>LimiteEfetivoDeQuadros</c> existe para a interface poder
        /// dizer a verdade.</para>
        /// </summary>
        [Test]
        public void ComVSyncLigada_OLimiteDeQuadrosNaoVale()
        {
            var p = new PreferenciasDoJogador { LimiteDeQuadros = 60 };

            p.SincronizacaoVertical = true;
            Assert.AreEqual(PreferenciasDoJogador.SemLimiteDeQuadros, p.LimiteEfetivoDeQuadros,
                "Com VSync ligada a Unity IGNORA o targetFrameRate. Mostrar 60 seria a " +
                "interface descrevendo um estado que o motor não tem.");

            p.SincronizacaoVertical = false;
            Assert.AreEqual(60, p.LimiteEfetivoDeQuadros,
                "Com VSync desligada o limite volta a valer.");
        }

        /// <summary>
        /// VSync ligada é o padrão <b>por recomendação explícita da documentação</b>: é
        /// sincronização por hardware, sem o microstuttering do limite por software.
        /// </summary>
        [Test]
        public void OPadraoDeFabrica_LigaASincronizacaoVertical()
        {
            var p = new PreferenciasDoJogador();

            Assert.IsTrue(p.SincronizacaoVertical,
                "A doc da Unity 6.4 recomenda vSyncCount sobre targetFrameRate: " +
                "'targetFrameRate is a software-based timing method and is subject to " +
                "microstuttering'.");

            Assert.Greater(p.VolumeGeral, 0f, "Nascer mudo faria o jogo parecer quebrado.");
            Assert.Less(p.VolumeGeral, 1f, "Nascer no máximo é agressivo com quem usa fone.");
        }

        // ── Eventos ───────────────────────────────────────────────────────────

        [Test]
        public void MudarUmaPreferencia_AvisaUmaVez()
        {
            var p = new PreferenciasDoJogador();
            int avisos = 0;
            p.OnMudou += () => avisos++;

            p.VolumeGeral = 0.5f;
            Assert.AreEqual(1, avisos);

            // Escrever o MESMO valor não é mudança: sem isto, arrastar uma barra dispararia
            // uma reconfiguração do motor por quadro.
            p.VolumeGeral = 0.5f;
            Assert.AreEqual(1, avisos, "Escrever o mesmo valor não pode disparar evento.");
        }

        /// <summary>
        /// Carregar quatro preferências do disco tem de ser <b>um</b> evento. Quatro disparos
        /// reconfigurariam o motor quatro vezes e a janela piscaria no arranque.
        /// </summary>
        [Test]
        public void CarregarDoDisco_DisparaUmEventoSo()
        {
            var p = new PreferenciasDoJogador();
            int avisos = 0;
            p.OnMudou += () => avisos++;

            p.Restaurar(volume: 0.3f, telaCheia: false, vsync: false, limite: 144);

            Assert.AreEqual(1, avisos, "Restaurar tem de ser atômico.");
            Assert.AreEqual(0.3f, p.VolumeGeral, 1e-4f);
            Assert.IsFalse(p.TelaCheia);
            Assert.AreEqual(144, p.LimiteEfetivoDeQuadros);
        }

        [Test]
        public void Restaurar_VoltaAoPadraoDeFabrica()
        {
            var p = new PreferenciasDoJogador();
            p.Restaurar(volume: 0f, telaCheia: false, vsync: false, limite: 30);

            p.Restaurar();

            Assert.IsTrue(p.SincronizacaoVertical);
            Assert.IsTrue(p.TelaCheia);
            Assert.Greater(p.VolumeGeral, 0f);
        }

        // ── A ligação com o jogo ──────────────────────────────────────────────

        /// <summary>
        /// A preferência precisa <b>chegar ao som</b>. O <c>MixerDeAudio</c> é o ponto único por
        /// onde todo áudio passa — é o único lugar onde ler o volume faz o jogo inteiro
        /// obedecer. Uma barra que não passa por ali é uma barra decorativa, que é como a barra
        /// de vida passou meses.
        /// </summary>
        [Test]
        public void OVolume_ChegaAoMixerDeAudio()
        {
            string fonte = File.ReadAllText("Assets/Scripts/Audio/MixerDeAudio.cs");

            StringAssert.Contains("PreferenciasBridge.Instancia", fonte,
                "O MixerDeAudio parou de consultar a preferência do jogador — a barra de volume " +
                "vira decorativa e o campo do Inspector volta a mandar.");

            StringAssert.Contains("float volume = VolumeGeral *", fonte,
                "O cálculo do volume voltou a usar o campo serializado direto.");
        }

        /// <summary>
        /// As preferências têm de nascer <b>antes de qualquer cena</b>, inclusive o menu
        /// principal — que é justamente onde o jogador vai procurar as opções. E em arquivo
        /// próprio: apagar o save para recomeçar não pode zerar o volume ajustado.
        /// </summary>
        [Test]
        public void APonte_NasceSozinhaEGuardaForaDoSave()
        {
            string fonte = File.ReadAllText(
                "Assets/Scripts/Preferencias/PreferenciasBridge.cs");

            StringAssert.Contains("RuntimeInitializeOnLoadMethod", fonte,
                "As preferências deixaram de nascer sozinhas — passam a valer só nas cenas " +
                "onde alguém lembrou de pôr o componente.");

            StringAssert.Contains("BeforeSceneLoad", fonte,
                "Nascem depois da cena: o menu principal abriria sem as opções aplicadas.");

            StringAssert.Contains("preferencias.json", fonte,
                "O arquivo próprio sumiu. Dentro do save, começar uma peregrinação nova zeraria " +
                "o volume que a pessoa ajustou.");
        }

        /// <summary>
        /// Só um lugar no projeto pode tocar <c>vSyncCount</c>, <c>targetFrameRate</c> e
        /// <c>Screen.fullScreen</c>. Espalhá-los produziria o mesmo desacordo silencioso que os
        /// dois números de dano por inimigo produziam.
        /// </summary>
        [Test]
        public void AConfiguracaoDeVideo_TemUmDonoSo()
        {
            var intrusos = new System.Collections.Generic.List<string>();

            foreach (var caminho in Directory.GetFiles("Assets/Scripts", "*.cs",
                                                       SearchOption.AllDirectories))
            {
                if (caminho.Replace('\\', '/').EndsWith("Preferencias/PreferenciasBridge.cs"))
                    continue;

                string src = File.ReadAllText(caminho);

                foreach (var api in new[] { "QualitySettings.vSyncCount",
                                            "Application.targetFrameRate" })
                {
                    if (src.Contains(api + " ="))
                        intrusos.Add($"{Path.GetFileName(caminho)} escreve {api}");
                }
            }

            Assert.IsEmpty(intrusos,
                "Configuração de vídeo escrita fora da PreferenciasBridge:" +
                Environment.NewLine + "  " + string.Join(Environment.NewLine + "  ", intrusos) +
                Environment.NewLine + "Dois donos divergem, e o motor fica num estado que a " +
                "interface não descreve.");
        }
    }
}
