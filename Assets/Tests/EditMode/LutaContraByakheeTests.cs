using NUnit.Framework;
using FavelaAmarela.Core.Combat;
using FavelaAmarela.Core.Enemies;

namespace FavelaAmarela.Tests.EditMode
{
    /// <summary>
    /// Trava a <b>intenção de dificuldade</b> da luta do Byakhee: vencível com jogo perfeito
    /// usando qualquer uma das 3 armas da Tumba, mas gastando uma fração real da Resiliência —
    /// "puxado para o difícil", não trivial, não impossível (pedido do Vini, 2026-08-11).
    ///
    /// <para>Simula a luta inteira com os POCOs de combate <b>reais</b> (não uma reimplementação
    /// paralela): <see cref="ByakheeFSM"/> para o boss, <see cref="Vitalidade"/> para o corpo
    /// dele, <see cref="ResilienciaMental"/> para a mente de Damião. O jogador simulado é
    /// perfeito — ataca a cada cooldown da arma sempre que <c>PodeReceberDano</c> é verdadeiro.
    /// Jogo imperfeito perde mais — isso é o próprio ponto do balanceamento.</para>
    ///
    /// <para><b>Contexto do número:</b> a primeira estimativa desta luta (feita de cabeça) errou
    /// para o lado pessimista — achou a luta impossível quando não era. Estes testes existem
    /// para que a próxima mudança de constante seja validada por simulação, não por intuição.</para>
    /// </summary>
    public class LutaContraByakheeTests
    {
        private const float VitalidadeByakhee = 500f;
        private const float DefesaByakhee = 8f;
        private const float MaxResiliencia = 100f;
        private const float ThresholdPanico = 25f;
        private const float Dt = 0.02f;

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

        /// <summary>
        /// Corre a luta até o Byakhee cair ou a Resiliência colapsar. Devolve se venceu e
        /// quanto de Resiliência sobrou — é o número que importa para calibrar "puxado".
        /// </summary>
        private static (bool venceu, float resilienciaRestante, float segundos) SimularLuta(Arma arma)
        {
            var fsm = new ByakheeFSM();
            fsm.IniciarLuta();

            var vidaByakhee = new Vitalidade(VitalidadeByakhee);
            var resiliencia = ResilienciaMental.ComThresholdFracional(MaxResiliencia, ThresholdPanico / MaxResiliencia);

            float tempoDesdeUltimoAtaque = 999f;
            float tempo = 0f;

            for (int passos = 0; passos < 5_000_000; passos++)
            {
                tempo += Dt;
                tempoDesdeUltimoAtaque += Dt;

                float dreno = fsm.DrenoDeResilienciaPorSegundo;
                if (dreno > 0f) resiliencia.SofrerTrauma(dreno * Dt);

                if (resiliencia.IsColapso)
                    return (false, 0f, tempo);

                if (fsm.PodeReceberDano && tempoDesdeUltimoAtaque >= arma.Cooldown)
                {
                    tempoDesdeUltimoAtaque = 0f;
                    float danoLiquido = System.Math.Max(arma.Dano * 0.15f, arma.Dano - DefesaByakhee);
                    vidaByakhee.Ferir(danoLiquido);

                    if (vidaByakhee.EstaAbatido)
                        return (true, resiliencia.Atual, tempo);
                }

                fsm.AtualizarFracaoDeVida(vidaByakhee.Percentual);

                // O frenesi só sai por golpe (não por PodeReceberDano, que é falso nele até
                // a interrupção acontecer) — um golpe do jogador o interrompe igual a um golpe
                // qualquer, e derruba o Byakhee pousado.
                if (fsm.CurrentState == ByakheeState.Frenesi && tempoDesdeUltimoAtaque >= arma.Cooldown)
                {
                    tempoDesdeUltimoAtaque = 0f;
                    fsm.InterromperFrenesi();
                }

                fsm.Tick(Dt);
            }

            return (false, resiliencia.Atual, tempo);
        }

        [TestCase(nameof(Cravo))]
        [TestCase(nameof(Estilete))]
        [TestCase(nameof(Alfanje))]
        public void JogoPerfeito_VenceComQualquerArma(string nomeArma)
        {
            var arma = nomeArma switch
            {
                nameof(Cravo) => Cravo,
                nameof(Estilete) => Estilete,
                _ => Alfanje,
            };

            var (venceu, resilienciaRestante, _) = SimularLuta(arma);

            Assert.IsTrue(venceu,
                $"A luta contra o Byakhee tem de ser vencível com {arma.Nome} em jogo " +
                "perfeito — mesma regra das 3 armas da Tumba: nenhuma pode ser 'a errada'.");

            // "Puxado para o difícil" = gasta uma fração real da RM, não sobra quase tudo.
            // Sem piso, jogo perfeito ficaria indistinguível de trivial.
            Assert.Less(resilienciaRestante, MaxResiliencia * 0.55f,
                $"Sobrou {resilienciaRestante:F0}/100 de Resiliência com {arma.Nome} em jogo " +
                "perfeito — a luta ficou fácil demais para o pedido do Vini.");
        }

        [Test]
        public void JogoPerfeito_ArmaMaisFracaEhAMaisApertada()
        {
            // Coerência com o resto do jogo (armas_da_tumba.md): o Estilete tem o menor dano
            // do baú e paga esse preço aqui também — não é acidente, é consistência de design.
            var (_, rmCravo, _) = SimularLuta(Cravo);
            var (_, rmEstilete, _) = SimularLuta(Estilete);

            Assert.Less(rmEstilete, rmCravo,
                "O Estilete deveria sobrar com menos Resiliência que o Cravo — é a arma mais " +
                "fraca em dano bruto, e essa fraqueza precisa aparecer aqui também.");
        }
    }
}
