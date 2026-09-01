using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;

namespace FavelaAmarela.Tests.EditMode
{
    /// <summary>
    /// Guarda que a navegação <b>esteja ligada</b> — e não só escrita.
    ///
    /// <para><b>O risco que este guarda cobre.</b> O <c>SeguidorDeCaminho</c> é opcional por
    /// design: sem ele o movimento degrada para a linha reta de sempre, em vez de a unidade
    /// travar. Essa escolha é boa e traz um risco conhecido — código que existe, compila, passa
    /// nos testes e <b>não está em prefab nenhum</b>. É o modo de falha assinatura deste
    /// repositório, e ele já custou: o <c>GeradorDeItem</c> sem chamador, o
    /// <c>ProgressionManager</c> fora de cena, a tabela de drop do Abdul apontando para nada, a
    /// barra de vida sem sprite.</para>
    ///
    /// <para><b>Guarda os dois lados</b>, porque os dois são erro: quem anda pelo chão precisa
    /// contornar, e quem <b>voa</b> não pode — dar navegação de chão ao Byakhee inverteria a
    /// identidade da luta dele.</para>
    /// </summary>
    public sealed class NavegacaoNasUnidadesTests
    {
        private const string Componente = "Navegacao.SeguidorDeCaminho";

        /// <summary>Quem anda pelo chão, com o que se perde sem contorno.</summary>
        private static readonly (string Prefab, string Perda)[] Contornam =
        {
            ("Assets/FavelaAmarela/Art/Enemies/Cultista.prefab",
             "onze deles perseguem no Deserto; sem contorno, todos encostam na parede"),

            ("Assets/FavelaAmarela/Art/Enemies/CoisaDoCemiterio.prefab",
             "caça por faro, e faro não atravessa parede"),

            ("Assets/FavelaAmarela/Art/Enemies/EspectroHali.prefab",
             "cercar sem contornar é encostar no muro"),

            ("Assets/FavelaAmarela/Art/Enemies/EsqueletoInvocado.prefab",
             "nasce numa arena com as Pedras de Poder no caminho"),

            ("Assets/FavelaAmarela/Art/Characters/MiGo/YugNeth.prefab",
             "o COMPANHEIRO — ele some atrás do muro e o jogador não entende por quê"),
        };

        /// <summary>Quem não deve contornar, com a razão.</summary>
        private static readonly (string Prefab, string Razao)[] NaoContornam =
        {
            ("Assets/FavelaAmarela/Art/Enemies/Byakhee.prefab",
             "VOA — os rasantes e o mergulho existem porque ele ignora o terreno"),

            ("Assets/FavelaAmarela/Art/Enemies/ConeDeGelo.prefab",
             "é projétil: linha reta é o comportamento correto"),

            ("Assets/FavelaAmarela/Art/Enemies/Abdul_Alhazred.prefab", "não se desloca"),
            ("Assets/FavelaAmarela/Art/Enemies/ReiEmAmarelo.prefab", "não se desloca"),
            ("Assets/FavelaAmarela/Art/Enemies/PedraDePoder.prefab", "é cenário quebrável"),
        };

        [Test]
        public void QuemAndaPeloChao_TemSeguidorDeCaminho()
        {
            var sem = new List<string>();

            foreach (var (prefab, perda) in Contornam)
            {
                if (!File.Exists(prefab)) { sem.Add($"{Nome(prefab)}: PREFAB AUSENTE"); continue; }

                if (!File.ReadAllText(prefab).Contains(Componente))
                    sem.Add($"{Nome(prefab)}: sem SeguidorDeCaminho — {perda}");
            }

            Assert.IsEmpty(sem,
                "Unidade(s) que andam pelo chão sem navegação:" + Environment.NewLine + "  " +
                string.Join(Environment.NewLine + "  ", sem) + Environment.NewLine +
                "Conserto: 'Tools/FavelaAmarela/Navegação: ligar nas unidades que andam'.");
        }

        [Test]
        public void QuemVoaOuNaoAnda_NaoTemSeguidorDeCaminho()
        {
            var demais = new List<string>();

            foreach (var (prefab, razao) in NaoContornam)
            {
                if (!File.Exists(prefab)) continue;

                if (File.ReadAllText(prefab).Contains(Componente))
                    demais.Add($"{Nome(prefab)}: ganhou SeguidorDeCaminho e não devia — {razao}");
            }

            Assert.IsEmpty(demais,
                "Unidade(s) com navegação de chão indevida:" + Environment.NewLine + "  " +
                string.Join(Environment.NewLine + "  ", demais));
        }

        /// <summary>
        /// Prefab de unidade novo tem de entrar numa das duas listas. Sem isto, o próximo ator
        /// nasce em linha reta e ninguém percebe — que é a história inteira deste repositório.
        /// </summary>
        [Test]
        public void NenhumaUnidade_FicaForaDasDuasListas()
        {
            var conhecidos = Contornam.Select(c => c.Prefab)
                .Concat(NaoContornam.Select(n => n.Prefab))
                .Select(Nome)
                .ToHashSet();

            var esquecidos = Directory
                .GetFiles("Assets/FavelaAmarela/Art/Enemies", "*.prefab")
                .Select(Path.GetFileNameWithoutExtension)
                .Where(n => !conhecidos.Contains(n))
                .ToList();

            Assert.IsEmpty(esquecidos,
                "Unidade(s) sem decisão registrada sobre navegação: " +
                string.Join(", ", esquecidos) + Environment.NewLine +
                "Acrescente a NavegacaoNasUnidadesTests.Contornam (com o que se perde) ou a " +
                "NaoContornam (com a razão).");
        }

        /// <summary>
        /// A navegação precisa <b>nascer sozinha</b>. Um <c>NavegacaoDoMundo</c> que dependesse
        /// de alguém pô-lo em cada cena estaria ausente exatamente na cena onde o inimigo
        /// trava — e o sintoma seria "a IA só funciona numa fase".
        /// </summary>
        [Test]
        public void ANavegacaoDoMundo_NasceSozinha()
        {
            string fonte = File.ReadAllText("Assets/Scripts/Navegacao/NavegacaoDoMundo.cs");

            StringAssert.Contains("RuntimeInitializeOnLoadMethod", fonte,
                "A navegação deixou de nascer sozinha.");

            StringAssert.Contains("LayerMask.GetMask(\"Obstacle\")", fonte,
                "A correção de máscara vazia sumiu. LayerMask serializada nasce ZERO num objeto " +
                "criado por código, e máscara zero significa 'nada bloqueia' — navegação que " +
                "atravessa parede, em silêncio. É o mesmo defeito que deixou o EnemyCombat sem " +
                "alvo.");

            StringAssert.Contains("Physics2D.OverlapBox", fonte,
                "A navegação parou de perguntar à FÍSICA. Ler o tilemap criaria uma segunda " +
                "representação do mundo, e o Lago de Hali — que é um colisor solto, fora de " +
                "tilemap nenhum — voltaria a ser atravessável.");
        }

        private static string Nome(string caminho) => Path.GetFileNameWithoutExtension(caminho);
    }
}
