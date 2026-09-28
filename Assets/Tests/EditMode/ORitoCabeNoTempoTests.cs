using FavelaAmarela.Core.Enemies;
using NUnit.Framework;

namespace FavelaAmarela.Tests.EditMode
{
    /// <summary>
    /// O Rito do Olhar cabe no tempo que o plano promete — medido por <b>simulação</b> de um
    /// jogador disciplinado contra a <see cref="RitoDoReiFSM"/> real.
    ///
    /// <para><b>O jogador simulado:</b> fica exposto no Altar até a mente cair a um piso, corre
    /// para a sombra, ancora até um teto, volta. Na Fase 2 foge da Máscara quando ela avisa. Na
    /// Fase 3 busca o fragmento quando tem mente para pagar. Na Fase 4 a sombra fica mais longe a
    /// cada Nobre desfeito, e some quando acabam. Cada travessia custa tempo <b>exposto fora do
    /// Altar</b> — é o preço de sair e voltar. Nada disso é o jogo perfeito; é um jogo limpo.</para>
    ///
    /// <para><b>O que guarda:</b> a promessa do plano (2 min a 3 min 30 s; nenhuma fase abaixo de
    /// 15 s nem acima de 60 s), que as relíquias pesam, e que a luta sem nenhuma continua vencível.
    /// Mudar um número do rito sem olhar para o relógio reprova aqui, não no playtest.</para>
    /// </summary>
    public sealed class ORitoCabeNoTempoTests
    {
        private const float Dt = 0.05f;
        private const int Coberturas = 5;   // o último resiste ao Verbo (ParametrosDoVerbo)
        private const int Altares = 3;

        /// <summary>Segundos de corrida entre o Altar e a sombra mais próxima.</summary>
        private const float Travessia = 1.0f;

        /// <summary>Quanto cada Nobre desfeito afasta a sombra seguinte, em segundos.</summary>
        private const float TravessiaExtraPorNobreDesfeito = 0.3f;

        /// <summary>Segundos até o altar do fragmento, exposto.</summary>
        private const float IdaAoFragmento = 1.2f;

        private const float Piso = 25f;
        private const float Teto = 90f;
        private const float MenteParaIrAoFragmento = 55f;

        private enum Onde { Altar, Sombra, IndoParaSombra, IndoParaAltar, IndoAoFragmento, NoFragmento }

        private sealed class Resultado
        {
            public bool Selou;
            public bool Colapsou;
            public float Total;
            public readonly float[] DuracaoDaFase = new float[8];
        }

        private static Resultado Simular(ModificadoresDoRito mods)
        {
            var p = new ParametrosDoRito();
            var rito = new RitoDoReiFSM(p, mods, Coberturas, Altares);
            var r = new Resultado();

            bool avisoDaMascara = false;
            rito.Olhar.OnMascaraVaiAbrir += () => avisoDaMascara = true;
            rito.OnSelado += () => r.Selou = true;
            rito.OnColapso += () => r.Colapsou = true;

            rito.Iniciar();

            float mente = 100f;
            var onde = Onde.Altar;
            float relogioDaTravessia = 0f;

            for (float t = 0f; t < 600f && !rito.Encerrado; t += Dt)
            {
                var fase = rito.Fase;
                r.DuracaoDaFase[(int)fase] += Dt;
                bool semSombra = rito.Verbo.Desfeitas >= Coberturas;
                float travessia = Travessia + rito.Verbo.Desfeitas * TravessiaExtraPorNobreDesfeito;

                // ── decisões ──
                switch (onde)
                {
                    case Onde.Altar:
                        bool mascaraVem = fase == FaseDoRito.Mascara && (avisoDaMascara || rito.MascaraAberta);
                        bool fragmentoChama = fase == FaseDoRito.Peca && rito.Fragmentos.Ativo >= 0
                                              && mente >= MenteParaIrAoFragmento;
                        if (fragmentoChama) { onde = Onde.IndoAoFragmento; relogioDaTravessia = IdaAoFragmento; }
                        else if (!semSombra && (mente <= PisoPara(fase, travessia, p, mods) || mascaraVem))
                        { onde = Onde.IndoParaSombra; relogioDaTravessia = travessia; }
                        break;
                    case Onde.Sombra:
                        avisoDaMascara = false;
                        bool mascaraAindaAberta = fase == FaseDoRito.Mascara && rito.MascaraAberta;
                        if ((mente >= Teto && !mascaraAindaAberta) || semSombra || fase == FaseDoRito.Queda)
                        { onde = Onde.IndoParaAltar; relogioDaTravessia = travessia; }
                        break;
                    case Onde.NoFragmento:
                        if (rito.Fragmentos.Ativo < 0) { onde = Onde.IndoParaAltar; relogioDaTravessia = IdaAoFragmento; }
                        break;
                    default:
                        relogioDaTravessia -= Dt;
                        if (relogioDaTravessia <= 0f)
                            onde = onde == Onde.IndoParaSombra ? Onde.Sombra
                                 : onde == Onde.IndoAoFragmento ? Onde.NoFragmento
                                 : Onde.Altar;
                        break;
                }

                // ── o mundo ──
                bool linhaLivre = onde != Onde.Sombra;
                bool noAltar = onde == Onde.Altar;
                int altarSobOPe = onde == Onde.NoFragmento ? rito.Fragmentos.Ativo : -1;
                // O Altar e o caminho até ele ficam na frente do Rei (ângulo 0).
                var leitura = new LeituraDoRito(linhaLivre, 0f, noAltar, altarSobOPe);

                var resultado = rito.Tick(Dt, leitura);
                mente = System.Math.Min(100f, mente + resultado.DeltaResiliencia);
                if (mente <= 0f) rito.Colapsar();

                r.Total = t;
            }

            return r;
        }

