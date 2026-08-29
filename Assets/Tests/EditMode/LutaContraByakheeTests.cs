using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using FavelaAmarela.Core.Abilities;
using FavelaAmarela.Core.Combat;
using FavelaAmarela.Core.Enemies;
using FavelaAmarela.Core.Loot;
using FavelaAmarela.Inventario;

namespace FavelaAmarela.Tests.EditMode
{
    /// <summary>
    /// Trava a <b>intenção de dificuldade</b> da luta do Byakhee: vencível com jogo perfeito
    /// usando qualquer uma das 3 armas da Tumba, mas gastando uma fração real da Resiliência —
    /// "equilíbrio levemente puxado para o difícil" (pedido do Vini, 2026-08-11).
    ///
    /// <para><b>O que mudou em 2026-08-28.</b> Este arquivo carregava as armas como constantes
    /// escritas à mão (<c>Cravo 40</c>, <c>Estilete 25</c>, <c>Alfanje 45</c>) e
    /// <b>reimplementava a fórmula de mitigação</b> numa linha própria. As duas coisas se
    /// provaram exatamente o que se temia: o <b>Estilete valia 30 no asset</b> e 25 aqui, e o
    /// teste passava verde defendendo um número que o jogo não usava. Agora ele lê os assets e
    /// chama <see cref="MitigacaoDeDano"/> e <see cref="ResolucaoDeGolpe"/> de verdade.</para>
    ///
    /// <para><b>E o modelo de dano mudou por baixo.</b> A arma deixou de ter um número fixo e
    /// passou a ter <b>faixa de dano branco, chance e multiplicador de crítico e precisão</b>,
    /// escalados pelo nível do item. Um teste com dano fixo não estava mais medindo a luta que
    /// o jogador joga.</para>
    ///
    /// <para><b>Por que este arquivo existe:</b> a primeira estimativa desta luta foi feita de
    /// cabeça e errou feio (concluiu "impossível" quando era vencível). Toda mudança de
    /// constante daqui em diante deve ser validada por estes testes, não por intuição.</para>
    /// </summary>
    public class LutaContraByakheeTests
    {
        private const float Dt = 0.02f;

        /// <summary>
        /// O nível em que o jogador chega aos Portões das Ruínas jogando o caminho crítico.
        /// Não é chute: <c>EconomiaDeExposicaoTests</c> conta os inimigos nas cenas e roda a
        /// soma pela curva — 11 Cultistas no Deserto, 2 na Tumba e o Abdul dão 475 de
        /// Exposição, e o nível 3 começa em 300.
        /// </summary>
        private const int NivelEsperado = 3;

        // ── Os assets, que agora são a fonte ──────────────────────────────────

        private static BaseDeArma Familia(string nome) =>
            AssetDatabase.LoadAssetAtPath<BaseDeArma>(
                $"Assets/FavelaAmarela/Config/Armas/BaseArma_{nome}.asset");

        private static FichaAtributosConfig Ficha(string nome) =>
            AssetDatabase.LoadAssetAtPath<FichaAtributosConfig>(
                $"Assets/FavelaAmarela/Config/Ficha_{nome}.asset");

        /// <summary>As 3 armas da Tumba, pelo nome da família que as autora.</summary>
        private static readonly string[] ArmasDaTumba = { "Alfanje", "Cravo", "LaminaFina" };

        /// <summary>
        /// Quanto do dano da arma o ataque básico aproveita. É o <c>TipoDeEfeito.DanoDaArma</c>
        /// autorado na habilidade — 90% no Alfanje, 75% no Cravo, 50% no Estilete, que é o que
        /// dá identidade às três sem que nenhuma seja "a errada".
        /// </summary>
        private static float PercentualDoBasico(BaseDeArma familia)
        {
            if (familia == null || familia.Habilidade == null) return 0f;

            return familia.Habilidade.EfeitosDoBasico
                .Where(e => e.Tipo == TipoDeEfeito.DanoDaArma)
                .Select(e => e.Valor)
                .DefaultIfEmpty(0f)
                .Sum();
        }

        /// <summary>
        /// Fonte determinística e reproduzível — um LCG. <c>UnityEngine.Random</c> é global e
        /// tornaria esta suíte instável; uma fonte de valor fixo mataria a variância, que é
        /// justamente o que crítico e erro acrescentaram ao combate.
        /// </summary>
        private sealed class Semente : IFonteDeAleatoriedade
        {
            private uint _estado;
            public Semente(uint semente) => _estado = semente == 0u ? 1u : semente;

