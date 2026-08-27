using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using FavelaAmarela.Core.Combat;
using FavelaAmarela.Core.Loot;
using FavelaAmarela.Inventario;

namespace FavelaAmarela.Tests.EditMode
{
    /// <summary>
    /// Guarda a <b>Mão Secundária</b> — o slot que existia e não fazia nada.
    ///
    /// <para>Até 2026-08-27 ele era o índice 6 da anatomia e servia só de <i>regra</i>: uma arma
    /// <c>DuasMaos</c> o bloqueava. <b>Não havia um único item autorado para ele</b>, assim como
    /// para Amuleto e Anel — três dos sete slots do corpo do Damião estavam vazios.</para>
    ///
    /// <para><b>Bloqueio é chance, não botão</b>, e a escolha é deliberada: num isométrico com
    /// câmera afastada, segurar um botão para aparar exige ler direção e tempo de um alvo que o
    /// jogador mal distingue. É a mecânica de um jogo de câmera baixa atrás do ombro, não deste.
    /// O D2 resolveu igual: o escudo é um atributo.</para>
    /// </summary>
    public sealed class MaoSecundariaNoCombateTests
    {
        /// <summary>Fonte determinística — é assim que se testa e balanceia bloqueio.</summary>
        private sealed class FonteFixa : IFonteDeAleatoriedade
        {
            private readonly float _v;
            public FonteFixa(float v) => _v = v;
            public float ProximoValor() => _v;
            public int ProximoInteiro(int min, int max) => min;
        }

        // ── A regra ───────────────────────────────────────────────────────────

        [Test]
        public void SemEscudo_NadaEAparado()
        {
            var r = Bloqueio.Tentar(100f, chance: 0f, reducao: 0.5f, new FonteFixa(0f));

            Assert.IsFalse(r.Bloqueou);
            Assert.AreEqual(100f, r.DanoFinal, 0.0001f, "O golpe passa inteiro.");
        }

        [Test]
        public void SorteDentroDaChance_Apara()
        {
            var r = Bloqueio.Tentar(100f, chance: 0.3f, reducao: 0.5f, new FonteFixa(0.29f));

            Assert.IsTrue(r.Bloqueou);
            Assert.AreEqual(50f, r.DanoFinal, 0.0001f, "Apara metade do golpe.");
        }

        [Test]
        public void SorteForaDaChance_NaoApara()
        {
            var r = Bloqueio.Tentar(100f, chance: 0.3f, reducao: 0.5f, new FonteFixa(0.3f));

            Assert.IsFalse(r.Bloqueou, "0,3 não é MENOR que 0,3 — a borda tem de ser exclusiva.");
            Assert.AreEqual(100f, r.DanoFinal, 0.0001f);
        }

        /// <summary>
        /// Sem teto, empilhar escudo e afixos levaria a 100% de bloqueio — imunidade, que
        /// nenhum item deveria conceder. É o defeito de balanceamento mais previsível de um
        /// sistema de bloqueio por chance.
        /// </summary>
        [Test]
        public void AChance_TemTeto()
        {
            // Sorte logo acima do teto: com chance "99%" mas teto de 60%, tem de passar.
            var r = Bloqueio.Tentar(100f, chance: 0.99f, reducao: 1f,
                                    new FonteFixa(Bloqueio.ChanceMaxima + 0.01f));

            Assert.IsFalse(r.Bloqueou,
                $"A chance foi limitada a {Bloqueio.ChanceMaxima:P0}; acima disso o golpe passa. " +
                "Sem teto, empilhar bônus daria imunidade.");
        }

        [Test]
        public void ReducaoTotal_ZeraOGolpeMasNaoOInverte()
        {
            var r = Bloqueio.Tentar(100f, chance: 1f, reducao: 5f, new FonteFixa(0f));

            Assert.IsTrue(r.Bloqueou);
            Assert.AreEqual(0f, r.DanoFinal, 0.0001f,
                "Redução acima de 1 tem de saturar em 0 — dano negativo curaria o alvo.");
        }

