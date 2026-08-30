using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;

namespace FavelaAmarela.EditorTools
{
    /// <summary>
    /// Monta os <b>Sprite Atlas</b> do projeto — sem eles, cada textura distinta quebra o
    /// <i>batch</i> e o isométrico paga em <i>draw calls</i> o que não precisa.
    ///
    /// <para><b>O estado que isto conserta (2026-08-29).</b> O empacotador já estava ligado —
    /// <c>m_SpritePackerMode: 5</c> (Sprite Atlas V2, sempre ativo) — e <b>não havia um único
    /// atlas no projeto</b>. Um empacotador sem atlas é um empacotador que não empacota nada:
    /// mais uma peça ligada que não acontece.</para>
    ///
    /// <para><b>Os números do atlas SOBREPÕEM os das texturas de origem</b>, e é aí que estava o
    /// risco real. Um atlas criado com o padrão da Unity (Bilinear + comprimido) borraria toda a
    /// pixel art de uma vez, e o sintoma apareceria só na build — as texturas soltas
    /// continuariam certas no Inspector. Por isso os três guardas da skill
    /// <c>favela-pixelart-standards</c> são explícitos aqui:</para>
    ///
    /// <list type="bullet">
    ///   <item><c>filterMode = Point</c> — nunca Bilinear.</item>
    ///   <item><c>textureCompression = Uncompressed</c> — compressão come as bordas e a paleta.</item>
    ///   <item><c>generateMipMaps = false</c> — mipmap borra pixel art conforme a câmera afasta.</item>
    /// </list>
    ///
    /// <para><b>Agrupado por PASTA, não por lista de arquivos.</b> Um atlas que enumera sprites
    /// é mais uma lista escrita à mão para envelhecer — este repositório já catalogou oito
    /// delas. Apontando para a pasta, arte nova entra no atlas sozinha no próximo import.</para>
    ///
    /// <para><b>O que fica DE FORA, de propósito:</b> <c>Assets/ThirdParty</c> (4.867 PNGs, dos
    /// quais o jogo usa uma fração — atlasar tudo empacotaria arte não usada na build) e a arte
    /// crua de <c>Assets/Arte/Inbox</c> e da raiz de <c>Assets</c>, que tem <b>zero</b>
    /// referências no projeto.</para>
    /// </summary>
    public static class MontarSpriteAtlas
    {
        private const string Marcador = "[SpriteAtlas]";
        private const string PastaDosAtlas = "Assets/FavelaAmarela/Art/Atlas";

        /// <summary>
        /// Os agrupamentos. O critério é <b>o que é desenhado junto</b>: sprites no mesmo atlas
        /// saem no mesmo <i>batch</i>, e sprites de atlas diferentes nunca compartilham um.
        /// Separar cenário de elenco e de UI é o corte natural de um isométrico — o chão é
        /// desenhado inteiro, depois os atores, depois o Canvas por cima.
        /// </summary>
        private static readonly (string Nome, string[] Pastas, string Razao)[] Grupos =
        {
            ("Atlas_Cenario",
             new[]
             {
                 "Assets/FavelaAmarela/Art/Tiles",
                 "Assets/FavelaAmarela/Art/Entradas",
                 "Assets/FavelaAmarela/Art/Environment",
             },
             "o chão e as portas: é o que cobre a tela inteira em toda cena"),

            ("Atlas_Elenco",
             new[]
             {
                 "Assets/FavelaAmarela/Art/Enemies",
                 "Assets/FavelaAmarela/Art/Characters",
             },
             "Damião, inimigos e NPCs — quem se move por cima do chão"),

            ("Atlas_UI",
             new[]
             {
                 "Assets/FavelaAmarela/Art/UI",
                 "Assets/FavelaAmarela/Art/Items",
             },
             "HUD e ícones de item: o Canvas é desenhado por último, num passe só"),
        };

        [MenuItem("Tools/FavelaAmarela/Arte: montar os Sprite Atlas")]
        public static void Executar()
        {
            if (!Directory.Exists(PastaDosAtlas))
            {
                Directory.CreateDirectory(PastaDosAtlas);
                AssetDatabase.Refresh();
            }

            var resumo = new List<string>();

            foreach (var (nome, pastas, razao) in Grupos)
                resumo.Add(Montar(nome, pastas, razao));

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"{Marcador} Concluído:\n  " + string.Join("\n  ", resumo));
        }

