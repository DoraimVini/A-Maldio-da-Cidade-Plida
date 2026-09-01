using System.IO;
using UnityEditor;
using UnityEngine;
using FavelaAmarela.Inventario;

namespace FavelaAmarela.EditorTools
{
    /// <summary>
    /// Gera o ícone <b>placeholder</b> da Carta das Areias e o liga ao item.
    ///
    /// <para><b>Por que gerado, e por que placeholder.</b> A Carta das Areias nasceu em
    /// 2026-09-01 e não há arte para ela — os 15 ícones do projeto cobrem armas, armaduras,
    /// consumíveis e relíquias, nenhum é um mapa. E <c>IconesDosItensTests</c> está certo em
    /// exigir: item sem ícone <b>aparece em branco na mochila e na barra, sem erro nenhum no
    /// console</b>.</para>
    ///
    /// <para>Reaproveitar o ícone do Necronomicon seria pior que um placeholder: o jogador leria
    /// "tomo" onde está "carta", e o erro seria <i>invisível</i> — que é justamente o tipo de
    /// defeito que este repositório mais produz.</para>
    ///
    /// <para><b>Segue a skill de pixel art:</b> PPU 32, filtro Point, compressão nenhuma. Um
    /// ícone gerado com filtro Bilinear sairia borrado ao lado dos outros quinze e denunciaria a
    /// provisoriedade pelo motivo errado.</para>
    /// </summary>
    public static class GerarIconeDaCarta
    {
        private const string Marcador = "[IconeDaCarta]";
        private const string Destino = "Assets/FavelaAmarela/Art/Items/Icones/Icone_CartaDasAreias.png";
        private const string Item = "Assets/FavelaAmarela/Config/Resources/Itens/Item_Chave_CartaDasAreias.asset";

        private const int Lado = 32;

        [MenuItem("Tools/FavelaAmarela/Arte: gerar o ícone da Carta das Areias")]
        public static void Executar()
        {
            if (!File.Exists(Destino)) Desenhar();

            AssetDatabase.ImportAsset(Destino, ImportAssetOptions.ForceUpdate);
            AjustarImport();

            string ligado = Ligar();

            Debug.Log($"{Marcador} Concluído: {Destino}" + System.Environment.NewLine + "  " +
                      ligado);
        }

        /// <summary>
        /// Um pergaminho: fundo de areia clara, dobra vertical ao meio, três traços de rota e o
        /// Sinal Amarelo no canto. Legível a 32 px porque é feito de blocos, não de detalhe.
        /// </summary>
        private static void Desenhar()
        {
            var pergaminho = new Color32(214, 194, 148, 255);
            var borda = new Color32(120, 100, 66, 255);
            var traco = new Color32(96, 78, 52, 255);
            var sinal = new Color32(212, 178, 60, 255);
            var vazio = new Color32(0, 0, 0, 0);

            var tex = new Texture2D(Lado, Lado, TextureFormat.RGBA32, mipChain: false);

            for (int y = 0; y < Lado; y++)
            for (int x = 0; x < Lado; x++)
            {
                // Margem transparente: o ícone não encosta na borda do slot.
                bool foraDoPergaminho = x < 3 || x > Lado - 4 || y < 5 || y > Lado - 6;

                if (foraDoPergaminho) { tex.SetPixel(x, y, vazio); continue; }

                bool naBorda = x == 3 || x == Lado - 4 || y == 5 || y == Lado - 6;

                tex.SetPixel(x, y, naBorda ? borda : pergaminho);
            }

            // A dobra ao meio -- é o que diz "isto é papel", e não "isto é uma placa".
            for (int y = 7; y < Lado - 7; y++) tex.SetPixel(Lado / 2, y, borda);

            // Três traços de rota, em alturas diferentes, sem simetria.
            foreach (var (yy, x0, x1) in new[] { (11, 6, 14), (17, 8, 24), (23, 6, 18) })
                for (int x = x0; x <= x1; x++)
                    tex.SetPixel(x, yy, traco);

            // O Sinal Amarelo: um losango pequeno no canto inferior direito.
            foreach (var (dx, dy) in new[] { (0, 0), (1, 1), (-1, 1), (0, 2), (1, -1), (-1, -1),
                                             (0, -2), (2, 0), (-2, 0) })
                tex.SetPixel(Lado - 9 + dx, 10 + dy, sinal);

            tex.Apply();

            Directory.CreateDirectory(Path.GetDirectoryName(Destino));
            File.WriteAllBytes(Destino, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        /// <summary>
        /// Padrão da skill <c>favela-pixelart-standards</c>: PPU 32, Point, sem compressão.
        /// </summary>
        private static void AjustarImport()
        {
            if (AssetImporter.GetAtPath(Destino) is not TextureImporter imp) return;

            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.spritePixelsPerUnit = 32;
            imp.filterMode = FilterMode.Point;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.mipmapEnabled = false;
            imp.alphaIsTransparency = true;

            imp.SaveAndReimport();
        }

        private static string Ligar()
        {
            var def = AssetDatabase.LoadAssetAtPath<ItemDef>(Item);
            if (def == null) return "Item_Chave_CartaDasAreias não existe — ícone criado e solto";

            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Destino);
            if (sprite == null) return "sprite não carregou depois do import";

            def.Icone = sprite;
            EditorUtility.SetDirty(def);
            AssetDatabase.SaveAssetIfDirty(def);

            return "ícone ligado ao ItemDef — PLACEHOLDER, trocar quando houver arte";
        }
    }
}
