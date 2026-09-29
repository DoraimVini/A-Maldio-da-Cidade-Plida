using System.Collections.Generic;
using FavelaAmarela.Core.Enemies;
using NUnit.Framework;

namespace FavelaAmarela.Tests.EditMode
{
    /// <summary>
    /// O Rito do Olhar, peça por peça, sem Unity. Plano em
    /// <c>Docs/KnowledgeBundle/systems/dossie_luta_do_rei.md</c>.
    /// </summary>
    public sealed class RitoDoReiTests
    {
        private static readonly ModificadoresDoRito Nenhum = ModificadoresDoRito.Nenhum;

        private static RitoDoReiFSM Novo(ParametrosDoRito p = null, int coberturas = 5, int altares = 3)
        {
            var rito = new RitoDoReiFSM(p ?? new ParametrosDoRito(), Nenhum, coberturas, altares);
            rito.Iniciar();
            return rito;
        }

        private static readonly LeituraDoRito ExpostoNoAltar = new LeituraDoRito(true, 0f, true);
        private static readonly LeituraDoRito ExpostoForaDoAltar = new LeituraDoRito(true, 0f, false);
        private static readonly LeituraDoRito Coberto = new LeituraDoRito(false, 0f, false);

        // ── ExposicaoAoRei: a tabela do laço ──────────────────────────────────

        [Test]
        public void Exposicao_ExpostoNoAltar_AvancaOSeloEDrena()
        {
            var p = new ParametrosDoRito();
            var (selo, rm) = ExposicaoAoRei.Calcular(FaseDoRito.Chegada, true, true, false, p, Nenhum, 1f);
            Assert.AreEqual(p.SeloPorSegundo, selo, 1e-4f);
            Assert.AreEqual(-p.DrenoChegada, rm, 1e-4f);
        }

        [Test]
        public void Exposicao_ExpostoForaDoAltar_SoDrena()
        {
            var p = new ParametrosDoRito();
            var (selo, rm) = ExposicaoAoRei.Calcular(FaseDoRito.Verbo, true, false, false, p, Nenhum, 1f);
            Assert.AreEqual(0f, selo);
            Assert.AreEqual(-p.DrenoVerbo, rm, 1e-4f);
        }

        [Test]
        public void Exposicao_Coberto_AncoraENaoAvanca()
        {
            var p = new ParametrosDoRito();
            var (selo, rm) = ExposicaoAoRei.Calcular(FaseDoRito.Peca, false, true, false, p, Nenhum, 1f);
            Assert.AreEqual(0f, selo, "Nenhum estado seguro avança o selo: é preciso ser visto.");
            Assert.AreEqual(p.AncoragemPorSegundo, rm, 1e-4f);
        }

        [Test]
        public void Exposicao_MascaraAberta_TriplicaODreno()
        {
            var p = new ParametrosDoRito();
            var (_, fechada) = ExposicaoAoRei.Calcular(FaseDoRito.Mascara, true, true, false, p, Nenhum, 1f);
            var (_, aberta) = ExposicaoAoRei.Calcular(FaseDoRito.Mascara, true, true, true, p, Nenhum, 1f);
            Assert.AreEqual(fechada * ExposicaoAoRei.DrenoComAMascaraAberta, aberta, 1e-4f);
        }

        [Test]
        public void Exposicao_CadaReliquiaMexeNoSeuLado()
        {
            var p = new ParametrosDoRito();
            var necro = ModificadoresDoRito.DasReliquias(true, false, false, p);
            var patua = ModificadoresDoRito.DasReliquias(false, true, false, p);
            var anel = ModificadoresDoRito.DasReliquias(false, false, true, p);

            Assert.AreEqual(p.SeloPorSegundo * p.BonusDoNecronomicon,
                ExposicaoAoRei.Calcular(FaseDoRito.Chegada, true, true, false, p, necro, 1f).deltaSelo, 1e-4f);
            Assert.AreEqual(p.AncoragemPorSegundo * p.BonusDoPatua,
                ExposicaoAoRei.Calcular(FaseDoRito.Chegada, false, false, false, p, patua, 1f).deltaResiliencia, 1e-4f);
            Assert.AreEqual(-p.DrenoChegada * p.BonusDoAnel,
                ExposicaoAoRei.Calcular(FaseDoRito.Chegada, true, false, false, p, anel, 1f).deltaResiliencia, 1e-4f);
        }

        [Test]
        public void Exposicao_NaQueda_SoOSeloAndaSozinho()
        {
            var p = new ParametrosDoRito();
            var (selo, rm) = ExposicaoAoRei.Calcular(FaseDoRito.Queda, true, false, false, p, Nenhum, p.DuracaoDaQueda);
            Assert.AreEqual(100f - p.MarcoQueda, selo, 1e-3f);
            Assert.AreEqual(0f, rm);
        }

        // ── A máquina ─────────────────────────────────────────────────────────

