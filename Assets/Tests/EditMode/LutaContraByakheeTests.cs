using NUnit.Framework;
using FavelaAmarela.Core.Combat;
using FavelaAmarela.Core.Enemies;

namespace FavelaAmarela.Tests.EditMode
{
    /// <summary>
    /// Trava a <b>intenção de dificuldade</b> da luta do Byakhee: vencível com jogo perfeito
    /// usando qualquer uma das 3 armas da Tumba, mas gastando uma fração real da Resiliência —
    /// "equilíbrio levemente puxado para o difícil" (pedido do Vini, 2026-08-11).
    ///
    /// <para>Simula a luta com os POCOs de combate <b>reais</b> (<see cref="ByakheeFSM"/>,
    /// <see cref="Vitalidade"/>, <see cref="ResilienciaMental"/>) e não com uma reimplementação
    /// paralela — reimplementar a regra no teste faria o teste concordar com o próprio erro.</para>
    ///
    /// <para><b>Por que este arquivo existe:</b> a primeira estimativa desta luta foi feita de
    /// cabeça e errou feio (concluiu "impossível" quando era vencível). Toda mudança de
    /// constante daqui em diante deve ser validada por estes testes, não por intuição.</para>
    /// </summary>
    public class LutaContraByakheeTests
    {
        private const float VitalidadeByakhee = 500f;
        private const float DefesaByakhee = 8f;
        private const float MaxResiliencia = 100f;
        private const float Dt = 0.02f;

        /// <summary>Uma das 3 armas da Tumba, com o dano e a cadência reais dela.</summary>
        private readonly struct Arma
        {
            public readonly string Nome;
            public readonly float Dano;
            public readonly float Cooldown;

            public Arma(string nome, float dano, float cooldown)
            {
                Nome = nome;
                Dano = dano;
                Cooldown = cooldown;
            }
        }

        private static readonly Arma Cravo = new Arma("Cravo de Aklo", 40f, 0.5f);
        private static readonly Arma Estilete = new Arma("Estilete de Irem", 25f, 0.3f);
        private static readonly Arma Alfanje = new Arma("Alfanje de Alhazred", 45f, 0.7f);

        private static Arma PorNome(string nome) => nome switch
        {
            nameof(Cravo) => Cravo,
            nameof(Estilete) => Estilete,
            _ => Alfanje,
        };

        private readonly struct Resultado
        {
            public readonly bool Venceu;
            public readonly float ResilienciaRestante;
            public readonly float Segundos;
            public readonly bool CircundouAlgumaVez;

            public Resultado(bool venceu, float rm, float segundos, bool circundou)
            {
                Venceu = venceu;
                ResilienciaRestante = rm;
                Segundos = segundos;
                CircundouAlgumaVez = circundou;
            }
        }

        /// <summary>
        /// Corre a luta com um jogador <b>perfeito</b>: ataca a cada cooldown sempre que a
        /// janela está aberta, e nunca leva dano evitável. Jogo real rende menos — é justamente
        /// essa folga que separa "difícil" de "impossível".
        /// </summary>
        private static Resultado Simular(Arma arma)
        {
            var fsm = new ByakheeFSM();
            fsm.IniciarLuta();

            var vida = new Vitalidade(VitalidadeByakhee);
            var resiliencia = ResilienciaMental.ComThresholdFracional(MaxResiliencia, 0.25f);

            float desdeUltimoAtaque = 999f;
            float tempo = 0f;
            bool circundou = false;

            for (int passos = 0; passos < 1_000_000; passos++)
            {
                tempo += Dt;
                desdeUltimoAtaque += Dt;

                float dreno = fsm.DrenoDeResilienciaPorSegundo;
                if (dreno > 0f) resiliencia.SofrerTrauma(dreno * Dt);
                if (resiliencia.IsColapso) return new Resultado(false, 0f, tempo, circundou);

                if (fsm.CurrentState == ByakheeState.Circundando) circundou = true;

                if (fsm.PodeReceberDano && desdeUltimoAtaque >= arma.Cooldown)
                {
                    desdeUltimoAtaque = 0f;

                    // Mesma fórmula de MitigacaoDeDano: subtrativa com piso de 15%.
                    float liquido = System.Math.Max(arma.Dano * 0.15f, arma.Dano - DefesaByakhee);

                    // O golpe que acerta durante o Frenesi também o interrompe.
                    if (fsm.CurrentState == ByakheeState.Frenesi) fsm.InterromperFrenesi();

                    vida.Ferir(liquido);
                    if (vida.EstaAbatido)
                        return new Resultado(true, resiliencia.Atual, tempo, circundou);
                }

                fsm.AtualizarFracaoDeVida(vida.Percentual);
                fsm.Tick(Dt);
            }

            return new Resultado(false, resiliencia.Atual, tempo, circundou);
        }

        [TestCase(nameof(Cravo))]
        [TestCase(nameof(Estilete))]
        [TestCase(nameof(Alfanje))]
        public void JogoPerfeito_VenceComQualquerArma(string nomeArma)
        {
            var arma = PorNome(nomeArma);
            var r = Simular(arma);

            Assert.IsTrue(r.Venceu,
                $"A luta tem de ser vencível com {arma.Nome} em jogo perfeito — mesma regra " +
                "das 3 armas da Tumba: nenhuma pode ser 'a errada'.");
        }

        [TestCase(nameof(Cravo))]
        [TestCase(nameof(Estilete))]
        [TestCase(nameof(Alfanje))]
        public void JogoPerfeito_CustaResilienciaDeVerdade(string nomeArma)
        {
            var arma = PorNome(nomeArma);
            var r = Simular(arma);

            // Sem piso de custo, jogo perfeito ficaria indistinguível de trivial e o grito
            // infrassônico deixaria de ser o relógio que o design pede.
            Assert.Less(r.ResilienciaRestante, MaxResiliencia * 0.60f,
                $"Sobrou {r.ResilienciaRestante:F0}/100 de Resiliência com {arma.Nome} em jogo " +
                "perfeito — fácil demais para o equilíbrio pedido.");

            // E o teto: se nem o jogo perfeito sobra folga, o jogo real é impossível.
            Assert.Greater(r.ResilienciaRestante, 0f,
                $"{arma.Nome} não deixa nenhuma margem em jogo perfeito — a luta vira " +
                "impossível para qualquer jogador humano.");
        }

        [TestCase(nameof(Cravo))]
        [TestCase(nameof(Estilete))]
        [TestCase(nameof(Alfanje))]
        public void Fase3_AconteceDeVerdade(string nomeArma)
        {
            // Regressão de um bug de design real: cair para 30% durante um pouso apenas
            // ESTENDIA aquela janela em vez de fazer o Byakhee decolar. O jogador matava ele
            // ali mesmo e a fase 3 — a identidade da luta — nunca aparecia.
            var r = Simular(PorNome(nomeArma));

            Assert.IsTrue(r.CircundouAlgumaVez,
                $"Com {PorNome(nomeArma).Nome} o Byakhee morreu sem nunca circundar: a fase 3 " +
                "não está acontecendo.");
        }

        [Test]
        public void EstileteEhAOpcaoMaisApertada()
        {
            // Coerência com armas_da_tumba.md: o Estilete tem o menor dano do baú e paga esse
            // preço aqui também. Não é acidente — é a mesma identidade das 3 armas.
            var cravo = Simular(Cravo);
            var estilete = Simular(Estilete);

            Assert.Less(estilete.ResilienciaRestante, cravo.ResilienciaRestante,
                "O Estilete deveria terminar a luta com menos Resiliência que o Cravo.");
        }
    }
}
