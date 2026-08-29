using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using FavelaAmarela.Core.Combat;
using FavelaAmarela.Inventario;

namespace FavelaAmarela.Tests.EditMode
{
    /// <summary>
    /// Trava os <b>alvos de dificuldade dos encontros</b> em número de golpes — a unidade em que
    /// o design foi escrito, e a única que um humano lê sem traduzir.
    ///
    /// <para><b>Por que em golpes e não em dano.</b> "Cultista 20 de Ataque" não diz nada
    /// sozinho: depende da Defesa do Damião, da fórmula de mitigação e da Vitalidade dele, e as
    /// três já mudaram. "Cinco golpes até o Colapso" sobrevive a todas as três. Quando o alvo é
    /// escrito em dano, mexer na Defesa quebra o encontro em silêncio — foi exatamente o que
    /// aconteceu: a <c>ficha_de_atributos.md</c> documentava 5 golpes com uma Defesa <b>4</b>
    /// que virou <b>6</b>, e ninguém refez a conta.</para>
    ///
    /// <para><b>O que estes testes protegem, concretamente.</b> Cada inimigo carregava
    /// <b>dois</b> números de dano — o <c>Ataque</c> da ficha e um campo serializado no
    /// <c>MonoBehaviour</c> — e só o segundo rodava. Ao unificá-los em 2026-08-28, o Cultista
    /// quase enfraqueceu de 20 para 14 sem que nenhum teste notasse.</para>
    /// </summary>
    public sealed class EncontrosCalibradosTests
    {
        private static FichaAtributosConfig Ficha(string nome)
        {
            var f = AssetDatabase.LoadAssetAtPath<FichaAtributosConfig>(
                $"Assets/FavelaAmarela/Config/Ficha_{nome}.asset");

            Assert.IsNotNull(f, $"Ficha_{nome}.asset não existe.");
            return f;
        }

        private static BaseDeArma Familia(string nome) =>
            AssetDatabase.LoadAssetAtPath<BaseDeArma>(
                $"Assets/FavelaAmarela/Config/Armas/BaseArma_{nome}.asset");

        /// <summary>Golpes para levar <paramref name="alvo"/> de cheio a zero.</summary>
        private static int Golpes(FichaDeAtributos alvo, float ataqueBruto)
        {
            float porGolpe = MitigacaoDeDano.Aplicar(ataqueBruto, alvo.Defesa);
            Assert.Greater(porGolpe, 0f, "O golpe não atravessa a Defesa do alvo.");

            return (int)Math.Ceiling(alvo.VitalidadeMax / porGolpe);
        }

        /// <summary>
        /// O valor esperado do ataque básico da arma: média da faixa, corrigida por precisão e
        /// por crítico. É o número comparável com o <c>Ataque</c> de uma ficha.
        /// </summary>
        private static float EsperadoDe(BaseDeArma familia, int nivelDoItem)
        {
            var p = familia.PerfilNoNivel(nivelDoItem);

            float percentual = familia.Habilidade.EfeitosDoBasico
                .Where(e => e.Tipo == TipoDeEfeito.DanoDaArma)
                .Select(e => e.Valor)
                .DefaultIfEmpty(0f)
                .Sum();

            float media = (p.DanoMin + p.DanoMax) * 0.5f * percentual;
            return media * p.Precisao * (1f + p.ChanceCritica * (p.MultiplicadorCritico - 1f));
        }

        // ── Cultista → Damião ─────────────────────────────────────────────────

        /// <summary>
        /// <b>O encontro que ensina o jogo.</b> O Cultista tem de ser punitivo o bastante para
        /// empurrar ao stealth e lento o bastante para haver janela de fuga.
        ///
        /// <para>Alvo autorado: entre <b>4 e 8</b> golpes no nível 1. Com Ataque 20 contra a
        /// Defesa 6 do Damião são 14 por golpe, e 100 de Vitalidade dão <b>8</b>. A faixa é
        /// larga de propósito — o número exato é decisão de playtest do Vini, o que este teste
        /// impede é que ele mude por acidente ao mexer em outra coisa.</para>
        /// </summary>
        [Test]
        public void OCultista_DerrubaODamiaoEmQuatroAOitoGolpes()
        {
            var damiao = Ficha("Damiao").CriarFicha(1);
            int golpes = Golpes(damiao, Ficha("Cultista").Ataque);

            Assert.GreaterOrEqual(golpes, 4,
                $"O Cultista derruba o Damião em {golpes} golpes no nível 1 — não sobra janela " +
                "de fuga, e o Deserto tem onze deles.");

            Assert.LessOrEqual(golpes, 8,
                $"O Cultista precisa de {golpes} golpes — deixou de empurrar ao stealth, que é " +
                "o núcleo tonal do jogo.");
        }

