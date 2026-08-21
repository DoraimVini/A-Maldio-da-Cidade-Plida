using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace FavelaAmarela.Tests.EditMode
{
    /// <summary>
    /// Guarda as pegadas de colisão do elenco — <b>em unidades de mundo</b>, que é a única
    /// medida que significa alguma coisa quando cada prefab tem uma escala diferente.
    ///
    /// <para><b>Por que existe (2026-08-20):</b> as pegadas nunca tinham sido calibradas entre
    /// si. Medidas antes desta rodada: Damião <b>1,467</b>, Abdul 1,200, Rei 1,000, Yug-Neth
    /// 0,600, Cultista 0,576, Espectro 0,416. O Damião tinha <b>2,5× a pegada do Cultista</b> —
    /// dois humanos do mesmo rig — e um colisor mais largo que a própria figura desenhada.</para>
    ///
    /// <para><b>E o Byakhee não tinha colisor nenhum</b>, só um <c>Rigidbody2D</c>. Como o golpe
    /// é resolvido por <c>Physics2D.OverlapCircle</c>, o chefe era <b>impossível de acertar</b> —
    /// a causa do "o Damião não causou dano na Byakhee" relatado no playtest. Um teste que só
    /// medisse tamanhos teria passado por cima disso, então aqui a <b>existência</b> vem antes
    /// do tamanho.</para>
    ///
    /// <para>Este arquivo <b>centraliza</b> checagens que antes viviam espalhadas em
    /// <c>AnimacaoDoDamiaoTests</c> e <c>AnimacaoDoCultistaTests</c>, cada uma com sua própria
    /// constante. Duas fontes da verdade para a mesma regra é como uma delas envelhece calada —
    /// o modo de falha mais repetido deste projeto.</para>
    /// </summary>
    public sealed class ColisoresDoElencoTests
    {
        private const string Pasta = "Assets/FavelaAmarela/Art";

        /// <summary>Pegada humana esperada em mundo. Ver <c>RevisarColisores</c> para a escolha.</summary>
        private static readonly (float x, float y) PegadaHumana = (0.60f, 0.30f);

        private static readonly string[] Humanos =
        {
            Pasta + "/Characters/Damiao/Player_Damiao.prefab",
            Pasta + "/Enemies/Cultista.prefab",
            Pasta + "/Enemies/Abdul_Alhazred.prefab",
            Pasta + "/Enemies/EspectroHali.prefab",
        };

        private const string PrefabByakhee = Pasta + "/Enemies/Byakhee.prefab";

        [Test]
        public void TodoHumano_TemAMesmaPegadaEmMundo()
        {
            var falhas = new List<string>();

            foreach (var caminho in Humanos)
            {
                if (!File.Exists(caminho)) { falhas.Add($"{Nome(caminho)}: prefab ausente"); continue; }

                string yaml = File.ReadAllText(caminho);
                float escala = EscalaDaRaiz(yaml);

                var box = ColisorDeMovimento(yaml);
                if (box == null) { falhas.Add($"{Nome(caminho)}: sem BoxCollider2D"); continue; }

                var tam = Regex.Match(box, @"m_Size:\s*\{x:\s*([\d.eE+-]+),\s*y:\s*([\d.eE+-]+)");
                if (!tam.Success) { falhas.Add($"{Nome(caminho)}: sem m_Size"); continue; }

                float mx = Num(tam.Groups[1].Value) * escala;
                float my = Num(tam.Groups[2].Value) * escala;

                if (System.Math.Abs(mx - PegadaHumana.x) > 0.02f ||
                    System.Math.Abs(my - PegadaHumana.y) > 0.02f)
                {
                    falhas.Add($"{Nome(caminho)}: {mx:0.000}×{my:0.000} " +
                               $"(esperado {PegadaHumana.x:0.00}×{PegadaHumana.y:0.00})");
                }
            }

            Assert.IsEmpty(falhas,
                "Pegadas fora do padrão do elenco:\n  " + string.Join("\n  ", falhas) +
                "\n\nConserto: 'Tools/FavelaAmarela/Colisores: revisar as pegadas'.");
        }

        /// <summary>
        /// O teste mais importante deste arquivo: um chefe sem colisor é um chefe invencível,
        /// e isso passou despercebido por semanas.
        /// </summary>
        [Test]
        public void OByakhee_TemColisorEPodeSerAcertado()
        {
            Assert.IsTrue(File.Exists(PrefabByakhee), "Byakhee.prefab ausente.");

            string yaml = File.ReadAllText(PrefabByakhee);

            bool temColisor = Regex.IsMatch(yaml, @"!u!(61|58|70|60|68)\b");

            Assert.IsTrue(temColisor,
                "O Byakhee está sem colisor. MaoFisicaBridge.ResolverGolpe resolve o golpe por " +
                "Physics2D.OverlapCircle — sem colisor o chefe não pode ser acertado, e a luta " +
                "fica impossível de vencer. Conserto: " +
                "'Tools/FavelaAmarela/Colisores: revisar as pegadas'.");
        }

        /// <summary>
        /// O colisor do Byakhee tem que ser <b>trigger</b>: o filtro do golpe usa
        /// <c>useTriggers = true</c>, então trigger basta para acertá-lo — e um chefe voador
        /// sólido empurraria o jogador e enroscaria nas paredes da arena.
        /// </summary>
        [Test]
        public void OColisorDoByakhee_ETrigger()
        {
            string yaml = File.ReadAllText(PrefabByakhee);

            var colisor = Documentos(yaml)
                .FirstOrDefault(d => Regex.IsMatch(d, @"!u!(61|58|70|60|68)\b"));

            Assert.IsNotNull(colisor, "Byakhee sem colisor — ver OByakhee_TemColisorEPodeSerAcertado.");

            Assert.IsTrue(Regex.IsMatch(colisor, @"m_IsTrigger:\s*1"),
                "O colisor do Byakhee não é trigger: ele vai empurrar o jogador pela arena e " +
                "esbarrar nas paredes em vez de voar por cima delas.");
        }

        /// <summary>
        /// O <c>BoxCollider2D</c> preso ao <b>GameObject raiz</b> — o colisor de <b>movimento</b>.
        ///
        /// <para><b>Por que não basta pegar o primeiro <c>!u!61</c> (2026-08-21):</b> desde que
        /// os inimigos ganharam <c>Hurtbox</c>, cada prefab tem <b>dois</b> BoxCollider2D — a
        /// pegada no chão, na raiz, e a área que recebe dano, num filho. Pegar o primeiro que
        /// aparece no YAML devolve um ou outro conforme a ordem de serialização, e o guarda
        /// passa a medir a coisa errada em silêncio.</para>
        /// </summary>
        private static string ColisorDeMovimento(string yaml)
        {
            var docs = Regex.Split(yaml, @"(?m)^--- ").Where(d => d.Contains("!u!")).ToList();

            var transformRaiz = docs.FirstOrDefault(d =>
                Regex.IsMatch(d, @"!u!4\b") && Regex.IsMatch(d, @"m_Father:\s*\{fileID:\s*0\}"));

            if (transformRaiz == null) return null;

            var go = Regex.Match(transformRaiz, @"m_GameObject:\s*\{fileID:\s*(-?\d+)\}");
            if (!go.Success) return null;

            return docs.FirstOrDefault(d =>
                Regex.IsMatch(d, @"!u!61\b") &&
                Regex.IsMatch(d, @"m_GameObject:\s*\{fileID:\s*" + go.Groups[1].Value + @"\}"));
        }

        private static IEnumerable<string> Documentos(string yaml)
            => Regex.Split(yaml, @"(?m)^--- ").Where(d => d.Contains("!u!"));

        private static float EscalaDaRaiz(string yaml)
        {
            var raiz = Documentos(yaml).FirstOrDefault(d =>
                Regex.IsMatch(d, @"!u!4\b") && Regex.IsMatch(d, @"m_Father:\s*\{fileID:\s*0\}"));

            if (raiz == null) return 1f;

            var m = Regex.Match(raiz, @"m_LocalScale:\s*\{x:\s*([\d.eE+-]+)");
            return m.Success ? Num(m.Groups[1].Value) : 1f;
        }

        private static float Num(string s)
            => float.Parse(s, CultureInfo.InvariantCulture);

        private static string Nome(string caminho) => Path.GetFileName(caminho);
    }
}
