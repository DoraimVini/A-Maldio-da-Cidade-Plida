using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace FavelaAmarela.EditorTools
{
    /// <summary>
    /// Recorta do <b>Dark Ages UI</b> as duas molduras de casa de inventário.
    ///
    /// <para><b>Por que existem duas:</b> decisão do Vini — uma para casa vazia, outra para casa
    /// ocupada. O estado do slot passa a ser lido pela <i>arte</i>, e não pelo desbotamento do
    /// grupo inteiro, que apagava a própria moldura junto e deixava a grade ilegível.</para>
    ///
    /// <para><b>Os retângulos foram medidos, não estimados:</b> uma varredura de componentes
    /// conexos no tilesheet devolveu as duas peças em <c>(210,18,60,60)</c> e
    /// <c>(306,18,60,60)</c>. Ler coordenada de olho em folha de UI é como se acumulam frestas
    /// de um pixel.</para>
    ///
    /// <para><b>Y invertido:</b> a varredura mede a partir do topo da imagem; o retângulo de
    /// sprite da Unity tem origem embaixo. A conversão está em <see cref="DeCima"/>.</para>
    ///
    /// <para>Preserva as fatias já autoradas na folha — só acrescenta o que falta.</para>
    /// </summary>
    public static class FatiarMoldurasDeSlot
    {
        private const string Folha =
            "Assets/ThirdParty/DarkAgesUI/DarkAgesUi_v1.0/32x32-Tilesheet.png";

        /// <summary>Altura da folha, para converter coordenada de topo em coordenada de base.</summary>
        private const int AlturaDaFolha = 352;

        private static readonly (string Nome, int X, int YDeCima, int L, int A)[] Pecas =
        {
            ("slot_vazio", 210, 18, 60, 60),
            ("slot_cheio", 306, 18, 60, 60),
        };

        [MenuItem("Tools/FavelaAmarela/Fatiar molduras de slot (Dark Ages UI)")]
        public static void Executar()
        {
            var importer = AssetImporter.GetAtPath(Folha) as TextureImporter;
            if (importer == null)
            {
                Debug.LogError($"[MoldurasDeSlot] Folha não encontrada: {Folha}");
                return;
            }

            if (importer.spriteImportMode != SpriteImportMode.Multiple)
            {
                importer.spriteImportMode = SpriteImportMode.Multiple;
                importer.SaveAndReimport();
            }

            var fabrica = new SpriteDataProviderFactories();
            fabrica.Init();

            var provider = fabrica.GetSpriteEditorDataProviderFromObject(importer);
            provider.InitSpriteEditorDataProvider();

            var rects = provider.GetSpriteRects().ToList();

            int novos = 0;
            foreach (var (nome, x, yDeCima, l, a) in Pecas)
            {
                // Não sobrescreve o que já existe: a folha traz 'painel_ornado' autorado, e
                // recriar fatias muda o spriteID, o que solta toda referência que aponte para elas.
                if (rects.Any(r => r.name == nome)) continue;

                rects.Add(new SpriteRect
                {
                    name = nome,
                    rect = new Rect(x, DeCima(yDeCima, a), l, a),
                    alignment = SpriteAlignment.Center,
                    pivot = new Vector2(0.5f, 0.5f),

                    // Borda de 9 fatias: a moldura tem canto ornamentado que NÃO pode esticar.
                    // Sem isto, uma casa retangular deforma os cantos e a arte parece derretida.
                    border = new Vector4(12f, 12f, 12f, 12f),
                    spriteID = GuidEstavelPara(nome),
                });

                novos++;
            }

            if (novos == 0)
            {
                Debug.Log("[MoldurasDeSlot] As duas molduras já estavam fatiadas.");
                return;
            }

            provider.SetSpriteRects(rects.ToArray());
            provider.Apply();

            importer.SaveAndReimport();
            AssetDatabase.Refresh();

            Debug.Log($"[MoldurasDeSlot] {novos} moldura(s) fatiada(s): " +
                      string.Join(", ", Pecas.Select(p => p.Nome)));
        }

        /// <summary>Converte Y medido do topo em Y medido da base, que é o que a Unity usa.</summary>
        private static float DeCima(int yDeCima, int altura) => AlturaDaFolha - yDeCima - altura;

        /// <summary>
        /// GUID derivado do nome, para o id da fatia não mudar a cada execução — quem já aponta
        /// para a moldura continua apontando.
        /// </summary>
        private static GUID GuidEstavelPara(string nome)
        {
            using (var md5 = System.Security.Cryptography.MD5.Create())
            {
                var bytes = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes("moldura:" + nome));
                return new GUID(string.Concat(bytes.Select(b => b.ToString("x2"))));
            }
        }
    }
}
