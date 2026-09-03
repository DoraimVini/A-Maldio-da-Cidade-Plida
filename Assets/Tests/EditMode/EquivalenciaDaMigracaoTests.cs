using NUnit.Framework;
using FavelaAmarela.Core.Combat;
using UnityEditor;
using FavelaAmarela.Core.Abilities;
using FavelaAmarela.Inventario;

namespace FavelaAmarela.Tests.EditMode
{
    /// <summary>
    /// Fixa os <b>números com que as três armas atravessaram a migração</b> para dado.
    ///
    /// <para><b>Como este arquivo nasceu.</b> A Fase 4 do plano de itemização deletou
    /// <c>MacaDeAklo</c>, <c>EstileteDeIrem</c> e <c>AlfanjeDeAlhazred</c>, que estavam
    /// testadas e jogáveis. Antes de deletar, estes testes comparavam <b>campo a campo</b> o
    /// <c>ArmaResult</c> da arma a dado com o da classe — e passaram. Com as classes fora, eles
    /// passaram a afirmar os mesmos valores como números literais.</para>
    ///
    /// <para><b>Por que os literais valem a pena.</b> O <c>armas_da_tumba.md</c> diverge do
    /// código justamente no dano básico: o documento diz <b>40/25/60</b>, os construtores diziam
    /// <b>40/30/45</b>. O <c>CLAUDE.md</c> §3.1 regra 4 manda seguir o código para "como
    /// funciona". Sem estes literais, uma edição de asset feita olhando a documentação mudaria
    /// o balanceamento de duas armas em silêncio.</para>
    ///
    /// <para>Alterar um número aqui é <b>decisão de balanceamento</b> e deve ser deliberada —
    /// não efeito colateral de mexer num asset.</para>
    ///
    /// <para><b>Segunda migração, 2026-08-28: dano fixo → faixa de dano branco.</b> O dano saiu
    /// do <c>HabilidadeDef</c> (um asset por família, o que tornava duas cópias da mesma arma
    /// sempre idênticas) e foi para a <c>BaseDeArma</c>, como intervalo. Os literais de dano
    /// deste arquivo <b>deixaram de existir</b> — não há mais um número de dano a fixar.</para>
    ///
    /// <para>O que sobreviveu, e é o que estes testes passam a guardar, é o <b>valor
    /// esperado</b>: <c>média × precisão × (1 + chanceCrítica × (multiplicador − 1))</c> tem de
    /// cair em cima do dano fixo antigo. Foi essa a regra de calibragem da migração, e é a
    /// afirmação que prova que a Fase 1 mudou a <i>textura</i> do combate — variância, crítico,
    /// erro — sem mexer na dificuldade. Rebalancear é decisão separada.</para>
    /// </summary>
    public sealed class EquivalenciaDaMigracaoTests
    {
        private const string PastaDasBases = "Assets/FavelaAmarela/Config/Armas";

        /// <summary>
        /// Constrói pela <b>BaseDeArma</b>, que é onde o dano branco mora desde 2026-08-28 e o
        /// caminho que o jogo usa ao equipar.
        /// </summary>
        private static IArmaComHabilidade Arma(string arquivo)
        {
            var def = AssetDatabase.LoadAssetAtPath<BaseDeArma>(
                $"{PastaDasBases}/{arquivo}.asset");

            Assert.IsNotNull(def,
                $"BaseDeArma ausente: {arquivo}. Conserto: " +
                "'Tools/FavelaAmarela/Armas: montar as bases (famílias)'.");

            var arma = def.ConstruirArma();
            Assert.IsNotNull(arma, $"'{arquivo}' não constrói arma — HabilidadeDef solta?");
            return arma;
        }

        /// <summary>Golpe resolvido na MÉDIA da faixa, sem erro nem crítico (fonte nula).</summary>
        private static ArmaResult Basico(IArmaComHabilidade a)
            => ResolucaoDeGolpe.Resolver(a.Execute(), a.Perfil);

