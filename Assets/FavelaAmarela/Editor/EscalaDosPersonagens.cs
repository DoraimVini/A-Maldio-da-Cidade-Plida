using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace FavelaAmarela.EditorTools
{
    /// <summary>
    /// Muda o tamanho em mundo das sprites de personagem <b>sem redesenhar arte</b>, baixando a
    /// PPU de importação.
    ///
    /// <para><b>Por que PPU e não escala de transform:</b> a PPU é o que define quantos pixels de
    /// arte cabem numa unidade de mundo. Metade da PPU = dobro do tamanho, com ampliação
    /// <b>inteira</b> — cada pixel da arte vira exatamente 2×2 na tela. Escalar o transform daria
    /// o mesmo tamanho mas com o Y-sort, os offsets e os filhos todos multiplicados junto, e
    /// qualquer fator não-inteiro borraria a pixel art.</para>
    ///
    /// <para><b>O colisor NÃO acompanha, e isso é deliberado.</b> Num isométrico o colisor é a
    /// <i>pegada no chão</i>, não a silhueta: o Damião tem caixa de 1,75 e os corredores do
    /// Castelo têm 4 de largura. Dobrar a pegada para 3,5 o deixaria mal passando. A sprite
    /// cresce para cima — o pivô das fatias é <c>BottomCenter</c> — e os pés continuam onde
    /// estavam.</para>
    ///
    /// <para><b>Confere o resultado no disco:</b> a Unity grava o campo como
    /// <c>spritePixelsToUnits</c>, não <c>spritePixelsPerUnit</c>. Procurar o nome errado numa
    /// meta faz parecer que o valor não existe — foi o que me levou a diagnosticar um problema de
    /// PPU que não existia, em 2026-08-20.</para>
    /// </summary>
    public static class EscalaDosPersonagens
    {
        private const string PastaDoDamiao = "Assets/FavelaAmarela/Art/Characters/Damiao/Animado";

        /// <summary>
        /// Demais atores. Separados do Damião de propósito: dobrar só o protagonista o deixa do
        /// tamanho do chefe e ao dobro dos cultistas. Se o elenco tiver que acompanhar, é chamar
        /// <see cref="DobrarElencoInteiro"/>.
        /// </summary>
        private static readonly string[] PastasDoElenco =
        {
            "Assets/FavelaAmarela/Art/Characters/MiGo",
            "Assets/FavelaAmarela/Art/Enemies",
            "Assets/Sprites/Cultistas",
        };

        [MenuItem("Tools/FavelaAmarela/Escala: dobrar a sprite do Damião")]
        public static void DobrarDamiao() => Aplicar(new[] { PastaDoDamiao }, "Damião");

        [MenuItem("Tools/FavelaAmarela/Escala: dobrar o elenco inteiro")]
        public static void DobrarElencoInteiro()
            => Aplicar(PastasDoElenco.Concat(new[] { PastaDoDamiao }).ToArray(), "elenco");

        [MenuItem("Tools/FavelaAmarela/Escala: voltar o Damião ao normal")]
        public static void ReverterDamiao() => Aplicar(new[] { PastaDoDamiao }, "Damião", ppuAlvo: 32);

        private static void Aplicar(string[] pastas, string rotulo, int ppuAlvo = 16)
        {
            var mexidos = new List<string>();
            var pulados = new List<string>();

            foreach (var pasta in pastas)
            {
                if (!Directory.Exists(pasta))
                {
                    pulados.Add($"{pasta}: pasta ausente");
                    continue;
                }

                foreach (var caminho in Directory.EnumerateFiles(pasta, "*.png", SearchOption.AllDirectories))
                {
                    if (!(AssetImporter.GetAtPath(caminho) is TextureImporter importer)) continue;
                    if (importer.textureType != TextureImporterType.Sprite) continue;

                    if (Mathf.Approximately(importer.spritePixelsPerUnit, ppuAlvo)) continue;

                    importer.spritePixelsPerUnit = ppuAlvo;
                    importer.filterMode = FilterMode.Point;               // pixel art, sem borrar
                    importer.textureCompression = TextureImporterCompression.Uncompressed;
                    importer.SaveAndReimport();

                    mexidos.Add(Path.GetFileName(caminho));
                }
            }

            AssetDatabase.Refresh();

            // Confere no DISCO, e pelo nome que a Unity realmente grava. Já houve mudança de
            // import neste projeto que a API aceitou e o arquivo não recebeu.
            var naoPegaram = new List<string>();
            foreach (var pasta in pastas)
            {
                if (!Directory.Exists(pasta)) continue;

                foreach (var meta in Directory.EnumerateFiles(pasta, "*.png.meta", SearchOption.AllDirectories))
                {
                    var m = System.Text.RegularExpressions.Regex.Match(
                        File.ReadAllText(meta), @"spritePixelsToUnits:\s*(\d+)");

                    if (!m.Success || m.Groups[1].Value != ppuAlvo.ToString())
                        naoPegaram.Add(Path.GetFileNameWithoutExtension(meta));
                }
            }

            Debug.Log($"[EscalaDosPersonagens] {rotulo}: PPU {ppuAlvo} em {mexidos.Count} sprite(s). " +
                      $"Tamanho em mundo {(ppuAlvo == 16 ? "dobrado" : "no padrão")}.");

            if (pulados.Count > 0)
                Debug.LogWarning("[EscalaDosPersonagens] Pulados: " + string.Join(", ", pulados));

            if (naoPegaram.Count > 0)
                Debug.LogError("[EscalaDosPersonagens] A escrita NÃO pegou em: " +
                               string.Join(", ", naoPegaram) +
                               " — confira no disco, o retorno da API não basta.");
        }
    }
}
