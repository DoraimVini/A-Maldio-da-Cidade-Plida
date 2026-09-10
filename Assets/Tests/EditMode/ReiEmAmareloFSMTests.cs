using System.Collections.Generic;
using NUnit.Framework;
using FavelaAmarela.Core.Enemies;

namespace FavelaAmarela.Tests.EditMode
{
    /// <summary>
    /// Trava as regras do confronto final: o ritual das relíquias (metade sem pressão) e o
    /// selamento (metade de posição, sem barra de vida — errar mata na hora).
    ///
    /// <para><b>A resposta mudou em 2026-09-10.</b> Era dar as costas ao Rei; passou a ser
    /// <b>estar dentro do escudo</b> que uma das relíquias ergue. O Vini jogou e relatou
    /// <i>"não tem como evitar o ataque do Rei, nem de costas"</i> — a leitura antiga dependia
    /// de <c>PlayerMovement.LookDirection</c>, que só é atualizada enquanto o jogador anda, e
    /// quem parava para ler o aviso morria sem resposta possível.</para>
    /// </summary>
    public class ReiEmAmareloFSMTests
    {
        private static readonly string[] TresReliquias = { "necronomicon", "patua_luas_gemeas", "anel_sinal_amarelo" };

        private static ReiEmAmareloFSM CriarEmRitual(int ciclos = 3, float janela = 1.5f, float intervalo = 6f)
        {
            var fsm = new ReiEmAmareloFSM(TresReliquias, ciclos, janela, intervalo);
            fsm.Iniciar();
            return fsm;
        }

        // ── Ritual das relíquias ─────────────────────────────────────────────

        [Test]
        public void Iniciar_VaiParaAtivandoReliquias()
        {
            var fsm = CriarEmRitual();
            Assert.AreEqual(ReiEmAmareloState.AtivandoReliquias, fsm.CurrentState);
        }

        [Test]
        public void AtivarReliquiaNaoExigida_NaoConta()
        {
            var fsm = CriarEmRitual();
            bool ativou = fsm.AtivarReliquia("coroa_de_ossos"); // fora da lista de 3

            Assert.IsFalse(ativou);
            Assert.AreEqual(0, fsm.ReliquiasAtivas);
        }

        [Test]
        public void AtivarMesmaReliquiaDuasVezes_SoContaUma()
        {
            var fsm = CriarEmRitual();
            fsm.AtivarReliquia("necronomicon");
            fsm.AtivarReliquia("necronomicon");

            Assert.AreEqual(1, fsm.ReliquiasAtivas);
        }

        [Test]
        public void AtivarTodasAsReliquias_IniciaOSelamento()
        {
            var fsm = CriarEmRitual();

            foreach (var id in TresReliquias) fsm.AtivarReliquia(id);

            Assert.IsTrue(fsm.TodasAsReliquiasAtivas);
            Assert.AreEqual(ReiEmAmareloState.Selando, fsm.CurrentState);
        }

        [Test]
        public void FaltandoUmaReliquia_NaoIniciaOSelamento()
        {
            var fsm = CriarEmRitual();

            fsm.AtivarReliquia(TresReliquias[0]);
            fsm.AtivarReliquia(TresReliquias[1]);

            Assert.AreEqual(ReiEmAmareloState.AtivandoReliquias, fsm.CurrentState);
        }

        [Test]
        public void AtivarReliquia_ComOSelamentoEmCurso_NaoTemEfeito()
        {
            var fsm = CriarEmRitual();
            foreach (var id in TresReliquias) fsm.AtivarReliquia(id);

            // Já está em Selando; reativar não deveria significar nada.
            bool efeito = fsm.AtivarReliquia(TresReliquias[0]);

            Assert.IsFalse(efeito);
        }

        // ── Selamento: janela de reação ──────────────────────────────────────

        private static ReiEmAmareloFSM CriarNoInicioDoSelamento(int ciclos = 3, float janela = 1.5f, float intervalo = 6f)
        {
            var fsm = CriarEmRitual(ciclos, janela, intervalo);
            foreach (var id in TresReliquias) fsm.AtivarReliquia(id);
            return fsm;
        }