        /// <summary>
        /// O piso do jogador disciplinado: ele sai do Altar com mente para pagar a travessia (e o
        /// pulso do Verbo, se vier no caminho), mais uma folga. É o que um jogador faz olhando a
        /// própria barra — um piso fixo matava o simulado quando a sombra ficava longe.
        /// </summary>
        private static float PisoPara(FaseDoRito fase, float travessia, ParametrosDoRito p, ModificadoresDoRito m)
        {
            float mascara = fase == FaseDoRito.Mascara ? ExposicaoAoRei.DrenoComAMascaraAberta : 1f;
            float pulso = fase == FaseDoRito.Verbo ? p.Verbo.Custo : 0f;
            return System.Math.Max(Piso, 10f + travessia * p.DrenoDa(fase) * m.Dreno * mascara + pulso);
        }

        private static ModificadoresDoRito Com(bool necro, bool patua, bool anel)
            => ModificadoresDoRito.DasReliquias(necro, patua, anel, new ParametrosDoRito());

        [Test]
        public void ComAsTresReliquias_ARitoDuraEntreDoisETresMinutosEMeio()
        {
            var r = Simular(Com(true, true, true));

            TestContext.WriteLine($"total {r.Total:F1} s | chegada {r.DuracaoDaFase[(int)FaseDoRito.Chegada]:F1} " +
                                  $"| máscara {r.DuracaoDaFase[(int)FaseDoRito.Mascara]:F1} " +
                                  $"| peça {r.DuracaoDaFase[(int)FaseDoRito.Peca]:F1} " +
                                  $"| verbo {r.DuracaoDaFase[(int)FaseDoRito.Verbo]:F1} " +
                                  $"| queda {r.DuracaoDaFase[(int)FaseDoRito.Queda]:F1}");

            Assert.IsTrue(r.Selou, $"O jogador disciplinado não selou (colapsou: {r.Colapsou}).");
            Assert.That(r.Total, Is.InRange(120f, 210f), "Fora da janela de 2 min a 3 min 30 s.");
        }

        [Test]
        public void ComAsTresReliquias_NenhumaFaseCurtaDemaisNemLongaDemais()
        {
            var r = Simular(Com(true, true, true));
            foreach (var fase in new[] { FaseDoRito.Chegada, FaseDoRito.Mascara, FaseDoRito.Peca, FaseDoRito.Verbo, FaseDoRito.Queda })
                Assert.That(r.DuracaoDaFase[(int)fase], Is.InRange(12f, 62f),
                    $"A fase {fase} durou {r.DuracaoDaFase[(int)fase]:F1} s — o plano pede de 15 a 60.");
        }

        [Test]
        public void SemOPatua_ALutaFicaMaisLonga()
        {
            var com = Simular(Com(true, true, true));
            var sem = Simular(Com(true, false, true));
            Assert.IsTrue(sem.Selou, "Sem o Patuá a luta tem de continuar vencível.");
            Assert.Greater(sem.Total, com.Total * 1.15f,
                $"Sem o Patuá: {sem.Total:F0} s contra {com.Total:F0} s — a relíquia quase não pesa.");
        }

        [Test]
        public void SemNenhumaReliquia_AindaEVencivel()
        {
            var r = Simular(ModificadoresDoRito.Nenhum);
            Assert.IsTrue(r.Selou, $"Sem relíquias o rito virou impossível (colapsou: {r.Colapsou}, {r.Total:F0} s).");
        }
    }
}
