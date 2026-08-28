using System;
using NUnit.Framework;
using FavelaAmarela.Core.Abilities;
using FavelaAmarela.Core.Combat;
using FavelaAmarela.Core.Loot;
using FavelaAmarela.Core.Progression;

namespace FavelaAmarela.Tests.EditMode
{
    /// <summary>
    /// O <b>eixo de poder</b> que o jogo não tinha: faixa de dano branco, crítico, precisão e
    /// escala por nível.
    ///
    /// <para><b>O que estava invertido (2026-08-28).</b> O dano morava no <c>HabilidadeDef</c>,
    /// que é <i>um asset por família</i>: todo Alfanje apontava para o mesmo <c>Valor: 45</c>.
    /// Duas cópias eram sempre idênticas e uma arma melhor era <b>inexprimível</b> — não havia
    /// campo onde escrever que esta é melhor que aquela. "Os itens são fracos demais" não era
    /// número: era a ausência do eixo.</para>
    ///
    /// <para>Tudo aqui é POCO com aleatoriedade injetada, no molde de <c>Bloqueio</c>: dá para
    /// afirmar erro, crítico e faixa sem cena e sem Unity rodando.</para>
    /// </summary>
    public sealed class DanoBrancoTests
    {
        /// <summary>
        /// Fonte determinística: devolve, em ordem, os números que o teste mandar. É o que torna
        /// "rolou 0,3 na faixa, 0,95 no acerto" uma afirmação e não uma torcida.
        /// </summary>
        private sealed class FonteFalsa : IFonteDeAleatoriedade
        {
            private readonly float[] _valores;
            private int _i;

            public FonteFalsa(params float[] valores) => _valores = valores;

            public int Consumidos => _i;

            public float ProximoValor()
            {
                Assert.Less(_i, _valores.Length,
                    "A resolução pediu mais números do que o teste preparou — a ordem das " +
                    "rolagens mudou, e isso é contrato: 2 números quando erra, 3 quando acerta.");

                return _valores[_i++];
            }

            public int ProximoInteiro(int min, int max) => min;
        }

        private static PerfilDeArma Arma(float min = 40f, float max = 60f,
                                         float chanceCritica = 0f, float multiplicador = 2f,
                                         float precisao = 1f)
            => new PerfilDeArma(min, max, chanceCritica, multiplicador, precisao);

        private static ArmaResult Golpe(float percentual = 1f, float danoPlano = 0f)
            => new ArmaResult(true, 0.3f, 0.5f, dano: danoPlano,
                              percentualDoDanoDaArma: percentual);

        // ── A faixa ───────────────────────────────────────────────────────────

        [Test]
        public void ADanoBranco_RolaDentroDaFaixa()
        {
            var arma = Arma(40f, 60f);

            Assert.AreEqual(40f, arma.RolarDanoBranco(new FonteFalsa(0f)), 0.001f, "piso");
            Assert.AreEqual(60f, arma.RolarDanoBranco(new FonteFalsa(1f)), 0.001f, "teto");
            Assert.AreEqual(50f, arma.RolarDanoBranco(new FonteFalsa(0.5f)), 0.001f, "meio");
        }

        [Test]
        public void AFaixaInvertida_EhOrdenadaEmVezDeEstourar()
        {
            var arma = Arma(60f, 40f);   // autorada ao contrário no Inspector

            Assert.AreEqual(40f, arma.DanoMin, 0.001f);
            Assert.AreEqual(60f, arma.DanoMax, 0.001f,
                "Faixa invertida é erro de autoria, não caso de jogo: ordenar é melhor que " +
                "estourar em runtime ou produzir dano negativo.");
        }

        [Test]
        public void SemFonte_ADanoBrancoCaiNaMedia()
        {
            Assert.AreEqual(50f, Arma(40f, 60f).RolarDanoBranco(null), 0.001f,
                "Sem aleatoriedade, o golpe tem de valer a média — é o que deixa a Forja do " +
                "Debugger mostrar a conta sem sortear.");
        }

        // ── A habilidade multiplica a arma ────────────────────────────────────

