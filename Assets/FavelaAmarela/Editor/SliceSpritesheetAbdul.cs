using System.Collections.Generic;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace FavelaAmarela.EditorTools
{
    /// <summary>
    /// Utilitário de Editor: fatia o spritesheet do <b>Abdul Alhazred</b> em frames
    /// nomeados por animação, batendo com os estados da <c>AbdulFSM</c>.
    ///
    /// <para>A folha é um grid de 64×64 numa tela 1024×1024, mas <b>só parte das colunas
    /// tem arte</b> — fatiar o grid cheio geraria ~29 sprites vazios. Por isso as linhas
    /// são declaradas com a contagem real de frames.</para>
    ///
    /// <para>Atenção à origem: o Aseprite conta Y de cima para baixo, a Unity de baixo para
    /// cima. A conversão está em <see cref="RectDoFrame"/> — errar isso fatia a folha
    /// espelhada verticalmente.</para>
    /// </summary>
    public static class SliceSpritesheetAbdul
    {
        private const string CaminhoTextura =
            "Assets/Sprites/Bosses/Alhazred/abdul_alhazred_spritesheet.png";

        private const int TamanhoFrame = 64;
        private const int AlturaTextura = 1024;

        /// <summary>Uma linha da folha: nome da animação e quantos frames ela tem de fato.</summary>
        private readonly struct Linha
        {
            public readonly string Nome;
            public readonly int Frames;
            public Linha(string nome, int frames) { Nome = nome; Frames = frames; }
        }

        // Ordem das linhas na folha (de cima para baixo), com a contagem real de frames.
        // Os nomes seguem os estados/eventos da AbdulFSM para o Animator ficar óbvio depois.
        private static readonly Linha[] _linhas =
        {
            new Linha("transe", 4),        // flutuando com o grimório (pré-luta)
            new Linha("flutuar", 4),       // deslocamento / reposicionamento
            new Linha("cone_de_gelo", 4),  // conjuração do Cone de Gelo (Fase 2)
            new Linha("invocar", 4),       // glifos verdes — invoca esqueletos
            new Linha("dissolver", 4),     // dissolução em partículas (teleporte / escudo)
            new Linha("dano", 2),          // reação a golpe / exausto
            new Linha("derrota", 6),       // cai de joelhos e solta o Necronomicon
        };

        [MenuItem("Tools/FavelaAmarela/Slice Spritesheet do Abdul")]
        public static void Slice()
        {
            var importer = AssetImporter.GetAtPath(CaminhoTextura) as TextureImporter;
            if (importer == null)
            {
                Debug.LogError($"[SliceAbdul] Textura não encontrada em '{CaminhoTextura}'.");
                return;
            }

            // Garante o padrão de pixel art do projeto junto do fatiamento
            // (favela-pixelart-standards: PPU 32, Point, sem compressão).
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = 32f;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;

            var rects = MontarRects();

            var factories = new SpriteDataProviderFactories();
            factories.Init();
            var provider = factories.GetSpriteEditorDataProviderFromObject(importer);
            if (provider == null)
            {
                Debug.LogError("[SliceAbdul] Não foi possível obter o data provider de sprites.");
                return;
            }

            provider.InitSpriteEditorDataProvider();
            provider.SetSpriteRects(rects.ToArray());
            provider.Apply();

            importer.SaveAndReimport();

            Debug.Log($"[SliceAbdul] {rects.Count} frames fatiados em '{CaminhoTextura}' " +
                      "(PPU 32, Point, sem compressão). Animações: " +
                      "transe, flutuar, cone_de_gelo, invocar, dissolver, dano, derrota.");
        }

        private static List<SpriteRect> MontarRects()
        {
            var lista = new List<SpriteRect>();

            for (int linha = 0; linha < _linhas.Length; linha++)
            {
                var info = _linhas[linha];
                for (int coluna = 0; coluna < info.Frames; coluna++)
                {
                    var sr = new SpriteRect
                    {
                        name = $"abdul_{info.Nome}_{coluna}",
                        rect = RectDoFrame(linha, coluna),
                        // Pivot nos pés: o Y-sorting do projeto ordena por Y do ator,
                        // então o ponto de referência precisa ser a base do sprite.
                        alignment = SpriteAlignment.Custom,
                        pivot = new Vector2(0.5f, 0.1f),
                        border = Vector4.zero,
                    };

                    // GUID **determinístico**, derivado do nome: rodar o slicer de novo tem
                    // de devolver exatamente os mesmos IDs, senão toda referência existente
                    // (sprite do prefab do Abdul, futuras AnimationClips) aponta para o
                    // vazio silenciosamente. Já aconteceu uma vez com GUID.Generate().
                    sr.spriteID = GuidEstavelPara(sr.name);
                    lista.Add(sr);
                }
            }

            return lista;
        }

        /// <summary>
        /// Deriva um <see cref="GUID"/> estável a partir do nome do sprite (MD5 do nome).
        /// Mesmo nome ⇒ mesmo ID, sempre — é o que torna o slicer idempotente e impede que
        /// reexecutá-lo invalide referências já feitas em prefabs e animações.
        /// </summary>
        private static GUID GuidEstavelPara(string nome)
        {
            using var md5 = System.Security.Cryptography.MD5.Create();
            byte[] hash = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(nome));

            var hex = new System.Text.StringBuilder(32);
            foreach (byte b in hash) hex.Append(b.ToString("x2"));

            return new GUID(hex.ToString());
        }

        /// <summary>
        /// Converte (linha, coluna) do grid do Aseprite para o <see cref="Rect"/> da Unity.
        /// O Aseprite conta a linha 0 no topo; a Unity mede Y a partir da base da textura.
        /// </summary>
        private static Rect RectDoFrame(int linha, int coluna)
        {
            float x = coluna * TamanhoFrame;
            float yTopo = linha * TamanhoFrame;
            float yUnity = AlturaTextura - yTopo - TamanhoFrame;
            return new Rect(x, yUnity, TamanhoFrame, TamanhoFrame);
        }
    }
}
