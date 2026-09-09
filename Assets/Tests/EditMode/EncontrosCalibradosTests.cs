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
        /// o combate ter peso e lento o bastante para haver janela de reação.
        ///
        /// <para><b>Faixa antiga: 4 a 8 golpes</b>, e o motivo escrito era "empurrar ao
        /// stealth, que é o núcleo tonal do jogo". Com Ataque 20 contra a Defesa 6 do Damião
        /// eram 14 por golpe, e 100 de Vitalidade davam exatamente 8 — o teto da faixa.</para>
        ///
        /// <para><b>Faixa nova: 4 a 12, e o motivo antigo caducou (2026-09-09).</b> A
        /// furtividade <b>deixou de ser pilar</b> em 2026-09-01 — o gênero passou a ser "combate
        /// + exploração", e o <c>CLAUDE.md</c> registra que a única ferramenta de stealth que
        /// existe é a tempestade abafando som. A faixa estava calibrada para empurrar a um pilar
        /// que foi aposentado.</para>
        ///
        /// <para><b>E o playtest confirmou pelo outro lado.</b> O Vini: <i>"não tem dano e
        /// armadura para se manter vivo contra os dois cultistas do início"</i>. Com Ataque 16
        /// são 10 por golpe e 10 golpes — e a curva de armadura passa a significar algo (10 sem
        /// nada, 6 com o conjunto inicial, 3 com o Sepulto). A 14, que foi a primeira sugestão,
        /// o conjunto Sepulto já bateria no piso de 15% e Yhtill e Set deixariam de valer
        /// qualquer coisa contra cultista.</para>
        /// </summary>
        [Test]
        public void OCultista_DerrubaODamiaoEmQuatroADozeGolpes()
        {
            var damiao = Ficha("Damiao").CriarFicha(1);
            int golpes = Golpes(damiao, Ficha("Cultista").Ataque);

            Assert.GreaterOrEqual(golpes, 4,
                $"O Cultista derruba o Damião em {golpes} golpes no nível 1 — não sobra janela " +
                "de fuga, e o Deserto tem onze deles.");

            Assert.LessOrEqual(golpes, 12,
                $"O Cultista precisa de {golpes} golpes — o combate de abertura perdeu o peso. " +
                "A faixa foi de 4-8 para 4-12 em 2026-09-09, quando a furtividade deixou de ser " +
                "pilar; ela não é para crescer de novo sem uma decisão igual.");
        }

        /// <summary>
        /// <b>O guarda do número que quase se perdeu.</b> A ficha do Cultista autorava
        /// <c>Ataque 14</c> enquanto o <c>EnemyCombat</c> batia com <c>20</c> — e só o 20 rodava.
        /// Unificar os dois na ficha, sem corrigir a ficha, teria enfraquecido o inimigo em 30%
        /// numa mudança que se anunciava como refatoração.
        ///
        /// <para><b>20 → 16 em 2026-09-09, e desta vez por decisão.</b> O guarda funcionou: ele
        /// pediu "se foi decisão de balanceamento, ajuste este teste junto e diga por quê", e é
        /// o que esta nota faz. O motivo é playtest do Vini — <i>"não tem dano e armadura para
        /// se manter vivo contra os dois cultistas do início"</i> — e a medição confirmou: 20
        /// bruto contra Defesa 6 passava 14, e 100 de Vitalidade morriam em 7 golpes, pouco mais
        /// de quatro segundos com dois cultistas.</para>
        ///
        /// <para><b>O prefab foi junto.</b> O <c>danoDoGolpe</c> do <c>EnemyCombat</c> é
        /// fallback e só vale quando a ficha traz Ataque 0 — mas deixá-lo em 20 recriaria
        /// exatamente a divergência que esta classe existe para impedir.</para>
        /// </summary>
        [Test]
        public void ODanoDoCultista_ContinuaSendoOQueOJogoJogava()
        {
            Assert.AreEqual(16f, Ficha("Cultista").Ataque, 0.01f,
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
        [TestCase("Maca", 3)]
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

        // ── O Templo do Povo Serpente ─────────────────────────────────────────

        /// <summary>
        /// Os três atores do Templo (Dungeon 2) precisam de ficha para <b>poder ser
        /// abatidos</b>.
        ///
        /// <para><b>O que a auditoria de 2026-08-29 encontrou:</b> os três são
        /// <c>MonoBehaviour</c> puros, sem <c>EnemyBase</c> e sem <c>IDanificavel</c> — eles
        /// <b>causam dano e não podem receber</b>. Nunca apareceu em jogo porque não há prefab
        /// nem cena do Templo; apareceria no dia em que a fase fosse montada, como "o inimigo
        /// não morre".</para>
        /// </summary>
        [TestCase("Sseth")]
        [TestCase("Nagaraja")]
        [TestCase("AvatarDeSet")]
        public void OsAtoresDoTemplo_TemFichaComVitalidade(string nome)
        {
            var ficha = Ficha(nome);

            Assert.Greater(ficha.VitalidadeMax, 0f,
                $"Ficha_{nome} sem Vitalidade — o ator não tem como ser abatido.");

            Assert.Greater(ficha.Ataque, 0f,
                $"Ficha_{nome} sem Ataque — o dano cairia no campo local do script, que é " +
                "exatamente o que a unificação desfez.");
        }

        /// <summary>
        /// O <c>Ataque</c> das fichas do Templo tem de ser <b>o número que os scripts já
        /// autoravam</b>. Criar a ficha com outro valor teria mudado o balanceamento de uma
        /// fase inteira dentro de uma mudança anunciada como estrutural — o mesmo erro que
        /// quase enfraqueceu o Cultista em 30%.
        /// </summary>
        [TestCase("Sseth", "SsethFarejadorAI", 20f)]
        [TestCase("Nagaraja", "NagarajaAI", 35f)]
        [TestCase("AvatarDeSet", "AvatarDeSetAI", 80f)]
        public void OAtaqueDoTemplo_PreservaOValorDoScript(string nome, string script, float esperado)
        {
            Assert.AreEqual(esperado, Ficha(nome).Ataque, 0.01f,
                $"Ficha_{nome}.Ataque divergiu do que {script} autorava. Se foi decisão de " +
                "balanceamento, mude o fallback do script junto e diga por quê.");

            // E o fallback do script continua igual: ele é quem responde enquanto o ator não
            // tiver EnemyBase no prefab.
            string fonte = File.ReadAllText($"Assets/Scripts/Enemies/{script}.cs");

            StringAssert.Contains($"danoDoGolpe = {esperado:0}f", fonte,
                $"O fallback de {script} divergiu da ficha. Enquanto não há prefab com " +
                "EnemyBase, é o fallback que roda — e os dois números voltariam a discordar.");
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

                         // Templo do Povo Serpente (Dungeon 2), unificados em 2026-08-29. Eles
                         // ficaram para trás na primeira rodada por não terem prefab nem cena --
                         // que é justamente como um ator some do radar neste repositório.
                         ("Assets/Scripts/Enemies/SsethFarejadorAI.cs", "danoDoGolpe"),
                         ("Assets/Scripts/Enemies/NagarajaAI.cs", "danoDoGolpe"),
                         ("Assets/Scripts/Enemies/AvatarDeSetAI.cs", "danoDoGolpe"),
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
