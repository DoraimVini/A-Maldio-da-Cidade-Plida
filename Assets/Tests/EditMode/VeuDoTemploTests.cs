using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;

namespace FavelaAmarela.Tests.EditMode
{
    /// <summary>
    /// Guarda que o <b>véu da tempestade realmente veda</b> o Templo do Povo Serpente.
    ///
    /// <para><b>O que motivou (2026-09-01).</b> O véu nasceu como uma caixa <b>14×26</b>
    /// centrada a oeste da entrada — um número escrito à mão quando o Deserto era 43×31. Depois
    /// o mapa <b>dobrou</b>, e a caixa não. Sobraram <b>29 unidades</b> de corredor livre por
    /// baixo e <b>7</b> por cima: o jogador contornava sem nunca tocar o gatilho, e nada no
    /// projeto reclamava. A ferramenta que montou o véu relatou sucesso, porque relatar o que se
    /// escreveu não é o mesmo que medir o que ficou (Corolário 4 do <c>CLAUDE.md</c>).</para>
    ///
    /// <para><b>Por que isto merece um teste, e não só uma ferramenta.</b> O defeito não estava
    /// no véu: estava na <b>relação</b> entre o véu e o tamanho do mapa. Qualquer mudança futura
    /// na geometria do Deserto — e já houve uma — quebra essa relação em silêncio. É a família
    /// do <c>ConsumiveisNoMundoTests</c>: a peça existe, está bem autorada, e o mundo não a
    /// entrega.</para>
    ///
    /// <para><b>Um portão contornável é pior que nenhum portão:</b> promete uma regra ao jogador
    /// e não a cumpre, e o único a descobrir é quem estiver jogando.</para>
    ///
    /// <para>Lê o YAML da cena em vez de chamar <c>OpenScene</c>, para não mexer no estado do
    /// Editor de quem está rodando a suíte — mesma técnica do <c>ConsumiveisNoMundoTests</c>.</para>
    /// </summary>
    public sealed class VeuDoTemploTests
    {
        private const string CenaDoDeserto = "Assets/Scenes/Deserto_Hali.unity";

        private const string NomeDoVeu = "Veu_DaTempestade_Templo";
        private const string NomeDaEntrada = "Entrada_TemploSerpente";
        private const string NomeDaCarta = "Coletavel_CartaDasAreias";

        // ── Mini-leitor do YAML de cena ───────────────────────────────────────

        /// <summary>Um documento do YAML da cena: a âncora, o tipo e o corpo em texto.</summary>
        private readonly struct Doc
        {
            public readonly string Ancora;
            public readonly string Tipo;
            public readonly string Corpo;

            public Doc(string ancora, string tipo, string corpo)
            {
                Ancora = ancora;
                Tipo = tipo;
                Corpo = corpo;
            }
        }

        private static List<Doc> LerCena()
        {
            Assert.IsTrue(File.Exists(CenaDoDeserto), $"Cena não encontrada: {CenaDoDeserto}");

            var docs = new List<Doc>();
            var cabecalho = new Regex(@"^--- !u!\d+ &(\d+)");

            string ancora = null, tipo = null;
            var corpo = new List<string>();

            void Fechar()
            {
                if (ancora != null) docs.Add(new Doc(ancora, tipo, string.Join("\n", corpo)));
            }

            foreach (string linha in File.ReadAllLines(CenaDoDeserto))
            {
                var m = cabecalho.Match(linha);
                if (m.Success)
                {
                    Fechar();
                    ancora = m.Groups[1].Value;
                    tipo = null;
                    corpo.Clear();
                    continue;
                }

                if (ancora == null) continue;

                // A primeira linha não-indentada depois do cabeçalho é o tipo do documento.
                if (tipo == null && linha.Length > 0 && linha[0] != ' ' && linha.EndsWith(":"))
                    tipo = linha.TrimEnd(':');

                corpo.Add(linha);
            }

            Fechar();
            return docs;
        }

        private static float Numero(string texto) =>
            float.Parse(texto, NumberStyles.Float, CultureInfo.InvariantCulture);

        private static Vector2 Vetor(string corpo, string campo)
        {
            var m = new Regex(campo + @": \{x: (-?[\d.eE+-]+), y: (-?[\d.eE+-]+)").Match(corpo);
            Assert.IsTrue(m.Success, $"Campo '{campo}' ausente no documento.");
            return new Vector2(Numero(m.Groups[1].Value), Numero(m.Groups[2].Value));
        }

        private static string Referencia(string corpo, string campo)
        {
            var m = new Regex(campo + @": \{fileID: (\d+)\}").Match(corpo);
            return m.Success ? m.Groups[1].Value : null;
        }

        private static string AncoraDoObjeto(List<Doc> docs, string nome) => docs
            .Where(d => d.Tipo == "GameObject" &&
                        Regex.IsMatch(d.Corpo, $@"^  m_Name: {Regex.Escape(nome)}\s*$",
                                      RegexOptions.Multiline))
            .Select(d => d.Ancora)
            .FirstOrDefault();

