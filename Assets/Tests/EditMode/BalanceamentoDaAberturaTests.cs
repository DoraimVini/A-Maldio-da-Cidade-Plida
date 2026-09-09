using System.Linq;
using FavelaAmarela.Core.Combat;
using FavelaAmarela.Inventario;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace FavelaAmarela.Tests.EditMode
{
    /// <summary>
    /// Guarda a <b>aritmética das duas primeiras lutas</b> — a que o playtest de 2026-09-09
    /// mostrou quebrada.
    ///
    /// <para><b>O relato do Vini:</b> <i>"não tem dano e armadura para se manter vivo contra os
    /// dois cultistas"</i> e <i>"mesmo com a arma do baú, não fica com vida suficiente nem para
    /// lutar contra Abdul"</i>. Medido, ele estava certo nos dois, e por motivos diferentes.</para>
    ///
    /// <para><b>Contra o cultista</b> o problema era o golpe: 20 bruto contra defesa 6 passava
    /// <b>14</b>, e 100 de Vitalidade morrem em 7 golpes. Com dois cultistas isso é pouco mais de
    /// quatro segundos de exposição.</para>
    ///
    /// <para><b>Contra o Abdul</b> o problema era outro canal inteiro. O ataque <i>físico</i> dele
    /// é 8 contra defesa 6 — <b>2 de dano</b>, irrelevante. Quem mata é a <b>conjuração</b>, e ela
    /// bate na Resiliência Mental mitigada por <c>ResistenciaAnomala</c>, que em Damião é
    /// <b>zero</b>. Pior: varridas as 14 peças de equipamento defensivo do projeto, <b>nenhuma
    /// dava DefesaAnomalia</b>. Não existia no jogo inteiro uma resposta possível ao canal que
    /// mata — a luta era um cronômetro, não uma luta.</para>
    ///
    /// <para><b>Por que 16 e não 14 no cultista.</b> O Vini sugeriu 14. Medida a matriz inteira,
    /// 14 faz a <i>curva de armadura colapsar</i>: com defesa 13 (conjunto Sepulto) o golpe já
    /// bate no piso de 15%, e Yhtill e Set deixam de valer qualquer coisa. Com 16 a progressão
    /// continua significando algo — 10 sem nada, 6 com o conjunto inicial, 3 com Sepulto.</para>
    /// </summary>
    public sealed class BalanceamentoDaAberturaTests
    {
        private const float VitalidadeDeDamiao = 100f;
        private const float ResilienciaDeDamiao = 100f;
        private const float DefesaDeDamiao = 6f;

        /// <summary>Golpe médio do Alfanje do baú (40–61), antes da mitigação.</summary>
        private const float GolpeMedioDeDamiao = 50.5f;

        private static T Carregar<T>(string nome) where T : Object
        {
            string caminho = AssetDatabase.FindAssets($"t:{typeof(T).Name} {nome}")
                .Select(AssetDatabase.GUIDToAssetPath)
                .FirstOrDefault(p => System.IO.Path.GetFileNameWithoutExtension(p) == nome);

            Assert.NotNull(caminho, $"Asset '{nome}' não encontrado.");
            return AssetDatabase.LoadAssetAtPath<T>(caminho);
        }

        private static float Modificador(ItemDef item, StatType stat)
            => item.Modificadores.Where(m => m.Stat == stat).Sum(m => m.Valor);

        // ── cultista ─────────────────────────────────────────────────────────

        /// <summary>
        /// Sem armadura nenhuma, o jogador precisa aguentar pelo menos 9 golpes de cultista.
        ///
        /// <para>Abaixo disso a abertura vira corrida contra o relógio: com dois cultistas a
        /// 1,2 s de cadência, 7 golpes são pouco mais de quatro segundos.</para>
        /// </summary>
        [Test]
        public void SemArmadura_OJogadorAguentaPeloMenosNoveGolpesDeCultista()
        {
            var ficha = Carregar<FichaAtributosConfig>("Ficha_Cultista");

            float porGolpe = MitigacaoDeDano.Aplicar(ficha.Ataque, DefesaDeDamiao);
            float golpes = VitalidadeDeDamiao / porGolpe;

            Assert.GreaterOrEqual(golpes, 9f,
                $"O cultista tira {porGolpe:0.0} por golpe e mata em {golpes:0.0}. A abertura " +
                "vira corrida contra o relógio.");
        }

        /// <summary>
        /// A armadura precisa <b>importar</b>: o conjunto inicial tem de cortar pelo menos um
        /// terço do golpe. Foi a queixa direta do Vini — "ou a defesa é baixa demais".
        /// </summary>
        [Test]
        public void OConjuntoInicial_CortaPeloMenosUmTercoDoGolpe()
        {
            var ficha = Carregar<FichaAtributosConfig>("Ficha_Cultista");

            float bonus = new[] { "Item_Armadura_CapuzDeFarrapos", "Item_Armadura_ColeteDeSucata",
                                  "Item_Armadura_CaneleirasDeFerro" }
                .Sum(n => Modificador(Carregar<ItemDef>(n), StatType.DefesaFisica));

            float semNada = MitigacaoDeDano.Aplicar(ficha.Ataque, DefesaDeDamiao);
            float comSet = MitigacaoDeDano.Aplicar(ficha.Ataque, DefesaDeDamiao + bonus);

            Assert.LessOrEqual(comSet, semNada * 0.67f,
                $"O conjunto inicial dá +{bonus} de defesa e o golpe só cai de {semNada:0.0} " +
                $"para {comSet:0.0}. Coletar três peças precisa ser sentido.");
        }

        // ── Abdul ────────────────────────────────────────────────────────────

        /// <summary>
        /// <b>O buraco que existia no jogo inteiro:</b> alguma peça alcançável antes do Abdul
        /// precisa dar <c>DefesaAnomalia</c>.
        ///
        /// <para>Sem isso a conjuração dele entra inteira na Resiliência Mental e não há item,
        /// build ou perícia que mude — a luta deixa de ser uma luta.</para>
        /// </summary>
        [Test]
        public void AlgumaArmaduraInicial_DefendeDoCanalAnomalo()
        {
            float anomala = new[] { "Item_Armadura_CapuzDeFarrapos", "Item_Armadura_ColeteDeSucata",
                                    "Item_Armadura_CaneleirasDeFerro" }
                .Sum(n => Modificador(Carregar<ItemDef>(n), StatType.DefesaAnomalia));

            Assert.Greater(anomala, 0f,
                "Nenhuma peça alcançável antes do Abdul dá DefesaAnomalia. A conjuração dele " +
                "entra inteira na Resiliência Mental e o jogador não tem resposta possível.");
        }

        /// <summary>
        /// A troca contra o Abdul precisa ser <b>uma troca</b>: com o conjunto inicial vestido,
        /// o jogador tem de aguentar pelo menos tantas conjurações quanto os golpes que precisa
        /// para derrubá-lo.
        ///
        /// <para>Antes do ajuste eram <b>6,6 golpes contra 4 conjurações</b> — perder era
        /// aritmética, não erro de execução.</para>
        /// </summary>
        [Test]
        public void ComOConjuntoInicial_ATrocaContraAbdulEhJusta()
        {
            var abdul = Carregar<FichaAtributosConfig>("Ficha_Abdul");

            float anomala = new[] { "Item_Armadura_CapuzDeFarrapos", "Item_Armadura_ColeteDeSucata",
                                    "Item_Armadura_CaneleirasDeFerro" }
                .Sum(n => Modificador(Carregar<ItemDef>(n), StatType.DefesaAnomalia));

            float porConjuracao = MitigacaoDeDano.Aplicar(abdul.Conjuracao, anomala);
            float conjuracoesAteOColapso = ResilienciaDeDamiao / porConjuracao;

            float golpesParaDerrubar =
                abdul.VitalidadeMax / MitigacaoDeDano.Aplicar(GolpeMedioDeDamiao, abdul.Defesa);

            Assert.GreaterOrEqual(conjuracoesAteOColapso, golpesParaDerrubar,
                $"Damião precisa de {golpesParaDerrubar:0.0} golpes e aguenta " +
                $"{conjuracoesAteOColapso:0.0} conjurações. Perder é aritmética, não execução.");
        }

        /// <summary>
        /// O canal <b>físico</b> do Abdul continua sendo o menor perigo dele — é conjurador, não
        /// espadachim. Se alguém subir o ataque dele achando que é isso que mata, este teste
        /// avisa que mudou a natureza da luta.
        /// </summary>
        [Test]
        public void OPerigoDoAbdul_ContinuaSendoAConjuracaoENaoOGolpe()
        {
            var abdul = Carregar<FichaAtributosConfig>("Ficha_Abdul");

            float fisico = MitigacaoDeDano.Aplicar(abdul.Ataque, DefesaDeDamiao);
            float anomalo = MitigacaoDeDano.Aplicar(abdul.Conjuracao, 0f);

            Assert.Greater(anomalo, fisico * 2f,
                $"O golpe físico do Abdul passou a valer {fisico:0.0} contra {anomalo:0.0} da " +
                "conjuração. Ele é um conjurador — se isso inverteu, foi decisão de design?");
        }
    }
}
