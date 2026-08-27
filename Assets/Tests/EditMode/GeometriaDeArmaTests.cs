using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using FavelaAmarela.Inventario;

namespace FavelaAmarela.Tests.EditMode
{
    /// <summary>
    /// Guarda a <b>família da arma</b> — a camada que faz trocar de arma ser <i>sentido</i>
    /// antes de ser lido.
    ///
    /// <para><b>O estado de antes (2026-08-27).</b> Alcance e forma do golpe eram um campo do
    /// <c>MaoFisicaBridge</c>: <c>alcance = 1.2f</c>, <b>um número só para todas as armas</b>. O
    /// <c>ItemDef</c> de uma arma não continha um único número de combate. Estilete e alfanje
    /// tinham a mesma pegada, a mesma área e a mesma janela; só o dano diferia.</para>
    /// </summary>
    public sealed class GeometriaDeArmaTests
    {
        private const string PastaDosItens = "Assets/FavelaAmarela/Config/Resources/Itens";

        private static ItemDef[] ArmasAutoradas() =>
            Directory.GetFiles(PastaDosItens, "*.asset", SearchOption.AllDirectories)
                     .Select(c => c.Replace(Path.DirectorySeparatorChar, '/'))
                     .Select(AssetDatabase.LoadAssetAtPath<ItemDef>)
                     .Where(d => d != null && d.Tipo == ItemType.Arma)
                     .OrderBy(d => d.name)
                     .ToArray();

        /// <summary>
        /// Arma sem família cai na geometria padrão <b>em silêncio</b> — ela funciona, só não
        /// tem identidade. É exatamente a classe de defeito que este repositório mais produz:
        /// a peça existe e não está ligada em nada.
        /// </summary>
        [Test]
        public void TodaArmaAutorada_TemFamilia()
        {
            var soltas = ArmasAutoradas().Where(d => d.Base == null)
                                         .Select(d => d.name)
                                         .ToList();

            Assert.IsEmpty(soltas,
                "Arma(s) sem BaseDeArma ligada: " + string.Join(", ", soltas) +
                ". Elas caem na geometria padrão sem avisar — o golpe funciona, mas a arma " +
                "perde a identidade. Conserto: 'Tools/FavelaAmarela/Armas: montar as bases " +
                "(famílias)'.");
        }

        /// <summary>
        /// A base diz qual POCO de combate construir. Se ela divergir do <c>ArmaFisica</c> do
        /// item, a arma teria a geometria de uma e o comportamento de outra — um alfanje com
        /// alcance de alfanje e dano de estilete, sem nada acusando.
        /// </summary>
        [Test]
        public void AFamilia_ConcordaComOComportamentoDaArma()
        {
            var divergentes = new List<string>();

            foreach (var def in ArmasAutoradas())
            {
                if (def.Base == null) continue;   // já coberto acima

                if (def.Base.Arquetipo != def.ArmaFisica)
                    divergentes.Add($"{def.name}: item diz {def.ArmaFisica}, " +
                                    $"família '{def.Base.name}' diz {def.Base.Arquetipo}");
            }

            Assert.IsEmpty(divergentes,
                "Família e comportamento divergem:" + Environment.NewLine + "  " +
                string.Join(Environment.NewLine + "  ", divergentes));
        }

        /// <summary>
        /// <b>O guarda que dá sentido a esta fase.</b> Se as três famílias tiverem a mesma
        /// geometria, todo o trabalho vira decoração: a arma volta a ser só um número de dano.
        /// </summary>
        [Test]
        public void AsTresArmas_NaoTemAMesmaPegada()
        {
            var alcances = ArmasAutoradas().Where(d => d.Base != null)
                                           .Select(d => d.Base.Alcance)
                                           .ToList();

            Assert.GreaterOrEqual(alcances.Count, 3, "Esperava ao menos 3 armas com família.");

            Assert.AreEqual(alcances.Count, alcances.Distinct().Count(),
                "Duas ou mais armas têm exatamente o mesmo alcance. Se a geometria não " +
                "diferencia, trocar de arma não é sentido — que era o estado anterior a esta " +
                "fase, e o motivo dela existir.");
        }

