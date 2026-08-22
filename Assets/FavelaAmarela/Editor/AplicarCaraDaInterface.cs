using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace FavelaAmarela.EditorTools
{
    /// <summary>
    /// Aplica a tipografia e os painéis de <see cref="PaletaDaInterface"/> às cenas que já
    /// existem.
    ///
    /// <para><b>Por que uma ferramenta separada:</b> apontar as ferramentas de montagem para a
    /// paleta só muda o que for construído <b>daqui em diante</b>. As telas do jogo já estão
    /// serializadas nas cenas, com a fonte antiga gravada em cada <c>Text</c>. Sem este
    /// retrofit, a mudança não apareceria em nada que já existe.</para>
    ///
    /// <para><b>Configura o import dos painéis antes de usá-los.</b> Os PNG da Kenney vêm com
    /// <c>spriteBorder</c> zerado — aplicar <c>Image.Type.Sliced</c> sem borda estica a moldura
    /// junto com o miolo, e o resultado fica pior que o retângulo chapado que havia antes. A
    /// borda de 12px é medida na arte: é a espessura da moldura desenhada nos 100×100.</para>
    /// </summary>
    public static class AplicarCaraDaInterface
    {
        // A borda e os retângulos vêm da PaletaDaInterface, que os documenta como medidos na
        // arte — não repetir os números aqui, senão eles divergem.

        private static readonly string[] Cenas =
        {
            "Assets/Scenes/Deserto_Hali.unity",
            "Assets/Scenes/Playtest_RuinasPalidas.unity",
            "Assets/Scenes/Santuario_Yhtill.unity",
            "Assets/Scenes/Cena_ArenaDeTestes.unity",
            "Assets/Scenes/Cena_Menu.unity",
            // Acrescentadas em 2026-08-22. Sem elas, as duas cenas mais novas — e as duas
            // lutas de chefe do Vertical Slice — ficavam com a interface no retângulo chapado
            // da Unity enquanto o resto do jogo tinha a moldura do Dark Ages UI. OITAVA lista
            // de cenas escrita à mão a ficar para trás neste projeto.
            "Assets/Scenes/Portoes_Das_Ruinas.unity",
            "Assets/Scenes/Castelo_Carcosa.unity",
        };

        /// <summary>
        /// Nomes que recebem a moldura ornamentada. <b>Lidos das cenas</b>, não inventados: a
        /// primeira versão desta lista tinha <c>CaixaDeTexto</c>, que não existe — o objeto se
        /// chama <c>CaixaDeDialogo</c>.
        /// </summary>
        private static readonly HashSet<string> Paineis = new HashSet<string>
        {
            "PainelDeFicha", "PainelDeInventario", "PainelDeEscolha", "Painel_Escolha",
            "Janela", "Painel", "Tela_Pause", "Tela_Colapso", "CaixaDeDialogo",

            // Painéis do menu principal, acrescentados em 2026-08-22. Eles CARREGAVAM a
            // moldura do Dark Ages UI mas não estavam nesta lista — ou seja, a arte tinha sido
            // posta à mão e não era reprodutível. Sobreviveu só enquanto ninguém remontou o
            // menu; ao remontar, voltou ao retângulo chapado da Unity e o guarda
            // CenaMenu_UsaAMolduraDoDarkAgesUI acusou. Agora a aplicação é repetível.
            "Menu", "Confirmacao",
        };

        /// <summary>
        /// Casas de item e de Artefato — recebem a moldura discreta, não a ornamentada. Uma
        /// espiral dourada em cada uma das 16 casas competiria com o ícone do item.
        /// </summary>
        private static bool EhSlot(string nome) =>
            nome.StartsWith("Slot_", System.StringComparison.Ordinal);

        [MenuItem("Tools/FavelaAmarela/Aplicar cara da interface (todas as cenas)")]
        public static void Aplicar()
        {
            FatiarTilesheet();

            var resumo = new List<string>();

            foreach (var caminho in Cenas)
            {
                if (!System.IO.File.Exists(caminho))
                {
                    resumo.Add($"{System.IO.Path.GetFileName(caminho)}: ausente");
                    continue;
                }

                var cena = EditorSceneManager.OpenScene(caminho, OpenSceneMode.Single);

                int textos = TrocarFontes();
                var (paineis, slots) = AplicarMolduras();

                if (textos > 0 || paineis > 0 || slots > 0)
                {
                    EditorSceneManager.MarkSceneDirty(cena);
                    EditorSceneManager.SaveScene(cena);
                }

                resumo.Add($"{System.IO.Path.GetFileName(caminho)}: {textos} texto(s), " +
                           $"{paineis} painel(is), {slots} slot(s)");
            }

            AssetDatabase.SaveAssets();
            Debug.Log("[CaraDaInterface] Concluído:\n  " + string.Join("\n  ", resumo));
        }

        /// <summary>
        /// Fatia o tilesheet do Dark Ages UI em sprites nomeados, com a borda 9-slice.
        ///
        /// <para>O pacote vem como <b>um PNG só</b> (384×352, 25 elementos). Sem fatiar, não há
        /// sprite para atribuir a <c>Image</c> nenhum. E sem <c>spriteBorder</c>, aplicar
        /// <c>Sliced</c> esticaria o ornamento de canto junto com o miolo — resultado pior que o
        /// retângulo chapado que havia antes.</para>
        ///
        /// <para>Idempotente: refatiar com os mesmos nomes só reescreve as mesmas entradas.</para>
        /// </summary>
        private static void FatiarTilesheet()
        {
            string caminho = PaletaDaInterface.CaminhoDoTilesheet;

            var importer = AssetImporter.GetAtPath(caminho) as TextureImporter;
            if (importer == null)
            {
                Debug.LogError($"[CaraDaInterface] Tilesheet não encontrado em '{caminho}'. " +
                               "O pacote Dark Ages UI está em Assets/ThirdParty/?");
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = 32f;          // PPU do projeto
            importer.filterMode = FilterMode.Point;      // regra de pixel art
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;

            int b = PaletaDaInterface.BordaDoPainel;

            var fatias = new[]
            {
                Fatia(PaletaDaInterface.SpritePainel, PaletaDaInterface.RectPainel, b),
                Fatia(PaletaDaInterface.SpritePergaminho, PaletaDaInterface.RectPergaminho, b),
                Fatia(PaletaDaInterface.SpriteSlot, PaletaDaInterface.RectSlot,
                      PaletaDaInterface.BordaDoSlot),
            };

            // A API antiga (spritesheet) continua sendo a que funciona em batch mode; o
            // SpriteDataProvider exige o Editor gráfico aberto.
#pragma warning disable CS0618
            importer.spritesheet = fatias;
#pragma warning restore CS0618

            importer.SaveAndReimport();

            Debug.Log($"[CaraDaInterface] Tilesheet fatiado: {fatias.Length} sprites " +
                      $"(borda 9-slice {b}px, PPU 32, Point, sem compressão).");
        }

        private static SpriteMetaData Fatia(string nome, Rect rect, int borda) => new SpriteMetaData
        {
            name = nome,
            rect = rect,
            alignment = (int)SpriteAlignment.Center,
            pivot = new Vector2(0.5f, 0.5f),
            border = new Vector4(borda, borda, borda, borda),
        };

        private static int TrocarFontes()
        {
            var fonte = PaletaDaInterface.Fonte;
            if (fonte == null) return 0;

            int trocados = 0;

            foreach (var texto in Object.FindObjectsByType<Text>(FindObjectsInactive.Include))
            {
                if (texto.font == fonte) continue;

                Undo.RecordObject(texto, "Trocar fonte da interface");
                texto.font = fonte;

                EditorUtility.SetDirty(texto);
                trocados++;
            }

            return trocados;
        }

        /// <summary>
        /// Aplica as molduras por <b>nome</b> do objeto.
        ///
        /// <para><b>Bug da primeira versão:</b> havia uma guarda <c>if (img.sprite != null)
        /// continue</c>, escrita para não sobrescrever ícone de item. Só que um <c>Image</c> da
        /// Unity nasce com o sprite embutido atribuído — então a guarda pulava quase todos os
        /// painéis. O resultado foi <b>1 painel por cena</b> em vez de seis, com a ferramenta
        /// reportando sucesso.</para>
        ///
        /// <para>A guarda saiu: a seleção por nome já é explícita, e ícone de item não está na
        /// lista. Quem nomeia um objeto de <c>PainelDeFicha</c> quer que ele seja um painel.</para>
        /// </summary>
        private static (int paineis, int slots) AplicarMolduras()
        {
            int paineis = 0, slots = 0;

            foreach (var img in Object.FindObjectsByType<Image>(FindObjectsInactive.Include))
            {
                string nome = img.gameObject.name;

                if (Paineis.Contains(nome))
                {
                    Undo.RecordObject(img, "Aplicar painel da interface");
                    PaletaDaInterface.AplicarPainel(img);
                    EditorUtility.SetDirty(img);
                    paineis++;
                }
                else if (EhSlot(nome))
                {
                    Undo.RecordObject(img, "Aplicar moldura de slot");
                    PaletaDaInterface.AplicarSlot(img);
                    EditorUtility.SetDirty(img);
                    slots++;
                }
            }

            return (paineis, slots);
        }
    }
}
