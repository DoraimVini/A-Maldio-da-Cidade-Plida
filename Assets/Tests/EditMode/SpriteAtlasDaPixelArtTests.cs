using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;

namespace FavelaAmarela.Tests.EditMode
{
    /// <summary>
    /// Guarda os <b>Sprite Atlas</b> contra o defeito que eles próprios podem causar.
    ///
    /// <para><b>Por que este guarda existe.</b> Os números do atlas <b>sobrepõem</b> os das
    /// texturas de origem. Um atlas com o padrão da Unity — <c>filterMode</c> Bilinear e
    /// compressão ligada — borraria <b>toda</b> a pixel art de uma vez, e o sintoma apareceria
    /// só na build: cada PNG continuaria certo no Inspector, porque o que muda no jogo é o
    /// atlas, não o PNG.</para>
    ///
    /// <para><b>E isso aconteceu ao criá-los (2026-08-29).</b> A primeira versão da ferramenta
    /// chamava <c>SetPackingSettings</c>/<c>SetTextureSettings</c> no <c>SpriteAtlasAsset</c> em
    /// memória, antes do <c>Save</c>. Na V2 aquilo <b>não persiste</b>: o
    /// <c>.spriteatlasv2</c> guarda só os <i>packables</i>, e empacotamento e textura vivem no
    /// <c>SpriteAtlasImporter</c>, isto é, no <c>.meta</c>. O arquivo saiu com
    /// <c>filterMode: 1</c> — <b>Bilinear</b> — e a ferramenta reportou sucesso.</para>
    ///
    /// <para>Os três requisitos vêm da skill <c>favela-pixelart-standards</c>: Point,
    /// compressão nenhuma, e — acrescentado aqui — mipmap nenhum, porque mipmap borra pixel art
    /// conforme a câmera afasta.</para>
    /// </summary>
    public sealed class SpriteAtlasDaPixelArtTests
    {
        private static IEnumerable<(string Caminho, SpriteAtlasImporter Importer)> Atlas()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:SpriteAtlas"))
            {
                string caminho = AssetDatabase.GUIDToAssetPath(guid);

                // Terceiros trazem atlas próprios com outras regras; o contrato é sobre a arte
                // do jogo.
                if (caminho.Contains("/ThirdParty/")) continue;

                if (AssetImporter.GetAtPath(caminho) is SpriteAtlasImporter importer)
                    yield return (caminho, importer);
            }
        }

        [Test]
        public void OProjeto_TemAtlasParaAArteDoJogo()
        {
            var nomes = Atlas().Select(a => Path.GetFileNameWithoutExtension(a.Caminho)).ToList();

            Assert.IsNotEmpty(nomes,
                "Nenhum Sprite Atlas no projeto. O empacotador está ligado " +
                "(m_SpritePackerMode: 5) e não tem o que empacotar — cada textura distinta " +
                "quebra o batch. Conserto: 'Tools/FavelaAmarela/Arte: montar os Sprite Atlas'.");

            foreach (var esperado in new[] { "Atlas_Cenario", "Atlas_Elenco", "Atlas_UI" })
                Assert.Contains(esperado, nomes,
                    $"O atlas '{esperado}' sumiu. Os três cortes existem porque sprites de " +
                    "atlas diferentes nunca compartilham um batch: chão, atores, Canvas.");
        }