            public float ProximoValor()
            {
                _estado = unchecked(_estado * 1664525u + 1013904223u);
                return (_estado >> 8) / 16777216f;   // 24 bits em [0, 1)
            }

            public int ProximoInteiro(int min, int max) =>
                max <= min ? min : min + (int)(ProximoValor() * (max - min));
        }

        // ── A simulação ───────────────────────────────────────────────────────

        private readonly struct Resultado
        {
            public readonly bool Venceu;
            public readonly float ResilienciaRestante;
            public readonly float Segundos;
            public readonly bool CircundouAlgumaVez;
            public readonly int Golpes;

            public Resultado(bool venceu, float rm, float segundos, bool circundou, int golpes)
            {
                Venceu = venceu;
                ResilienciaRestante = rm;
                Segundos = segundos;
                CircundouAlgumaVez = circundou;
                Golpes = golpes;
            }
        }

        /// <summary>
        /// Corre a luta com um jogador <b>perfeito</b>: ataca a cada cadência sempre que a
        /// janela está aberta, e nunca leva o dano evitável das garras. Jogo real rende menos —
        /// é justamente essa folga que separa "difícil" de "impossível". O que ele <b>não</b>
        /// consegue evitar é o grito infrassônico, que é o relógio da luta.
        /// </summary>
        private static Resultado Simular(string nomeDaFamilia, uint semente,
                                         int nivelDoItem = NivelEsperado)
        {
            var familia = Familia(nomeDaFamilia);
            Assert.IsNotNull(familia, $"BaseArma_{nomeDaFamilia}.asset não existe.");

            var fichaDoChefe = Ficha("Byakhee");
            Assert.IsNotNull(fichaDoChefe, "Ficha_Byakhee.asset não existe.");

            var chefe = fichaDoChefe.CriarFicha(1);   // nivelDaUnidade autorado no prefab
            var perfil = familia.PerfilNoNivel(nivelDoItem);
            float percentual = PercentualDoBasico(familia);
            float cadencia = familia.Habilidade.CooldownBasico;

            var fonte = new Semente(semente);
            var fsm = new ByakheeFSM();
            fsm.IniciarLuta();

            var vida = new Vitalidade(chefe.VitalidadeMax);
            var resiliencia = ResilienciaMental.ComThresholdFracional(100f, 0.25f);

            float desdeUltimoAtaque = 999f;
            float tempo = 0f;
            bool circundou = false;
            int golpes = 0;

            for (int passos = 0; passos < 1_000_000; passos++)
            {
                tempo += Dt;
                desdeUltimoAtaque += Dt;

                float dreno = fsm.DrenoDeResilienciaPorSegundo;
                if (dreno > 0f) resiliencia.SofrerTrauma(dreno * Dt);
                if (resiliencia.IsColapso) return new Resultado(false, 0f, tempo, circundou, golpes);

                if (fsm.CurrentState == ByakheeState.Circundando) circundou = true;

                if (fsm.PodeReceberDano && desdeUltimoAtaque >= cadencia)
                {
                    desdeUltimoAtaque = 0f;
                    golpes++;

                    // O golpe passa pela resolução REAL: faixa branca rolada, percentual da
                    // habilidade, precisão e crítico. Copiar a conta aqui foi o erro que este
                    // arquivo cometeu por três meses.
                    var bruto = new ArmaResult(true, 0f, 0f, false, 0f, 0f,
                                               percentualDoDanoDaArma: percentual);

                    var resolvido = ResolucaoDeGolpe.Resolver(bruto, perfil, fonte: fonte);

                    // E pela mitigação REAL, que continua sendo a mesma classe do jogo.
                    float liquido = MitigacaoDeDano.Aplicar(resolvido.Dano, chefe.Defesa);

                    // O golpe que acerta durante o Frenesi também o interrompe.
                    if (fsm.CurrentState == ByakheeState.Frenesi) fsm.InterromperFrenesi();

                    if (liquido > 0f) vida.Ferir(liquido);

                    if (vida.EstaAbatido)
                        return new Resultado(true, resiliencia.Atual, tempo, circundou, golpes);
                }

                fsm.AtualizarFracaoDeVida(vida.Percentual);
                fsm.Tick(Dt);
            }

            return new Resultado(false, resiliencia.Atual, tempo, circundou, golpes);
        }

        /// <summary>As sementes fixas da bateria. Várias porque uma só pode dar sorte.</summary>
        private static readonly uint[] Sementes = { 1u, 7u, 42u, 1337u, 90210u };

