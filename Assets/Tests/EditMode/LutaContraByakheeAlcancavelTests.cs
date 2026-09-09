using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace FavelaAmarela.Tests.EditMode
{
    /// <summary>
    /// Guarda que a <b>janela de dano do Byakhee é alcançável</b>, e que a coleira da arena tem
    /// centro.
    ///
    /// <para><b>O relato que originou (2026-09-09).</b> O Vini jogou e disse três coisas:
    /// <i>"não dá para ganhar da Byakhee"</i>, <i>"ela continua saindo do mapa"</i> e <i>"a
    /// hurtbox dela é muito difícil de atingir"</i>. Medido, as três saem de uma causa só, e não
    /// da hurtbox — que aliás é <b>generosa</b>: 8,45 × 10,09 unidades de mundo contra um corpo
    /// desenhado de 5,72.</para>
    ///
    /// <para><b>A causa.</b> O chefe só pode ser ferido no chão (imunidade em voo, por design).
    /// O rasante corre <c>velocidadeRasante × duracaoRasante</c> = <b>12 unidades em linha
    /// reta</b>, atravessando o jogador e seguindo adiante; e <c>DepoisDoVoo</c> alterna
    /// mergulho e pouso, então <b>um pouso em cada dois acontece a 12 unidades do jogador</b>.
    /// Ele pousava e ficava <i>parado</i> lá. A janela da fase 2 dura 1,5 s, e correndo a 7,5
    /// un/s o Damião cobre 11,25 — <b>menos que 12</b>. Metade das janelas era fisicamente
    /// inalcançável, e o dreno passivo de Resiliência corria o tempo todo.</para>
    ///
    /// <para>Este teste é a conta acima, escrita. Ele teria reprovado antes do conserto.</para>
    /// </summary>
    public sealed class LutaContraByakheeAlcancavelTests
    {
        private const string Prefab = "Assets/FavelaAmarela/Art/Enemies/Byakhee.prefab";
        private const string Fsm = "Assets/Scripts/Core/Enemies/ByakheeFSM.cs";
        private const string Locomocao = "Assets/FavelaAmarela/Config/LocomocaoConfig.asset";
        private const string CenaDosPortoes = "Assets/Scenes/Portoes_Das_Ruinas.unity";

        [Test]
        public void AJanelaDeDano_EAlcancavelNaPiorFase()
        {
            float velocidadeRasante = CampoDoPrefab("velocidadeRasante");
            float velocidadeNoChao = CampoDoPrefab("velocidadeNoChao");
            float raioDaArena = CampoDoPrefab("raioDaArena");

            float duracaoRasante = PadraoDaFsm("duracaoRasante");
            float menorJanela = PadraoDaFsm("duracaoPousoFase2");

            float corridaDoJogador = CampoDoAsset(Locomocao, "runSpeed");

            float distanciaDoRasante = velocidadeRasante * duracaoRasante;

            // O chefe agora se arrasta na direção do jogador ENQUANTO vulnerável, então a
            // distância fecha pelos dois lados.
            float velocidadeDeFechamento = corridaDoJogador + velocidadeNoChao;
            float tempoParaChegar = distanciaDoRasante / velocidadeDeFechamento;

            TestContext.WriteLine(
                $"rasante {velocidadeRasante:0.##} un/s × {duracaoRasante:0.##} s = " +
                $"{distanciaDoRasante:0.##} un | fechamento {corridaDoJogador:0.##} + " +
                $"{velocidadeNoChao:0.##} = {velocidadeDeFechamento:0.##} un/s | " +
                $"chega em {tempoParaChegar:0.##} s | janela mínima {menorJanela:0.##} s");

            Assert.Less(tempoParaChegar, menorJanela,
                $"O Byakhee termina o rasante a {distanciaDoRasante:0.##} unidades do jogador e " +
                $"a janela de dano mais curta dura {menorJanela:0.##} s. Fechando a " +
                $"{velocidadeDeFechamento:0.##} un/s, chegar leva {tempoParaChegar:0.##} s — a " +
                "janela fecha antes, e METADE das oportunidades de dano da luta deixa de " +
                "existir. Some a isso o dreno passivo de Resiliência, que corre o tempo todo, e " +
                "a luta não fecha: foi o 'não dá para ganhar da Byakhee' do playtest.\n" +
                "Saídas: encurtar o rasante, acelerar o arrasto no chão (velocidadeNoChao), ou " +
                "alongar a janela da fase 2.");

            Assert.LessOrEqual(distanciaDoRasante, raioDaArena * 2f,
                $"Um rasante percorre {distanciaDoRasante:0.##} unidades e a arena tem " +
                $"{raioDaArena * 2f:0.##} de diâmetro. O chefe atravessa a arena inteira num " +
                "movimento só e passa a viver colado na coleira, brigando com ela em vez de " +
                "voar o padrão.");
        }

        /// <summary>
        /// A coleira da arena precisa de <b>centro</b>. Sem o campo ligado, o código cai no
        /// fallback <c>transform.position</c> — o ponto onde o chefe nasceu —, e uma coleira
        /// presa ao próprio chefe não segura nada.
        ///
        /// <para><b>Este teste nasceu de um diagnóstico meu que estava errado (2026-09-09).</b>
        /// Eu li <c>centroDaArena: {fileID: 0}</c> no <b>prefab</b> e anunciei que a ligação
        /// estava faltando. Ela nunca esteve: um prefab <i>não consegue</i> referenciar objeto
        /// de cena, então o <c>{fileID: 0}</c> de lá é o normal — a referência vive no override
        /// da <b>instância</b>, e estava correta desde o commit anterior. Cheguei a escrever uma
        /// ferramenta para "consertar" e ela gravou exatamente o mesmo valor, sem mudar um byte.
        /// Prefab não é a instância.</para>
        ///
        /// <para>O guarda fica porque o requisito é real e não era coberto — e porque ele mede
        /// no lugar certo, que é onde eu não medi.</para>
        /// </summary>
        [Test]
        public void OCentroDaArena_EstaLigadoNaCena()
        {
            Assert.IsTrue(File.Exists(CenaDosPortoes), $"Cena ausente: {CenaDosPortoes}");

            string yaml = File.ReadAllText(CenaDosPortoes);

            StringAssert.Contains("Centro_DaArena", yaml,
                "O objeto 'Centro_DaArena' sumiu da cena dos Portões. Sem ele a coleira da " +
                "arena não tem para onde puxar o chefe de volta.");

            var mod = Regex.Match(yaml,
                @"propertyPath: centroDaArena\s*\n\s*value:[^\n]*\n\s*objectReference: \{fileID: (-?\d+)");

            Assert.IsTrue(mod.Success,
                "A instância do Byakhee na cena não tem override de 'centroDaArena'. O prefab " +
                "guarda {fileID: 0} — um prefab não consegue referenciar objeto de cena —, " +
                "então a ligação SÓ pode existir como modificação da instância. " +
                "Conserto: selecione o Byakhee NA CENA e arraste 'Centro_DaArena' para o campo " +
                "'Centro Da Arena' no Inspector.");

            Assert.AreNotEqual("0", mod.Groups[1].Value,
                "O override de 'centroDaArena' existe e aponta para NADA (fileID 0). O código " +
                "cai no fallback transform.position, e a coleira passa a seguir o chefe em vez " +
                "de segurá-lo — que é o 'ela continua saindo do mapa' do playtest.");
        }

        // ── leitura ──────────────────────────────────────────────────────────

        /// <summary>
        /// O valor que vale em jogo: o do prefab quando ele carrega o campo, e o
        /// <b>inicializador do C#</b> quando não carrega.
        ///
        /// <para>Não é conveniência, é como a Unity resolve de verdade: acrescentar um
        /// <c>[SerializeField]</c> não reescreve os prefabs existentes, e até alguém salvá-los
        /// o campo simplesmente não está no YAML — a desserialização cai no inicializador. Uma
        /// primeira versão deste teste só olhava o prefab e reprovou por não achar
        /// <c>velocidadeNoChao</c> minutos depois de o campo nascer.</para>
        /// </summary>
        private static float CampoDoPrefab(string nome)
        {
            var noPrefab = Procurar(Prefab, $@"(?m)^\s*{Regex.Escape(nome)}: ([-\d.eE]+)\s*$");
            if (noPrefab != null)
                return float.Parse(noPrefab, CultureInfo.InvariantCulture);

            const string fonte = "Assets/Scripts/Enemies/ByakheeAI.cs";
            var noCodigo = Procurar(fonte, $@"private float {Regex.Escape(nome)} = ([-\d.]+)f");

            Assert.IsNotNull(noCodigo,
                $"Campo '{nome}' não existe nem no {Path.GetFileName(Prefab)} nem como campo " +
                $"serializado de {Path.GetFileName(fonte)}. Foi renomeado — e sem ele este " +
                "guarda mediria outra coisa em silêncio, que é pior do que não existir.");

            return float.Parse(noCodigo, CultureInfo.InvariantCulture);
        }

        private static string Procurar(string caminho, string padrao)
        {
            Assert.IsTrue(File.Exists(caminho), $"Arquivo ausente: {caminho}");

            var m = Regex.Match(File.ReadAllText(caminho), padrao);
            return m.Success ? m.Groups[1].Value : null;
        }


        private static float CampoDoAsset(string caminho, string nome)
        {
            Assert.IsTrue(File.Exists(caminho), $"Arquivo ausente: {caminho}");

            var m = Regex.Match(File.ReadAllText(caminho),
                                $@"(?m)^\s*{Regex.Escape(nome)}: ([-\d.eE]+)\s*$");

            Assert.IsTrue(m.Success,
                $"Campo '{nome}' não encontrado em {Path.GetFileName(caminho)}. Ele foi " +
                "renomeado ou nunca foi serializado — e sem ele este guarda mediria outra " +
                "coisa em silêncio, que é pior do que não existir.");

            return float.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Lê um valor padrão do construtor de <c>ByakheeFSM</c>. O prefab chama
        /// <c>new ByakheeFSM()</c> sem argumento nenhum, então <b>os padrões são os números que
        /// valem em jogo</b> — não há asset onde lê-los.
        /// </summary>
        private static float PadraoDaFsm(string parametro)
        {
            Assert.IsTrue(File.Exists(Fsm), $"Arquivo ausente: {Fsm}");

            string fonte = File.ReadAllText(Fsm);

            StringAssert.Contains("new ByakheeFSM()", File.ReadAllText(
                    "Assets/Scripts/Enemies/ByakheeAI.cs"),
                "O ByakheeAI deixou de construir a FSM sem argumentos. Os padrões do construtor " +
                "pararam de ser os números que valem em jogo, e este teste passou a medir " +
                "ficção — conserte a leitura antes de confiar no resultado.");

            var m = Regex.Match(fonte,
                $@"float {Regex.Escape(parametro)} = ([-\d.]+)f");

            Assert.IsTrue(m.Success,
                $"Parâmetro '{parametro}' não encontrado no construtor de ByakheeFSM. Foi " +
                "renomeado, e este guarda pararia de guardar.");

            return float.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
        }
    }
}
