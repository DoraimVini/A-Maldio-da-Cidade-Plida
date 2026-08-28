using NUnit.Framework;
using FavelaAmarela.Core.Combat;
using UnityEditor;
using FavelaAmarela.Core.Abilities;
using FavelaAmarela.Inventario;

namespace FavelaAmarela.Tests.EditMode
{
    /// <summary>
    /// Suite das 3 armas da Tumba de Alhazred (Cravo de Aklo, Estilete de Irem, Alfanje de
    /// Alhazred).
    ///
    /// <para>Cobre: ataque básico aplica dano; cada habilidade tem seu efeito-assinatura
    /// (Cravo interrompe, Estilete sangra, Alfanje repele + atordoa); cooldown do básico e da
    /// habilidade são validados e independentes.</para>
    ///
    /// <para><b>Mudou de fonte em 2026-08-27, não de contrato.</b> Estes testes instanciavam
    /// as classes C# direto (<c>new CravoDeAklo()</c>). As três armas migraram para dado
    /// (<c>HabilidadeDef</c>) e as classes saíram, então a suíte passou a montar as armas a
    /// partir dos <b>assets que o jogo realmente usa</b>. As asserções são as mesmas — e agora
    /// valem mais, porque testam o que está no disco em vez de um default de construtor que
    /// poderia divergir do asset sem ninguém notar.</para>
    ///
    /// <para>A equivalência entre as duas formas foi provada campo a campo antes da troca, em
    /// <c>EquivalenciaDaMigracaoTests</c>.</para>
    /// </summary>
    [TestFixture]
    public class ArmasDaTumbaTests
    {
        private const string PastaDasBases = "Assets/FavelaAmarela/Config/Armas";

        /// <summary>
        /// Monta a arma a partir da <b>BaseDeArma</b>, como o jogo faz ao equipar.
        ///
        /// <para><b>Mudou em 2026-08-28.</b> Antes carregava o <c>HabilidadeDef</c> e chamava
        /// <c>Construir()</c> sem perfil — o que hoje devolve uma arma com faixa de dano ZERO,
        /// porque o dano branco passou a morar na base. O comentário anterior dizia "como o jogo
        /// faz ao equipar" e deixou de ser verdade no mesmo instante: o caminho vivo é
        /// <c>BaseDeArma.ConstruirArma(nível)</c>, e é ele que entrega o perfil de combate.</para>
        /// </summary>
        private static IArmaComHabilidade Arma(string arquivo)
        {
            var def = AssetDatabase.LoadAssetAtPath<BaseDeArma>(
                $"{PastaDasBases}/{arquivo}.asset");

            Assert.IsNotNull(def,
                $"BaseDeArma ausente: {arquivo}. Conserto: " +
                "'Tools/FavelaAmarela/Armas: montar as bases (famílias)'.");

            var arma = def.ConstruirArma();

            Assert.IsNotNull(arma,
                $"{arquivo} não tem HabilidadeDef ligada — a arma sai inerte.");

            return arma;
        }

        /// <summary>
        /// O golpe básico <b>resolvido</b>: faixa de dano branco → percentual → dano final.
        /// Fonte nula = média, sem erro nem crítico, que é o que um teste de contrato quer.
        /// </summary>
        private static ArmaResult Basico(IArmaComHabilidade arma)
            => ResolucaoDeGolpe.Resolver(arma.Execute(), arma.Perfil);

        /// <inheritdoc cref="Basico"/>
        private static ArmaResult Habilidade(IArmaComHabilidade arma)
            => ResolucaoDeGolpe.Resolver(arma.ExecuteHabilidade(), arma.Perfil);

        private static IArmaComHabilidade Cravo() => Arma("BaseArma_Cravo");
        private static IArmaComHabilidade Estilete() => Arma("BaseArma_LaminaFina");
        private static IArmaComHabilidade Alfanje() => Arma("BaseArma_Alfanje");

        // ── Ataque básico ────────────────────────────────────────────────────

        [Test]
        public void Basico_DasTresArmas_AplicaDanoESucesso()
        {
            foreach (IArmaComHabilidade arma in new[] { Cravo(), Estilete(), Alfanje() })
            {
                var r = Basico(arma);
                Assert.IsTrue(r.Success, $"{arma.NomeDaArma}: básico deveria ter sucesso.");
                Assert.Greater(r.Dano, 0f, $"{arma.NomeDaArma}: básico deveria causar dano.");
            }
        }