        // ── Vencível com as três, no nível esperado ───────────────────────────

        [TestCase("Alfanje")]
        [TestCase("Cravo")]
        [TestCase("LaminaFina")]
        public void NoNivelEsperado_JogoPerfeitoVenceComQualquerArma(string arma)
        {
            foreach (uint semente in Sementes)
            {
                var r = Simular(arma, semente);

                Assert.IsTrue(r.Venceu,
                    $"A luta tem de ser vencível com {arma} no nível {NivelEsperado} em jogo " +
                    $"perfeito (semente {semente}, {r.Golpes} golpes em {r.Segundos:F1}s). " +
                    "Nenhuma das 3 armas da Tumba pode ser 'a errada'.");
            }
        }

        /// <summary>
        /// <b>O relato do Vini, virado em teste.</b> Ele jogou e disse: <i>"não tem como ganhar
        /// da Byakhee, os itens são fracos demais"</i>. Estava certo — com a arma travada no
        /// nível 1, que era o que o Baú da Tumba entregava, a luta realmente não fechava. Este
        /// teste afirma que a progressão é a resposta: <b>a mesma arma, no nível que a
        /// Exposição entrega, vence.</b>
        /// </summary>
        [TestCase("Alfanje")]
        [TestCase("Cravo")]
        [TestCase("LaminaFina")]
        public void ANivelDoItem_MudaOResultadoDaLuta(string arma)
        {
            var noPiso = Simular(arma, 42u, nivelDoItem: 1);
            var noEsperado = Simular(arma, 42u, nivelDoItem: NivelEsperado);

            Assert.Less(noEsperado.Golpes, noPiso.Golpes,
                $"Subir o nível do item de 1 para {NivelEsperado} não reduziu os golpes " +
                $"necessários com {arma} ({noPiso.Golpes} → {noEsperado.Golpes}). Sem isso a " +
                "progressão não chega no combate, e o jogador continua preso na luta que não " +
                "conseguiu vencer.");
        }

        // ── E continua custando caro ──────────────────────────────────────────

        [TestCase("Alfanje")]
        [TestCase("Cravo")]
        [TestCase("LaminaFina")]
        public void JogoPerfeito_CustaResilienciaDeVerdade(string arma)
        {
            var r = Simular(arma, 42u);

            // Sem piso de custo, jogo perfeito ficaria indistinguível de trivial e o grito
            // infrassônico deixaria de ser o relógio que o design pede.
            Assert.Less(r.ResilienciaRestante, 60f,
                $"Sobrou {r.ResilienciaRestante:F0}/100 de Resiliência com {arma} em jogo " +
                "perfeito — fácil demais para o equilíbrio pedido.");

            // E o teto: se nem o jogo perfeito sobra folga, o jogo real é impossível.
            Assert.Greater(r.ResilienciaRestante, 0f,
                $"{arma} não deixa nenhuma margem em jogo perfeito — a luta vira impossível " +
                "para qualquer jogador humano.");
        }

        [TestCase("Alfanje")]
        [TestCase("Cravo")]
        [TestCase("LaminaFina")]
        public void Fase3_AconteceDeVerdade(string arma)
        {
            // Regressão de um bug de design real: cair para 30% durante um pouso apenas
            // ESTENDIA aquela janela em vez de fazer o Byakhee decolar. O jogador matava ele
            // ali mesmo e a fase 3 — a identidade da luta — nunca aparecia.
            var r = Simular(arma, 42u);

            Assert.IsTrue(r.CircundouAlgumaVez,
                $"Com {arma} o Byakhee morreu sem nunca circundar: a fase 3 não está " +
                "acontecendo.");
        }

        // ── A troca de golpes, que é o número que o design mira ────────────────

        /// <summary>
        /// <b>A conta que decide se a luta é justa</b>, e a que estava desastrosa antes desta
        /// fase: o jogador precisava de <b>~14</b> acertos e o Byakhee de <b>5</b>.
        ///
        /// <para>No nível esperado a troca fica próxima: a Vitalidade e a Defesa do Damião
        /// sobem pela <c>EscalaDeNivel</c> junto com o dano da arma. Não exige empate — o chefe
        /// pode e deve levar vantagem —, exige que a diferença caiba dentro do que esquiva,
        /// Mão Secundária e consumíveis cobrem.</para>
        /// </summary>
        [TestCase("Alfanje")]
        [TestCase("Cravo")]
        [TestCase("LaminaFina")]
        public void ATrocaDeGolpes_NaoEhDesigual(string arma)
        {
            int golpesDoJogador = GolpesParaAbaterOChefe(arma);
            int golpesDoChefe = GolpesParaDerrubarODamiao();

            Assert.LessOrEqual(golpesDoJogador, golpesDoChefe * 2,
                $"Com {arma} no nível {NivelEsperado} o jogador precisa de {golpesDoJogador} " +
                $"acertos e o Byakhee de {golpesDoChefe}. Mais que o dobro é a luta que o Vini " +
                "jogou e não conseguiu vencer (era 14 contra 5).");
        }