        /// <inheritdoc cref="Basico"/>
        private static ArmaResult Habilidade(IArmaComHabilidade a)
            => ResolucaoDeGolpe.Resolver(a.ExecuteHabilidade(), a.Perfil);

        /// <summary>
        /// O valor esperado de um golpe desta arma — média da faixa, corrigida por precisão e
        /// crítico. É o número que a migração preservou.
        /// </summary>
        private static float Esperado(PerfilDeArma p, float percentual)
            => (p.DanoMin + p.DanoMax) * 0.5f * percentual
               * p.Precisao * (1f + p.ChanceCritica * (p.MultiplicadorCritico - 1f));

        [Test]
        public void MacaDeAklo_MantemOsNumerosDaMigracao()
        {
            var arma = Arma("BaseArma_Maca");
            Assert.AreEqual("Maça de Aklo", arma.NomeDaArma);
            Assert.AreEqual("Calar o Aklo", arma.NomeHabilidade);

            var b = Basico(arma);
            Assert.AreEqual(40f, Esperado(arma.Perfil, b.PercentualDoDanoDaArma), 1.0f,
                "O Maca causava 40 fixos antes da migração. O valor ESPERADO do golpe tem de " +
                "cair em cima disso — é o que separa 'mudei a textura' de 'rebalanceei sem " +
                "querer'.");
            Assert.AreEqual(0.35f, b.DurationSeconds, 0.0001f, "duração do básico");
            Assert.AreEqual(0.5f, b.CooldownSeconds, 0.0001f, "cadência do básico");
            Assert.IsFalse(b.InterrompeConjuracao, "o básico do Maca não interrompe");

            var h = Habilidade(arma);
            Assert.AreEqual(30f, Esperado(arma.Perfil, h.PercentualDoDanoDaArma), 1.5f,
                "Calar o Aklo causava 30 fixos.");
            Assert.AreEqual(0.4f, h.DurationSeconds, 0.0001f);
            Assert.AreEqual(6f, h.CooldownSeconds, 0.0001f);
            Assert.IsTrue(h.InterrompeConjuracao,
                "É o que faz o Maca ser a arma anti-mago do arsenal.");
            Assert.AreEqual(0, h.AcumulosDeSangramento);
            Assert.AreEqual(0f, h.ForcaRepulsao, 0.0001f);
            Assert.IsFalse(h.Atordoou);
        }

        [Test]
        public void EstileteDeIrem_MantemOsNumerosDaMigracao()
        {
            var arma = Arma("BaseArma_LaminaFina");
            Assert.AreEqual("Estilete de Irem", arma.NomeDaArma);
            Assert.AreEqual("Ferida de Aklo", arma.NomeHabilidade);

            var b = Basico(arma);
            Assert.AreEqual(30f, Esperado(arma.Perfil, b.PercentualDoDanoDaArma), 1.0f,
                "O Estilete causava 30 fixos antes da migração.");
            Assert.AreEqual(0.25f, b.DurationSeconds, 0.0001f);
            Assert.AreEqual(0.3f, b.CooldownSeconds, 0.0001f, "é a arma rápida do arsenal");
            Assert.AreEqual(1, b.AcumulosDeSangramento,
                "O básico abre 1 acúmulo — é o que torna o teto de 10 alcançável.");
            Assert.AreEqual(4f, b.SangramentoPorSegundo, 0.0001f);
            Assert.AreEqual(5f, b.DuracaoSangramento, 0.0001f);

            var h = Habilidade(arma);
            Assert.AreEqual(15f, Esperado(arma.Perfil, h.PercentualDoDanoDaArma), 1.5f,
                "Ferida de Aklo causava 15 fixos.");
            Assert.AreEqual(0.3f, h.DurationSeconds, 0.0001f);
            Assert.AreEqual(5f, h.CooldownSeconds, 0.0001f);
            Assert.AreEqual(3, h.AcumulosDeSangramento,
                "A habilidade é o empurrão guardado para a janela de dano.");
            Assert.IsFalse(h.InterrompeConjuracao);
            Assert.AreEqual(0f, h.ForcaRepulsao, 0.0001f);
        }