        [Test]
        public void AHabilidade_EhPercentualDoDanoDaArma()
        {
            var arma = Arma(40f, 60f);
            var fonte = new FonteFalsa(0.5f, 0f, 0.99f);   // meio da faixa, acerta, não critica

            var r = ResolucaoDeGolpe.Resolver(Golpe(percentual: 0.8f), arma, fonte: fonte);

            Assert.AreEqual(40f, r.Dano, 0.001f,
                "80% de uma arma de 50 são 40. É esta linha que faz trocar de arma melhorar " +
                "TODAS as habilidades de uma vez — o loop que faz o loot valer a pena.");
        }

        [Test]
        public void OGolpeSemPercentual_PassaIntacto()
        {
            var r = ResolucaoDeGolpe.Resolver(
                new ArmaResult(true, 0.3f, 0.5f, dano: 26f), Arma(),
                fonte: new FonteFalsa(0.5f, 0f, 0.99f));

            Assert.AreEqual(26f, r.Dano, 0.001f,
                "Golpe de inimigo e habilidade de dano fixo não têm arma de onde escalar. " +
                "Reescrevê-los mudaria o que eles significam — e o Byakhee bate com número " +
                "próprio, não com faixa.");
        }

        [Test]
        public void ODanoPlano_SomaAoDanoDaArma()
        {
            var fonte = new FonteFalsa(0.5f, 0f, 0.99f);

            var r = ResolucaoDeGolpe.Resolver(Golpe(1f, danoPlano: 7f), Arma(40f, 60f),
                                              fonte: fonte);

            Assert.AreEqual(57f, r.Dano, 0.001f,
                "O bônus plano de afixo (TraumaFisico) entra somando, depois da arma. Se " +
                "multiplicasse, um afixo pequeno viraria enorme numa arma de tier alto.");
        }

        // ── Errar de verdade ──────────────────────────────────────────────────

        /// <summary>
        /// O Vini escolheu o modelo do D2: falhar na precisão é <b>dano zero</b>, não golpe de
        /// raspão. O campo <c>Errou</c> existe para a UI poder dizer "Errou" em vez de mostrar
        /// um zero — que o jogador leria como imunidade do alvo.
        /// </summary>
        [Test]
        public void OGolpeQueErra_NaoCausaDanoENaoCritica()
        {
            var arma = Arma(precisao: 0.9f, chanceCritica: 1f);
            var fonte = new FonteFalsa(0.5f, 0.95f);   // 0,95 > 0,9 → erra

            var r = ResolucaoDeGolpe.Resolver(Golpe(), arma, fonte: fonte);

            Assert.IsTrue(r.Errou, "O golpe passou da precisão e mesmo assim conectou.");
            Assert.AreEqual(0f, r.Dano, 0.001f);
            Assert.IsFalse(r.Critico, "Um golpe que não aconteceu não pode ter sido crítico.");

            Assert.AreEqual(2, fonte.Consumidos,
                "Errar consome DOIS números (faixa + acerto) e retorna antes do crítico. " +
                "É contrato: mudar isso quebra todo teste determinístico daqui.");
        }

        [Test]
        public void PrecisaoCheia_NuncaErra()
        {
            var fonte = new FonteFalsa(0.5f, 0.9999f, 0.99f);

            var r = ResolucaoDeGolpe.Resolver(Golpe(), Arma(precisao: 1f), fonte: fonte);

            Assert.IsFalse(r.Errou,
                "Precisão 1,0 tem de ser garantia. Um erro de arredondamento aqui viraria " +
                "'às vezes o golpe não sai' — o defeito mais difícil de reproduzir que existe.");
        }

        // ── Crítico ───────────────────────────────────────────────────────────

        [Test]
        public void OCritico_MultiplicaODanoEMarcaOResultado()
        {
            var arma = Arma(40f, 60f, chanceCritica: 0.5f, multiplicador: 2f);
            var fonte = new FonteFalsa(0.5f, 0f, 0.1f);   // 0,1 < 0,5 → critica

            var r = ResolucaoDeGolpe.Resolver(Golpe(), arma, fonte: fonte);

            Assert.IsTrue(r.Critico);
            Assert.AreEqual(100f, r.Dano, 0.001f);
            Assert.AreEqual(3, fonte.Consumidos, "Acertar consome TRÊS números.");
        }

