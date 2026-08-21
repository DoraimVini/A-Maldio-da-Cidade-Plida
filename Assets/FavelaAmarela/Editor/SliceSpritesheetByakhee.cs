using System.Collections.Generic;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace FavelaAmarela.EditorTools
{
    /// <summary>
    /// Utilitário de Editor: fatia o spritesheet real do <b>Byakhee</b> (trazido pelo Vini,
    /// <c>Assets/Arte/Inbox/byakhee_v2_animated.aseprite</c>, exportado via Aseprite CLI para
    /// <c>Byakhee_Spritesheet.png</c>) em frames nomeados por animação.
    ///
    /// <para><b>26 frames em grid 6×6, 164×164 cada.</b> O <c>.aseprite</c> já vem com 6
    /// tags (Idle/Walk/Attack/Special/Hurt/Death), mas o campo "to" de cada tag está com bug
    /// — todas terminam no frame 25. Os "from" continuam confiáveis (0, 4, 10, 14, 20, 22) e
    /// são não-sobrepostos entre si, então cada tag foi reconstruída aqui como
    /// [from da tag, from da próxima tag − 1] — 4+6+4+6+2+4 = 26, bate exato com o total.</para>
    ///
    /// <para>Nomes das linhas seguem o vocabulário da <c>ByakheeFSM</c>
    /// (<c>ByakheeState.Espreita/Rasante/MergulhoDeGarras/GritoDirecionado/Derrotado</c>), não
    /// os nomes genéricos das tags do Aseprite — "Hurt" vira "dano" porque não é um estado da
    /// FSM, é reação a golpe.</para>
    /// </summary>
    public static class SliceSpritesheetByakhee
    {
        private const string CaminhoTextura =
            "Assets/FavelaAmarela/Art/Enemies/Byakhee_Spritesheet.png";

        private const int TamanhoFrame = 164;

        private const int AlturaTextura = TamanhoFrame * 6; // 6 linhas

        private readonly struct Linha
        {
            public readonly string Nome;
            public readonly int FrameInicial;
            public readonly int Frames;
            public Linha(string nome, int frameInicial, int frames)
            {
                Nome = nome; FrameInicial = frameInicial; Frames = frames;
            }
        }

        // Reconstrução dos 6 segmentos a partir dos "from" das tags do .aseprite (ver acima).
        private static readonly Linha[] _linhas =
        {
            new Linha("espreita", 0, 4),        // Idle — parado no arco, antes da luta
            new Linha("rasante", 4, 6),         // Walk — atravessando a arena
            new Linha("garras", 10, 4),         // Attack — mergulho de garras / pouso agressivo
            new Linha("grito", 14, 6),          // Special — grito direcionado (fase 2+)
            new Linha("dano", 20, 2),           // Hurt — reação a golpe recebido
            new Linha("derrota", 22, 4),        // Death — Derrotado
        };

        [MenuItem("Tools/FavelaAmarela/Slice Spritesheet do Byakhee")]
        public static void Slice()
        {
            var importer = AssetImporter.GetAtPath(CaminhoTextura) as TextureImporter;
            if (importer == null)
            {
                Debug.LogError($"[SliceByakhee] Textura não encontrada em '{CaminhoTextura}'.");
                return;
            }

            // favela-pixelart-standards: PPU 32, Point, sem compressão.
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = 32f;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;

            var rects = MontarRects();

            var factories = new SpriteDataProviderFactories();
            factories.Init();
            var provider = factories.GetSpriteEditorDataProviderFromObject(importer);
            if (provider == null)
            {
                Debug.LogError("[SliceByakhee] Não foi possível obter o data provider de sprites.");
                return;
            }

            provider.InitSpriteEditorDataProvider();
            provider.SetSpriteRects(rects.ToArray());
            provider.Apply();

            importer.SaveAndReimport();

            Debug.Log($"[SliceByakhee] {rects.Count} frames fatiados em '{CaminhoTextura}' " +
                      "(PPU 32, Point, sem compressão). Animações: espreita, rasante, garras, " +
                      "grito, dano, derrota.");
        }

        private static List<SpriteRect> MontarRects()
        {
            var lista = new List<SpriteRect>();

            // O indice da LINHA e o que posiciona no grid, nao o numero global do quadro:
            // as fileiras tem 4, 6, 4, 6, 2 e 4 quadros, entao 'frameGlobal / 6' cairia na
            // fileira errada. Cada Linha deste array E uma fileira do arquivo.
            for (int fileira = 0; fileira < _linhas.Length; fileira++)
            {
                var info = _linhas[fileira];

                for (int i = 0; i < info.Frames; i++)
                {
                    var sr = new SpriteRect
                    {
                        name = $"byakhee_{info.Nome}_{i}",
                        rect = RectDoFrame(fileira, i),
                        // Pivot nos pés/sombra: a arte já desenha uma sombra elíptica na base
                        // de cada frame, então o centro-base bate com o Y-sort do projeto.
                        alignment = SpriteAlignment.Custom,
                        pivot = new Vector2(0.5f, 0f),
                        border = Vector4.zero,
                    };

                    sr.spriteID = GuidEstavelPara(sr.name);
                    lista.Add(sr);
                }
            }

            return lista;
        }

        /// <summary>Mesmo esquema do slicer do Abdul: GUID determinístico (MD5 do nome), para
        /// reexecutar não invalidar referências já feitas em prefabs.</summary>
        private static GUID GuidEstavelPara(string nome)
        {
            using var md5 = System.Security.Cryptography.MD5.Create();
            byte[] hash = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(nome));

            var hex = new System.Text.StringBuilder(32);
            foreach (byte b in hash) hex.Append(b.ToString("x2"));

            return new GUID(hex.ToString());
        }

        /// <summary>
        /// A folha é uma fita única (1 linha, 26 colunas) — sem a inversão de eixo Y que o
        /// grid do Abdul precisa, só a coluna do frame.
        /// </summary>
        /// <summary>
        /// Retângulo de um quadro no <b>grid</b> 6×6.
        ///
        /// <para><b>Deixou de ser fita única em 2026-08-20, por dois motivos.</b> O primeiro: a
        /// fita anterior estava <b>quebrada</b> — as células do arquivo original medem
        /// 1024/6 = 170,67 px e quem gerou a fita cortou blocos de 140, então o erro acumulava e
        /// cada quadro continha o fim de um e o começo do próximo, com as linhas de grade
        /// atravessando. O segundo: 26 quadros de 164 em uma linha dão <b>4264 px</b>, e o
        /// <c>maxTextureSize</c> é 2048 — a Unity reduziria a textura em silêncio e os
        /// retângulos daqui apontariam para o lugar errado. Em grid são 984×984, que cabe.</para>
        ///
        /// <para><b>Y invertido:</b> a fileira 0 do arquivo é a de cima, mas o retângulo de
        /// sprite da Unity tem origem embaixo. Sem a inversão, "espreita" traria os quadros de
        /// derrota.</para>
        ///
        /// <para><b>Recebe a fileira, e não o número global do quadro:</b> as fileiras têm 4, 6,
        /// 4, 6, 2 e 4 quadros, então dividir o índice global por 6 cairia na fileira errada —
        /// erro que a primeira versão desta reescrita cometeu e que só apareceu ao imprimir o
        /// mapeamento antes de rodar.</para>
        /// </summary>
        private static Rect RectDoFrame(int fileira, int coluna)
        {
            float x = coluna * TamanhoFrame;
            float y = AlturaTextura - (fileira + 1) * TamanhoFrame;

            return new Rect(x, y, TamanhoFrame, TamanhoFrame);
        }
    }
}