        /// <summary>
        /// Empacota os atlas e <b>conta quantos sprites cada um realmente contém</b>.
        ///
        /// <para><b>Por que existe.</b> Criar o asset não é empacotar. Um atlas apontando para
        /// uma pasta vazia, ou para sprites que outro atlas já reivindicou, é criado sem erro e
        /// entrega <b>zero</b> — o modo de falha assinatura deste repositório, aplicado a
        /// textura. O número abaixo é a única resposta honesta para "o atlas funcionou?".</para>
        /// </summary>
        [MenuItem("Tools/FavelaAmarela/Arte: conferir o que os Sprite Atlas empacotaram")]
        public static void Conferir()
        {
            var atlas = AssetDatabase.FindAssets("t:SpriteAtlas")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(c => !c.Contains("/ThirdParty/"))
                .Select(AssetDatabase.LoadAssetAtPath<UnityEngine.U2D.SpriteAtlas>)
                .Where(a => a != null)
                .ToArray();

            if (atlas.Length == 0)
            {
                Debug.LogError($"{Marcador} Nenhum atlas do projeto encontrado.");
                return;
            }

            SpriteAtlasUtility.PackAtlases(atlas, EditorUserBuildSettings.activeBuildTarget,
                                           canCancel: false);

            var linhas = new List<string>();
            int vazios = 0;

            foreach (var a in atlas.OrderBy(a => a.name))
            {
                int n = a.spriteCount;
                if (n == 0) vazios++;

                linhas.Add($"{a.name}: {n} sprite(s) empacotado(s)" +
                           (n == 0 ? "  <<< VAZIO: o atlas existe e não entrega nada" : ""));
            }

            string quebra = System.Environment.NewLine + "  ";
            string texto = $"{Marcador} Conferido:" + quebra + string.Join(quebra, linhas);

            if (vazios > 0) Debug.LogError(texto);
            else Debug.Log(texto);
        }

        private static string Montar(string nome, string[] pastas, string razao)
        {
            var objetos = new List<Object>();
            var ausentes = new List<string>();
            int sprites = 0;

            foreach (var pasta in pastas)
            {
                if (!Directory.Exists(pasta)) { ausentes.Add(pasta); continue; }

                var alvo = AssetDatabase.LoadAssetAtPath<Object>(pasta);
                if (alvo == null) { ausentes.Add(pasta); continue; }

                objetos.Add(alvo);

                // Só para o relatório: quantos sprites a pasta traz hoje. O atlas segue
                // apontando para a PASTA, então este número muda sozinho com arte nova.
                sprites += AssetDatabase.FindAssets("t:Sprite", new[] { pasta }).Length;
            }

            if (objetos.Count == 0)
                return $"{nome}: NENHUMA pasta válida ({string.Join(", ", ausentes)})";

            var atlas = new SpriteAtlasAsset();
            atlas.SetIncludeInBuild(true);
            atlas.Add(objetos.ToArray());

            string caminho = $"{PastaDosAtlas}/{nome}.spriteatlasv2";
            SpriteAtlasAsset.Save(atlas, caminho);

            // ── E AGORA os ajustes, no IMPORTER ────────────────────────────────
            //
            // Na V2 o .spriteatlasv2 guarda SÓ os packables; empacotamento e textura vivem no
            // SpriteAtlasImporter, ou seja, no .meta. Chamar SetPackingSettings/SetTextureSettings
            // no SpriteAtlasAsset em memória, antes do Save, NÃO persiste -- foi o que esta
            // ferramenta fez na primeira versão, e o .meta saiu com os padrões da Unity:
            // filterMode 1 (BILINEAR), rotação e tight packing ligados. Ou seja, exatamente o
            // atlas que borraria toda a pixel art, com a ferramenta reportando sucesso.
            AssetDatabase.ImportAsset(caminho, ImportAssetOptions.ForceUpdate);

            var importer = AssetImporter.GetAtPath(caminho) as SpriteAtlasImporter;
            if (importer == null)
                return $"{nome}: SALVO mas sem SpriteAtlasImporter — ajustes NÃO aplicados";

            importer.packingSettings = new SpriteAtlasPackingSettings
            {
                padding = 4,
                blockOffset = 1,

                // Rotação e empacotamento justo desalinham pixel art: o primeiro gira o sprite
                // dentro da folha, o segundo recorta pela silhueta. Os dois trocam previsibilidade
                // por alguns bytes, e num jogo de 73 sprites os bytes não são o problema.
                enableRotation = false,
                enableTightPacking = false,

                // Preenche os transparentes com a cor vizinha. Com Point não há amostragem entre
                // texels, então é apólice barata contra o dia em que alguém mexer no filtro.
                enableAlphaDilation = true,
            };

            importer.textureSettings = new SpriteAtlasTextureSettings
            {
                filterMode = FilterMode.Point,   // skill favela-pixelart-standards
                generateMipMaps = false,         // mipmap borra pixel art ao afastar a câmera
                anisoLevel = 0,

                // maxTextureSize NÃO entra aqui: em SpriteAtlasTextureSettings ele é somente
                // leitura (CS0200). O valor efetivo vem do override de plataforma abaixo, e o
                // zero que sobra neste bloco no .meta é inerte -- não é campo esquecido.
                readable = false,
                sRGB = true,
            };

            importer.SetPlatformSettings(new TextureImporterPlatformSettings
            {
                name = "DefaultTexturePlatform",
                maxTextureSize = 2048,
                textureCompression = TextureImporterCompression.Uncompressed,
                crunchedCompression = false,
                overridden = true,
            });

            importer.includeInBuild = true;
            importer.SaveAndReimport();

            string aviso = ausentes.Count > 0
                ? $"  [pasta(s) ausente(s): {string.Join(", ", ausentes)}]"
                : "";

            return $"{nome}: {objetos.Count} pasta(s), {sprites} sprite(s) hoje — {razao}{aviso}";
        }
    }
}
