using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace FavelaAmarela.Tests.EditMode
{
    /// <summary>
    /// Guarda a arte dos cinco prefabs que usavam o sprite embutido da Unity
    /// (<c>fileID: 10905</c>, o "Knob"): Pedra de Poder, Cone de Gelo, Esqueleto Invocado,
    /// Necronomicon e Yug-Neth.
    ///
    /// <para><b>Por que não basta banir o Knob no projeto inteiro:</b> ele aparece de 7 a 16
    /// vezes em <b>cada</b> cena, porque todo <c>Image</c> de UI nasce com o sprite embutido
    /// atribuído — e o próprio Yug-Neth continua com um, na barra de vida dele. Um guarda que
    /// procurasse a string daria dezenas de falsos positivos. Este confere, para cada prefab,
    /// se ele referencia o <b>GUID do PNG esperado</b>.</para>
    ///
    /// <para><b>O que o terceiro teste protege</b> é menos óbvio: escala e colisor foram
    /// recalculados juntos, para o volume de mundo (escala × tamanho local) continuar
    /// exatamente o mesmo de antes da troca de arte. Mexer num sem o outro muda hitbox sem
    /// ninguém perceber — e hitbox errada não aparece no console nem quebra compilação.</para>
    /// </summary>
    public sealed class ArteDosPlaceholdersTests
    {
        private const string Enemies = "Assets/FavelaAmarela/Art/Enemies/";
        private const string Items = "Assets/FavelaAmarela/Art/Items/";
        private const string MiGo = "Assets/FavelaAmarela/Art/Characters/MiGo/";

        /// <summary>Alinhamento de pivô como a Unity serializa: 7 = BottomCenter, 0 = Center.</summary>
        private const int PivoBottomCenter = 7;

        private const int PivoCenter = 0;

        private sealed class Esperado
        {
            public string Prefab;
            public string Sprite;
            public int Alinhamento;
            public float Escala;
            public float ColisorX, ColisorY;
        }

        private static readonly Esperado[] Alvos =
        {
            new Esperado { Prefab = MiGo + "YugNeth.prefab", Sprite = MiGo + "yug_neth_idle.png",
                           Alinhamento = PivoBottomCenter, Escala = 0.5f, ColisorX = 0.6f, ColisorY = 0.6f },
            new Esperado { Prefab = Enemies + "EsqueletoInvocado.prefab", Sprite = Enemies + "EsqueletoInvocado.png",
                           Alinhamento = PivoBottomCenter, Escala = 0.5f, ColisorX = 0.416f, ColisorY = 0.544f },
            // A Pedra parte do PRIMEIRO QUADRO DA AURA, e não do cristal solto (2026-09-03).
            // O quadro é o mesmo cristal, sem um pixel de retoque, com o anel roxo composto
            // atrás — ver Art/Enemies/PedraDePoder/PROCEDENCIA.txt. Apontar para o cristal solto
            // faria a Pedra nascer sem anel e só ganhá-lo no primeiro tique do animador.
            // A escala e o volume de colisor abaixo NÃO mudaram: o cristal está colado no
            // centro-base da tela de 64×96 e o pivô dos dois é BottomCenter, então ele ocupa os
            // mesmos pixels, no mesmo ponto do chão.
            new Esperado { Prefab = Enemies + "PedraDePoder.prefab",
                           Sprite = Enemies + "PedraDePoder/PedraDePoder_Aura_00.png",
                           Alinhamento = PivoBottomCenter, Escala = 0.9f, ColisorX = 1.0f, ColisorY = 1.35f },
            new Esperado { Prefab = Enemies + "ConeDeGelo.prefab", Sprite = Enemies + "ConeDeGelo.png",
                           Alinhamento = PivoCenter, Escala = 0.4f, ColisorX = 0.6f, ColisorY = 0.3f },
            new Esperado { Prefab = Items + "Necronomicon.prefab", Sprite = Items + "Necronomicon.png",
                           Alinhamento = PivoBottomCenter, Escala = 0.4f, ColisorX = 0.84f, ColisorY = 1.05f },
        };

        [Test]
        public void CadaPrefab_ReferenciaOSpriteDeVerdade()
        {
            var falhas = new List<string>();

            foreach (var a in Alvos)
            {
                if (!File.Exists(a.Prefab)) { falhas.Add($"{a.Prefab}: prefab ausente"); continue; }
                if (!File.Exists(a.Sprite)) { falhas.Add($"{a.Sprite}: PNG ausente"); continue; }

                string guid = GuidDoMeta(a.Sprite);
                if (guid == null) { falhas.Add($"{a.Sprite}: sem .meta (a Unity ainda não importou)"); continue; }

                string conteudo = File.ReadAllText(a.Prefab);

                if (!Regex.IsMatch(conteudo, $@"m_Sprite:\s*\{{fileID:\s*\d+,\s*guid:\s*{guid}"))
                    falhas.Add($"{Path.GetFileName(a.Prefab)}: não aponta para " +
                               $"{Path.GetFileName(a.Sprite)} — provavelmente ainda está no Knob embutido");
            }

            Assert.IsEmpty(falhas,
                "Prefabs sem a arte real. Rode 'Tools/FavelaAmarela/Aplicar arte dos placeholders'.\n  " +
                string.Join("\n  ", falhas));
        }

        [Test]
        public void SpritesNovos_SeguemOPadraoDePixelArtDoProjeto()
        {
            var falhas = new List<string>();

            foreach (var a in Alvos)
            {
                string meta = a.Sprite + ".meta";
                if (!File.Exists(meta)) { falhas.Add($"{a.Sprite}: sem .meta"); continue; }

                string txt = File.ReadAllText(meta);
                string nome = Path.GetFileName(a.Sprite);

                // PPU 32, Point e sem compressão: skill favela-pixelart-standards. Sem isso o
                // sprite entra a PPU 100 (tamanho errado) e interpolado (borrado).
                if (!Regex.IsMatch(txt, @"spritePixelsToUnits:\s*32\b"))
                    falhas.Add($"{nome}: PPU != 32");

                if (!Regex.IsMatch(txt, @"filterMode:\s*0\b"))
                    falhas.Add($"{nome}: filterMode != Point");

                // Conferir dígito por dígito, e não com [^0]: o meta traz um
                // textureCompression por plataforma, e `\s*[^0]` casava o próprio ESPAÇO
                // depois dos dois-pontos — acusava compressão em textura descomprimida.
                foreach (Match c in Regex.Matches(txt, @"textureCompression:\s*(\d+)"))
                {
                    if (c.Groups[1].Value != "0")
                    {
                        falhas.Add($"{nome}: textura comprimida (textureCompression={c.Groups[1].Value})");
                        break;
                    }
                }

                var alin = Regex.Match(txt, @"(?m)^\s{2}alignment:\s*(\d+)");
                if (!alin.Success)
                    falhas.Add($"{nome}: sem 'alignment' no meta");
                else if (int.Parse(alin.Groups[1].Value) != a.Alinhamento)
                    falhas.Add($"{nome}: pivô {alin.Groups[1].Value}, esperado {a.Alinhamento} " +
                               (a.Alinhamento == PivoCenter
                                   ? "(Center — o projétil gira em torno do pivô)"
                                   : "(BottomCenter — DynamicYSort ordena pelos pés)"));
            }

            Assert.IsEmpty(falhas, "Import fora do padrão de pixel art:\n  " + string.Join("\n  ", falhas));
        }

        /// <summary>
        /// Escala e colisor têm que continuar produzindo o <b>mesmo volume de mundo</b> de antes
        /// da troca de arte. A escala antiga fora calibrada para o Knob (32 px a PPU 100 = 0.32
        /// unidades); a nova é para arte a PPU 32. Mudar uma sem a outra reequilibra combate em
        /// silêncio.
        /// </summary>
        [Test]
        public void Escala_E_Colisor_PreservamOVolumeDeMundo()
        {
            var falhas = new List<string>();

            foreach (var a in Alvos)
            {
                if (!File.Exists(a.Prefab)) { falhas.Add($"{a.Prefab}: ausente"); continue; }

                string txt = File.ReadAllText(a.Prefab);
                string nome = Path.GetFileName(a.Prefab);

                var docs = Regex.Split(txt, @"(?m)^--- ").Where(d => d.Contains("!u!")).ToList();

                var raiz = docs.FirstOrDefault(d =>
                    Regex.IsMatch(d, @"!u!4\b") && Regex.IsMatch(d, @"m_Father:\s*\{fileID:\s*0\}"));

                if (raiz == null) { falhas.Add($"{nome}: Transform raiz não achado"); continue; }

                var escala = Regex.Match(raiz, @"m_LocalScale:\s*\{x:\s*([\d.eE+-]+),\s*y:\s*([\d.eE+-]+)");
                if (!escala.Success) { falhas.Add($"{nome}: sem m_LocalScale na raiz"); continue; }

                float sx = float.Parse(escala.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
                float sy = float.Parse(escala.Groups[2].Value, System.Globalization.CultureInfo.InvariantCulture);

                if (System.Math.Abs(sx - a.Escala) > 0.001f || System.Math.Abs(sy - a.Escala) > 0.001f)
                    falhas.Add($"{nome}: escala ({sx}, {sy}), esperado ({a.Escala}, {a.Escala})");

                var box = ColisorDeMovimento(txt);
                if (box == null) { falhas.Add($"{nome}: sem BoxCollider2D"); continue; }

                var tam = Regex.Match(box, @"m_Size:\s*\{x:\s*([\d.eE+-]+),\s*y:\s*([\d.eE+-]+)");
                if (!tam.Success) { falhas.Add($"{nome}: sem m_Size no colisor"); continue; }

                float cx = float.Parse(tam.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
                float cy = float.Parse(tam.Groups[2].Value, System.Globalization.CultureInfo.InvariantCulture);

                float mundoX = cx * sx, mundoY = cy * sy;

                if (System.Math.Abs(mundoX - a.ColisorX) > 0.01f || System.Math.Abs(mundoY - a.ColisorY) > 0.01f)
                    falhas.Add($"{nome}: colisor no mundo ({mundoX:0.###}, {mundoY:0.###}), " +
                               $"esperado ({a.ColisorX}, {a.ColisorY}) — o volume de jogo mudou junto com a arte");
            }

            Assert.IsEmpty(falhas, "Volume de colisor divergente:\n  " + string.Join("\n  ", falhas));
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

        private static string GuidDoMeta(string asset)
        {
            string meta = asset + ".meta";
            if (!File.Exists(meta)) return null;

            var m = Regex.Match(File.ReadAllText(meta), @"(?m)^guid:\s*([0-9a-f]{32})");
            return m.Success ? m.Groups[1].Value : null;
        }
    }
}
