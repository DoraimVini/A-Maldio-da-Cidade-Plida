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
    /// <para>Nasceu porque a primeira estimativa desta luta, feita à mão, errou por completo.</para>
    ///
    /// <para><b>Lê a ficha do disco</b> em vez de hardcodar. Os valores estavam fixos aqui, e foi
    /// exatamente por isso que o bug de serialização de 2026-08-12 passou meses sem ser notado:
    /// o relatório mostrava a luta PRETENDIDA (500 de Vitalidade) enquanto o jogo rodava a REAL
    /// (100, porque a ficha não carregava). Lendo o asset, relatório e jogo não divergem.</para>
    ///
    /// <para><b>Modela os dois canais</b> desde 2026-08-12: o grito drena Resiliência e as garras
    /// ferem a Vitalidade. Em combate <b>não há Refúgio</b> — poste de luz é recuperação entre
    /// encontros, nunca durante. Dentro da luta só existem <b>consumível e esquiva</b>.</para>
    ///
    /// <para>Roda junto com a suíte e nunca falha: quem julga se o número está bom é o
    /// <see cref="LutaContraByakheeTests"/>.</para>
    /// </summary>
    public class ByakheeRelatorioDeBalanceamento
    {
        private const string FichaDoByakhee = "Assets/FavelaAmarela/Config/Ficha_Byakhee.asset";
        private const string FichaDoDamiao = "Assets/FavelaAmarela/Config/Ficha_Damiao.asset";
        private const float Dt = 0.02f;

        // Valores do ByakheeAI (conferidos no Byakhee.prefab).
        private const float DanoDasGarras = 26f;
        private const float TraumaDoGrito = 20f;

        // Consumíveis, dos ItemDef.
        private const float RmPorErva = 25f;
        private const float VitalidadePorAgua = 30f;
        private const float LimiarDeRm = 30f;
        private const float LimiarDeVitalidade = 35f;

        // GerenciadorDeVigor.
        private const float VigorMaximo = 100f;
        private const float CustoDaEsquiva = 25f;
        private const float RegeneracaoDeVigor = 25f;

        [Test]
        public void ImprimirTabelaDaLuta()
        {
            var byakhee = AssetDatabase.LoadAssetAtPath<FichaAtributosConfig>(FichaDoByakhee);
            var damiao = AssetDatabase.LoadAssetAtPath<FichaAtributosConfig>(FichaDoDamiao);

            Assert.IsNotNull(byakhee, $"Ficha não encontrada: {FichaDoByakhee}");
            Assert.IsNotNull(damiao, $"Ficha não encontrada: {FichaDoDamiao}");

            float golpeDasGarras = MitigacaoDeDano.Aplicar(DanoDasGarras, damiao.Defesa);

            TestContext.WriteLine("=== LUTA CONTRA O BYAKHEE — fichas lidas do disco ===");
            TestContext.WriteLine($"Byakhee: vitalidade {byakhee.VitalidadeMax:F0} | defesa {byakhee.Defesa:F0}");
            TestContext.WriteLine($"Damião:  vitalidade {damiao.VitalidadeMax:F0} | defesa {damiao.Defesa:F0} | vigor {VigorMaximo:F0}");
            TestContext.WriteLine("");
            TestContext.WriteLine($"Garras {DanoDasGarras:F0} − defesa {damiao.Defesa:F0} = " +
                                  $"{golpeDasGarras:F0} por acerto → " +
                                  $"{damiao.VitalidadeMax / golpeDasGarras:F0} golpes matam.");
            TestContext.WriteLine($"Grito direcionado: {TraumaDoGrito:F0} de RM (fase 2+). " +
                                  "Grito passivo: 2/s, frenesi 5/s.");
            TestContext.WriteLine("");
            TestContext.WriteLine("SEM REFÚGIO em combate: só consumível e esquiva. Esquiva custa " +
                                  $"{CustoDaEsquiva:F0} de vigor (regen {RegeneracaoDeVigor:F0}/s).");
            TestContext.WriteLine("Bolsa = 2 Ervas (25 RM) + 2 Águas (30 vitalidade), usadas ao " +
                                  $"cair abaixo de {LimiarDeRm:F0} RM / {LimiarDeVitalidade:F0} vitalidade.");
            TestContext.WriteLine("");

            // Sangramento por golpe: só o Estilete abre feridas. É a identidade dele — o
            // Sangramento existe justamente porque ele tem o menor dano do baú e perderia a
            // disputa por dano-por-janela. Ignorá-lo aqui subestimava a arma inteira.
            var armas = new Arma[]
            {
                new Arma("Cravo de Aklo", 40f, 0.5f),
                new Arma("Estilete de Irem", 30f, 0.3f, sangraPorSeg: 4f, duracaoSangra: 5f, acumulos: 1),
                new Arma("Alfanje de Alhazred", 45f, 0.7f),
            };

            foreach (var arma in armas)
            {
                // acerto = golpes que conectam na janela; esquiva = mergulhos evitados.
                Linha(arma, acerto: 1.00f, esquiva: 0.90f, bolsa: false, byakhee, damiao);
                Linha(arma, acerto: 0.85f, esquiva: 0.75f, bolsa: false, byakhee, damiao);
                Linha(arma, acerto: 0.70f, esquiva: 0.60f, bolsa: false, byakhee, damiao);
                Linha(arma, acerto: 0.70f, esquiva: 0.60f, bolsa: true, byakhee, damiao);
                TestContext.WriteLine("");
            }

            Assert.Pass("Relatório impresso — ver saída acima.");
        }

        /// <summary>Uma arma do baú da Tumba, com o sangramento que ela abre (0 se não sangra).</summary>
        private readonly struct Arma
        {
            public readonly string Nome;
            public readonly float Dano;
            public readonly float Cooldown;
            public readonly float SangraPorSeg;
            public readonly float DuracaoSangra;
            public readonly int Acumulos;

            public Arma(string nome, float dano, float cooldown,
                float sangraPorSeg = 0f, float duracaoSangra = 0f, int acumulos = 0)
            {
                Nome = nome; Dano = dano; Cooldown = cooldown;
                SangraPorSeg = sangraPorSeg; DuracaoSangra = duracaoSangra; Acumulos = acumulos;
            }

            public bool Sangra => Acumulos > 0 && SangraPorSeg > 0f;
        }

        private static void Linha(Arma arma, float acerto, float esquiva,
            bool bolsa, FichaAtributosConfig byakhee, FichaAtributosConfig damiao)
        {
            var r = Simular(byakhee, damiao, arma, acerto, esquiva,
                ervas: bolsa ? 2 : 0, aguas: bolsa ? 2 : 0, semente: 12345);

            string estouros = arma.Sangra ? $" estouros={r.estouros}" : "";

            TestContext.WriteLine(
                $"{arma.Nome,-22} acerto={acerto,4:P0} esquiva={esquiva,4:P0} bolsa={(bolsa ? "sim" : "nao"),-3}  " +
                $"{r.desfecho,-16} tempo={r.segundos,5:F1}s  " +
                $"RM={r.rm,3:F0}/100  vida={r.vida,3:F0}/{damiao.VitalidadeMax:F0}  " +
                $"garradas={r.garradas}  pousos={r.pousos}{estouros}");
        }

        private static (string desfecho, float rm, float vida, float segundos, int pousos, int garradas, int estouros)
            Simular(FichaAtributosConfig fichaByakhee, FichaAtributosConfig fichaDamiao,
                Arma arma, float taxaDeAcerto, float taxaDeEsquiva,
                int ervas, int aguas, int semente)
        {
            float dano = arma.Dano;
            float cooldown = arma.Cooldown;

            var rng = new System.Random(semente);
            var fsm = new ByakheeFSM();
            fsm.IniciarLuta();

            // O Byakhee é Aparição Primordial: o estouro usa dano percentual (10% da vida
            // máxima, com teto 60), e não o valor fixo dos inimigos comuns.
            var sangramento = new Sangramento();
            float danoDoEstouro = ExplosaoDeSangramento.Calcular(
                fichaByakhee.VitalidadeMax, ehAparicaoPrimordial: true);
            int estouros = 0;

            var vidaDoByakhee = new Vitalidade(fichaByakhee.VitalidadeMax);
            var vidaDoDamiao = new Vitalidade(fichaDamiao.VitalidadeMax);
            var rm = ResilienciaMental.ComThresholdFracional(100f, 0.25f);

            float golpeDasGarras = MitigacaoDeDano.Aplicar(DanoDasGarras, fichaDamiao.Defesa);
            float vigor = VigorMaximo;

            float desdeAtaque = 999f;
            float tempo = 0f;
            int pousos = 0;
            int garradas = 0;
            var estadoAnterior = fsm.CurrentState;

            for (int i = 0; i < 1_000_000; i++)
            {
                tempo += Dt;
                desdeAtaque += Dt;
                vigor = System.Math.Min(VigorMaximo, vigor + RegeneracaoDeVigor * Dt);

                // ── O que o Byakhee faz com Damião ──────────────────────────
                float dreno = fsm.DrenoDeResilienciaPorSegundo;
                if (dreno > 0f) rm.SofrerTrauma(dreno * Dt);

                bool entrouEm(ByakheeState e) =>
                    fsm.CurrentState == e && estadoAnterior != e;

                // Mergulho de garras: o único golpe que fere o CORPO. Esquivar sai de graça em
                // vida mas custa vigor — sem vigor, o golpe entra.
                if (entrouEm(ByakheeState.MergulhoDeGarras))
                {
                    bool esquivou = vigor >= CustoDaEsquiva && rng.NextDouble() < taxaDeEsquiva;
                    if (esquivou)
                    {
                        vigor -= CustoDaEsquiva;
                    }
                    else
                    {
                        vidaDoDamiao.Ferir(golpeDasGarras);
                        garradas++;
                    }
                }

                // Cone de pressão sonora: fere a MENTE, não o corpo. Aproxima o
                // OnGritoEmitido, que dispara depois do telegrama.
                if (entrouEm(ByakheeState.GritoDirecionado)) rm.SofrerTrauma(TraumaDoGrito);

                // ── Consumíveis: a única cura em combate ────────────────────
                if (ervas > 0 && rm.Atual > 0f && rm.Atual < LimiarDeRm)
                {
                    rm.Ancorar(RmPorErva);
                    ervas--;
                }

                if (aguas > 0 && !vidaDoDamiao.EstaAbatido && vidaDoDamiao.Atual < LimiarDeVitalidade)
                {
                    vidaDoDamiao.Curar(VitalidadePorAgua);
                    aguas--;
                }

                if (rm.IsColapso)
                    return ("COLAPSO MENTAL", 0f, vidaDoDamiao.Atual, tempo, pousos, garradas, estouros);

                if (vidaDoDamiao.EstaAbatido)
                    return ("MORTE FISICA", rm.Atual, 0f, tempo, pousos, garradas, estouros);

                // ── Sangramento: escoa mesmo com o Byakhee no ar ────────────
                // Diferente do golpe, a ferida não espera a janela de pouso — é isso que
                // converte permanência em dano no Estilete.
                var tick = sangramento.Tick(Dt);
                if (tick.DanoContinuo > 0f) vidaDoByakhee.Ferir(tick.DanoContinuo);
                if (tick.Explodiu)
                {
                    vidaDoByakhee.Ferir(danoDoEstouro);
                    estouros++;
                }

                if (vidaDoByakhee.EstaAbatido)
                    return ("VENCEU", rm.Atual, vidaDoDamiao.Atual, tempo, pousos, garradas, estouros);

                if (fsm.CurrentState == ByakheeState.Pousado && estadoAnterior != ByakheeState.Pousado)
                    pousos++;
                estadoAnterior = fsm.CurrentState;

                // ── O que Damião faz com o Byakhee ──────────────────────────
                if (fsm.PodeReceberDano && desdeAtaque >= cooldown)
                {
                    desdeAtaque = 0f;

                    if (rng.NextDouble() < taxaDeAcerto)
                    {
                        if (fsm.CurrentState == ByakheeState.Frenesi) fsm.InterromperFrenesi();

                        vidaDoByakhee.Ferir(MitigacaoDeDano.Aplicar(dano, fichaByakhee.Defesa));

                        // Cada golpe abre mais uma ferida e renova a duração: parar de bater
                        // deixa estancar, então acumular exige manter a pressão.
                        if (arma.Sangra)
                            sangramento.Aplicar(arma.Acumulos, arma.SangraPorSeg, arma.DuracaoSangra);

                        if (vidaDoByakhee.EstaAbatido)
                            return ("VENCEU", rm.Atual, vidaDoDamiao.Atual, tempo, pousos, garradas, estouros);
                    }
                }

                fsm.AtualizarFracaoDeVida(vidaDoByakhee.Percentual);
                fsm.Tick(Dt);
            }

            return ("TEMPO ESGOTADO", rm.Atual, vidaDoDamiao.Atual, tempo, pousos, garradas, estouros);
        }
    }
}
