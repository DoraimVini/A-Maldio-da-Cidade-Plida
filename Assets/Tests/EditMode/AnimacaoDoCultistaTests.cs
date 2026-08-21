using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace FavelaAmarela.Tests.EditMode
{
    /// <summary>
    /// Guarda a animação do Cultista — <c>AnimadorDoCultista</c>, no molde do
    /// <c>AnimadorDoByakhee</c> (lê a FSM do Core, sem <c>AnimatorController</c>).
    ///
    /// <para><b>Reescrito em 2026-08-20</b>, quando a arte foi trocada. A folha antiga
    /// (<c>Sprites/Cultistas/Cultista_Spritesheet_16x32.png</c>) estava destruída — fundo opaco
    /// e buracos na figura — e o <c>.aseprite</c> de origem tinha o mesmo dano. As quatro tiras
    /// novas saem do mesmo pacote do Damião, recoloridas. Os três testes originais continuam
    /// aqui; o de folha única virou o das quatro tiras.</para>
    /// </summary>
    public sealed class AnimacaoDoCultistaTests
    {
        private const string Pasta = "Assets/FavelaAmarela/Art/Enemies/Cultista";
        private const string Prefab = "Assets/FavelaAmarela/Art/Enemies/Cultista.prefab";
        private const string Componente = "Assets/Scripts/Enemies/AnimadorDoCultista.cs";

        private const float AlturaDoQuadro = 86f;
        private const float Ppu = 32f;

        /// <summary>Margem em px sob os pés, ocupada pela elipse de sombra.</summary>
        private const float MargemDaSombra = 2f;

        /// <summary>Altura da figura do Cultista em px, sem a elipse de sombra.</summary>
        private const float AlturaDaFiguraEmPx = 79f;

        /// <summary>
        /// Altura da figura do Damião em unidades: 81 px a <c>localScale 0,8381</c>. Cultista e
        /// Damião são os dois humanos, do mesmo rig, e medem o mesmo.
        /// </summary>
        private const float AlturaDaFiguraDoDamiao = 2.12f;

        private static readonly string[] Ciclos = { "idle", "walk", "attack", "death" };

        private static readonly (string tira, int quadros)[] Tiras =
        {
            ("idle", 4), ("walk", 5), ("attack", 3), ("death", 4),
        };

        [Test]
        public void Prefab_TemOAnimadorComOsQuatroCiclosPreenchidos()
        {
            Assert.IsTrue(File.Exists(Prefab), $"Prefab ausente: {Prefab}");
            Assert.IsTrue(File.Exists(Componente + ".meta"), $"Meta ausente: {Componente}.meta");

            string guid = Regex.Match(File.ReadAllText(Componente + ".meta"),
                                      @"(?m)^guid:\s*([0-9a-f]{32})").Groups[1].Value;

            string txt = File.ReadAllText(Prefab);
            var bloco = Regex.Split(txt, @"(?m)^--- ").FirstOrDefault(d => d.Contains(guid));

            Assert.IsNotNull(bloco, "Cultista.prefab está sem o AnimadorDoCultista.");

            var vazios = new System.Collections.Generic.List<string>();
            foreach (var ciclo in Ciclos)
            {
                var m = Regex.Match(bloco, $@"(?ms)^\s+{ciclo}:\s*(.*?)(?=^\s+\w+:)");
                int n = m.Success ? Regex.Matches(m.Groups[1].Value, "fileID:").Count : -1;
                if (n <= 0) vazios.Add($"{ciclo}: {(n < 0 ? "campo ausente" : "vazio")}");
            }

            Assert.IsEmpty(vazios,
                "Ciclos do AnimadorDoCultista sem quadros. Rode 'Tools/FavelaAmarela/Montar " +
                "Animação do Cultista'.\n  " + string.Join("\n  ", vazios));
        }

        /// <summary>
        /// PPU 32 e pivô na linha do chão em <b>todas</b> as fatias das quatro tiras.
        ///
        /// <para><b>O rodapé não é y = 0.</b> O gerador desenha a elipse de sombra centrada
        /// <c>MargemDaSombra</c> px acima da base do quadro, e é o centro dela que marca onde o
        /// Cultista pisa. Pivô em zero o enterra 2 px — o mesmo defeito que o Damião teve ao
        /// contrário (lá ele flutuava). Antes desta troca de arte o teste exigia y = 0, que era
        /// correto para a folha antiga, cujo último pixel eram os pés.</para>
        /// </summary>
        [Test]
        public void AsQuatroTiras_TemPpu32EPivoNaLinhaDoChao()
        {
            var falhas = new System.Collections.Generic.List<string>();

            foreach (var (tira, quadros) in Tiras)
            {
                string png = $"{Pasta}/Cultista_{tira}.png";
                string meta = png + ".meta";

                if (!File.Exists(png)) { falhas.Add($"{tira}: PNG ausente"); continue; }
                if (!File.Exists(meta)) { falhas.Add($"{tira}: sem .meta"); continue; }

                string txt = File.ReadAllText(meta);

                if (!Regex.IsMatch(txt, @"spritePixelsToUnits:\s*32\b"))
                    falhas.Add($"{tira}: PPU != 32");

                int fatias = Regex.Matches(txt, @"(?m)^\s+name:\s*cultista_").Count;
                if (fatias != quadros)
                    falhas.Add($"{tira}: {fatias} fatia(s), esperado {quadros}");

                var pivosY = Regex.Matches(
                        txt, @"(?m)^\s+pivot:\s*\{x:\s*[\d.eE+-]+,\s*y:\s*([\d.eE+-]+)\}")
                    .Cast<Match>()
                    .Select(m => float.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture))
                    .ToList();

                float esperado = MargemDaSombra / AlturaDoQuadro;

                if (pivosY.Count == 0)
                    falhas.Add($"{tira}: nenhum pivô de fatia encontrado");
                else if (pivosY.Any(y => System.Math.Abs(y - esperado) > 0.0005f))
                    falhas.Add($"{tira}: pivô fora da linha do chão (esperado {esperado:0.000000}, " +
                               $"achei {string.Join(", ", pivosY.Select(y => y.ToString("0.000000")))})");
            }

            Assert.IsEmpty(falhas,
                "Tiras do Cultista incompletas. Rode 'Tools/FavelaAmarela/Montar Animação do " +
                "Cultista'.\n  " + string.Join("\n  ", falhas));
        }

        /// <summary>
        /// <b>Cultista e Damião medem o mesmo.</b> Os dois são humanos e vieram do mesmo rig
        /// (<i>4 directional character</i>).
        ///
        /// <para><b>Este teste já afirmou o contrário</b>, e o contrário estava errado. Ele
        /// exigia <c>altura &lt; 2,20</c>, "porque um capanga do tamanho do protagonista quebra
        /// a leitura da cena" — raciocínio que eu inventei para justificar o alvo de 1,80, que
        /// por sua vez era só o que o <c>localScale 1.8</c> antigo produzia sobre a arte de
        /// 32 px. Nenhuma das duas coisas era decisão de design. Corrigido pelo Vini em
        /// 2026-08-20: <i>"o Damião e o Cultista têm que ser do mesmo tamanho"</i>.</para>
        ///
        /// <para>Compara a <b>figura</b>, não o quadro: as folhas têm margens de sombra
        /// diferentes (88 px contra 86), então igualar altura de imagem deixaria os corpos
        /// desiguais.</para>
        /// </summary>
        [Test]
        public void OCultista_TemOMesmoTamanhoQueODamiao()
        {
            string txt = File.ReadAllText(Prefab);

            var raiz = Regex.Split(txt, @"(?m)^--- ")
                            .FirstOrDefault(d => Regex.IsMatch(d, @"!u!4\b")
                                              && Regex.IsMatch(d, @"m_Father:\s*\{fileID:\s*0\}"));

            Assert.IsNotNull(raiz, "Transform raiz do Cultista não encontrado.");

            var m = Regex.Match(raiz, @"m_LocalScale:\s*\{x:\s*([\d.eE+-]+)");
            Assert.IsTrue(m.Success, "localScale não encontrado.");

            float escala = float.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
            float figura = escala * AlturaDaFiguraEmPx / Ppu;

            Assert.That(figura, Is.EqualTo(AlturaDaFiguraDoDamiao).Within(0.05f),
                $"Figura do Cultista com {figura:0.00} un, e a do Damião tem " +
                $"{AlturaDaFiguraDoDamiao:0.00}. Os dois são humanos do mesmo rig e medem " +
                "igual. Conserto: 'Tools/FavelaAmarela/Montar Animação do Cultista'.");
        }

        [Test]
        public void AFolhaAntigaDanificada_NaoEUsadaEmLugarNenhum()
        {
            const string antiga = "Assets/Sprites/Cultistas/Cultista_Spritesheet_16x32.png";

            if (!File.Exists(antiga + ".meta"))
                Assert.Pass("A folha antiga já saiu do projeto.");

            var guid = Regex.Match(File.ReadAllText(antiga + ".meta"), @"guid: ([0-9a-f]{32})");
            Assert.IsTrue(guid.Success, "Não consegui ler o GUID da folha antiga.");

            var usos = Directory.GetFiles("Assets", "*.prefab", SearchOption.AllDirectories)
                .Concat(Directory.GetFiles("Assets/Scenes", "*.unity"))
                .Where(f => File.ReadAllText(f).Contains(guid.Groups[1].Value))
                .ToList();

            Assert.IsEmpty(usos,
                "A folha antiga do Cultista (fundo opaco, buracos na figura) voltou a ser " +
                "referenciada em:\n  " + string.Join("\n  ", usos));
        }

        /// <summary>
        /// Mede código, não prosa. Guarda contra a regressão já cometida com o Byakhee: um
        /// <c>AnimatorController</c> por cima de um animador que já lê a FSM do Core duplica a
        /// máquina de estados — proibido por <c>Assets/Scripts/CLAUDE.md</c>.
        /// </summary>
        [Test]
        public void Componente_NaoUsaAnimatorController()
        {
            Assert.IsTrue(File.Exists(Componente), $"Script ausente: {Componente}");

            string codigo = string.Join("\n", File.ReadAllLines(Componente)
                .Where(l => !l.TrimStart().StartsWith("///") && !l.TrimStart().StartsWith("//")));

            Assert.IsFalse(codigo.Contains("AnimatorController") || codigo.Contains(": Animator"),
                "AnimadorDoCultista passou a referenciar Animator/AnimatorController — isso " +
                "duplicaria a EnemyStateMachine numa segunda máquina de estados.");
        }
    }
}