        [Test]
        public void Selando_AposOIntervalo_AbreADesvelacao()
        {
            var fsm = CriarNoInicioDoSelamento(intervalo: 6f);

            fsm.Tick(6.1f, jogadorEstaAbrigado: false);

            Assert.AreEqual(ReiEmAmareloState.Desvelado, fsm.CurrentState);
        }

        [Test]
        public void Desvelado_AbrigadoATempo_Sobrevive()
        {
            var fsm = CriarNoInicioDoSelamento(intervalo: 1f, janela: 1.5f);

            fsm.Tick(1.1f, jogadorEstaAbrigado: false); // abre a janela
            Assert.AreEqual(ReiEmAmareloState.Desvelado, fsm.CurrentState);

            fsm.Tick(0.5f, jogadorEstaAbrigado: true); // entra no abrigo dentro do 1,5 s

            Assert.AreEqual(1, fsm.CiclosSobrevividos);
            Assert.AreNotEqual(ReiEmAmareloState.Colapso, fsm.CurrentState);
        }

        [Test]
        public void Desvelado_ForaDoAbrigoAteOFimDaJanela_Colapsa()
        {
            var fsm = CriarNoInicioDoSelamento(intervalo: 1f, janela: 1.5f);

            fsm.Tick(1.1f, jogadorEstaAbrigado: false); // abre a janela
            fsm.Tick(1.6f, jogadorEstaAbrigado: false); // nunca entra, janela estoura

            Assert.AreEqual(ReiEmAmareloState.Colapso, fsm.CurrentState);
        }

        [Test]
        public void Desvelado_ReagirNoUltimoInstante_AindaSalva()
        {
            // A janela é de reação, não de antecipação: pisar dentro a 0,01 s do fim ainda conta.
            var fsm = CriarNoInicioDoSelamento(intervalo: 1f, janela: 1.5f);

            fsm.Tick(1.1f, jogadorEstaAbrigado: false);
            fsm.Tick(1.49f, jogadorEstaAbrigado: false);
            fsm.Tick(0.005f, jogadorEstaAbrigado: true);

            Assert.AreEqual(1, fsm.CiclosSobrevividos);
        }

        /// <summary>
        /// <b>Até 2026-09-10 este teste se chamava <c>SobreviverTodosOsCiclos_Sela</c>.</b>
        /// Sobreviver os ciclos não sela mais: <b>desmascara</b>. O rito entra no Confronto e
        /// continua girando — a vitória agora vem por <see cref="ReiEmAmareloFSM.Abater"/>.
        /// </summary>
        [Test]
        public void SobreviverTodosOsCiclos_AbreOConfronto_NaoSela()
        {
            var fsm = CriarNoInicioDoSelamento(ciclos: 2, intervalo: 1f, janela: 1.5f);
            int comecou = 0;
            fsm.OnComecouOConfronto += () => comecou++;

            for (int i = 0; i < 2; i++)
            {
                fsm.Tick(1.1f, jogadorEstaAbrigado: false); // abre
                fsm.Tick(0.1f, jogadorEstaAbrigado: true);  // sobrevive
            }

            Assert.IsTrue(fsm.EmConfronto, "Os selos fecharam: o Confronto tinha de abrir.");
            Assert.AreEqual(1, comecou, "OnComecouOConfronto dispara uma vez só.");
            Assert.AreEqual(ReiEmAmareloState.Selando, fsm.CurrentState,
                "O ritmo continua: o Confronto começa numa calmaria, com um escudo aceso.");
            Assert.AreNotEqual(ReiEmAmareloState.Selado, fsm.CurrentState,
                "Sobreviver não é vencer. Vencer é ferir.");
            Assert.AreEqual(2, fsm.CiclosSobrevividos);
        }

        // ── O Confronto ──────────────────────────────────────────────────────

