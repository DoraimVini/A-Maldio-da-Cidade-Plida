using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace FavelaAmarela.Tests.EditMode
{
    /// <summary>
    /// Guarda que o <b>Escudo Mágico do Abdul existe na tela</b>.
    ///
    /// <para><b>O defeito que motivou (2026-09-03).</b> O campo <c>visualDoEscudo</c> estava em
    /// <c>{fileID: 0}</c> — <b>nulo</b>. <c>AbdulAlhazredAI.AplicarVisualDeEscudo</c> chama
    /// <c>visualDoEscudo.SetActive(ativo)</c> conforme a FSM, e não havia objeto nenhum para
    /// ligar. O único retorno era um tint na sprite — e o prefab ainda o havia trocado para
    /// creme, por cima do azul que o próprio script declara como padrão.</para>
    ///
    /// <para>Isso não é cosmético: o Escudo <b>é</b> a Fase 1. Enquanto ele está de pé o Abdul
    /// não toma dano, e a única forma de derrubá-lo é quebrar uma Pedra de Poder. Escudo
    /// invisível transforma a fase em "bater e ver se o número de dano aparece".</para>
    ///
    /// <para><b>O terceiro teste é uma cicatriz fresca.</b> Ao criar a cúpula eu copiei o
    /// <c>SpriteRenderer</c> do Abdul como molde e troquei o <c>m_Sprite</c> com o padrão
    /// <c>fileID: \d+</c>. O sprite do Abdul vem de uma folha <b>Multiple</b>, e o
    /// <c>fileID</c> dele é <b>negativo</b> — o padrão não casou, o <c>re.sub</c> não trocou
    /// nada, e como eu não conferi a contagem, a cúpula nasceu desenhando <b>o próprio
    /// Abdul</b> por cima do Abdul. Compila, abre no Inspector, e só aparece jogando.</para>
    /// </summary>
    public sealed class EscudoDoAbdulTests
    {
        private const string Prefab = "Assets/FavelaAmarela/Art/Enemies/Abdul_Alhazred.prefab";
        private const string PastaDoEscudo = "Assets/FavelaAmarela/Art/Enemies/Abdul/Escudo";
        private const string Animador = "AnimadorEmLaco";

        private const int QuadrosMinimos = 8;

        private sealed class Bloco
        {
            public string Tipo;
            public string Id;
            public string Corpo;
            public string GameObject;
            public string Classe;
        }

        private static List<Bloco> Blocos()
        {
            Assert.IsTrue(File.Exists(Prefab), $"Prefab ausente: {Prefab}");

            var saida = new List<Bloco>();

            foreach (var bruto in Regex.Split(File.ReadAllText(Prefab), @"(?m)^--- ").Skip(1))
            {
                var cab = Regex.Match(bruto, @"^!u!(\d+) &(\d+)");
                if (!cab.Success) continue;

                var go = Regex.Match(bruto, @"(?m)^  m_GameObject: \{fileID: (\d+)\}$");
                var cls = Regex.Match(bruto, @"(?m)^  m_EditorClassIdentifier: (.*)$");

                saida.Add(new Bloco
                {
                    Tipo = cab.Groups[1].Value,
                    Id = cab.Groups[2].Value,
                    Corpo = bruto,
                    GameObject = go.Success ? go.Groups[1].Value : null,
                    Classe = cls.Success ? cls.Groups[1].Value.Trim().Split('.').Last() : "",
                });
            }

            return saida;
        }

        /// <summary>O GameObject apontado por <c>visualDoEscudo</c>, ou falha dizendo por quê.</summary>
        private static string CorpoDoEscudo(List<Bloco> blocos)
        {
            var ai = blocos.FirstOrDefault(b => b.Classe == "AbdulAlhazredAI");
            Assert.IsNotNull(ai,
                $"Não achei AbdulAlhazredAI em {Prefab}. Este teste não está medindo o prefab.");

            var campo = Regex.Match(ai.Corpo, @"(?m)^  visualDoEscudo: \{fileID: (\d+)\}$");
            Assert.IsTrue(campo.Success, "O campo visualDoEscudo sumiu do prefab.");

            Assert.AreNotEqual("0", campo.Groups[1].Value,
                "visualDoEscudo está NULO. AplicarVisualDeEscudo chama SetActive nele a cada " +
                "mudança de estado da FSM e não há objeto para ligar — o Escudo Mágico, que é " +
                "toda a Fase 1, fica invisível. Não dá erro de compilação nem de console.");

            return campo.Groups[1].Value;
        }

        [Test]
        public void OEscudo_TemObjetoNaCena_ComRendererEAnimador()
        {
            var blocos = Blocos();
            string escudo = CorpoDoEscudo(blocos);

            var doEscudo = blocos.Where(b => b.GameObject == escudo).ToList();

            Assert.IsNotEmpty(doEscudo,
                $"visualDoEscudo aponta para o fileID {escudo}, e nenhum componente do prefab " +
                "pertence a esse GameObject. A referência aponta para o vazio.");

            Assert.IsTrue(doEscudo.Any(b => b.Tipo == "212"),
                "O objeto do Escudo não tem SpriteRenderer — não há o que desenhar.");

            var anim = doEscudo.FirstOrDefault(b => b.Classe == Animador);
            Assert.IsNotNull(anim,
                $"O objeto do Escudo não tem {Animador}. Uma cúpula parada num quadro só lê " +
                "como textura colada, não como campo de energia.");

            int quadros = Regex.Matches(anim.Corpo,
                @"(?m)^  - \{fileID: \d+, guid: [0-9a-f]{32}, type: 3\}$").Count;

            Assert.GreaterOrEqual(quadros, QuadrosMinimos,
                $"O {Animador} do Escudo tem {quadros} quadro(s), abaixo do piso de " +
                $"{QuadrosMinimos}.");
        }

        [Test]
        public void OEscudo_DesenhaUmQuadroDoEscudo_ENaoOutroSprite()
        {
            var blocos = Blocos();
            string escudo = CorpoDoEscudo(blocos);

            var renderer = blocos.FirstOrDefault(b => b.Tipo == "212" && b.GameObject == escudo);
            Assert.IsNotNull(renderer, "O objeto do Escudo não tem SpriteRenderer.");

            // fileID NEGATIVO é o caso normal aqui: sprite vindo de folha Multiple. Foi
            // justamente o `-` que faltou no padrão que criou este defeito.
            var sprite = Regex.Match(renderer.Corpo,
                @"(?m)^  m_Sprite: \{fileID: -?\d+, guid: ([0-9a-f]{32}), type: 3\}$");

            Assert.IsTrue(sprite.Success, "O SpriteRenderer do Escudo está sem sprite.");

            var quadros = new List<string>();
            foreach (var caminho in Directory.EnumerateFiles(PastaDoEscudo, "*.png.meta"))
            {
                var g = Regex.Match(File.ReadAllText(caminho), @"(?m)^guid: ([0-9a-f]{32})");
                if (g.Success) quadros.Add(g.Groups[1].Value);
            }

            Assert.IsNotEmpty(quadros,
                $"Nenhum quadro em {PastaDoEscudo} — a pasta sumiu ou o teste olha para o " +
                "lugar errado.");

            Assert.Contains(sprite.Groups[1].Value, quadros,
                "O SpriteRenderer do Escudo aponta para um sprite que NÃO é um quadro do " +
                "escudo. Foi assim que a cúpula nasceu desenhando o próprio Abdul por cima do " +
                "Abdul: o molde veio do SpriteRenderer dele e a troca do m_Sprite falhou calada.");
        }

        /// <summary>
        /// A cúpula tem de sortar <b>na frente</b> do Abdul, e continuar na frente quando ele
        /// anda. <c>DynamicYSort</c> escreve <c>round(-(y + offsetPes) * fator)</c> e só mexe no
        /// próprio objeto — então a diferença entre os dois <c>offsetPes</c> precisa compensar
        /// exatamente a altura do filho.
        /// </summary>
        [Test]
        public void OEscudo_SortaNaFrenteDoAbdul_EmQualquerPosicao()
        {
            var blocos = Blocos();
            string escudo = CorpoDoEscudo(blocos);

            var ai = blocos.First(b => b.Classe == "AbdulAlhazredAI");

            var ysAbdul = blocos.FirstOrDefault(
                b => b.Classe == "DynamicYSort" && b.GameObject == ai.GameObject);
            var ysEscudo = blocos.FirstOrDefault(
                b => b.Classe == "DynamicYSort" && b.GameObject == escudo);

            Assert.IsNotNull(ysAbdul, "O Abdul perdeu o DynamicYSort.");
            Assert.IsNotNull(ysEscudo,
                "A cúpula não tem DynamicYSort. Com sortingOrder fixo ela fica para trás do " +
                "Abdul assim que ele anda — o chefe passa a andar na frente do próprio escudo.");

            var trEscudo = blocos.FirstOrDefault(b => b.Tipo == "4" && b.GameObject == escudo);
            Assert.IsNotNull(trEscudo, "A cúpula não tem Transform.");

            float altura = Numero(trEscudo.Corpo, @"m_LocalPosition: \{x: [-\d.]+, y: ([-\d.]+)");
            float offAbdul = Numero(ysAbdul.Corpo, @"offsetPes: ([-\d.]+)");
            float offEscudo = Numero(ysEscudo.Corpo, @"offsetPes: ([-\d.]+)");
            float fator = Numero(ysAbdul.Corpo, @"fator: ([-\d.]+)");

            // ordem = round(-(y + off) * fator). Para o filho ficar NA FRENTE, a ordem dele
            // precisa ser MAIOR — ou seja, (altura + offEscudo) menor que offAbdul.
            float diferenca = -((altura + offEscudo) - offAbdul) * fator;

            Assert.Greater(diferenca, 0f,
                $"A cúpula sortaria {diferenca:0.##} unidade(s) em relação ao Abdul — ela ficaria " +
                $"ATRÁS dele. Altura do filho {altura}, offsetPes do escudo {offEscudo}, do " +
                $"Abdul {offAbdul}, fator {fator}.");
        }

        private static float Numero(string corpo, string padrao)
        {
            var m = Regex.Match(corpo, padrao);
            Assert.IsTrue(m.Success, $"Não achei '{padrao}' no bloco.");
            return float.Parse(m.Groups[1].Value,
                System.Globalization.CultureInfo.InvariantCulture);
        }
    }
}
