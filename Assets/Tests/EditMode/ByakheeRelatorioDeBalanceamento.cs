using NUnit.Framework;
using UnityEditor;
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
        private const string CaminhoDaFicha = "Assets/FavelaAmarela/Config/Ficha_Byakhee.asset";
        private const float Dt = 0.02f;

        /// <summary>Quanta RM a Erva de Ancoragem devolve (ver o <c>ItemDef</c>).</summary>
        private const float RmPorErva = 25f;

        /// <summary>Abaixo disto o jogador simulado bebe uma Erva, se ainda tiver.</summary>
        private const float LimiarDeUso = 30f;

        [Test]
        public void ImprimirTabelaDaLuta()
        {
            // Lê a ficha do disco em vez de hardcodar. Os valores estavam fixos aqui, e foi
            // exatamente por isso que o bug de serialização de 2026-08-12 passou meses sem ser
            // notado: o relatório mostrava a luta PRETENDIDA (500 de Vitalidade) enquanto o jogo
            // rodava a luta REAL (100, porque a ficha não carregava). Lendo o asset, relatório e
            // jogo não podem mais divergir.
            var ficha = AssetDatabase.LoadAssetAtPath<FichaAtributosConfig>(CaminhoDaFicha);
            Assert.IsNotNull(ficha, $"Ficha não encontrada: {CaminhoDaFicha}");

            float vitalidade = ficha.VitalidadeMax;
            float defesa = ficha.Defesa;

            var armas = new (string nome, float dano, float cd)[]
            {
                ("Cravo de Aklo", 40f, 0.5f),
                ("Estilete de Irem", 25f, 0.3f),
                ("Alfanje de Alhazred", 45f, 0.7f),
            };

            // Taxa de acerto simula jogo imperfeito: 1,0 = perfeito; 0,7 = erra ~1 em cada 3
            // oportunidades de golpe dentro da janela.
            var taxas = new[] { 1.0f, 0.85f, 0.7f };

            TestContext.WriteLine($"=== LUTA CONTRA O BYAKHEE — lido de {CaminhoDaFicha} ===");
            TestContext.WriteLine($"Vitalidade {vitalidade:F0} | defesa {defesa:F0} | " +
                                  $"resistência anômala {ficha.ResistenciaAnomala:F0} | " +
                                  $"mente {ficha.ResilienciaMax:F0}");
            TestContext.WriteLine("(Damião: RM inicial 100; grito passivo 2/s; frenesi 5/s)");
            TestContext.WriteLine("");
            TestContext.WriteLine("'ervas' = quantas Ervas de Ancoragem (25 RM) o jogador bebe " +
                                  "ao cair abaixo de 30 de RM.");
            TestContext.WriteLine("");

            foreach (var (nome, dano, cd) in armas)
            {
                foreach (var taxa in taxas)
                {
                    Linha(nome, dano, cd, taxa, vitalidade, defesa, ervas: 0);
                }

                // O mesmo cenário com uma Erva no bolso: até 2026-08-12 não havia como obter
                // consumível nenhum no mundo, então a coluna de baixo era teoria. Agora há 3
                // Ervas espalhadas no Deserto.
                Linha(nome, dano, cd, 0.7f, vitalidade, defesa, ervas: 1);
                TestContext.WriteLine("");
            }

            Assert.Pass("Relatório impresso — ver saída acima.");
        }

        private static void Linha(string nome, float dano, float cd, float taxa,
            float vitalidade, float defesa, int ervas)
        {
            var r = Simular(vitalidade, defesa, dano, cd, taxa, ervas, semente: 12345);

            string veredito = r.venceu ? "VENCEU" : "COLAPSOU";
            TestContext.WriteLine(
                $"{nome,-22} acerto={taxa,4:P0} ervas={ervas}  {veredito,-9} " +
                $"tempo={r.segundos,5:F1}s  RM restante={r.rm,5:F0}/100  " +
                $"pousos={r.pousos}  circundou={(r.circundou ? "sim" : "NAO")}");
        }

        private static (bool venceu, float rm, float segundos, int pousos, bool circundou)
            Simular(float vitalidadeMax, float defesa, float dano, float cooldown,
                float taxaDeAcerto, int ervas, int semente)
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

                // Bebe uma Erva ao cruzar o limiar, enquanto tiver. Modela o jogador atento,
                // não o ótimo: quem espera o Colapso não teria tempo de reagir mesmo.
                if (ervas > 0 && rm.Atual > 0f && rm.Atual < LimiarDeUso)
                {
                    rm.Ancorar(RmPorErva);
                    ervas--;
                }

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

                        vida.Ferir(System.Math.Max(dano * 0.15f, dano - defesa));
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