        private static ReiEmAmareloFSM CriarNoConfronto(float intervalo = 1f, float janela = 1.5f,
                                                        float intervaloNoConfronto = 0f)
        {
            var fsm = new ReiEmAmareloFSM(TresReliquias, 1, janela, intervalo, intervaloNoConfronto);
            fsm.Iniciar();
            foreach (var id in TresReliquias) fsm.AtivarReliquia(id);

            fsm.Tick(intervalo + 0.1f, jogadorEstaAbrigado: false); // abre o único desvelo
            fsm.Tick(0.1f, jogadorEstaAbrigado: true);             // sobrevive: Confronto

            Assert.IsTrue(fsm.EmConfronto, "O rig não chegou ao Confronto.");
            return fsm;
        }

        /// <summary>
        /// <i>"Ele sangra entre os desvelos"</i>: antes do Confronto, nunca; no Confronto, só
        /// na calmaria. Durante o desvelo ele é imune — quem está batendo nele nesse instante
        /// está fora do abrigo, e o rito já cobra isso.
        /// </summary>
        [Test]
        public void ORei_SoSangraNoConfronto_ESoNaCalmaria()
        {
            var antes = CriarNoInicioDoSelamento(intervalo: 1f, janela: 1.5f);
            Assert.IsFalse(antes.PodeReceberDano, "Mascarado, o Rei não pode ser ferido.");

            var fsm = CriarNoConfronto(intervalo: 1f, janela: 1.5f);
            Assert.AreEqual(ReiEmAmareloState.Selando, fsm.CurrentState);
            Assert.IsTrue(fsm.PodeReceberDano, "Na calmaria do Confronto, o Rei sangra.");

            fsm.Tick(1.1f, jogadorEstaAbrigado: false); // desvela
            Assert.AreEqual(ReiEmAmareloState.Desvelado, fsm.CurrentState);
            Assert.IsFalse(fsm.PodeReceberDano, "Desvelado, ele é imune — a janela é do jogador.");
        }

        [Test]
        public void Abater_NoConfronto_Sela()
        {
            var fsm = CriarNoConfronto();
            int selou = 0;
            fsm.OnSelado += () => selou++;

            Assert.IsTrue(fsm.Abater());

            Assert.AreEqual(ReiEmAmareloState.Selado, fsm.CurrentState);
            Assert.AreEqual(1, selou, "Selar dispara OnSelado — é por ali que vêm vitória e espólio.");
            Assert.IsNull(fsm.ReliquiaDoCiclo, "Nenhum escudo fica aceso depois do fim.");
        }

        /// <summary>Antes do Confronto o Rei não tem carne: abater não tem efeito.</summary>
        [Test]
        public void Abater_AntesDoConfronto_NaoTemEfeito()
        {
            var fsm = CriarNoInicioDoSelamento();

            Assert.IsFalse(fsm.Abater());
            Assert.AreEqual(ReiEmAmareloState.Selando, fsm.CurrentState);
        }

        [Test]
        public void Abater_DepoisDeColapsar_NaoRessuscitaORito()
        {
            var fsm = CriarNoConfronto(intervalo: 1f, janela: 1.5f);
            fsm.Tick(1.1f, jogadorEstaAbrigado: false);
            fsm.Tick(1.6f, jogadorEstaAbrigado: false); // fora do abrigo até o fim: Colapso
            Assert.AreEqual(ReiEmAmareloState.Colapso, fsm.CurrentState);

            Assert.IsFalse(fsm.Abater());
            Assert.AreEqual(ReiEmAmareloState.Colapso, fsm.CurrentState);
        }

        /// <summary>
        /// No Confronto o rito continua a cobrar o abrigo — é o que dá à fase o seu risco:
        /// sair do escudo para ferir e não voltar a tempo mata do mesmo jeito.
        /// </summary>
        [Test]
        public void NoConfronto_PerderODesvelo_AindaColapsa()
        {
            var fsm = CriarNoConfronto(intervalo: 1f, janela: 1.5f);

            fsm.Tick(1.1f, jogadorEstaAbrigado: false);
            fsm.Tick(1.6f, jogadorEstaAbrigado: false);

            Assert.AreEqual(ReiEmAmareloState.Colapso, fsm.CurrentState);
        }

