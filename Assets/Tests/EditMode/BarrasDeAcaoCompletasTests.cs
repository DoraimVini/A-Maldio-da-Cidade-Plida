using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using FavelaAmarela.Inventario;

namespace FavelaAmarela.Tests.EditMode
{
    /// <summary>
    /// Guarda que a <b>barra de ações</b> e a <b>barra de artefatos</b> continuam ligadas por
    /// inteiro.
    ///
    /// <para><b>O que a auditoria de 2026-09-02 encontrou.</b> As duas estavam
    /// <b>funcionalmente incompletas</b>, e o código das duas já fazia tudo:</para>
    ///
    /// <list type="bullet">
    ///   <item><c>BarraDeAcoes.slots</c> estava <b>vazio</b>. O <c>Update()</c> lê
    ///   <c>slots[0]</c> para animar a recarga; com o array vazio, a recarga da habilidade
    ///   <b>nunca era desenhada</b>. Os objetos existiam soltos na hierarquia.</item>
    ///
    ///   <item>Os 4 slots da <c>BarraDeArtefatos</c> tinham <c>icone</c>,
    ///   <c>preenchimentoRecarga</c> e <c>rotuloTecla</c> <b>nulos</b> — atrás de um
    ///   <c>if (!= null)</c> que nunca passava.</item>
    /// </list>
    ///
    /// <para>Nenhuma das duas dava erro. Uma barra que não desenha recarga é indistinguível de
    /// uma habilidade que nunca fica pronta, e é o jogador quem descobre.</para>
    /// </summary>
    public sealed class BarrasDeAcaoCompletasTests
    {
        private const string Hud = "Assets/FavelaAmarela/Resources/HUD_Gameplay.prefab";

        /// <summary>Corpo serializado do componente de nome dado.</summary>
        private static string Componente(string classe)
        {
            Assert.IsTrue(File.Exists(Hud), $"Prefab ausente: {Hud}");

            var m = Regex.Match(File.ReadAllText(Hud),
                $@"m_EditorClassIdentifier:.*\.{Regex.Escape(classe)}\s*\n((?:.*\n)*?)(?=^--- )",
                RegexOptions.Multiline);

            Assert.IsTrue(m.Success, $"'{classe}' não está no HUD_Gameplay.prefab.");
            return m.Groups[1].Value;
        }

        /// <summary>Os campos de cada entrada de `slots:`, na ordem em que aparecem.</summary>
        private static string[][] Entradas(string corpo, string[] campos)
        {
            var bloco = Regex.Match(corpo, @"^\s*slots:\s*\n((?:\s*[-\s].*\n)*)",
                                    RegexOptions.Multiline);

            if (!bloco.Success) return new string[0][];

            return Regex.Split(bloco.Groups[1].Value, @"(?m)^\s*-\s")
                        .Where(t => !string.IsNullOrWhiteSpace(t))
                        .Select(t => campos
                            .Select(cp => Regex.Match(t, $@"{cp}: \{{fileID: (-?\d+)\}}")
                                               .Groups[1].Value)
                            .ToArray())
                        .ToArray();
        }

        [Test]
        public void ABarraDeAcoesTemOSlotDaHabilidadeMontado()
        {
            string corpo = Componente("BarraDeAcoes");

            var campos = new[] { "grupo", "nomeDaHabilidade", "icone",
                                 "preenchimentoRecarga", "rotuloTecla" };
            var entradas = Entradas(corpo, campos);

            Assert.GreaterOrEqual(entradas.Length, 1,
                "BarraDeAcoes.slots está VAZIO. O Update() lê slots[0] para animar a recarga: " +
                "com o array vazio, a recarga da habilidade nunca é desenhada e o jogador " +
                "dispara às cegas. Foi o estado do projeto até 2026-09-02.");

            var faltando = campos
                .Where((_, i) => string.IsNullOrEmpty(entradas[0][i]) || entradas[0][i] == "0")
                .ToList();

            Assert.IsEmpty(faltando,
                "slots[0] da BarraDeAcoes com campo(s) nulo(s): " + string.Join(", ", faltando));
        }

        [Test]
        public void OsQuatroSlotsDeArtefatoEstaoLigadosPorInteiro()
        {
            string corpo = Componente("BarraDeArtefatos");

            var campos = new[] { "grupo", "nomeDaHabilidade", "icone",
                                 "preenchimentoRecarga", "rotuloTecla" };
            var entradas = Entradas(corpo, campos);

            Assert.AreEqual(4, entradas.Length,
                $"A barra de artefatos tem {entradas.Length} slot(s); o design pede 4 (F1–F4).");

            var problemas = entradas
                .SelectMany((e, i) => campos
                    .Where((_, k) => string.IsNullOrEmpty(e[k]) || e[k] == "0")
                    .Select(cp => $"F{i + 1}.{cp}"))
                .ToList();

            Assert.IsEmpty(problemas,
                "Referência(s) nula(s) na barra de artefatos: " + string.Join(", ", problemas) +
                ". O Redesenhar já pinta ícone e tecla, e o Update já preenche a recarga — " +
                "tudo atrás de um 'if (!= null)'. Nulo aqui não dá erro: só não aparece.");
        }

        [Test]
        public void ABarraDeAcoesMostraOIconeDaArma()
        {
            string corpo = Componente("BarraDeAcoes");

            Assert.IsTrue(Regex.IsMatch(corpo, @"^\s*iconeDaArma: \{fileID: [1-9]",
                                        RegexOptions.Multiline),
                "BarraDeAcoes.iconeDaArma está nulo: a barra nomeia a arma e não a mostra.");
        }

        /// <summary>
        /// Era a última lacuna de ícone do projeto: os 25 <c>ItemDef</c> e os 4
        /// <c>ArtefatoDef</c> já tinham o seu, e o <c>HabilidadeDef</c> <b>nem campo</b> tinha.
        /// </summary>
        [Test]
        public void TodaHabilidadeTemIcone()
        {
            var todas = AssetDatabase.FindAssets("t:HabilidadeDef")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<HabilidadeDef>)
                .Where(h => h != null)
                .ToArray();

            Assert.IsNotEmpty(todas, "Nenhum HabilidadeDef no projeto.");

            var sem = todas.Where(h => h.Icone == null).Select(h => h.name).ToList();

            Assert.IsEmpty(sem,
                "Habilidade(s) sem ícone: " + string.Join(", ", sem) +
                ". Conserto: 'Tools/FavelaAmarela/UI: completar as barras de ação e de " +
                "artefatos' — o ícone é DERIVADO do ItemDef que usa a habilidade, então " +
                "habilidade sem item que a use aparece aqui de propósito.");
        }
    }
}
