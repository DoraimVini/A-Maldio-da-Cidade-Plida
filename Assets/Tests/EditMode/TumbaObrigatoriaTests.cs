using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace FavelaAmarela.Tests.EditMode
{
    /// <summary>
    /// Guarda a decisão de <b>2026-08-20</b>: a Tumba de Alhazred é obrigatória antes dos
    /// Portões das Ruínas.
    ///
    /// <para><b>Por quê:</b> é na Tumba que Yug-Neth é libertado de Abdul, e
    /// <c>TravessiaDoCompanheiro</c> deriva a presença dele de
    /// <c>ChavesDeSave.AbdulResolvido</c>. Sem a trava, dava para ir do Deserto direto aos
    /// Portões e encarar o Byakhee <b>sem arma e sem companheiro</b> — e depois entrar no
    /// Castelo sem o Yug-Neth que vira o NPC de artesanato. Foi a raiz do "o Damião não causou
    /// dano na Byakhee" relatado no playtest.</para>
    ///
    /// <para>O Vini pesou as alternativas (destravar com aviso, ou dar arma inicial no Deserto)
    /// e escolheu manter a Tumba obrigatória para simplificar o Vertical Slice.</para>
    /// </summary>
    public sealed class TumbaObrigatoriaTests
    {
        private const string Deserto = "Assets/Scenes/Deserto_Hali.unity";

        /// <summary>Valor de <c>ChavesDeSave.AbdulResolvido</c>, repetido aqui de propósito:
        /// o teste tem que falhar se alguém renomear a chave sem revisar a trava.</summary>
        private const string ChaveDaTumba = "Quest.Tumba.AbdulResolvido";

        [Test]
        public void OPortalDosPortoes_ExigeATumbaResolvida()
        {
            Assert.IsTrue(File.Exists(Deserto), $"{Deserto} não existe.");
            string yaml = File.ReadAllText(Deserto);

            // O portal para os Portões, dentro do bloco do MonoBehaviour que o declara.
            var bloco = Regex.Match(
                yaml,
                @"MonoBehaviour:(?:(?!^---)[\s\S])*?cenaDestino:\s*Portoes_Das_Ruinas(?:(?!^---)[\s\S])*",
                RegexOptions.Multiline);

            Assert.IsTrue(bloco.Success,
                "Não achei no Deserto o PortalDeCena com destino Portoes_Das_Ruinas.");

            var chave = Regex.Match(bloco.Value, @"chaveExigida:[ 	]*([^\r\n]*)");

            Assert.IsTrue(chave.Success,
                "O PortalDeCena para os Portões não tem o campo 'chaveExigida'. " +
                "Se ele foi removido do componente, esta decisão de design se perdeu.");

            Assert.AreEqual(ChaveDaTumba, chave.Groups[1].Value.Trim(),
                "O portal do Deserto para os Portões deixou de exigir a Tumba. " +
                "Sem isso o jogador chega ao Byakhee sem arma e sem Yug-Neth. " +
                "Conserto: rode 'Tools/FavelaAmarela/Montar Portões das Ruínas'.");
        }

        [Test]
        public void OPortalTrancado_TemComoAvisarOJogador()
        {
            string yaml = File.ReadAllText(Deserto);

            var bloco = Regex.Match(
                yaml,
                @"MonoBehaviour:(?:(?!^---)[\s\S])*?cenaDestino:\s*Portoes_Das_Ruinas(?:(?!^---)[\s\S])*",
                RegexOptions.Multiline);

            Assert.IsTrue(bloco.Success, "Portal para os Portões não encontrado.");

            var caixa = Regex.Match(bloco.Value, @"caixaDeTexto:\s*\{fileID:\s*(-?\d+)");

            Assert.IsTrue(caixa.Success && caixa.Groups[1].Value != "0",
                "O portal está trancado mas sem caixa de texto ligada: o jogador esbarraria " +
                "numa parede invisível sem explicação nenhuma. Uma trava sem aviso lê como bug.");

            var linha = Regex.Match(bloco.Value, @"linhaSeTrancado:[ 	]*([^\r\n]*)");

            Assert.IsTrue(linha.Success && !string.IsNullOrWhiteSpace(linha.Groups[1].Value),
                "O portal trancado está sem linha de recusa.");
        }
    }
}