        /// <summary>
        /// Os escudos continuam se revezando no Confronto, dando a volta na lista — um ciclo
        /// sem abrigo seria morte certa sem resposta possível.
        /// </summary>
        [Test]
        public void NoConfronto_OsEscudosContinuamSeRevezando()
        {
            var fsm = CriarNoConfronto(intervalo: 1f, janela: 1.5f);
            var vistas = new List<string> { fsm.ReliquiaDoCiclo };

            for (int i = 0; i < 3; i++)
            {
                fsm.Tick(1.1f, jogadorEstaAbrigado: false);
                fsm.Tick(0.1f, jogadorEstaAbrigado: true);
                Assert.IsNotNull(fsm.ReliquiaDoCiclo, $"Ciclo {i + 2} do Confronto sem abrigo.");
                vistas.Add(fsm.ReliquiaDoCiclo);
            }

            // 1 ciclo de selamento + 4 de Confronto = indices 0,1,2,0,1 -- a volta completa.
            CollectionAssert.AreEqual(
                new[] { TresReliquias[1], TresReliquias[2], TresReliquias[0], TresReliquias[1] },
                vistas);
        }

        /// <summary>
        /// A calmaria do Confronto tem o próprio relógio: a fase pede mais (sair do abrigo, e
        /// não só chegar nele), e o número tem de poder ser afinado sem mexer no selamento.
        /// </summary>
        [Test]
        public void NoConfronto_ACalmariaTemOProprioRelogio()
        {
            var fsm = CriarNoConfronto(intervalo: 1f, janela: 1.5f, intervaloNoConfronto: 3f);

            fsm.Tick(1.1f, jogadorEstaAbrigado: false);
            Assert.AreEqual(ReiEmAmareloState.Selando, fsm.CurrentState,
                "1,1 s passou do intervalo do selamento (1 s) mas não do Confronto (3 s).");

            fsm.Tick(2f, jogadorEstaAbrigado: false);
            Assert.AreEqual(ReiEmAmareloState.Desvelado, fsm.CurrentState);
        }

        [Test]
        public void SemIntervaloDoConfronto_UsaODoSelamento()
        {
            var fsm = CriarNoConfronto(intervalo: 1f, janela: 1.5f, intervaloNoConfronto: 0f);

            fsm.Tick(1.1f, jogadorEstaAbrigado: false);
            Assert.AreEqual(ReiEmAmareloState.Desvelado, fsm.CurrentState);
        }

        [Test]
        public void Colapso_DisparaUmaVezSo()
        {
            var fsm = CriarNoInicioDoSelamento(intervalo: 1f, janela: 1.5f);
            int vezes = 0;
            fsm.OnColapso += () => vezes++;

            fsm.Tick(1.1f, jogadorEstaAbrigado: false);
            fsm.Tick(1.6f, jogadorEstaAbrigado: false);
            fsm.Tick(5f, jogadorEstaAbrigado: false); // continua chamando Tick depois de morto

            Assert.AreEqual(1, vezes);
        }

        // ── Um escudo por vez ────────────────────────────────────────────────

        /// <summary>
        /// <i>"Cada artefato gera um escudo por vez"</i> — pedido do Vini, 2026-09-10. O
        /// primeiro ciclo é abrigado pela <b>primeira relíquia ativada</b>, não por uma
        /// qualquer: a ordem tem de ser previsível para o jogador aprender a luta.
        /// </summary>
        [Test]
        public void OPrimeiroCiclo_EhAbrigadoPelaPrimeiraReliquiaAtivada()
        {
            var fsm = CriarEmRitual();

            foreach (var id in TresReliquias) fsm.AtivarReliquia(id);

            Assert.AreEqual(TresReliquias[0], fsm.ReliquiaDoCiclo,
                "O escudo do primeiro ciclo tem de ser o da primeira relíquia ativada.");
        }

