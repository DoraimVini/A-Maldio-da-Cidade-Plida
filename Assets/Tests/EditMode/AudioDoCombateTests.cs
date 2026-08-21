using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace FavelaAmarela.Tests.EditMode
{
    /// <summary>
    /// Guarda o som do combate.
    ///
    /// <para><b>Por que existe (2026-08-20):</b> o Vini relatou que a luta contra o Byakhee
    /// <i>"não tem feel bom"</i>. Contando os disparos de <c>SomDoJogo</c> no código,
    /// <b>quatro dos nove sons nunca eram tocados por ninguém</b> — existiam só como forma de
    /// onda em <c>SinteseDeSom</c>. Dois deles eram <c>GolpeDesferido</c> e
    /// <c>HabilidadeDeArma</c>. Somado ao <c>AudioDeCombate</c> estar apenas no
    /// <c>Cultista.prefab</c>, o resultado era: atacar o chefe não fazia som e acertá-lo
    /// também não.</para>
    ///
    /// <para>Não era bug de mixagem. Era wiring ausente — sistema inteiro escrito e ligado em
    /// ponta nenhuma, o modo de falha mais repetido deste projeto.</para>
    ///
    /// <para><b>Este teste mede disparo, não existência.</b> Um guarda que só verificasse "o
    /// enum tem nove valores" teria passado durante todo o período em que o combate era mudo.
    /// O que importa é alguém <b>chamar</b> <c>Tocar</c> com cada som.</para>
    /// </summary>
    public sealed class AudioDoCombateTests
    {
        private const string PastaDeAudio = "Assets/Scripts/Audio";

        /// <summary>
        /// Sons que precisam ter um disparador de verdade, e o que quebra em jogo sem ele.
        ///
        /// <para><c>ItemRecolhido</c> e <c>ArtefatoInvocado</c> ficam <b>fora</b> de propósito:
        /// não existe hoje um evento de "peguei do chão" nem de "invoquei artefato" para
        /// assinar (só <c>OnItemConsumed</c> e <c>OnArtefatosMudaram</c>, que são outra coisa).
        /// Ligá-los exige evento novo, e isso não cabia na véspera da build. Ficam registrados
        /// aqui como dívida conhecida em vez de sumirem.</para>
        /// </summary>
        private static readonly Dictionary<string, string> SonsQueDevemTocar =
            new Dictionary<string, string>
            {
                ["PassoDeDamiao"] = "o pilar de furtividade sonora fica inaudível",
                ["GolpeDesferido"] = "o jogador ataca e não ouve o próprio golpe",
                ["HabilidadeDeArma"] = "a habilidade da arma sai muda",
                ["EntidadeFerida"] = "acertar um inimigo não dá retorno nenhum",
                ["EntidadeAbatida"] = "matar um inimigo não dá retorno nenhum",
                ["EntrouEmPanico"] = "a virada de estado mental passa despercebida",
                ["Colapso"] = "o fim de jogo por Colapso é silencioso",
            };

        [Test]
        public void TodoSomDeCombate_TemQuemODispare()
        {
            var arquivos = Directory.GetFiles("Assets/Scripts", "*.cs", SearchOption.AllDirectories)
                // SomDoJogo.cs só declara o enum; SinteseDeSom.cs só desenha a onda. Nenhum dos
                // dois é disparo — contá-los faria todo som parecer ligado.
                .Where(f => !f.EndsWith("SomDoJogo.cs") && !f.EndsWith("SinteseDeSom.cs"))
                .ToDictionary(f => f, File.ReadAllText);

            var mudos = new List<string>();

            foreach (var som in SonsQueDevemTocar)
            {
                bool alguemDispara = arquivos.Values.Any(
                    t => t.Contains("SomDoJogo." + som.Key));

                if (!alguemDispara)
                    mudos.Add($"{som.Key}: ninguém dispara — {som.Value}");
            }

            Assert.IsEmpty(mudos,
                "Sons definidos que nunca tocam:\n  " + string.Join("\n  ", mudos));
        }

        /// <summary>
        /// O componente que dá voz ao golpe precisa estar <b>no prefab do Damião</b>, não solto
        /// numa cena: pondo no prefab, toda cena presente e futura herda, e não nasce mais uma
        /// lista de cenas para alguém esquecer de atualizar.
        /// </summary>
        [Test]
        public void ODamiao_CarregaOAudioDoJogador()
            => AssertPrefabTemComponente(
                "Assets/FavelaAmarela/Art/Characters/Damiao/Player_Damiao.prefab",
                "AudioDoJogador",
                "os golpes de Damião saem mudos");

        /// <summary>
        /// O Byakhee estava sem <c>AudioDeCombate</c> — acertar o chefe não produzia som.
        /// </summary>
        [Test]
        public void OByakhee_CarregaOAudioDeCombate()
            => AssertPrefabTemComponente(
                "Assets/FavelaAmarela/Art/Enemies/Byakhee.prefab",
                "AudioDeCombate",
                "acertar e abater o chefe não produz som");

        private static void AssertPrefabTemComponente(string prefab, string componente,
                                                      string consequencia)
        {
            Assert.IsTrue(File.Exists(prefab), $"Prefab ausente: {prefab}");

            var meta = Directory.GetFiles(PastaDeAudio, componente + ".cs.meta",
                                          SearchOption.AllDirectories).FirstOrDefault();

            Assert.IsNotNull(meta, $"Não achei {componente}.cs.meta — o guarda não se verifica.");

            var guid = Regex.Match(File.ReadAllText(meta), @"guid: ([0-9a-f]{32})");
            Assert.IsTrue(guid.Success, $"Sem GUID em {componente}.cs.meta");

            Assert.IsTrue(File.ReadAllText(prefab).Contains(guid.Groups[1].Value),
                $"{Path.GetFileName(prefab)} está sem {componente}: {consequencia}. " +
                "Conserto: 'Tools/FavelaAmarela/Áudio: ligar o som do combate'.");
        }
    }
}