        [Test]
        public void Basico_NaoCarregaEfeitosDeOutrasArmas()
        {
            // O básico do Estilete NÃO interrompe conjuração (Cravo) nem repele (Alfanje).
            var r = Estilete().Execute();
            Assert.IsFalse(r.InterrompeConjuracao);
            Assert.AreEqual(0f, r.ForcaRepulsao, 0.0001f);
        }

        [Test]
        public void EstileteBasico_AcumulaSangramento()
        {
            // Decisão de design (2026-07-31): o ataque básico do Estilete acumula 1 de
            // sangramento. É o que torna o teto de 10 acúmulos alcançável — a habilidade
            // sozinha (cooldown 5 s) levaria quase um minuto para chegar lá.
            var r = Estilete().Execute();
            Assert.AreEqual(1, r.AcumulosDeSangramento);
            Assert.Greater(r.SangramentoPorSegundo, 0f);
        }

        [Test]
        public void OutrasArmas_NaoAcumulamSangramento()
        {
            // Sangramento é a assinatura do Estilete — Cravo e Alfanje não o aplicam.
            Assert.AreEqual(0, Cravo().Execute().AcumulosDeSangramento);
            Assert.AreEqual(0, Cravo().ExecuteHabilidade().AcumulosDeSangramento);
            Assert.AreEqual(0, Alfanje().Execute().AcumulosDeSangramento);
            Assert.AreEqual(0, Alfanje().ExecuteHabilidade().AcumulosDeSangramento);
        }

        [Test]
        public void EstileteHabilidade_AcumulaMaisQueOBasico()
        {
            var arma = Estilete();
            Assert.Greater(arma.ExecuteHabilidade().AcumulosDeSangramento,
                           arma.Execute().AcumulosDeSangramento,
                           "A habilidade é o empurrão guardado para a janela de dano.");
        }

        // ── Habilidades-assinatura ───────────────────────────────────────────

        [Test]
        public void CravoDeAklo_Habilidade_InterrompeConjuracao()
        {
            var r = Habilidade(Cravo());
            Assert.IsTrue(r.Success);
            Assert.IsTrue(r.InterrompeConjuracao, "Fincar o Aklo deve interromper a conjuração.");
            Assert.Greater(r.Dano, 0f);
        }

        [Test]
        public void EstileteDeIrem_Habilidade_AplicaSangramento()
        {
            var r = Estilete().ExecuteHabilidade();
            Assert.IsTrue(r.Success);
            Assert.Greater(r.SangramentoPorSegundo, 0f, "Ferida de Aklo deve sangrar por segundo.");
            Assert.Greater(r.DuracaoSangramento, 0f, "Sangramento deve ter duração.");
            Assert.IsFalse(r.InterrompeConjuracao);
        }

        [Test]
        public void AlfanjeDeAlhazred_Habilidade_RepeleEAtordoa()
        {
            var r = Alfanje().ExecuteHabilidade();
            Assert.IsTrue(r.Success);
            Assert.Greater(r.ForcaRepulsao, 0f, "Golpe do Deserto deve repelir.");
            Assert.IsTrue(r.Atordoou, "Golpe do Deserto deve atordoar brevemente.");
            Assert.Greater(r.DuracaoAtordoamento, 0f);
        }

        /// <summary>
        /// A regra de ouro do balanceamento da Tumba, em <c>armas_da_tumba.md</c>: <i>"a Tumba
        /// tem de ser vencível com qualquer uma das três"</i>. Nenhuma pode ser a obrigatória
        /// nem a errada — e uma arma que não causa dano no básico seria a errada.
        /// </summary>
        [Test]
        public void NenhumaDasTres_EInutilNoBasico()
        {
            foreach (var arma in new[] { Cravo(), Estilete(), Alfanje() })
            {
                var r = Basico(arma);
                Assert.Greater(r.Dano, 0f, $"{arma.NomeDaArma} não causa dano no básico.");
                Assert.Greater(r.DurationSeconds, 0f,
                    $"{arma.NomeDaArma}: golpe de duração zero — a FSM nunca entraria em " +
                    "Atacando e a animação não tocaria.");
            }
        }