        [Test]
        public void AlfanjeDeAlhazred_MantemOsNumerosDaMigracao()
        {
            var arma = Arma("BaseArma_Alfanje");
            Assert.AreEqual("Alfanje de Alhazred", arma.NomeDaArma);
            Assert.AreEqual("Golpe do Deserto", arma.NomeHabilidade);

            var b = Basico(arma);
            Assert.AreEqual(45f, Esperado(arma.Perfil, b.PercentualDoDanoDaArma), 1.0f,
                "O Alfanje causava 45 fixos antes da migração.");
            Assert.AreEqual(0.45f, b.DurationSeconds, 0.0001f);
            Assert.AreEqual(0.7f, b.CooldownSeconds, 0.0001f, "é a arma pesada do arsenal");
            Assert.IsFalse(b.Atordoou, "o básico do Alfanje não atordoa");
            Assert.AreEqual(0f, b.ForcaRepulsao, 0.0001f, "o básico do Alfanje não repele");

            var h = Habilidade(arma);
            Assert.AreEqual(40f, Esperado(arma.Perfil, h.PercentualDoDanoDaArma), 1.5f,
                "Golpe do Deserto causava 40 fixos.");
            Assert.AreEqual(0.5f, h.DurationSeconds, 0.0001f);
            Assert.AreEqual(5f, h.CooldownSeconds, 0.0001f);
            Assert.IsTrue(h.Atordoou);
            Assert.AreEqual(2f, h.DuracaoAtordoamento, 0.0001f);
            Assert.AreEqual(6f, h.ForcaRepulsao, 0.0001f,
                "É o 'espaço' que armas_da_tumba.md promete — e que ficou sem leitor nenhum " +
                "até a física de impacto entrar, em 2026-08-27.");
            Assert.AreEqual(0, h.AcumulosDeSangramento);
        }

        /// <summary>
        /// A família tem de entregar a arma <b>a dado</b>. Se a <c>HabilidadeDef</c> se soltar,
        /// a arma deixa de ser construída e Damião fica desarmado com a arma na mão — a
        /// migração existiria só no disco.
        /// </summary>
        [Test]
        public void AsFamilias_ConstroemPeloDado()
        {
            foreach (var (item, esperado) in new[]
                     {
                         ("Item_Arma_MacaDeAklo", "Maça de Aklo"),
                         ("Item_Arma_EstileteDeIrem", "Estilete de Irem"),
                         ("Item_Arma_AlfanjeDeAlhazred", "Alfanje de Alhazred"),
                     })
            {
                var def = AssetDatabase.LoadAssetAtPath<ItemDef>(
                    $"Assets/FavelaAmarela/Config/Resources/Itens/{item}.asset");

                Assert.IsNotNull(def, $"ItemDef ausente: {item}");
                Assert.IsNotNull(def.Base, $"'{item}' está sem família ligada.");

                Assert.IsNotNull(def.Base.Habilidade,
                    $"'{def.Base.name}' está sem HabilidadeDef — a arma não seria construída e " +
                    "Damião ficaria desarmado com ela equipada. Conserto: " +
                    "'Tools/FavelaAmarela/Armas: montar as habilidades a dado'.");

                var arma = def.Base.ConstruirArma();
                Assert.IsNotNull(arma, $"'{item}' não constrói arma nenhuma.");
                Assert.AreEqual(esperado, arma.NomeDaArma, $"'{item}' construiu a arma errada.");
                Assert.IsInstanceOf<HabilidadeComposta>(arma,
                    $"'{item}' não está sendo construída por dado.");
            }
        }
    }
}
