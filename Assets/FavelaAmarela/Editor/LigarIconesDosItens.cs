using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace FavelaAmarela.EditorTools
{
    /// <summary>
    /// Atribui ícone aos <c>ItemDef</c> que estavam sem nenhum.
    ///
    /// <para><b>O buraco:</b> <b>18 dos 20 itens</b> tinham <c>Icone</c> vazio — toda arma,
    /// armadura, consumível e artefato aparecia em branco na mochila e na barra de ações. Não
    /// dá erro, não quebra compilação: só se vê jogando.</para>
    ///
    /// <para><b>De onde vem cada ícone e por quê:</b></para>
    /// <list type="bullet">
    /// <item>Armas e artefatos — pacotes pintados (<i>weapon RPG icons</i>, <i>Free Warlock
    /// Skills</i>), reescalados para 64×64 com <b>bicúbico</b>: são pintura, e reduzir pintura
    /// com vizinho-mais-próximo suja a imagem.</item>
    /// <item>Consumíveis — <i>Dark World</i>, que já é pixel art e entra sem reescala.</item>
    /// <item>Armaduras — <b>autoradas</b>: não existe ícone de armadura em pacote nenhum dos que
    /// temos. Três formas (elmo com crista, peitoral, pernas) em duas paletas.</item>
    /// <item>Necronomicon — reaproveita o sprite de mundo que já existe.</item>
    /// </list>
    ///
    /// <para><b>Filtro de textura por origem, não único:</b> Point nos de pixel art (a regra do
    /// projeto) e Bilinear nos pintados — Point num ícone pintado reduzido serrilha as bordas
    /// suaves e fica pior que a fonte.</para>
    ///
    /// <para>Tudo é <b>placeholder</b> declarado pelo Vini; a arte final entra depois.</para>
    /// </summary>
    public static class LigarIconesDosItens
    {
        private const string PastaDosIcones = "Assets/FavelaAmarela/Art/Items/Icones";

        /// <summary>Nome do asset (sem extensão) → nome do ícone (sem extensão).</summary>
        private static readonly Dictionary<string, string> Mapa = new Dictionary<string, string>
        {
            { "Item_Arma_AlfanjeDeAlhazred",       "Icone_Alfanje" },
            { "Item_Arma_EstileteDeIrem",          "Icone_Estilete" },
            { "Item_Arma_CravoDeAklo",             "Icone_Cravo" },

            { "Item_Armadura_CapuzDeFarrapos",     "Icone_CapuzDeFarrapos" },
            { "Item_Armadura_ColeteDeSucata",      "Icone_ColeteDeSucata" },
            { "Item_Armadura_CaneleirasDeFerro",   "Icone_CaneleirasDeFerro" },
            { "Item_Armadura_ElmoDeSet",           "Icone_ElmoDeSet" },
            { "Item_Armadura_PeitoralDeSet",       "Icone_PeitoralDeSet" },
            { "Item_Armadura_GrevasDeSet",         "Icone_GrevasDeSet" },

            { "Item_Consumivel_AguaDaCacimba",     "Icone_AguaDaCacimba" },
            { "Item_Consumivel_ErvaDeAncoragem",   "Icone_ErvaDeAncoragem" },
            { "Item_Consumivel_RaizDeYhtill",      "Icone_RaizDeYhtill" },

            // Relíquias existem em dois assets cada (ItemDef e ArtefatoDef) e compartilham arte.
            { "Item_Necronomicon",                 "Icone_Necronomicon" },
            { "Artefato_Necronomicon",             "Icone_Necronomicon" },
            { "Item_AnelDoSinalAmarelo",           "Icone_AnelDoSinalAmarelo" },
            { "Artefato_AnelDoSinalAmarelo",       "Icone_AnelDoSinalAmarelo" },
            { "Item_CoroaDeOssos",                 "Icone_CoroaDeOssos" },
            { "Artefato_CoroaDeOssos",             "Icone_CoroaDeOssos" },
        };

        /// <summary>
        /// Ícones que são pixel art de verdade e vão a Point. Os demais são pintura reduzida e
        /// vão a Bilinear.
        /// </summary>
        private static readonly HashSet<string> PixelArt = new HashSet<string>
        {
            "Icone_AguaDaCacimba", "Icone_ErvaDeAncoragem", "Icone_RaizDeYhtill",
            "Icone_CapuzDeFarrapos", "Icone_ColeteDeSucata", "Icone_CaneleirasDeFerro",
            "Icone_ElmoDeSet", "Icone_PeitoralDeSet", "Icone_GrevasDeSet",
            // Trocados em 2026-08-19 por pixel art 32×32 do CraftPix "Undead Loot", que é do
            // tema e da escala do projeto — antes eram pintura reduzida (o Anel e o Estilete)
            // e um sprite de 16×16 autorado (o Necronomicon).
            "Icone_Necronomicon", "Icone_AnelDoSinalAmarelo", "Icone_Estilete",
        };

        [MenuItem("Tools/FavelaAmarela/Ligar icones dos itens")]
        public static void Executar()
        {
            foreach (var icone in Mapa.Values.Distinct())
                ConfigurarImport($"{PastaDosIcones}/{icone}.png", PixelArt.Contains(icone));

            AssetDatabase.Refresh();

            int ligados = 0;
            var faltando = new List<string>();

            foreach (var par in Mapa)
            {
                string caminhoDoAsset = AssetDatabase.FindAssets($"{par.Key} t:ScriptableObject")
                    .Select(AssetDatabase.GUIDToAssetPath)
                    .FirstOrDefault(p => System.IO.Path.GetFileNameWithoutExtension(p) == par.Key);

                if (string.IsNullOrEmpty(caminhoDoAsset))
                {
                    faltando.Add($"{par.Key}: asset não encontrado");
                    continue;
                }

                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{PastaDosIcones}/{par.Value}.png");
                if (sprite == null)
                {
                    faltando.Add($"{par.Value}.png: sprite não carregou");
                    continue;
                }

                var alvo = AssetDatabase.LoadAssetAtPath<ScriptableObject>(caminhoDoAsset);
                var so = new SerializedObject(alvo);
                var prop = so.FindProperty("Icone");

                if (prop == null)
                {
                    faltando.Add($"{par.Key}: sem campo 'Icone'");
                    continue;
                }

                prop.objectReferenceValue = sprite;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(alvo);
                ligados++;
            }

            AssetDatabase.SaveAssets();

            string resumo = $"[IconesDosItens] {ligados} item(ns) com ícone.";
            if (faltando.Count > 0) resumo += "\n  Pendências:\n  " + string.Join("\n  ", faltando);

            Debug.Log(resumo);
        }

        private static void ConfigurarImport(string caminho, bool ehPixelArt)
        {
            var importer = AssetImporter.GetAtPath(caminho) as TextureImporter;
            if (importer == null)
            {
                Debug.LogError($"[IconesDosItens] Textura não encontrada: {caminho}");
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 32f;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;

            importer.filterMode = ehPixelArt ? FilterMode.Point : FilterMode.Bilinear;

            foreach (string plataforma in new[] { "Standalone", "WebGL", "WindowsStoreApps" })
            {
                var ps = importer.GetPlatformTextureSettings(plataforma);
                ps.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SetPlatformTextureSettings(ps);
            }

            importer.SaveAndReimport();
        }
    }
}
