using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace FavelaAmarela.Tests.EditMode
{
    /// <summary>
    /// Guarda que os Pontos Focais de Relíquia <b>mudam na tela</b> quando acendem.
    ///
    /// <para><b>O defeito que motivou (2026-09-03).</b> Os três <c>Ponto_Focal_*</c> de
    /// <c>Castelo_Carcosa.unity</c> estavam com <c>spriteInativo: {fileID: 0}</c> e
    /// <c>spriteAtivo: {fileID: 0}</c> — os dois campos vazios, nos três. Ativar uma relíquia
    /// não mudava um pixel. O Vini relatou como <i>"os altares não estão funcionando"</i>; eles
    /// funcionavam, e eram <b>mudos</b>. O próprio doc do <c>PontoFocalDeReliquia</c> já
    /// registrava o buraco — <i>"trocava um sprite que não está autorado"</i> — e mesmo assim
    /// ninguém foi avisado, porque campo de cena vazio não é erro de compilação, não aparece no
    /// console e não quebra teste nenhum.</para>
    ///
    /// <para><b>O segundo teste é uma cicatriz.</b> Ao ligar o feixe eu escrevi os três
    /// componentes no YAML com um regex que atravessava o separador de documento: ele colhia o
    /// <c>m_GameObject</c> de <b>outro</b> objeto vários blocos adiante. Os animadores nasceram
    /// grudados em GameObjects sem relação, os pontos focais ficaram sem feixe, e o script
    /// relatou sucesso — porque os campos de sprite, esses, tinham sido preenchidos certo. Só
    /// conferir objeto por objeto revelou. <c>OFeixeEstaNoMesmoObjetoDoPontoFocal</c> é essa
    /// conferência, virada teste.</para>
    ///
    /// <para>Lê o YAML da cena porque é onde a ligação mora: em EditMode não há cena carregada,
    /// e um teste que instanciasse o componente à mão mediria o prefab, não o Castelo.</para>
    /// </summary>
    public sealed class AltaresRespondemNaTelaTests
    {
        private const string PastaDeCenas = "Assets/Scenes";
        private const string PontoFocal = "PontoFocalDeReliquia";
        private const string Feixe = "AnimadorDeAltarDeReliquia";

        /// <summary>Um objeto do YAML: tipo, id, e o corpo cru.</summary>
        private sealed class Bloco
        {
            public string Tipo;
            public string Id;
            public string Corpo;
            public string GameObject;
            public string Classe;
        }

        private static Dictionary<string, List<Bloco>> Cenas()
        {
            var saida = new Dictionary<string, List<Bloco>>();

            foreach (var cena in Directory.EnumerateFiles(PastaDeCenas, "*.unity"))
            {
                var blocos = new List<Bloco>();

                // Quebra pelo separador de documento ANTES de olhar o conteúdo. Foi exatamente
                // o passo que faltou no script que errou: um regex de arquivo inteiro salta o
                // separador sem perceber e mistura campos de objetos diferentes.
                foreach (var bruto in Regex.Split(File.ReadAllText(cena), @"(?m)^--- ").Skip(1))
                {
                    var m = Regex.Match(bruto, @"^!u!(\d+) &(\d+)");
                    if (!m.Success) continue;

                    var classe = Regex.Match(bruto, @"(?m)^  m_EditorClassIdentifier: (.*)$");
                    var go = Regex.Match(bruto, @"(?m)^  m_GameObject: \{fileID: (\d+)\}$");

                    blocos.Add(new Bloco
                    {
                        Tipo = m.Groups[1].Value,
                        Id = m.Groups[2].Value,
                        Corpo = bruto,
                        GameObject = go.Success ? go.Groups[1].Value : null,
                        Classe = classe.Success
                            ? classe.Groups[1].Value.Trim().Split('.').Last()
                            : "",
                    });
                }

                saida[Path.GetFileName(cena)] = blocos;
            }

            return saida;
        }

        private static string Artefato(Bloco b)
        {
            var m = Regex.Match(b.Corpo, @"(?m)^  artefatoId: (\S+)$");
            return m.Success ? m.Groups[1].Value : "(sem artefatoId)";
        }

        [Test]
        public void TodoPontoFocal_TemOsDoisSpritesPreenchidos()
        {
            var falhas = new List<string>();
            int achados = 0;

            foreach (var (cena, blocos) in Cenas())
            {
                foreach (var b in blocos.Where(x => x.Classe == PontoFocal))
                {
                    achados++;

                    foreach (var campo in new[] { "spriteInativo", "spriteAtivo" })
                    {
                        var m = Regex.Match(b.Corpo,
                            $@"(?m)^  {campo}: \{{fileID: (-?\d+)");

                        if (!m.Success)
                            falhas.Add($"  {cena} / {Artefato(b)}: campo '{campo}' sumiu");
                        else if (m.Groups[1].Value == "0")
                            falhas.Add($"  {cena} / {Artefato(b)}: '{campo}' vazio");
                    }
                }
            }

            // REGRA DURA: sem ponto focal nenhum, tudo acima passaria vazio e verde — que é
            // como o buraco original sobreviveu tanto tempo.
            Assert.Greater(achados, 0,
                $"Não achei um único {PontoFocal} em {PastaDeCenas}. Este teste não está " +
                "medindo o projeto — conserte a varredura antes de confiar no verde.");

            Assert.IsEmpty(falhas,
                "Ponto focal que não muda de cara ao acender — o jogador aperta E, a relíquia " +
                "é aceita, e a tela fica igual. É indistinguível de um altar quebrado:" +
                System.Environment.NewLine + string.Join(System.Environment.NewLine, falhas));
        }

        [Test]
        public void OFeixeEstaNoMesmoObjetoDoPontoFocal()
        {
            var falhas = new List<string>();
            int achados = 0;

            foreach (var (cena, blocos) in Cenas())
            {
                var donosDePontoFocal = new HashSet<string>(blocos
                    .Where(b => b.Classe == PontoFocal && b.GameObject != null)
                    .Select(b => b.GameObject));

                foreach (var b in blocos.Where(x => x.Classe == Feixe))
                {
                    achados++;

                    // O PontoFocalDeReliquia acha o feixe por GetComponent. Num GameObject
                    // qualquer, o componente existe, não dá erro, e nunca é chamado.
                    if (b.GameObject == null || !donosDePontoFocal.Contains(b.GameObject))
                        falhas.Add($"  {cena}: {Feixe} &{b.Id} está no GameObject " +
                                   $"{b.GameObject ?? "(nenhum)"}, que não tem {PontoFocal}. " +
                                   "O GetComponent nunca vai encontrá-lo.");

                    int quadros = Regex.Matches(
                        b.Corpo, @"(?m)^  - \{fileID: \d+, guid: [0-9a-f]{32}, type: 3\}$").Count;

                    if (quadros == 0)
                        falhas.Add($"  {cena}: {Feixe} &{b.Id} está sem quadros — " +
                                   "Acender() não faz nada e o altar fica na pedra.");
                }
            }

            Assert.Greater(achados, 0,
                $"Nenhum {Feixe} em cena. Se o feixe foi removido de propósito, apague este " +
                "teste junto; se não, a ligação se perdeu.");

            Assert.IsEmpty(falhas, "Feixe de altar ligado no objeto errado:" +
                System.Environment.NewLine + string.Join(System.Environment.NewLine, falhas));
        }
    }
}