        [Test]
        public void GolpeSemDano_NaoConsomeSorte()
        {
            var r = Bloqueio.Tentar(0f, chance: 1f, reducao: 1f, new FonteFixa(0f));

            Assert.IsFalse(r.Bloqueou,
                "Aparar um golpe de dano zero contaria como bloqueio para o áudio e a UI, e o " +
                "jogador ouviria o escudo funcionar sem nada ter acontecido.");
        }

        // ── Os itens autorados ────────────────────────────────────────────────

        private const string PastaDosItens = "Assets/FavelaAmarela/Config/Resources/Itens";

        private static ItemDef[] DaMaoSecundaria() =>
            Directory.GetFiles(PastaDosItens, "*.asset", SearchOption.AllDirectories)
                     .Select(c => c.Replace(Path.DirectorySeparatorChar, '/'))
                     .Select(AssetDatabase.LoadAssetAtPath<ItemDef>)
                     .Where(d => d != null && d.SlotEquipamento == EquipmentSlot.MaoSecundaria)
                     .ToArray();

        [Test]
        public void OSlot_DeixouDeEstarVazio()
        {
            Assert.IsNotEmpty(DaMaoSecundaria(),
                "Nenhum item de Mão Secundária autorado. O slot voltaria a existir só como " +
                "regra de empunhadura, que era o estado até 2026-08-27. Conserto: " +
                "'Tools/FavelaAmarela/Itens: montar a Mão Secundária'.");
        }

        /// <summary>
        /// Item de mão secundária sem função é indistinguível de um item comum ocupando o
        /// slot — o jogador equipa e não entende o que ganhou.
        /// </summary>
        [Test]
        public void TodoItemDoSlot_TemFuncaoEPotencia()
        {
            foreach (var def in DaMaoSecundaria())
            {
                Assert.AreNotEqual(FuncaoDeMaoSecundaria.Nenhuma, def.Funcao,
                    $"'{def.Nome}' ocupa a Mão Secundária sem função definida.");

                Assert.Greater(def.PotenciaDaMaoSecundaria, 0f,
                    $"'{def.Nome}' tem função {def.Funcao} com potência 0 — o item não faz nada.");
            }
        }

        [Test]
        public void OEscudo_AparaAlgoDeVerdade()
        {
            var escudos = DaMaoSecundaria()
                .Where(d => d.Funcao == FuncaoDeMaoSecundaria.Escudo)
                .ToArray();

            Assert.IsNotEmpty(escudos, "Nenhum escudo autorado.");

            foreach (var e in escudos)
            {
                Assert.Greater(e.ReducaoAoBloquear, 0f,
                    $"'{e.Nome}' bloqueia e não reduz dano nenhum — o bloqueio seria cosmético.");

                Assert.LessOrEqual(e.PotenciaDaMaoSecundaria, Bloqueio.ChanceMaxima,
                    $"'{e.Nome}' promete mais bloqueio que o teto do sistema " +
                    $"({Bloqueio.ChanceMaxima:P0}). O número na ficha mentiria.");
            }
        }

        [Test]
        public void OFoco_NaoZeraARecarga()
        {
            foreach (var f in DaMaoSecundaria()
                         .Where(d => d.Funcao == FuncaoDeMaoSecundaria.Foco))
            {
                Assert.Less(f.PotenciaDaMaoSecundaria, 1f,
                    $"'{f.Nome}' descontaria 100% da recarga — a habilidade viraria o ataque " +
                    "básico, e a distinção entre os dois botões sumiria.");
            }
        }

        /// <summary>
        /// Uma arma de duas mãos bloqueia o slot. Essa regra já existia e é guardada por
        /// <c>MaoSecundariaTests</c>; aqui só se garante que os itens novos não a contornam
        /// pedindo empunhadura de duas mãos num item de off-hand.
        /// </summary>
        [Test]
        public void ItemDeOffHand_NaoPedeDuasMaos()
        {
            foreach (var def in DaMaoSecundaria())
                Assert.AreEqual(Empunhadura.UmaMao, def.Empunhadura,
                    $"'{def.Nome}' é de Mão Secundária e pede duas mãos — ele bloquearia o " +
                    "próprio slot em que se equipa.");
        }
    }
}