        /// <summary>Quantos acertos, no valor esperado, para derrubar as 500 do chefe.</summary>
        private static int GolpesParaAbaterOChefe(string nomeDaFamilia)
        {
            var familia = Familia(nomeDaFamilia);
            var chefe = Ficha("Byakhee").CriarFicha(1);

            var perfil = familia.PerfilNoNivel(NivelEsperado);
            float percentual = PercentualDoBasico(familia);

            // Valor esperado do golpe: média da faixa, corrigida por precisão e crítico.
            float media = (perfil.DanoMin + perfil.DanoMax) * 0.5f * percentual;
            float esperado = media * perfil.Precisao
                             * (1f + perfil.ChanceCritica * (perfil.MultiplicadorCritico - 1f));

            float porGolpe = MitigacaoDeDano.Aplicar(esperado, chefe.Defesa);

            Assert.Greater(porGolpe, 0f, $"{nomeDaFamilia} não atravessa a Defesa do Byakhee.");

            return (int)Math.Ceiling(chefe.VitalidadeMax / porGolpe);
        }

        /// <summary>Quantas garradas até o Damião cair, no nível esperado.</summary>
        private static int GolpesParaDerrubarODamiao()
        {
            var damiao = Ficha("Damiao").CriarFicha(NivelEsperado);
            var chefe = Ficha("Byakhee").CriarFicha(1);

            float porGarrada = MitigacaoDeDano.Aplicar(chefe.Ataque, damiao.Defesa);

            Assert.Greater(porGarrada, 0f, "O Byakhee não atravessa a Defesa do Damião.");

            return (int)Math.Ceiling(damiao.VitalidadeMax / porGarrada);
        }

        /// <summary>
        /// A escala tem de <b>chegar</b> no jogador, não só na arma. Se a ficha do Damião não
        /// subir junto, o nível 3 o deixa batendo mais forte e morrendo igual — que é meio
        /// eixo, e o pedido do Vini foi <i>"saber que ele no nível 2 está mais forte e com mais
        /// defesa"</i>.
        /// </summary>
        [Test]
        public void ODamiao_FicaMaisResistenteACadaNivel()
        {
            var config = Ficha("Damiao");
            Assert.IsNotNull(config, "Ficha_Damiao.asset não existe.");

            var chefe = Ficha("Byakhee").CriarFicha(1);

            int noNivel1 = Golpes(config.CriarFicha(1), chefe.Ataque);
            int noEsperado = Golpes(config.CriarFicha(NivelEsperado), chefe.Ataque);

            Assert.Greater(noEsperado, noNivel1,
                $"O Damião aguenta {noNivel1} garradas no nível 1 e {noEsperado} no nível " +
                $"{NivelEsperado} — a ficha não está escalando com o nível.");

            static int Golpes(FichaDeAtributos ficha, float ataque) =>
                (int)Math.Ceiling(ficha.VitalidadeMax / MitigacaoDeDano.Aplicar(ataque, ficha.Defesa));
        }

        /// <summary>
        /// O dano das garras vem da <b>ficha</b>, não de um segundo número no
        /// <c>ByakheeAI</c>. Eram dois valores independentes mantidos à mão (ambos 26, por
        /// sorte), e enquanto fossem dois, rebalancear o chefe pela ficha não mudaria nada em
        /// jogo.
        /// </summary>
        [Test]
        public void OAtaqueDoByakhee_VemDaFicha()
        {
            string fonte = System.IO.File.ReadAllText("Assets/Scripts/Enemies/ByakheeAI.cs");

            StringAssert.Contains("_enemyBase.Atributos.Ataque", fonte,
                "O ByakheeAI voltou a usar só o campo local para o dano das garras. A ficha " +
                "vira dado morto, e o nivelDaUnidade deixa de fazer efeito.");

            StringAssert.Contains("new ArmaResult(true, 0f, 0f, false, 0f, DanoDasGarras)", fonte,
                "O golpe das garras voltou a ler o campo serializado em vez da propriedade que " +
                "consulta a ficha.");
        }
    }
}
