using NUnit.Framework;
using UnityEditor;
using FavelaAmarela.Core.Abilities;
using FavelaAmarela.Inventario;

namespace FavelaAmarela.Tests.EditMode
{
    /// <summary>
    /// Fixa os <b>números com que as três armas atravessaram a migração</b> para dado.
    ///
    /// <para><b>Como este arquivo nasceu.</b> A Fase 4 do plano de itemização deletou
    /// <c>CravoDeAklo</c>, <c>EstileteDeIrem</c> e <c>AlfanjeDeAlhazred</c>, que estavam
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
    /// </summary>
    public sealed class EquivalenciaDaMigracaoTests
    {
        private const string PastaDasHabilidades = "Assets/FavelaAmarela/Config/Habilidades";

        private static IArmaComHabilidade Arma(string arquivo)
        {
            var def = AssetDatabase.LoadAssetAtPath<HabilidadeDef>(
                $"{PastaDasHabilidades}/{arquivo}.asset");

            Assert.IsNotNull(def,
                $"HabilidadeDef ausente: {arquivo}. Conserto: " +
                "'Tools/FavelaAmarela/Armas: montar as habilidades a dado'.");

            return def.Construir();
        }

        [Test]
        public void CravoDeAklo_MantemOsNumerosDaMigracao()
        {
            var arma = Arma("Habilidade_CravoDeAklo");
            Assert.AreEqual("Cravo de Aklo", arma.NomeDaArma);
            Assert.AreEqual("Fincar o Aklo", arma.NomeHabilidade);

            var b = arma.Execute();
            Assert.AreEqual(40f, b.Dano, 0.0001f, "dano básico");
            Assert.AreEqual(0.35f, b.DurationSeconds, 0.0001f, "duração do básico");
            Assert.AreEqual(0.5f, b.CooldownSeconds, 0.0001f, "cadência do básico");
            Assert.IsFalse(b.InterrompeConjuracao, "o básico do Cravo não interrompe");

            var h = arma.ExecuteHabilidade();
            Assert.AreEqual(30f, h.Dano, 0.0001f, "dano da habilidade");
            Assert.AreEqual(0.4f, h.DurationSeconds, 0.0001f);
            Assert.AreEqual(6f, h.CooldownSeconds, 0.0001f);
            Assert.IsTrue(h.InterrompeConjuracao,
                "É o que faz o Cravo ser a arma anti-mago do arsenal.");
            Assert.AreEqual(0, h.AcumulosDeSangramento);
            Assert.AreEqual(0f, h.ForcaRepulsao, 0.0001f);
            Assert.IsFalse(h.Atordoou);
        }

        [Test]
        public void EstileteDeIrem_MantemOsNumerosDaMigracao()
        {
            var arma = Arma("Habilidade_EstileteDeIrem");
            Assert.AreEqual("Estilete de Irem", arma.NomeDaArma);
            Assert.AreEqual("Ferida de Aklo", arma.NomeHabilidade);

            var b = arma.Execute();
            Assert.AreEqual(30f, b.Dano, 0.0001f, "dano básico");
            Assert.AreEqual(0.25f, b.DurationSeconds, 0.0001f);
            Assert.AreEqual(0.3f, b.CooldownSeconds, 0.0001f, "é a arma rápida do arsenal");
            Assert.AreEqual(1, b.AcumulosDeSangramento,
                "O básico abre 1 acúmulo — é o que torna o teto de 10 alcançável.");
            Assert.AreEqual(4f, b.SangramentoPorSegundo, 0.0001f);
            Assert.AreEqual(5f, b.DuracaoSangramento, 0.0001f);

            var h = arma.ExecuteHabilidade();
            Assert.AreEqual(15f, h.Dano, 0.0001f, "dano da habilidade");
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
            var arma = Arma("Habilidade_AlfanjeDeAlhazred");
            Assert.AreEqual("Alfanje de Alhazred", arma.NomeDaArma);
            Assert.AreEqual("Golpe do Deserto", arma.NomeHabilidade);

            var b = arma.Execute();
            Assert.AreEqual(45f, b.Dano, 0.0001f, "dano básico");
            Assert.AreEqual(0.45f, b.DurationSeconds, 0.0001f);
            Assert.AreEqual(0.7f, b.CooldownSeconds, 0.0001f, "é a arma pesada do arsenal");
            Assert.IsFalse(b.Atordoou, "o básico do Alfanje não atordoa");
            Assert.AreEqual(0f, b.ForcaRepulsao, 0.0001f, "o básico do Alfanje não repele");

            var h = arma.ExecuteHabilidade();
            Assert.AreEqual(40f, h.Dano, 0.0001f, "dano da habilidade");
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
                         ("Item_Arma_CravoDeAklo", "Cravo de Aklo"),
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
