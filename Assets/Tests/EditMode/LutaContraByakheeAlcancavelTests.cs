using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

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

            // Registro, sem asserção: a coleira agora é a sala inteira (63 de largura), e o
            // único limite que um rasante encontra é a linha dos Portões ao norte -- uma parede,
            // que ele respeita como qualquer parede. A asserção que ficava aqui ("rasante cabe
            // na altura da arena") guardava o defeito da elipse de 9 x 5, que já não existe.
            float alturaDaArena = AlturaDaArenaNaCena();
            TestContext.WriteLine(
                $"rasante {distanciaDoRasante:0.##} un | arena {alturaDaArena:0.##} un do gatilho " +
                "aos Portões (a largura é a sala, 63)");
        }

        /// <summary>
        /// Do <c>Gatilho_DaArena</c> (onde a luta começa) ao <c>Os_Portoes</c> (a muralha que o
        /// Byakhee não passa), em unidades de mundo, lidos da cena.
        /// </summary>
        private static float AlturaDaArenaNaCena()
        {
            EditorSceneManager.OpenScene(CenaDosPortoes, OpenSceneMode.Single);

            var gatilho = GameObject.Find("Gatilho_DaArena");
            var portoes = GameObject.Find("Os_Portoes");

            Assert.IsNotNull(gatilho, "'Gatilho_DaArena' não está na cena dos Portões.");
            Assert.IsNotNull(portoes, "'Os_Portoes' não está na cena dos Portões.");

            return portoes.transform.position.y - gatilho.transform.position.y;
        }

        /// <summary>
        /// <b>Onde o jogador pisa, o Byakhee pode voar.</b> A coleira dele é o mesmo Tilemap
        /// que barra Damião — logo não existe posição do jogador que a coleira não alcance.
        ///
        /// <para><b>O teste que estava aqui antes afirmava o contrário</b> —
        /// <c>AArena_CabeNoQueACameraMostra</c> exigia que a coleira coubesse na câmera
        /// (elipse de 9 × 5). Mas a câmera segue o jogador, e o jogador anda por uma sala de
        /// 63 × 31: bastava pisar fora da elipse e o chefe ficava pregado na borda mirando
        /// nele. O Vini relatou em 2026-09-10: <i>"presa a uma faixa da arena e não andando
        /// livremente"</i>. O teste passava verde enquanto isso acontecia — ele guardava a
        /// causa do defeito.</para>
        /// </summary>
        [Test]
        public void AColeiraDoByakhee_EhOChaoQueOJogadorPisa()
        {
            EditorSceneManager.OpenScene(CenaDosPortoes, OpenSceneMode.Single);

            var byakhee = Object.FindFirstObjectByType<FavelaAmarela.Runtime.Enemies.ByakheeAI>();
            Assert.IsNotNull(byakhee, "Nenhum ByakheeAI na cena dos Portões.");

            var so = new SerializedObject(byakhee);
            var chao = so.FindProperty("chaoDaArena").objectReferenceValue as Tilemap;
            var muralha = so.FindProperty("muralhaNorte").objectReferenceValue as Transform;

            Assert.IsNotNull(chao,
                "A coleira do Byakhee não está ligada a um Tilemap. Rode " +
                "Tools/FavelaAmarela/Arena: povoar a arena da Byakhee.");

            // O chão da coleira tem de ser o Tilemap com mais células PINTADAS — o que define a
            // sala. Pela caixa envolvente, o anel de paredes ganharia, e a coleira apontaria
            // para onde NÃO há chão.
            var maior = FavelaAmarela.Runtime.Enemies.ByakheeAI.ChaoComMaisTiles(
                Object.FindObjectsByType<Tilemap>(FindObjectsSortMode.None));

            Assert.AreSame(maior, chao,
                $"A coleira aponta para '{chao.name}', mas o chão da sala é '{maior.name}'. " +
                "Coleira que não é o chão prega o chefe — na borda ou no centro.");

            Assert.AreNotEqual("Colisao", chao.name,
                "A coleira está no anel de PAREDES: HasTile dá falso em todo o chão e o chefe " +
                "é puxado ao centro sem parar.");

            Assert.IsNotNull(muralha, "Sem muralha norte, o Byakhee voa por cima dos Portões.");
            Assert.AreEqual("Os_Portoes", muralha.name,
                "A muralha norte tem de ser o colisor dos Portões — é ele que o jogador não passa.");
        }

        /// <summary>
        /// <b>A arte do portão está onde o colisor está.</b> O <c>Batente</c> é filho de
        /// <c>Os_Portoes</c>; em 09/09 eu escrevi nele uma posição local achando que era mundo,
        /// e a arte foi parar 5,5 unidades acima da tela — os Portões sumiram da luta enquanto
        /// o colisor continuava lá. Este guarda mede em mundo, que é o que o jogador vê.
        /// </summary>
        [Test]
        public void AArteDoPortao_EstaNaLinhaDoColisor()
        {
            EditorSceneManager.OpenScene(CenaDosPortoes, OpenSceneMode.Single);

            var colisor = GameObject.Find("Os_Portoes");
            Assert.IsNotNull(colisor, "'Os_Portoes' não está na cena.");

            var batente = colisor.transform.Find("Batente");
            Assert.IsNotNull(batente, "'Batente' deixou de ser filho de 'Os_Portoes'.");

            // A arte é um DIORAMA: plataforma de pedra embaixo (2,7 un na arte, à escala 2) e
            // os pilares em cima. A linha que tem de coincidir com o colisor é a dos PILARES,
            // não a base — com a base no colisor, da arena só se via a plataforma (uma faixa
            // bege). O 2,7 é medido no PNG e documentado em CenarioDaArenaDaByakhee.
            const float AlturaDaPlataformaNaArte = 2.7f;

            float pilares = batente.position.y + AlturaDaPlataformaNaArte;
            float colisao = colisor.transform.position.y;

            Assert.AreEqual(colisao, pilares, 0.01f,
                $"Os pilares do portão estão em y={pilares:F2} e o colisor em y={colisao:F2}. " +
                "Ou o jogador bate numa parede invisível, ou vê um portão que não barra nada.");
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