        /// <summary>
        /// Posição de MUNDO, somando a cadeia de pais. Afirma que cada ancestral tem escala
        /// unitária — se um dia deixar de ter, este teste falha em vez de medir errado calado.
        /// </summary>
        private static Vector2 PosicaoDeMundo(List<Doc> docs, string nomeDoObjeto)
        {
            string objeto = AncoraDoObjeto(docs, nomeDoObjeto);
            Assert.IsNotNull(objeto, $"GameObject '{nomeDoObjeto}' não existe na cena do Deserto.");

            var atual = docs.FirstOrDefault(d => d.Tipo == "Transform" &&
                                                 Referencia(d.Corpo, "m_GameObject") == objeto);
            Assert.IsNotNull(atual.Corpo, $"'{nomeDoObjeto}' sem Transform.");

            var acumulado = Vector2.zero;
            int guarda = 0;

            while (atual.Corpo != null && guarda++ < 32)
            {
                var escala = Vetor(atual.Corpo, "m_LocalScale");

                // Só o próprio objeto pode ter escala (as paredes são esticadas); um ANCESTRAL
                // escalado tornaria a soma de posições locais uma medição errada.
                if (guarda > 1)
                {
                    Assert.AreEqual(1f, escala.x, 0.001f,
                        $"Ancestral de '{nomeDoObjeto}' escalado em X — a soma mediria errado.");
                    Assert.AreEqual(1f, escala.y, 0.001f,
                        $"Ancestral de '{nomeDoObjeto}' escalado em Y — a soma mediria errado.");
                }

                acumulado += Vetor(atual.Corpo, "m_LocalPosition");

                string pai = Referencia(atual.Corpo, "m_Father");
                if (string.IsNullOrEmpty(pai) || pai == "0") break;

                atual = docs.FirstOrDefault(d => d.Tipo == "Transform" && d.Ancora == pai);
            }

            return acumulado;
        }

        /// <summary>Faixa que o véu ocupa no mundo.</summary>
        private static Rect FaixaDoVeu(List<Doc> docs)
        {
            string objeto = AncoraDoObjeto(docs, NomeDoVeu);
            Assert.IsNotNull(objeto, $"'{NomeDoVeu}' não existe na cena — o Templo está aberto.");

            var caixa = docs.FirstOrDefault(d => d.Tipo == "BoxCollider2D" &&
                                                 Referencia(d.Corpo, "m_GameObject") == objeto);
            Assert.IsNotNull(caixa.Corpo, "O véu não tem BoxCollider2D — não há gatilho nenhum.");

            StringAssert.Contains("m_IsTrigger: 1", caixa.Corpo,
                "O collider do véu não é gatilho: viraria parede sólida em vez de véu.");

            var centro = PosicaoDeMundo(docs, NomeDoVeu) + Vetor(caixa.Corpo, "m_Offset");
            var tamanho = Vetor(caixa.Corpo, "m_Size");

            return new Rect(centro - tamanho / 2f, tamanho);
        }

        private static Vector2 LimitesDoMapa(List<Doc> docs)
        {
            var nomes = docs
                .Where(d => d.Tipo == "GameObject")
                .Select(d => Regex.Match(d.Corpo, @"^  m_Name: (Limite_\w+)\s*$",
                                         RegexOptions.Multiline))
                .Where(m => m.Success)
                .Select(m => m.Groups[1].Value)
                .Distinct()
                .ToArray();

            Assert.IsNotEmpty(nomes, "Nenhum 'Limite_*' na cena — não dá para medir o mapa.");

            var pos = nomes.Select(n => PosicaoDeMundo(docs, n)).ToArray();
            return new Vector2(pos.Max(p => Mathf.Abs(p.x)), pos.Max(p => Mathf.Abs(p.y)));
        }

        // ── As guardas ────────────────────────────────────────────────────────

        [Test]
        public void OVeuCobreAEntradaDoTemplo()
        {
            var docs = LerCena();
            var faixa = FaixaDoVeu(docs);
            var entrada = PosicaoDeMundo(docs, NomeDaEntrada);

            Assert.GreaterOrEqual(entrada.x, faixa.xMin,
                $"A entrada do Templo (x {entrada.x}) está a OESTE do véu (começa em " +
                $"x {faixa.xMin}) — chega-se nela sem atravessar véu nenhum.");

            Assert.LessOrEqual(entrada.x, faixa.xMax,
                $"A entrada do Templo (x {entrada.x}) está a LESTE do véu (termina em " +
                $"x {faixa.xMax}).");
        }

        [Test]
        public void NaoSobraPassagemPorCimaNemPorBaixoDoVeu()
        {
            var docs = LerCena();
            var faixa = FaixaDoVeu(docs);
            var mapa = LimitesDoMapa(docs);

            Assert.LessOrEqual(faixa.yMin, -mapa.y,
                $"Sobram {(-mapa.y) - faixa.yMin:0.0} unidades de corredor livre POR BAIXO do " +
                "véu: o jogador contorna e chega ao Templo sem a Carta das Areias. Foi " +
                "exatamente isto que o véu 14×26 fazia depois de o mapa dobrar.");

            Assert.GreaterOrEqual(faixa.yMax, mapa.y,
                $"Sobram {faixa.yMax - mapa.y:0.0} unidades de corredor livre POR CIMA do véu.");

            Assert.GreaterOrEqual(faixa.xMax, mapa.x,
                $"O véu termina em x {faixa.xMax} e o mapa vai até x {mapa.x}: sobra faixa " +
                "jogável a LESTE dele, alcançável pela borda.");
        }

        [Test]
        public void ACartaDasAreiasNaoFicaTrancadaAtrasDoVeu()
        {
            var docs = LerCena();
            var faixa = FaixaDoVeu(docs);
            var carta = PosicaoDeMundo(docs, NomeDaCarta);

            Assert.Less(carta.x, faixa.xMin,
                $"A Carta das Areias está em x {carta.x}, dentro ou além do véu (começa em " +
                $"x {faixa.xMin}): a chave ficou trancada do lado de dentro da fechadura, e o " +
                "Templo vira conteúdo inalcançável.");
        }
    }
}
