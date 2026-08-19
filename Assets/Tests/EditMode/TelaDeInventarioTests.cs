using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace FavelaAmarela.Tests.EditMode
{
    /// <summary>
    /// Guarda a tela de inventário nas cenas.
    ///
    /// <para><b>O bug (2026-08-19, relatado pelo Vini):</b> "o botão TAB serve basicamente como
    /// um botão de pause, pois nada aparece na tela". O <c>PainelDeInventario</c> estava nas 4
    /// cenas, com <c>raizDoPainel</c> atribuído — por isso TAB pausava — mas
    /// <c>slotsDaMochila</c> e <c>slotsDoCorpo</c> tinham <b>zero entradas</b>. Abria um
    /// retângulo sem nenhuma casa.</para>
    ///
    /// <para>Este guarda existe porque o defeito é <b>invisível</b> em compilação, console e
    /// Inspector: um array serializado vazio não avisa nada, e o painel abre normalmente.</para>
    /// </summary>
    public sealed class TelaDeInventarioTests
    {
        /// <summary><c>MainInventory.DefaultCapacidadeSurvivalHorror</c>.</summary>
        private const int CasasDaMochila = 12;

        /// <summary>O array <c>anatomia</c> do <c>InventoryManager</c>: Arma, Elmo, Peitoral, Grevas, Amuleto, Anel.</summary>
        private const int CasasDoCorpo = 6;

        private const string ScriptDoPainel = "Assets/Scripts/UI/PainelDeInventario.cs";

        private static readonly string[] Cenas =
        {
            "Assets/Scenes/Deserto_Hali.unity",
            "Assets/Scenes/Playtest_RuinasPalidas.unity",
            "Assets/Scenes/Santuario_Yhtill.unity",
            "Assets/Scenes/Cena_ArenaDeTestes.unity",
        };

        private static string GuidDoPainel()
        {
            string meta = ScriptDoPainel + ".meta";
            if (!File.Exists(meta)) return null;
            return Regex.Match(File.ReadAllText(meta), @"(?m)^guid:\s*([0-9a-f]{32})").Groups[1].Value;
        }

        /// <summary>
        /// Conta os elementos de uma lista serializada dentro do bloco do componente.
        ///
        /// <para><b>Duas versões anteriores deste contador estavam erradas</b>, e as duas me
        /// fizeram concluir que a ferramenta não tinha gravado nada quando ela tinha. A
        /// indentação real do YAML da Unity aqui é de <b>2 espaços</b> para o campo e para o
        /// traço do item (<c>  slotsDaMochila:</c> / <c>  - grupo:</c>), com os sub-campos em 4
        /// — não 4/6 como eu supus. Medido no arquivo, contando os espaços.</para>
        ///
        /// <para>Corta a seção entre o campo e o próximo campo de mesmo nível, e conta os
        /// traços de item dentro dela.</para>
        /// </summary>
        private static int ContarEntradas(string bloco, string campo)
        {
            var m = Regex.Match(bloco, $@"(?ms)^  {campo}:[^\S\r\n]*\r?\n(.*?)(?=^  \w)");
            if (!m.Success) return -1;
            return Regex.Matches(m.Groups[1].Value, @"(?m)^  - ").Count;
        }

        [Test]
        public void TodaCena_TemOsSlotsDoInventarioPreenchidos()
        {
            string guid = GuidDoPainel();
            Assert.IsNotNull(guid, $"Sem .meta para {ScriptDoPainel}");

            var falhas = new List<string>();

            foreach (var caminho in Cenas)
            {
                if (!File.Exists(caminho)) { falhas.Add($"{caminho}: ausente"); continue; }

                string nome = Path.GetFileNameWithoutExtension(caminho);
                string txt = File.ReadAllText(caminho);

                var bloco = Regex.Split(txt, @"(?m)^--- ")
                                 .FirstOrDefault(d => d.Contains(guid) && d.Contains("!u!114"));

                if (bloco == null) { falhas.Add($"{nome}: sem PainelDeInventario"); continue; }

                int mochila = ContarEntradas(bloco, "slotsDaMochila");
                int corpo = ContarEntradas(bloco, "slotsDoCorpo");

                if (mochila != CasasDaMochila)
                    falhas.Add($"{nome}: slotsDaMochila com {mochila} entrada(s), esperado " +
                               $"{CasasDaMochila} — a tela abre sem casa nenhuma e o TAB vira " +
                               "só um pause.");

                if (corpo != CasasDoCorpo)
                    falhas.Add($"{nome}: slotsDoCorpo com {corpo} entrada(s), esperado {CasasDoCorpo}.");
            }

            Assert.IsEmpty(falhas,
                "Tela de inventário vazia. Rode 'Tools/FavelaAmarela/Montar tela de " +
                "inventario'.\n  " + string.Join("\n  ", falhas));
        }

        /// <summary>
        /// <c>raizDoPainel</c> nulo faz o painel abrir sem ligar objeto nenhum — o sintoma seria
        /// "TAB pausa e não acontece nada", parecido com o bug original mas por outra causa.
        /// </summary>
        [Test]
        public void TodaCena_TemRaizDoPainelAtribuida()
        {
            string guid = GuidDoPainel();
            Assert.IsNotNull(guid);

            var falhas = new List<string>();

            foreach (var caminho in Cenas)
            {
                if (!File.Exists(caminho)) continue;

                string nome = Path.GetFileNameWithoutExtension(caminho);
                var bloco = Regex.Split(File.ReadAllText(caminho), @"(?m)^--- ")
                                 .FirstOrDefault(d => d.Contains(guid) && d.Contains("!u!114"));
                if (bloco == null) continue;

                var m = Regex.Match(bloco, @"raizDoPainel:\s*\{fileID:\s*(-?\d+)\}");
                if (!m.Success || m.Groups[1].Value == "0")
                    falhas.Add($"{nome}: raizDoPainel não atribuído");
            }

            Assert.IsEmpty(falhas, string.Join("\n  ", falhas));
        }
    }
}