        /// <summary>
        /// <b>O guarda do número que quase se perdeu.</b> A ficha do Cultista autorava
        /// <c>Ataque 14</c> enquanto o <c>EnemyCombat</c> batia com <c>20</c> — e só o 20 rodava.
        /// Unificar os dois na ficha, sem corrigir a ficha, teria enfraquecido o inimigo em 30%
        /// numa mudança que se anunciava como refatoração.
        /// </summary>
        [Test]
        public void ODanoDoCultista_ContinuaSendoOQueOJogoJogava()
        {
            Assert.AreEqual(20f, Ficha("Cultista").Ataque, 0.01f,
                "O Ataque do Cultista mudou. Se foi decisão de balanceamento, ajuste este teste " +
                "junto e diga por quê; se foi efeito colateral, o Deserto inteiro mudou de " +
                "dificuldade sem ninguém pedir.");
        }

        // ── Damião → Cultista ─────────────────────────────────────────────────

        /// <summary>
        /// <b>2 a 4 golpes</b> com qualquer arma da Tumba, no nível 1. Menos que isso e o
        /// combate aberto vira a resposta óbvia para tudo; mais e o jogador que escolheu lutar
        /// é punido por escolher.
        ///
        /// <para>O Estilete pode chegar a 4 — ele bate 2,3× mais rápido que o Alfanje, e essa é
        /// a identidade dele.</para>
        /// </summary>
        [TestCase("Alfanje", 3)]
        [TestCase("Cravo", 3)]
        [TestCase("LaminaFina", 4)]
        public void CadaArmaDaTumba_AbateOCultistaNoRitmoDela(string arma, int teto)
        {
            var familia = Familia(arma);
            Assert.IsNotNull(familia, $"BaseArma_{arma}.asset não existe.");

            var cultista = Ficha("Cultista").CriarFicha(1);
            float esperado = EsperadoDe(familia, nivelDoItem: 1);
            int golpes = Golpes(cultista, esperado);

            Assert.GreaterOrEqual(golpes, 2,
                $"{arma} abate um Cultista em {golpes} golpe(s) no nível 1 — o encontro que " +
                "ensina o combate acaba antes de ensinar.");

            Assert.LessOrEqual(golpes, teto,
                $"{arma} precisa de {golpes} golpes contra um Cultista no nível 1 (teto {teto}). " +
                "Lutar deixou de ser uma escolha viável contra a tropa comum.");
        }

        // ── A ficha é a fonte, em todo o elenco ───────────────────────────────

        /// <summary>
        /// <b>Nenhum inimigo pode ter dois números de dano.</b> Enquanto tiver, rebalancear pela
        /// ficha não muda nada em jogo — e foi assim que a <c>ficha_de_atributos.md</c> passou
        /// meses documentando contas sobre valores que ninguém lia.
        ///
        /// <para>O campo serializado continua existindo como <b>fallback</b> para a unidade sem
        /// ficha; o que não pode existir é um caminho que o prefira à ficha.</para>
        /// </summary>
        [Test]
        public void NenhumAtacante_PrefereOCampoLocalAFicha()
        {
            var desalinhados = new List<string>();

            foreach (var (arquivo, campo) in new[]
                     {
                         ("Assets/Scripts/Enemies/Components/EnemyCombat.cs", "danoDoGolpe"),
                         ("Assets/Scripts/Enemies/ByakheeAI.cs", "danoDasGarras"),
                     })
            {
                if (!File.Exists(arquivo)) { desalinhados.Add($"{arquivo}: ausente"); continue; }

                string fonte = File.ReadAllText(arquivo);

                if (!fonte.Contains("Atributos.Ataque"))
                    desalinhados.Add($"{Path.GetFileName(arquivo)}: não consulta a ficha");

                // O campo tem de aparecer como fallback dentro da propriedade, e não solto no
                // sítio do golpe. Duas ocorrências: a declaração e o fallback.
                int usos = System.Text.RegularExpressions.Regex
                    .Matches(fonte, $@"\b{campo}\b").Count;

                if (usos > 2)
                    desalinhados.Add($"{Path.GetFileName(arquivo)}: '{campo}' aparece {usos} " +
                                     "vezes — alguém voltou a usá-lo direto no golpe");
            }

            Assert.IsEmpty(desalinhados,
                "Atacante(s) com dano fora da ficha:" + Environment.NewLine + "  " +
                string.Join(Environment.NewLine + "  ", desalinhados));
        }
    }
}