        /// <summary>
        /// <b>O guarda principal.</b> Bilinear ou compressão num atlas destrói a pixel art do
        /// jogo inteiro, e nenhum PNG denuncia.
        /// </summary>
        [Test]
        public void NenhumAtlas_BorraOuComprimeAPixelArt()
        {
            var quebrados = new List<string>();
            var vistos = 0;

            foreach (var (caminho, importer) in Atlas())
            {
                vistos++;
                string nome = Path.GetFileNameWithoutExtension(caminho);
                var t = importer.textureSettings;

                if (t.filterMode != FilterMode.Point)
                    quebrados.Add($"{nome}: filterMode {t.filterMode} — a pixel art sai BORRADA " +
                                  "no jogo, e cada PNG continua certo no Inspector");

                if (t.generateMipMaps)
                    quebrados.Add($"{nome}: mipmaps ligados — borra conforme a câmera afasta");

                if (t.anisoLevel != 0)
                    quebrados.Add($"{nome}: anisoLevel {t.anisoLevel} — filtragem anisotrópica " +
                                  "não faz sentido com Point e custa banda");

                var p = importer.GetPlatformSettings("DefaultTexturePlatform");

                if (p != null && p.textureCompression != TextureImporterCompression.Uncompressed)
                    quebrados.Add($"{nome}: compressão {p.textureCompression} — come as bordas " +
                                  "e a paleta, que é o que a arte inteira do jogo depende");

                if (p != null && p.crunchedCompression)
                    quebrados.Add($"{nome}: crunched compression ligada");
            }

            Assert.Greater(vistos, 0, "Nenhum atlas conferido — este guarda virou decorativo.");

            Assert.IsEmpty(quebrados,
                "Sprite Atlas fora do padrão de pixel art:" + Environment.NewLine + "  " +
                string.Join(Environment.NewLine + "  ", quebrados) + Environment.NewLine +
                "Conserto: 'Tools/FavelaAmarela/Arte: montar os Sprite Atlas'. Os ajustes vivem " +
                "no SpriteAtlasImporter (o .meta), não no .spriteatlasv2.");
        }

        /// <summary>
        /// Rotação e empacotamento justo trocam previsibilidade por alguns bytes. Num jogo de
        /// dezenas de sprites os bytes não são o problema; alinhamento de pixel é.
        /// </summary>
        [Test]
        public void NenhumAtlas_GiraOuRecortaOsSprites()
        {
            var tortos = new List<string>();

            foreach (var (caminho, importer) in Atlas())
            {
                string nome = Path.GetFileNameWithoutExtension(caminho);
                var p = importer.packingSettings;

                if (p.enableRotation)
                    tortos.Add($"{nome}: enableRotation — o sprite gira dentro da folha");

                if (p.enableTightPacking)
                    tortos.Add($"{nome}: enableTightPacking — recorta pela silhueta");

                if (p.padding < 2)
                    tortos.Add($"{nome}: padding {p.padding} — vizinho vaza na borda");
            }

            Assert.IsEmpty(tortos,
                "Sprite Atlas com empacotamento arriscado:" + Environment.NewLine + "  " +
                string.Join(Environment.NewLine + "  ", tortos));
        }

        [Test]
        public void TodoAtlas_EntraNaBuild()
        {
            var forade = Atlas()
                .Where(a => !a.Importer.includeInBuild)
                .Select(a => Path.GetFileNameWithoutExtension(a.Caminho))
                .ToList();

            Assert.IsEmpty(forade,
                "Atlas fora da build: " + string.Join(", ", forade) + Environment.NewLine +
                "Ele existe no Editor, o jogo roda sem ele, e o ganho de batch some justamente " +
                "onde importa.");
        }

        /// <summary>
        /// Arte crua não pode entrar em atlas — ela infla a textura empacotada com sprites que
        /// o jogo nunca desenha. As 14 imagens de <c>Arte/Inbox</c> e da raiz de
        /// <c>Assets</c> têm <b>zero</b> referências, e é por isso que estão fora dos grupos.
        /// </summary>
        [Test]
        public void ArteCrua_FicaForaDosAtlas()
        {
            string fonte = File.ReadAllText(
                "Assets/FavelaAmarela/Editor/MontarSpriteAtlas.cs");

            foreach (var proibida in new[] { "Assets/Arte/Inbox", "Assets/ThirdParty" })
                StringAssert.DoesNotContain($"\"{proibida}", fonte,
                    $"'{proibida}' entrou num grupo de atlas. É arte não usada ou de terceiros: " +
                    "empacotá-la infla a textura e a build com sprites que o jogo não desenha.");
        }
    }
}
