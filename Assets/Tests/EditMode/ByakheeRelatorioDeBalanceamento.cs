using NUnit.Framework;
using FavelaAmarela.Core.Combat;
using FavelaAmarela.Core.Enemies;

namespace FavelaAmarela.Tests.EditMode
{
    /// <summary>
    /// <b>Não é teste de regressão — é instrumento de balanceamento.</b> Imprime os números
    /// reais da luta do Byakhee no console do test runner, para o Vini calibrar sem precisar
    /// entrar em Play Mode nem confiar em conta de cabeça.
    ///
    /// <para>Nasceu porque a primeira estimativa desta luta, feita à mão, errou por completo.
    /// Enquanto o <c>CarcosaDebuggerWindow</c> não existir (ver relatório da branch
    /// <c>develop_items</c>), este é o jeito mais barato de auditar os números.</para>
    ///
    /// <para>Roda junto com a suíte e nunca falha: quem julga se o número está bom é o
    /// <see cref="LutaContraByakheeTests"/>.</para>
    /// </summary>
    public class ByakheeRelatorioDeBalanceamento
    {
        private const float Defesa = 8f;
        private const float Dt = 0.02f;

        [Test]
        public void ImprimirTabelaDaLuta()
        {
            var armas = new (string nome, float dano, float cd)[]
            {
                ("Cravo de Aklo", 40f, 0.5f),
                ("Estilete de Irem", 25f, 0.3f),
                ("Alfanje de Alhazred", 45f, 0.7f),
            };

            // Taxa de acerto simula jogo imperfeito: 1,0 = perfeito; 0,7 = erra ~1 em cada 3
            // oportunidades de golpe dentro da janela.
            var taxas = new[] { 1.0f, 0.85f, 0.7f };

            TestContext.WriteLine("=== LUTA CONTRA O BYAKHEE — Vitalidade 500, defesa 8 ===");
            TestContext.WriteLine("(RM inicial 100; grito passivo 2/s; frenesi 5/s)");
            TestContext.WriteLine("");

            foreach (var (nome, dano, cd) in armas)
            {
                foreach (var taxa in taxas)
                {
                    var r = Simular(500f, dano, cd, taxa, semente: 12345);

                    string veredito = r.venceu ? "VENCEU" : "COLAPSOU";
                    TestContext.WriteLine(
                        $"{nome,-22} acerto={taxa,4:P0}  {veredito,-9} " +
                        $"tempo={r.segundos,5:F1}s  RM restante={r.rm,5:F0}/100  " +
                        $"pousos={r.pousos}  circundou={(r.circundou ? "sim" : "NAO")}");
                }
                TestContext.WriteLine("");
            }

            Assert.Pass("Relatório impresso — ver saída acima.");
        }

        private static (bool venceu, float rm, float segundos, int pousos, bool circundou)
            Simular(float vitalidadeMax, float dano, float cooldown, float taxaDeAcerto, int semente)
        {
            var rng = new System.Random(semente);
            var fsm = new ByakheeFSM();
            fsm.IniciarLuta();

            var vida = new Vitalidade(vitalidadeMax);
            var rm = ResilienciaMental.ComThresholdFracional(100f, 0.25f);

            float desdeAtaque = 999f;
            float tempo = 0f;
            int pousos = 0;
            bool circundou = false;
            var estadoAnterior = fsm.CurrentState;

            for (int i = 0; i < 1_000_000; i++)
            {
                tempo += Dt;
                desdeAtaque += Dt;

                float dreno = fsm.DrenoDeResilienciaPorSegundo;
                if (dreno > 0f) rm.SofrerTrauma(dreno * Dt);
                if (rm.IsColapso) return (false, 0f, tempo, pousos, circundou);

                if (fsm.CurrentState == ByakheeState.Circundando) circundou = true;
                if (fsm.CurrentState == ByakheeState.Pousado && estadoAnterior != ByakheeState.Pousado)
                    pousos++;
                estadoAnterior = fsm.CurrentState;

                if (fsm.PodeReceberDano && desdeAtaque >= cooldown)
                {
                    desdeAtaque = 0f;

                    if (rng.NextDouble() < taxaDeAcerto)
                    {
                        if (fsm.CurrentState == ByakheeState.Frenesi) fsm.InterromperFrenesi();

                        vida.Ferir(System.Math.Max(dano * 0.15f, dano - Defesa));
                        if (vida.EstaAbatido) return (true, rm.Atual, tempo, pousos, circundou);
                    }
                }

                fsm.AtualizarFracaoDeVida(vida.Percentual);
                fsm.Tick(Dt);
            }

            return (false, rm.Atual, tempo, pousos, circundou);
        }
    }
}
