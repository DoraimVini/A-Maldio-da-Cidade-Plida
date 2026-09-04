using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace FavelaAmarela.Tests.EditMode
{
    /// <summary>
    /// Guarda o padrão de material físico do projeto: <b>elasticidade 0, atrito 0,4</b>
    /// (decisão do Vini, 2026-09-04).
    ///
    /// <para><b>Por que este arquivo existe.</b> Medido em 2026-09-04, o padrão já vale — os
    /// 141 colisores reportam <c>friction 0,4</c> e <c>bounciness 0</c>. Mas ele vale por
    /// <b>acidente</b>: não há um único <c>PhysicsMaterial2D</c> no projeto, e nem o padrão
    /// global do Physics2D está preenchido, então todo colisor cai no built-in da Unity.
    /// Padrão que se cumpre sozinho é padrão que ninguém percebe quando deixa de valer.</para>
    ///
    /// <para><b>O que exatamente é guardado.</b> A doc da 6000.4 (<c>Collider2D.friction</c>)
    /// diz que o material chega por <b>quatro</b> caminhos: o <c>sharedMaterial</c> do colisor,
    /// <i>indiretamente</i> o do <c>Rigidbody2D</c>, o padrão global, ou o built-in. Os três
    /// primeiros são autorados e ficam no disco — é neles que este teste mexe. O quarto é da
    /// engine e não há o que guardar.</para>
    ///
    /// <para><b>Isto não proíbe material.</b> Proíbe material <b>silencioso</b>: no dia em que
    /// alguma superfície precisar quicar ou escorregar de propósito, este teste falha, e o
    /// conserto é acrescentar o caso à lista com o motivo — que é como a decisão fica
    /// registrada em vez de virar um valor solto num Inspector.</para>
    /// </summary>
    public sealed class MateriaisDeFisicaTests
    {
        private const string Physics2D = "ProjectSettings/Physics2DSettings.asset";

        /// <summary>
        /// Materiais autorados de propósito, com o motivo. <b>Vazia hoje.</b> Toda entrada
        /// precisa de justificativa: a alternativa é alguém calar uma falha real pondo um nome
        /// aqui.
        /// </summary>
        private static readonly Dictionary<string, string> MateriaisIntencionais =
            new Dictionary<string, string>();

        private static IEnumerable<string> CenasEPrefabs()
            => Directory.EnumerateFiles("Assets", "*.prefab", SearchOption.AllDirectories)
                .Concat(Directory.EnumerateFiles("Assets", "*.unity", SearchOption.AllDirectories));

        [Test]
        public void NenhumColisorOuCorpo_CarregaMaterialFisicoNaoDeclarado()
        {
            // Colliders e Rigidbody2D serializam o material no MESMO campo. Varrer o campo
            // cobre os dois caminhos autorados de uma vez -- e o do Rigidbody2D é justamente o
            // que passa despercebido, porque o material aparece no corpo e o efeito no colisor.
            var comMaterial = new List<string>();

            foreach (var caminho in CenasEPrefabs())
            {
                string yaml = File.ReadAllText(caminho);

                foreach (Match m in Regex.Matches(yaml, @"m_Material: \{fileID: (-?\d+)(, guid: (\w+))?"))
                {
                    bool vazio = m.Groups[1].Value == "0" && !m.Groups[3].Success;
                    if (vazio) continue;

                    string nome = Path.GetFileName(caminho);
                    if (MateriaisIntencionais.ContainsKey(nome)) continue;

                    comMaterial.Add($"{nome}: {m.Value}");
                }
            }

            Assert.IsEmpty(comMaterial,
                "Apareceu material físico atribuído onde o projeto não declara nenhum:\n  " +
                string.Join("\n  ", comMaterial) +
                "\n\nO padrão é elasticidade 0 e atrito 0,4. Num isométrico visto de cima, " +
                "corpo que quica desliza para fora da célula e a pegada some do lugar onde o " +
                "jogador a viu. Se este material é intencional, acrescente-o a " +
                "MateriaisIntencionais COM O MOTIVO.");
        }

        [Test]
        public void OPadraoGlobalDoPhysics2D_ContinuaVazio()
        {
            Assert.IsTrue(File.Exists(Physics2D), $"{Physics2D} não existe.");

            string yaml = File.ReadAllText(Physics2D);
            var m = Regex.Match(yaml, @"m_DefaultMaterial: \{fileID: (-?\d+)(, guid: (\w+))?");

            Assert.IsTrue(m.Success, "m_DefaultMaterial sumiu do Physics2DSettings.");

            bool vazio = m.Groups[1].Value == "0" && !m.Groups[3].Success;

            Assert.IsTrue(vazio,
                $"O material padrão global do Physics2D foi preenchido ({m.Value}). Ele se " +
                "aplica a TODO colisor do jogo que não tenha material próprio — hoje, os 141 — " +
                "e muda atrito e elasticidade do projeto inteiro de uma vez, sem tocar em " +
                "nenhum prefab. É a mudança de física mais silenciosa que existe neste projeto.");
        }

        /// <summary>
        /// Um <c>PhysicsMaterial2D</c> no disco ainda não faz mal: mal só existe quando alguém
        /// o atribui. Mas ele existir sem estar na lista significa que foi criado sem decisão
        /// registrada, e o próximo passo natural é atribuí-lo.
        /// </summary>
        [Test]
        public void NenhumAssetDeMaterialFisico_ApareceSemDeclaracao()
        {
            var achados = Directory
                .EnumerateFiles("Assets", "*.physicsMaterial2D", SearchOption.AllDirectories)
                .Select(Path.GetFileName)
                .Where(n => !MateriaisIntencionais.ContainsKey(n))
                .ToList();

            Assert.IsEmpty(achados,
                "Apareceu asset de PhysicsMaterial2D sem declaração:\n  " +
                string.Join("\n  ", achados) +
                "\n\nSe ele existe para uma superfície que precisa quicar ou escorregar de " +
                "propósito, acrescente-o a MateriaisIntencionais com o motivo.");
        }
    }
}
