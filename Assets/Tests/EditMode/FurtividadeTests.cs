using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using FavelaAmarela.Player;

namespace FavelaAmarela.Tests.EditMode
{
    /// <summary>
    /// Guarda o <b>pilar do jogo</b> — e ele nunca tinha sido guardado.
    ///
    /// <para><b>O defeito, achado em 2026-08-27.</b> A percepção dos inimigos é 100% sonora,
    /// mas <c>EnemyPerception.HandleSomEmitido</c> comparava a distância <b>apenas</b> com o
    /// próprio <c>raioAudicao</c> (10 no Cultista) e <b>descartava</b> o
    /// <c>SomEmitido.RaioEfetivo</c>. Agachado (2,0) e correndo (8,5) eram ouvidos
    /// <b>exatamente igual</b>. Modo Furtivo, corrida e o abafamento da tempestade não tinham
    /// efeito nenhum em jogo.</para>
    ///
    /// <para><b>E o código certo já existia, testado:</b>
    /// <c>CultistaFSM.ReceberEstimuloSonoro</c> sempre comparou com <c>raioEfetivo</c> — mas ela
    /// só é instanciada em teste. O caminho vivo em produção é <c>CultistaAI</c> +
    /// <c>EnemyPerception</c>, que é o que ignorava. Um POCO testado e morto: o modo de falha
    /// desta casa, na sua forma mais cara.</para>
    /// </summary>
    public sealed class FurtividadeTests
    {
        // ── A curva de ruído ──────────────────────────────────────────────────

        private const float RuidoFurtivo = 2.0f;
        private const float RuidoAndando = 5.5f;
        private const float RuidoCorrendo = 8.5f;

        /// <summary>
        /// A ordem é o design e precisa sobreviver a qualquer recalibragem: agachar tem de
        /// valer a pena, e correr tem de custar.
        /// </summary>
        [Test]
        public void AgacharFazMenosBarulhoQueAndar_QueFazMenosQueCorrer()
        {
            Assert.Less(RuidoFurtivo, RuidoAndando,
                "Se agachar não for mais silencioso que andar, o modo Furtivo não tem função.");

            Assert.Less(RuidoAndando, RuidoCorrendo,
                "Correr precisa custar alguma coisa, senão andar não é uma escolha.");
        }

        /// <summary>
        /// <b>O guarda que impede a tempestade de conceder invisibilidade.</b>
        ///
        /// <para>Sem o piso, tempestade cheia levava o Furtivo de 2,0 para <b>0,8</b> — menos
        /// que a própria pegada do Cultista (0,70 × 0,35). Seria preciso encostar nele para ser
        /// ouvido. Isso só passou a importar quando os inimigos começaram a de fato usar o raio
        /// do som: antes, o abafamento não fazia efeito nenhum.</para>
        /// </summary>
        [Test]
        public void ATempestade_AjudaMuitoMasNaoDaInvisibilidade()
        {
            float furtivoNaTempestade =
                PlayerStealthState.AplicarAbafamentoTempestade(RuidoFurtivo, 1f);

            Assert.GreaterOrEqual(furtivoNaTempestade, PlayerStealthState.PisoDeRuidoEmMovimento,
                "Tempestade cheia abafou o Furtivo abaixo do piso: quem se move nunca pode ser " +
                "literalmente inaudível.");

            Assert.Less(furtivoNaTempestade, RuidoFurtivo,
                "A tempestade precisa ABAFAR de verdade — se não reduz nada, o pilar de " +
                "'stealth invertido' do Deserto deixa de existir.");
        }

        /// <summary>
        /// Parado não faz barulho, e o piso não pode inventar ruído do nada.
        /// </summary>
        [Test]
        public void Parado_NaoEmiteRuidoNenhum()
        {
            Assert.AreEqual(0f, PlayerStealthState.AplicarAbafamentoTempestade(0f, 0f), 0.0001f);
            Assert.AreEqual(0f, PlayerStealthState.AplicarAbafamentoTempestade(0f, 1f), 0.0001f,
                "O piso é um MÍNIMO para ruído real, não uma fonte de ruído.");
        }

        /// <summary>
        /// A tempestade não pode inverter a ordem: correr na pior tempestade ainda tem de ser
        /// mais arriscado que agachar no tempo limpo. Sem isso, a decisão de modo de movimento
        /// deixa de significar algo.
        /// </summary>
        [Test]
        public void ATempestade_NaoInverteAEscolhaDeMovimento()
        {
            float correndoNaTempestade =
                PlayerStealthState.AplicarAbafamentoTempestade(RuidoCorrendo, 1f);

            Assert.Greater(correndoNaTempestade, RuidoFurtivo,
                "Correr na tempestade cheia ficou MAIS silencioso que agachar no tempo limpo — " +
                "aí não há motivo para agachar nunca.");
        }

        // ── O caminho vivo tem de usar o raio do som ──────────────────────────

        /// <summary>
        /// <b>O guarda que teria evitado tudo isto.</b> O caminho de percepção que roda em
        /// produção precisa consultar o alcance do SOM, não só a acuidade do inimigo.
        /// </summary>
        [Test]
        public void OCaminhoVivoDaPercepcao_UsaORaioDoSom()
        {
            string codigo = File.ReadAllText(
                "Assets/Scripts/Enemies/Components/EnemyPerception.cs");

            StringAssert.Contains("som.RaioEfetivo", codigo,
                "EnemyPerception voltou a ignorar o raio do som. Agachado e correndo passam a " +
                "ser ouvidos igual, e a furtividade — o pilar do jogo — deixa de existir.");
        }

        // ── Áudio: a cena tem de ter ouvido ───────────────────────────────────

        /// <summary>
        /// Toda cena do build precisa de <b>exatamente um</b> <c>AudioListener</c>.
        ///
        /// <para>Três cenas não tinham nenhum — incluindo <c>Deserto_Hali</c> e
        /// <c>Playtest_RuinasPalidas</c>, que juntas são metade do Vertical Slice. Elas tinham
        /// <c>MixerDeAudio</c> e <c>AudioDeStealth</c> montados: tudo tocava e <b>nada era
        /// ouvido</b>. A Unity vinha avisando no console a cada som, e ninguém tinha olhado.</para>
        ///
        /// <para>Dois é pior que nenhum: a Unity desliga um arbitrariamente.</para>
        /// </summary>
        [Test]
        public void TodaCenaDoBuild_TemExatamenteUmAudioListener()
        {
            var problemas = new List<string>();

            foreach (var cena in EditorBuildSettings.scenes.Where(c => c.enabled))
            {
                if (!File.Exists(cena.path))
                {
                    problemas.Add($"{Path.GetFileName(cena.path)}: ausente no disco");
                    continue;
                }

                // AudioListener é um componente nativo (!u!81) — a contagem sai do YAML, sem
                // precisar abrir a cena.
                int n = Regex.Matches(File.ReadAllText(cena.path), @"^AudioListener:",
                                      RegexOptions.Multiline).Count;

                if (n != 1)
                    problemas.Add($"{Path.GetFileName(cena.path)}: {n} AudioListener" +
                                  (n == 0 ? " — a cena é MUDA" : " — a Unity desliga um sozinha"));
            }

            Assert.IsEmpty(problemas,
                "Cena(s) com problema de ouvinte:" + Environment.NewLine + "  " +
                string.Join(Environment.NewLine + "  ", problemas) + Environment.NewLine +
                "Conserto: 'Tools/FavelaAmarela/Áudio: garantir um AudioListener por cena'.");
        }
    }
}
