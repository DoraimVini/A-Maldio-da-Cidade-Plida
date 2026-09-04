using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace FavelaAmarela.Tests.EditMode
{
    /// <summary>
    /// Guarda a tela de inventário: as casas que já existiam na cena precisam estar
    /// <b>ligadas</b> ao <c>PainelDeInventario</c>, e as grades duplicadas que uma versão
    /// anterior desta sessão criou não podem voltar.
    ///
    /// <para><b>Os dois erros que motivaram (2026-08-19).</b> Primeiro: os arrays
    /// <c>slotsDaMochila</c>/<c>slotsDoCorpo</c> estavam vazios, então TAB abria um retângulo
    /// sem casa nenhuma — o componente existia, o código estava certo, ninguém preencheu.
    /// Segundo, ao corrigir isso eu <b>construí grades novas</b> em vez de ligar as que já
    /// existiam (<c>Mochila/Slot_0..11</c>, <c>Corpo/Corpo_0..6</c>), e a tela passou a mostrar
    /// dois inventários sobrepostos. O primeiro teste cobre o buraco original; o segundo cobre
    /// a regressão que eu mesmo introduzi.</para>
    /// </summary>
    public sealed class SlotsDoInventarioTests
    {
        private static readonly string[] Cenas =
        {
            "Assets/Scenes/Deserto_Hali.unity",
            "Assets/Scenes/Tumba_De_Alhazred.unity",
            "Assets/Scenes/Santuario_Yhtill.unity",
            "Assets/Scenes/Cena_ArenaDeTestes.unity",
        };

        /// <summary>
        /// <c>MainInventory.DefaultCapacidadeSurvivalHorror</c> — medido no código, não estimado.
        /// </summary>
        private const int CasasDaMochila = 12;

        /// <summary>
        /// O array <c>anatomia</c> do <c>InventoryManager</c> tem <b>7</b> entradas: Arma, Elmo,
        /// Peitoral, Grevas, Amuleto, Anel e <b>MaoSecundaria</b>. A sétima foi justamente a que
        /// eu perdi ao construir a grade duplicada — por isso ela está escrita aqui.
        /// </summary>
        private const int CasasDoCorpo = 7;

        [Test]
        public void NenhumaCena_TemAsGradesDuplicadas()
        {
            var falhas = new List<string>();

            foreach (var caminho in Cenas)
            {
                if (!File.Exists(caminho)) { falhas.Add($"{caminho}: ausente"); continue; }

                string txt = File.ReadAllText(caminho);

                foreach (var nome in new[] { "Grade_Mochila", "Grade_Corpo" })
                {
                    if (Regex.IsMatch(txt, $@"(?m)^\s+m_Name:\s*{nome}\s*$"))
                        falhas.Add($"{Path.GetFileNameWithoutExtension(caminho)}: '{nome}' voltou");
                }
            }

            Assert.IsEmpty(falhas,
                "Grades duplicadas de inventário na cena. As casas de verdade são " +
                "Mochila/Slot_0..11 e Corpo/Corpo_0..6, que já existiam — construir outras faz " +
                "a tela mostrar dois inventários sobrepostos.\n  " + string.Join("\n  ", falhas));
        }

                // MIGRADO EM 2026-08-22 (Bloco 6): o HUD deixou de ser montado por cena e passou
        // a viver em Resources/HUD_Gameplay.prefab, carregado por
        // HUDController.GarantirInstancia com DontDestroyOnLoad. Verificar a presenca
        // dele nas cenas passou a ser verificar o vazio. A cobertura NAO sumiu: mudou de
        // alvo para HudPersistenteTests, que checa o prefab.
        [Ignore("Contrato mudou: ver HudPersistenteTests")]
        [Test]
        public void TodaCena_TemOsSlotsDoInventarioPreenchidos()
        {
            var falhas = new List<string>();

            foreach (var caminho in Cenas)
            {
                if (!File.Exists(caminho)) { falhas.Add($"{caminho}: ausente"); continue; }

                string txt = File.ReadAllText(caminho);
                string cena = Path.GetFileNameWithoutExtension(caminho);

                // O bloco do PainelDeInventario é o único que traz os dois arrays juntos.
                var bloco = Regex.Split(txt, @"(?m)^--- ")
                    .FirstOrDefault(d => d.Contains("slotsDaMochila:") && d.Contains("slotsDoCorpo:"));

                if (bloco == null) { falhas.Add($"{cena}: sem PainelDeInventario"); continue; }

                int mochila = ContarEntradas(bloco, "slotsDaMochila");
                int corpo = ContarEntradas(bloco, "slotsDoCorpo");

                if (mochila != CasasDaMochila)
                    falhas.Add($"{cena}: slotsDaMochila tem {mochila}, esperado {CasasDaMochila}");

                if (corpo != CasasDoCorpo)
                    falhas.Add($"{cena}: slotsDoCorpo tem {corpo}, esperado {CasasDoCorpo}");
            }

            Assert.IsEmpty(falhas,
                "Tela de inventário sem casas ligadas — TAB abre um retângulo vazio, sem erro " +
                "no console. Rode 'Tools/FavelaAmarela/Ligar slots do inventario existentes'.\n  " +
                string.Join("\n  ", falhas));
        }

        /// <summary>
        /// <c>raizDoPainel</c> nulo faz o painel abrir sem ligar objeto nenhum — o sintoma seria
        /// "TAB pausa e não acontece nada", parecido com o buraco original mas por outra causa.
        ///
        /// <para>Herdado de <c>TelaDeInventarioTests</c>, que foi removida: os outros dois
        /// testes dela duplicavam os daqui, e um cravava 6 casas de corpo — o número errado, que
        /// reprovava a cena correta depois que a sétima (<c>MaoSecundaria</c>) foi ligada.</para>
        /// </summary>
        [Test]
        public void TodaCena_TemRaizDoPainelAtribuida()
        {
            var falhas = new List<string>();

            foreach (var caminho in Cenas)
            {
                if (!File.Exists(caminho)) continue;

                var bloco = Regex.Split(File.ReadAllText(caminho), @"(?m)^--- ")
                    .FirstOrDefault(d => d.Contains("slotsDaMochila:") && d.Contains("slotsDoCorpo:"));
                if (bloco == null) continue;

                var m = Regex.Match(bloco, @"raizDoPainel:\s*\{fileID:\s*(-?\d+)\}");
                if (!m.Success || m.Groups[1].Value == "0")
                    falhas.Add($"{Path.GetFileNameWithoutExtension(caminho)}: raizDoPainel não atribuído");
            }

            Assert.IsEmpty(falhas, string.Join("\n  ", falhas));
        }

        /// <summary>
        /// Conta as entradas de um array serializado: cada elemento de <c>SlotVisual</c> começa
        /// com a linha <c>- grupo:</c>. Delimita no próximo campo de mesmo recuo para não
        /// contar entradas do array seguinte.
        ///
        /// <para><b>O recorte para no fim da linha do campo, não em <c>\s*</c>.</b> Com
        /// <c>{campo}:\s*</c> o quantificador engolia a quebra de linha <b>e o recuo da primeira
        /// entrada</b>, que então deixava de casar com <c>^\s+-</c> — a contagem vinha sempre um
        /// a menos e este guarda reprovava cena correta. Mesmo erro do teste de
        /// <c>alignment</c> do Byakhee: medir o dado certo pelo padrão errado.</para>
        /// </summary>
        private static int ContarEntradas(string bloco, string campo)
        {
            var m = Regex.Match(bloco, $@"(?ms)^\s+{campo}:[ \t]*\r?\n(.*?)(?=^\s{{2}}\w)");
            if (!m.Success) return -1;

            return Regex.Matches(m.Groups[1].Value, @"(?m)^\s+-\s+grupo:").Count;
        }
    }
}