        /// <summary>
        /// A <b>ordem</b> é o design, e é o que precisa sobreviver ao balanceamento: a lâmina
        /// fina fura um ponto de perto; o alfanje varre um arco e alcança. Os valores absolutos
        /// são botões; esta relação não é.
        /// </summary>
        [Test]
        public void OAlfanje_AlcancaMaisEPerdoaMaisQueALaminaFina()
        {
            var fina = Base("Item_Arma_EstileteDeIrem");
            var cravo = Base("Item_Arma_CravoDeAklo");
            var alfanje = Base("Item_Arma_AlfanjeDeAlhazred");

            Assert.Less(fina.Alcance, cravo.Alcance, "O estilete tem de ser a arma mais curta.");
            Assert.Less(cravo.Alcance, alfanje.Alcance, "O alfanje tem de ser a mais longa.");

            Assert.Less(fina.Raio, alfanje.Raio,
                "O alfanje varre um arco; o estilete fura um ponto.");

            Assert.Less(fina.JanelaAtiva, alfanje.JanelaAtiva,
                "Janela curta exige mira; janela longa perdoa. O estilete é a arma que cobra " +
                "precisão, o alfanje é a que perdoa.");
        }

        private static BaseDeArma Base(string item)
        {
            var def = AssetDatabase.LoadAssetAtPath<ItemDef>($"{PastaDosItens}/{item}.asset");
            Assert.IsNotNull(def, $"ItemDef ausente: {item}");
            Assert.IsNotNull(def.Base, $"'{item}' está sem família ligada.");
            return def.Base;
        }

        // ── O que torna a migração para Hitbox segura ─────────────────────────

        /// <summary>
        /// <b>A invariante que sustenta o golpe do jogador.</b> A <c>Hitbox</c> só acerta quem
        /// tem <c>Hurtbox</c>; a consulta antiga acertava qualquer <c>IDanificavel</c>. Hoje os
        /// dois conjuntos coincidem — todo implementador de <c>IDanificavel</c> chama
        /// <c>Hurtbox.GarantirPara</c>.
        ///
        /// <para>No dia em que alguém escrever um inimigo danificável e esquecer a hurtbox, ele
        /// ficará <b>impossível de acertar</b>, sem erro nenhum — foi exatamente assim que o
        /// Byakhee passou nove dias marcado como pronto sendo invencível.</para>
        /// </summary>
        [Test]
        public void TodoDanificavel_TemHurtbox()
        {
            var semHurtbox = new List<string>();

            foreach (var caminho in Directory.GetFiles("Assets/Scripts", "*.cs",
                                                       SearchOption.AllDirectories))
            {
                string nome = Path.GetFileName(caminho);

                // A própria Hurtbox implementa IDanificavel para repassar ao dono.
                if (nome == "Hurtbox.cs" || nome == "IDanificavel.cs") continue;

                string codigo = File.ReadAllText(caminho);

                // Casa a DECLARAÇÃO (herança), não a menção — um arquivo que só recebe um
                // IDanificavel como parâmetro não implementa nada.
                if (!Regex.IsMatch(codigo, @"class\s+\w+\s*:[^{]*\bIDanificavel\b")) continue;

                if (!codigo.Contains("Hurtbox.GarantirPara"))
                    semHurtbox.Add(nome);
            }

            Assert.IsEmpty(semHurtbox,
                "Implementador(es) de IDanificavel sem Hurtbox: " +
                string.Join(", ", semHurtbox) + Environment.NewLine +
                "O golpe do jogador é resolvido pela Hitbox, que só encontra hurtboxes. Sem " +
                "ela o inimigo é IMPOSSÍVEL DE ACERTAR, e nada no console avisa.");
        }

        /// <summary>
        /// O golpe do jogador não pode voltar a resolver dano por conta própria: dois modelos
        /// de dano no mesmo jogo foi o estado anterior, e só um deles permitia esquivar no
        /// tempo certo.
        /// </summary>
        [Test]
        public void OGolpeDoJogador_PassaPelaHitbox()
        {
            string bridge = File.ReadAllText("Assets/Scripts/Player/MaoFisicaBridge.cs");

            StringAssert.Contains("_hitbox.Armar(", bridge,
                "A MaoFisicaBridge parou de armar a Hitbox — o golpe do jogador voltaria a ser " +
                "um teste de posição num quadro só, sem janela para esquivar.");

            // Casa a CHAMADA, não a palavra: o XML doc do próprio método explica que
            // ele DEIXOU de usar OverlapCircle, e a primeira versão deste guarda
            // falhou contra o comentário que ela mesma motivou.
            StringAssert.DoesNotContain("Physics2D.OverlapCircle(", bridge,
                "A MaoFisicaBridge voltou a consultar física por conta própria. A resolução do " +
                "golpe mora na Hitbox — duas cópias divergem.");

            StringAssert.Contains("pouparAliados: true", bridge,
                "A hitbox do jogador precisa poupar aliados. Sem isso o Damião passa a acertar " +
                "o Yug-Neth no meio da luta — a taxonomia de layers é fechada, então quem " +
                "protege o companheiro é o marcador Aliado, não uma camada própria.");
        }
    }
}