        [Test]
        public void OMultiplicadorAbaixoDeUm_NaoReduzODano()
        {
            var arma = new PerfilDeArma(50f, 50f, 1f, multiplicadorCritico: 0.5f, precisao: 1f);

            var r = ResolucaoDeGolpe.Resolver(Golpe(), arma,
                                              fonte: new FonteFalsa(0.5f, 0f, 0f));

            Assert.AreEqual(50f, r.Dano, 0.001f,
                "Crítico que REDUZ dano seria armadilha silenciosa de autoria: o Inspector " +
                "aceitaria 0,5 e o jogador levaria menos dano ao ter sorte.");
        }

        // ── Escala por nível ──────────────────────────────────────────────────

        [Test]
        public void ONivel1_ValeExatamenteOAutorado()
        {
            Assert.AreEqual(1f, EscalaDeNivel.FatorDeDano(1), 0.0001f,
                "Nível 1 tem de devolver o valor do Inspector sem tocar em nada — foi essa a " +
                "condição para migrar as armas sem rebalancear o jogo inteiro junto.");

            Assert.AreEqual(1f, EscalaDeNivel.FatorDeDano(0), 0.0001f,
                "Nível 0 é dado não serializado ou erro de autoria, nunca 'mais fraco que o " +
                "começo'.");
        }

        [Test]
        public void OFatorDeNivel_CresceLinearmente()
        {
            Assert.AreEqual(1.25f, EscalaDeNivel.FatorDeDano(2), 0.0001f);
            Assert.AreEqual(1.50f, EscalaDeNivel.FatorDeDano(3), 0.0001f);

            // Teto da curva de Exposição: 12 níveis (Progressao).
            Assert.AreEqual(3.75f, EscalaDeNivel.FatorDeDano(12), 0.0001f,
                "No teto, uma arma bate 3,75× o que batia no começo. Se este número mudar, " +
                "TODO encontro do jogo muda junto — é para isso que a lei é única.");
        }

        [Test]
        public void UmaArmaDeNivelMaior_BateMais()
        {
            var t1 = new PerfilDeArma(40f, 60f, 0f, 2f, 1f);

            float fator = EscalaDeNivel.FatorDeDano(4);
            var t4 = new PerfilDeArma(40f * fator, 60f * fator, 0f, 2f, 1f);

            Assert.Greater(t4.RolarDanoBranco(null), t1.RolarDanoBranco(null),
                "É a afirmação que o jogo inteiro não conseguia fazer até hoje: duas cópias da " +
                "mesma arma podem ser diferentes.");

            Assert.AreEqual(87.5f, t4.RolarDanoBranco(null), 0.001f);
        }

        // ── Afixos ────────────────────────────────────────────────────────────

        [Test]
        public void OAumentoPercentual_MultiplicaODanoTotal()
        {
            var r = ResolucaoDeGolpe.Resolver(Golpe(), Arma(40f, 60f),
                                              aumentoPercentual: 0.2f,
                                              fonte: new FonteFalsa(0.5f, 0f, 0.99f));

            Assert.AreEqual(60f, r.Dano, 0.001f, "50 × 1,2 = 60.");
        }

        [Test]
        public void OsBonusDeEquipamento_SomamAoPerfilDaArma()
        {
            var arma = Arma(50f, 50f, chanceCritica: 0f, multiplicador: 2f, precisao: 0.5f);

            // Precisão 0,5 + 0,5 de equipamento = 1,0: o golpe passa a nunca errar.
            var r = ResolucaoDeGolpe.Resolver(Golpe(), arma, bonusPrecisao: 0.5f,
                                              fonte: new FonteFalsa(0.5f, 0.99f, 0.99f));

            Assert.IsFalse(r.Errou,
                "Precisão de equipamento tem de somar à da arma — senão o atributo é " +
                "decorativo, que é o defeito que a Fase 0 acabou de fechar em dois outros.");
        }

        // ── O punho ───────────────────────────────────────────────────────────

        [Test]
        public void ODesarmado_NaoCausaDanoENaoErra()
        {
            var punho = PerfilDeArma.Desarmado;

            Assert.IsFalse(punho.TemDanoBranco);
            Assert.AreEqual(1f, punho.Precisao, 0.001f,
                "Errar um golpe que já não causa dano só confundiria o jogador.");
        }
    }
}