        [Test]
        public void Rito_NadaAcontece_AntesDeIniciar()
        {
            var rito = new RitoDoReiFSM(new ParametrosDoRito(), Nenhum, 5, 3);
            var r = rito.Tick(10f, ExpostoNoAltar);
            Assert.AreEqual(FaseDoRito.Aguardando, rito.Fase);
            Assert.AreEqual(0f, rito.Selo);
            Assert.AreEqual(0f, r.DeltaResiliencia);
        }

        [Test]
        public void Rito_AsFasesAbremNosMarcosDoSelo()
        {
            var p = new ParametrosDoRito();
            var rito = Novo(p);
            var vistas = new List<FaseDoRito>();
            rito.OnFaseMudou += (_, nova) => vistas.Add(nova);

            // Sempre exposto no Altar, sempre no centro do farol: só o selo interessa aqui.
            for (int i = 0; i < 20000 && !rito.Encerrado; i++)
            {
                var leitura = new LeituraDoRito(true, rito.Olhar.Centro, true);
                rito.Tick(0.05f, leitura);
            }

            CollectionAssert.AreEqual(new[]
            {
                FaseDoRito.Mascara, FaseDoRito.Peca, FaseDoRito.Verbo, FaseDoRito.Queda, FaseDoRito.Selado,
            }, vistas);
            Assert.AreEqual(100f, rito.Selo, 1e-3f);
        }

        [Test]
        public void Rito_Coberto_NaoSaiDoLugar()
        {
            var rito = Novo();
            rito.Tick(60f, Coberto);
            Assert.AreEqual(FaseDoRito.Chegada, rito.Fase);
            Assert.AreEqual(0f, rito.Selo);
        }

        [Test]
        public void Rito_ExpostoForaDoAltar_SoPerdeMente()
        {
            var rito = Novo();
            var r = rito.Tick(1f, ExpostoForaDoAltar);
            Assert.AreEqual(0f, rito.Selo);
            Assert.Less(r.DeltaResiliencia, 0f);
            Assert.IsTrue(r.Exposto);
        }

        [Test]
        public void Rito_Colapsar_Encerra_UmaVezSo()
        {
            var rito = Novo();
            int colapsos = 0;
            rito.OnColapso += () => colapsos++;

            rito.Colapsar();
            rito.Colapsar();

            Assert.AreEqual(FaseDoRito.Colapso, rito.Fase);
            Assert.AreEqual(1, colapsos);
            Assert.AreEqual(0f, rito.Tick(1f, ExpostoNoAltar).DeltaResiliencia, "Encerrado, nada mais anda.");
        }

        [Test]
        public void Rito_NaQueda_OSeloFechaSozinho_EmTrintaSegundos()
        {
            var p = new ParametrosDoRito { MarcoMascara = 0.01f, MarcoPeca = 0.02f, MarcoVerbo = 0.03f, MarcoQueda = 0.04f };
            var rito = Novo(p);
            rito.Tick(0.1f, ExpostoNoAltar);
            Assert.AreEqual(FaseDoRito.Queda, rito.Fase, "Marcos minúsculos: um quadro exposto chega à Queda.");

            bool selou = false;
            rito.OnSelado += () => selou = true;

            float t = 0f;
            while (!selou && t < 60f)
            {
                rito.Tick(0.1f, Coberto);   // parado na sombra: na Queda não importa
                t += 0.1f;
            }

            Assert.IsTrue(selou);
            Assert.AreEqual(p.DuracaoDaQueda, t, 0.5f);
        }

        [Test]
        public void Rito_NaMascara_ForaDoConeNaoEVisto()
        {
            var p = new ParametrosDoRito { MarcoMascara = 0.01f };
            var rito = Novo(p);
            rito.Tick(0.1f, ExpostoNoAltar);
            Assert.AreEqual(FaseDoRito.Mascara, rito.Fase);

            float longeDoCone = rito.Olhar.Centro + 170f;
            Assert.IsFalse(rito.Ve(new LeituraDoRito(true, longeDoCone, true)),
                "No farol, estar fora do cone salva mesmo sem cobertura.");
            Assert.IsTrue(rito.Ve(new LeituraDoRito(true, rito.Olhar.Centro, true)));
        }

        // ── O farol ───────────────────────────────────────────────────────────

        [Test]
        public void Olhar_VarreDeUmLadoAoOutro_NumaVarredura()
        {
            var p = new ParametrosDoOlhar();
            var olhar = new OlharDoRei(p);
            Assert.AreEqual(-p.Amplitude, olhar.Centro, 1e-3f);

            olhar.Avancar(p.DuracaoDaVarredura);
            Assert.AreEqual(p.Amplitude, olhar.Centro, 1e-3f);

            olhar.Avancar(p.DuracaoDaVarredura);
            Assert.AreEqual(-p.Amplitude, olhar.Centro, 1e-3f);
        }