        /// <summary>
        /// A cada ciclo sobrevivido, o abrigo passa para a relíquia seguinte. Três relíquias,
        /// três ciclos: cada artefato tem exatamente uma vez.
        /// </summary>
        [Test]
        public void CadaCiclo_PassaOAbrigoParaAProximaReliquia()
        {
            var fsm = CriarNoInicioDoSelamento(ciclos: 3, intervalo: 1f, janela: 1.5f);

            var vistas = new List<string> { fsm.ReliquiaDoCiclo };

            for (int i = 0; i < 2; i++)
            {
                fsm.Tick(1.1f, jogadorEstaAbrigado: false); // abre a janela
                fsm.Tick(0.1f, jogadorEstaAbrigado: true);  // sobrevive
                vistas.Add(fsm.ReliquiaDoCiclo);
            }

            CollectionAssert.AreEqual(TresReliquias, vistas,
                "Com três relíquias e três ciclos, cada artefato abriga exatamente um ciclo, " +
                "na ordem em que foram ativadas.");
        }

        /// <summary>
        /// Mais ciclos do que relíquias dá a volta na lista, em vez de ficar sem abrigo — um
        /// ciclo sem escudo seria morte certa sem resposta possível.
        /// </summary>
        [Test]
        public void MaisCiclosQueReliquias_DaAVoltaNaLista()
        {
            var fsm = CriarNoInicioDoSelamento(ciclos: 4, intervalo: 1f, janela: 1.5f);

            for (int i = 0; i < 3; i++)
            {
                fsm.Tick(1.1f, jogadorEstaAbrigado: false);
                fsm.Tick(0.1f, jogadorEstaAbrigado: true);
            }

            Assert.AreEqual(TresReliquias[0], fsm.ReliquiaDoCiclo,
                "O quarto ciclo volta para a primeira relíquia.");
            Assert.AreNotEqual(ReiEmAmareloState.Selado, fsm.CurrentState);
        }

        /// <summary>
        /// O escudo acende no <b>começo da calmaria</b>, não no desvelo — é o que dá ao jogador
        /// os 6 s de travessia. Se acendesse junto com a janela, a corrida mais longa da arena
        /// (20 un, 4,44 s andando) não caberia em 1,5 s e a luta seria invencível.
        /// </summary>
        [Test]
        public void OEscudoAcende_NoComecoDaCalmaria_NaoNoDesvelo()
        {
            var fsm = CriarEmRitual(intervalo: 6f);

            var acesos = new List<string>();
            fsm.OnEscudoAceso += id => acesos.Add(id);

            foreach (var id in TresReliquias) fsm.AtivarReliquia(id);

            Assert.AreEqual(ReiEmAmareloState.Selando, fsm.CurrentState);
            Assert.AreEqual(1, acesos.Count,
                "O escudo tem de acender ao entrar em Selando — a calmaria inteira é a corrida.");

            fsm.Tick(6.1f, jogadorEstaAbrigado: false); // abre o desvelo

            Assert.AreEqual(ReiEmAmareloState.Desvelado, fsm.CurrentState);
            Assert.AreEqual(1, acesos.Count,
                "O desvelo não acende escudo nenhum: quem não correu, não corre mais.");
        }

        /// <summary>Fim do rito, de um jeito ou de outro: nenhuma relíquia fica abrigando.</summary>
        [Test]
        public void AoTerminarORito_NaoSobraReliquiaAbrigando()
        {
            // Selado vem por abate, desde a quarta fase: sobreviver os ciclos só desmascara.
            var selado = CriarNoConfronto(intervalo: 1f, janela: 1.5f);
            Assert.IsNotNull(selado.ReliquiaDoCiclo, "No Confronto há sempre um escudo aceso.");
            selado.Abater();
            Assert.AreEqual(ReiEmAmareloState.Selado, selado.CurrentState);
            Assert.IsNull(selado.ReliquiaDoCiclo);

            var colapsou = CriarNoInicioDoSelamento(ciclos: 1, intervalo: 1f, janela: 1.5f);
            colapsou.Tick(1.1f, jogadorEstaAbrigado: false);
            colapsou.Tick(1.6f, jogadorEstaAbrigado: false);
            Assert.AreEqual(ReiEmAmareloState.Colapso, colapsou.CurrentState);
            Assert.IsNull(colapsou.ReliquiaDoCiclo);
        }

        [Test]
        public void ListaDeReliquiasVazia_LancaExcecao()
        {
            Assert.Throws<System.ArgumentException>(() => new ReiEmAmareloFSM(new List<string>()));
        }
    }
}