        // ── Mão Vazia (golpe desarmado) ──────────────────────────────────────
        //
        // A MaoVazia NÃO migrou para dado, de propósito: desarmado é um ESTADO do jogo, não
        // um item que alguém equipa. Ela continua sendo POCO instanciado direto.

        [Test]
        public void MaoVazia_Basico_TemSucessoMasDanoZero()
        {
            var r = new MaoVazia().Execute();
            Assert.IsTrue(r.Success, "O gesto desarmado deve executar (entra no estado Atacando).");
            Assert.AreEqual(0f, r.Dano, 0.0001f,
                "Mão Vazia causa dano ZERO por decisão de design — bater de mão vazia não mata.");
        }

        [Test]
        public void MaoVazia_NaoAtordoaNemRepeleNemInterrompe()
        {
            var r = new MaoVazia().Execute();
            Assert.IsFalse(r.Atordoou);
            Assert.IsFalse(r.InterrompeConjuracao);
            Assert.AreEqual(0f, r.ForcaRepulsao, 0.0001f);
            Assert.AreEqual(0f, r.SangramentoPorSegundo, 0.0001f);
        }

        [Test]
        public void MaoVazia_RespeitaCooldownProprio()
        {
            var mao = new MaoVazia(cooldown: 0.25f);
            Assert.IsFalse(mao.CanActivate(0.24f));
            Assert.IsTrue(mao.CanActivate(0.25f));
        }

        [Test]
        public void MaoVazia_NaoTemHabilidade()
        {
            // Sem arma não há habilidade em botão separado — contrato explícito.
            //
            // Compara TIPOS, não uma instância. A forma antiga (`new MaoVazia() is
            // IArmaComHabilidade`) o compilador resolvia estaticamente e avisava com CS0184:
            // ele já sabia a resposta, ou seja, era um teste que NÃO PODIA FALHAR. Com
            // IsAssignableFrom a checagem acontece em runtime e volta a valer alguma coisa —
            // se alguém fizer MaoVazia implementar a interface, este teste acusa.
            Assert.IsFalse(typeof(IArmaComHabilidade).IsAssignableFrom(typeof(MaoVazia)),
                "MaoVazia não deve implementar IArmaComHabilidade: desarmado é um ESTADO do " +
                "jogo, não uma arma com habilidade em botão separado.");
        }

        // ── Cooldowns ────────────────────────────────────────────────────────
        //
        // Estes números deixaram de ser default de construtor e passaram a ser AUTORADOS no
        // asset. É uma melhora: antes o teste podia passar com um default que o jogo nem usava.

        [Test]
        public void CooldownBasico_BloqueiaAntesELiberaDepois()
        {
            var arma = Cravo();   // cooldown do básico: 0,5 s no asset
            Assert.IsFalse(arma.CanActivate(0.49f), "Não deveria liberar antes do cooldown.");
            Assert.IsTrue(arma.CanActivate(0.5f), "Deveria liberar exatamente no cooldown.");
        }

        [Test]
        public void CooldownHabilidade_EIndependenteDoBasico()
        {
            var arma = Cravo();   // básico 0,5 s · habilidade 6 s

            // Passou tempo suficiente pro básico, mas não pra habilidade.
            Assert.IsTrue(arma.CanActivate(1f));
            Assert.IsFalse(arma.CanActivateHabilidade(1f),
                "Habilidade não deve liberar com o cooldown do básico.");
            Assert.IsTrue(arma.CanActivateHabilidade(6f),
                "Habilidade deve liberar no próprio cooldown.");
        }

        /// <summary>
        /// A cadência é metade da identidade de uma arma, e só passou a ser consultada de
        /// verdade em 2026-08-27 — antes disso <c>IArma.CanActivate</c> não era chamado por
        /// ninguém e a cadência caía em <c>DurationSeconds</c>, fazendo as três baterem quase
        /// na mesma velocidade.
        /// </summary>
        [Test]
        public void AsTresArmas_NaoTemAMesmaCadencia()
        {
            // O Estilete é a arma rápida; o Alfanje é a pesada. Se isso empatar, a diferença
            // entre elas volta a ser só um número na ficha.
            Assert.IsTrue(Estilete().CanActivate(0.35f),
                "O Estilete tem de estar pronto de novo em 0,35 s — é a arma rápida.");

            Assert.IsFalse(Alfanje().CanActivate(0.35f),
                "O Alfanje não pode estar pronto em 0,35 s — é a arma pesada.");
        }
    }
}