        [Test]
        public void Olhar_AMascaraAbre_NaTerceiraVarredura_EAvisaAntes()
        {
            var p = new ParametrosDoOlhar();
            var olhar = new OlharDoRei(p);
            float avisoEm = -1f;
            olhar.OnMascaraVaiAbrir += () => avisoEm = olhar.Tempo;

            float aberturaEm = -1f;
            for (int i = 0; i < 1000 && aberturaEm < 0f; i++)
            {
                olhar.Avancar(0.02f);
                if (olhar.MascaraAberta) aberturaEm = olhar.Tempo;
            }

            float esperado = p.DuracaoDaVarredura * (p.VarredurasEntreAberturas - 1);
            Assert.AreEqual(esperado, aberturaEm, 0.05f);
            Assert.AreEqual(esperado - p.AvisoAntesDaAbertura, avisoEm, 0.05f);
            Assert.AreEqual(p.LarguraDoCone * 2f, olhar.Largura, 1e-3f, "Aberta, o cone dobra.");

            olhar.Avancar(p.DuracaoDaAbertura + 0.1f);
            Assert.IsFalse(olhar.MascaraAberta);
        }

        // ── Os fragmentos ─────────────────────────────────────────────────────

        [Test]
        public void Fragmentos_LerCustaEPaga_EOProximoApareceEmOutroAltar()
        {
            var p = new ParametrosDosFragmentos();
            var frag = new FragmentosDaPeca(p, 3);
            frag.Comecar();
            Assert.AreEqual(0, frag.Ativo);

            var (s1, r1) = frag.Avancar(p.TempoDeLeitura - 0.1f, 0, true);
            Assert.AreEqual(0f, s1, "Ainda lendo.");

            var (s2, r2) = frag.Avancar(0.2f, 0, true);
            Assert.AreEqual(p.Selo, s2, 1e-4f);
            Assert.AreEqual(-p.Custo, r2, 1e-4f);
            Assert.AreEqual(-1, frag.Ativo);

            frag.Avancar(p.Reaparecimento + 0.1f, -1, false);
            Assert.AreEqual(1, frag.Ativo, "O próximo nunca aparece no mesmo altar.");
        }

        [Test]
        public void Fragmentos_SairDoAltar_ZeraALeitura_ECobertoNaoLe()
        {
            var p = new ParametrosDosFragmentos();
            var frag = new FragmentosDaPeca(p, 3);
            frag.Comecar();

            frag.Avancar(p.TempoDeLeitura * 0.9f, 0, true);
            frag.Avancar(0.1f, -1, true);
            Assert.AreEqual(0f, frag.ProgressoDaLeitura, 1e-4f);

            var (s, _) = frag.Avancar(p.TempoDeLeitura * 2f, 0, false);
            Assert.AreEqual(0f, s, "Ler exige ser visto: coberto, não lê.");
        }

        // ── O Verbo ───────────────────────────────────────────────────────────

        [Test]
        public void Verbo_CadaPulsoDesfazUmaCobertura_NaOrdem_ECobraSoDoExposto()
        {
            var p = new ParametrosDoVerbo { CoberturasQueResistem = 0 };
            var verbo = new VerboDoRei(p, 2);
            var desfeitas = new List<int>();
            verbo.OnPulso += desfeitas.Add;

            Assert.AreEqual(0f, verbo.Avancar(p.Intervalo, false), "Coberto no pulso: nada perde.");
            Assert.AreEqual(-p.Custo, verbo.Avancar(p.Intervalo, true), 1e-4f);
            verbo.Avancar(p.Intervalo, false);

            CollectionAssert.AreEqual(new[] { 0, 1, -1 }, desfeitas);
            Assert.AreEqual(2, verbo.Desfeitas);
        }

        [Test]
        public void Verbo_OUltimoNobreResiste()
        {
            var verbo = new VerboDoRei(new ParametrosDoVerbo(), 3);
            for (int i = 0; i < 10; i++) verbo.Avancar(new ParametrosDoVerbo().Intervalo, false);
            Assert.AreEqual(2, verbo.Desfeitas, "De 3 coberturas, o Verbo desfaz 2 e a última resiste.");
        }

        // ── Depuração ─────────────────────────────────────────────────────────

        [Test]
        public void PularPara_PassaPorCadaFaseDoCaminho_ESoAvanca()
        {
            var rito = Novo();
            var fases = new List<FaseDoRito>();
            rito.OnFaseMudou += (_, nova) => fases.Add(nova);

            rito.PularPara(FaseDoRito.Verbo);
            CollectionAssert.AreEqual(new[] { FaseDoRito.Mascara, FaseDoRito.Peca, FaseDoRito.Verbo }, fases,
                "Pular não pode esconder transições: a cena reage a cada uma (animação, fala, fragmentos).");

            rito.PularPara(FaseDoRito.Mascara);
            Assert.AreEqual(FaseDoRito.Verbo, rito.Fase, "Pedir uma fase já passada não volta o rito.");
        }

        [Test]
        public void PularPara_Selado_DisparaOSelamento_EAntesDeIniciarNaoFazNada()
        {
            var parado = new RitoDoReiFSM(new ParametrosDoRito(), Nenhum, 5, 3);
            parado.PularPara(FaseDoRito.Queda);
            Assert.AreEqual(FaseDoRito.Aguardando, parado.Fase);

            var rito = Novo();
            bool selou = false;
            rito.OnSelado += () => selou = true;
            rito.PularPara(FaseDoRito.Selado);
            Assert.IsTrue(selou);
            Assert.IsTrue(rito.Encerrado);
        }
    }
}
