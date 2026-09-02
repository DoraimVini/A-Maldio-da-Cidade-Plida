using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace FavelaAmarela.Tests.EditMode
{
    /// <summary>
    /// Guarda que a interface continua <b>vestida</b> com o pacote Dark Ages UI.
    ///
    /// <para><b>O estado que isto travou (2026-09-01).</b> O pacote estava no projeto desde
    /// sempre com 25 artes, das quais <b>3 fatiadas</b> e <b>1 referenciada</b>. O
    /// <c>HUD_Gameplay.prefab</c> tinha <b>37 Images no sprite padrão da Unity</b> — inventário,
    /// barra de itens, pause e Colapso desenhados com o retângulo branco genérico — e os 6
    /// botões do menu principal não tinham sprite nenhum.</para>
    ///
    /// <para><b>O teste que mais importa aqui é o terceiro</b>, e ele guarda o oposto dos
    /// outros: os 27 <c>Icone</c> dentro dos slots <b>têm de continuar vazios</b>. Quem os
    /// preenche é o runtime, com o ícone do item que estiver naquele slot. Uma varredura futura
    /// de "achar todas as Image sem sprite e consertar" desenharia um item fantasma em cada
    /// slot vazio do inventário — e passaria por melhoria.</para>
    /// </summary>
    public sealed class InterfaceVestidaTests
    {
        private const string Folha =
            "Assets/ThirdParty/DarkAgesUI/DarkAgesUi_v1.0/32x32-Tilesheet.png.meta";

        private const string Hud = "Assets/FavelaAmarela/Resources/HUD_Gameplay.prefab";
        private const string Menu = "Assets/Scenes/Cena_Menu.unity";

        /// <summary>Guid do script <c>UnityEngine.UI.Image</c>.</summary>
        private const string GuidDaImage = "fe87c0e1cc204ed48ad3b37840f39efc";

        /// <summary>Guid da textura do pacote.</summary>
        private const string GuidDaFolha = "4c5c05e2de460ba4f9ac5c58d70daa9a";

        /// <summary>
        /// Os recortes que a interface consome. Sair da folha é quebrar tudo que os usa, sem
        /// erro de compilação — a referência simplesmente vira nula e o painel some.
        /// </summary>
        private static readonly string[] Fatiados =
        {
            "painel_ornado", "painel_pergaminho", "moldura_slot", "moldura_slot_cheia",
            "slot_vazio", "slot_cheio", "botao", "botao_realce",
        };

        private static string Ler(string caminho)
        {
            Assert.IsTrue(File.Exists(caminho), $"Arquivo ausente: {caminho}");
            return File.ReadAllText(caminho);
        }

        [Test]
        public void AFolhaMantemOsRecortesQueAInterfaceUsa()
        {
            string meta = Ler(Folha);

            var faltando = Fatiados
                .Where(n => !Regex.IsMatch(meta, $@"^      name: {Regex.Escape(n)}\s*$",
                                           RegexOptions.Multiline))
                .ToList();

            Assert.IsEmpty(faltando,
                "Recorte(s) que sumiram da folha: " + string.Join(", ", faltando) +
                ". Refatiar a textura no Sprite Editor apaga recortes silenciosamente, e toda " +
                "Image que os usava fica sem sprite — sem erro nenhum.");
        }

        [Test]
        public void ONucleoDaInterfaceNaoUsaMaisOSpritePadraoDaUnity()
        {
            string hud = Ler(Hud);

            // Um m_Sprite built-in tem guid começando em zeros (recurso interno da engine).
            var builtIn = Regex.Matches(hud, @"m_Sprite: \{fileID: -?\d+, guid: (0{4}[0-9a-f]{28})")
                               .Count;

            Assert.Zero(builtIn,
                $"{builtIn} Image(s) do HUD voltaram ao sprite padrão da Unity — aquele " +
                "retângulo branco arredondado. Era o estado do inventário, da barra de itens, " +
                "do pause e da tela de Colapso inteiros até 2026-09-01.");
        }

        [Test]
        public void OsBotoesDoMenuTemSprite()
        {
            string menu = Ler(Menu);

            int doPacote = Regex.Matches(menu, $@"m_Sprite: \{{fileID: -?\d+, guid: {GuidDaFolha}")
                                .Count;

            // 2 painéis + 6 botões.
            Assert.GreaterOrEqual(doPacote, 8,
                $"O menu principal usa só {doPacote} sprite(s) do pacote. Os 6 botões — Nova " +
                "Partida, Continuar, Opções, Sair, Confirmar, Cancelar — já foram texto sobre " +
                "o nada; é ali que o jogador chega primeiro.");
        }

        [Test]
        public void OsIconesDosSlotsContinuamVazios()
        {
            string hud = Ler(Hud);

            var docs = Regex.Split(hud, @"^--- !u!\d+ &(\d+)\n", RegexOptions.Multiline);
            var pares = new List<(string Ancora, string Corpo)>();
            for (int i = 1; i + 1 < docs.Length; i += 2) pares.Add((docs[i], docs[i + 1]));

            var nomes = pares.ToDictionary(
                p => p.Ancora,
                p => Regex.Match(p.Corpo, @"^  m_Name: (.*)$", RegexOptions.Multiline) is var m
                     && m.Success ? m.Groups[1].Value.Trim() : "");

            var comArte = new List<string>();
            int total = 0;

            foreach (var (_, corpo) in pares)
            {
                if (!corpo.Contains($"m_Script: {{fileID: 11500000, guid: {GuidDaImage}")) continue;

                var go = Regex.Match(corpo, @"m_GameObject: \{fileID: (\d+)\}");
                if (!go.Success) continue;
                if (!nomes.TryGetValue(go.Groups[1].Value, out var nome) || nome != "Icone") continue;

                total++;
                if (Regex.IsMatch(corpo, @"m_Sprite: \{fileID: -?\d+, guid: [0-9a-f]{32}"))
                    comArte.Add(go.Groups[1].Value);
            }

            Assert.Greater(total, 0, "Nenhum 'Icone' encontrado no HUD — a estrutura dos slots " +
                                     "mudou e este teste deixou de guardar o que dizia guardar.");

            Assert.IsEmpty(comArte,
                $"{comArte.Count} de {total} 'Icone' ganharam sprite fixo. Eles são preenchidos " +
                "em RUNTIME com o ícone do item que estiver no slot: arte fixa aqui desenha um " +
                "item fantasma em todo slot VAZIO do inventário e da barra. " +
                "Uma varredura de 'consertar toda Image sem sprite' cai exatamente aqui.");
        }
        // -- Os estados: casa ocupada, e botao sob o cursor --------------------

        /// <summary>Corpo serializado do componente de nome dado, dentro do HUD.</summary>
        private static string Componente(string classe)
        {
            string hud = Ler(Hud);

            var m = Regex.Match(hud,
                $@"m_EditorClassIdentifier:.*{Regex.Escape(classe)}\s*\n((?:.*\n)*?)(?=^--- )",
                RegexOptions.Multiline);

            Assert.IsTrue(m.Success, $"'{classe}' não está no HUD_Gameplay.prefab.");
            return m.Groups[1].Value;
        }

        /// <summary>
        /// O defeito que manteve este mecanismo mudo: o <c>PainelDeInventario</c> já trocava a
        /// moldura conforme o slot tinha item, as 19 <c>Image</c> já estavam ligadas, e os
        /// <b>campos de Sprite estavam vazios</b>. A peça inteira existia, compilava, e não
        /// acontecia — o modo de falha assinado deste repositório.
        /// </summary>
        [Test]
        public void AsMolduraDeSlotTemArteNosQuatroCampos()
        {
            string corpo = Componente("PainelDeInventario");

            var vazios = new[] { "molduraVazia", "molduraCheia",
                                 "molduraCorpoVazia", "molduraCorpoCheia" }
                .Where(campo => !Regex.IsMatch(corpo,
                    $@"^\s*{campo}: \{{fileID: -?[1-9]", RegexOptions.Multiline))
                .ToList();

            Assert.IsEmpty(vazios,
                "Campo(s) de moldura sem arte no PainelDeInventario: " +
                string.Join(", ", vazios) + ". Com eles vazios o Pintar não troca nada e a " +
                "única pista de 'tem item aqui' volta a ser o desbotamento do slot.");
        }

        [Test]
        public void ABarraDeItensTrocaAMolduraDasOitoCasas()
        {
            string corpo = Componente("BarraDeItens");

            foreach (var campo in new[] { "molduraVazia", "molduraCheia" })
                Assert.IsTrue(Regex.IsMatch(corpo, $@"^\s*{campo}: \{{fileID: -?[1-9]",
                                            RegexOptions.Multiline),
                    $"BarraDeItens.{campo} está sem arte.");

            var ligadas = Regex.Matches(corpo, @"^\s*moldura: \{fileID: ([1-9]\d*)\}",
                                        RegexOptions.Multiline).Count;

            Assert.AreEqual(8, ligadas,
                $"Só {ligadas} das 8 casas da barra de itens têm moldura ligada. A barra fica " +
                "sempre na tela: 'qual das oito tem consumível' é pergunta feita no meio da " +
                "luta, sem tempo de abrir nada.");
        }

        [Test]
        public void OsBotoesTrocamDeArteSobOCursor()
        {
            // Selectable.Transition: 0 None, 1 ColorTint, 2 SpriteSwap, 3 Animation.
            foreach (var arquivo in new[] { Hud, Menu })
            {
                string txt = Ler(arquivo);
                var docs = Regex.Split(txt, @"^--- !u!114 &\d+\n", RegexOptions.Multiline);

                int botoes = 0, semTroca = 0;

                foreach (var d in docs)
                {
                    if (!d.Contains("guid: 4e29b1a8efbd4b44bb3f3716e73f07ff")) continue;
                    botoes++;

                    bool spriteSwap = Regex.IsMatch(d, @"^\s*m_Transition: 2\s*$",
                                                    RegexOptions.Multiline);
                    bool temRealce = Regex.IsMatch(d, @"^\s*m_HighlightedSprite: \{fileID: -?[1-9]",
                                                   RegexOptions.Multiline);

                    if (!spriteSwap || !temRealce) semTroca++;
                }

                Assert.Greater(botoes, 0, $"Nenhum Button em {arquivo}.");

                Assert.Zero(semTroca,
                    $"{semTroca} de {botoes} botão(ões) em {System.IO.Path.GetFileName(arquivo)} " +
                    "sem troca de arte sob o cursor. Em ColorTint, o tint escurece a moldura " +
                    "dourada junto e o botão lê como DESABILITADO em vez de realçado.");
            }
        }
    }
}
