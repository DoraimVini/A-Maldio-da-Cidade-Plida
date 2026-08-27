using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using FavelaAmarela.Runtime.Interaction;

namespace FavelaAmarela.Tests.EditMode
{
    /// <summary>
    /// Guarda o preço de ter estreitado a máscara do <see cref="DetectorDeInteracao"/>.
    ///
    /// <para><b>O que mudou e por quê (2026-08-27).</b> O detector varria <b>as 32 camadas</b>,
    /// com o argumento de que "é melhor achar demais, porque o filtro real é ter
    /// <c>IInteragivel</c>". O argumento ignora que o buffer tem tamanho fixo:
    /// <c>Physics2D.OverlapCircle</c> enche 8 slots e <b>descarta o resto em ordem
    /// arbitrária</b>. Os dois colisores do próprio Damião entram sempre, cada inimigo por perto
    /// gasta mais slots, a parede gasta um, o gatilho de setor gasta outro — e perto de um baú
    /// com inimigos em volta, <b>o baú é o que sobra de fora</b>. O sintoma é "às vezes o E não
    /// faz nada", que é indistinguível de estar longe demais.</para>
    ///
    /// <para><b>Mas máscara estreita tem o defeito oposto</b>, e é o mais caro desta casa: um
    /// interagível numa camada de fora não dá erro, não loga, e simplesmente <b>nunca é
    /// encontrado</b>. Este teste é o que paga esse preço.</para>
    ///
    /// <para><b>E ele já rendeu antes de existir.</b> A medição que definiu a lista mostrou que
    /// nem tudo está na <c>Default</c>: <c>Os_Portoes</c> está na <c>Obstacle</c> (é parede e
    /// porta ao mesmo tempo) e o <b>Abdul</b> está na <c>Enemy</c> — ele é
    /// <c>IInteragivel</c> porque se conversa com ele antes de lutar. Uma máscara "só Default",
    /// que era o palpite óbvio, teria deixado o chefe da Tumba <b>mudo</b>.</para>
    ///
    /// <para><b>Limite conhecido:</b> a varredura lê a camada <b>autorada</b> no documento que
    /// declara o componente (a cena, ou o prefab). Um override de camada feito numa instância de
    /// prefab dentro da cena não é resolvido aqui.</para>
    /// </summary>
    public sealed class InteragivelAlcancavelTests
    {
        [Test]
        public void TodaCamadaDoDetector_Existe()
        {
            var inexistentes = DetectorDeInteracao.CamadasPadraoDeInteragiveis
                .Where(n => LayerMask.NameToLayer(n) < 0)
                .ToList();

            Assert.IsEmpty(inexistentes,
                "Camada(s) na lista do DetectorDeInteracao que não existem no TagManager: " +
                string.Join(", ", inexistentes) + ". LayerMask.GetMask ignora nome " +
                "desconhecido em silêncio, então a máscara sai menor do que o código diz.");
        }

        [Test]
        public void TodoInteragivelAutorado_EstaNumaCamadaQueODetectorVarre()
        {
            int mascara = LayerMask.GetMask(DetectorDeInteracao.CamadasPadraoDeInteragiveis);
            Assert.AreNotEqual(0, mascara, "A máscara padrão de interação ficou vazia.");

            var guids = GuidsDosInteragiveis();
            Assert.IsNotEmpty(guids,
                "Nenhum script IInteragivel foi encontrado. Este guarda parou de olhar para o " +
                "jogo — provavelmente a interface mudou de nome ou de assembly.");

            var invisiveis = new List<string>();
            var achados = 0;

            foreach (var arquivo in ArquivosAutorados())
            {
                string yaml = File.ReadAllText(arquivo);
                if (!guids.Keys.Any(yaml.Contains)) continue;

                foreach (var (script, dono, camada) in Componentes(yaml, guids))
                {
                    achados++;
                    if (camada < 0) continue;   // dono não resolvido no mesmo documento

                    if ((mascara & (1 << camada)) == 0)
                        invisiveis.Add($"{Path.GetFileName(arquivo)} · {script} em '{dono}' " +
                                       $"está na camada {camada} " +
                                       $"({LayerMask.LayerToName(camada)})");
                }
            }

            Assert.Greater(achados, 0,
                "Nenhuma instância de IInteragivel foi encontrada em cena ou prefab nenhum. " +
                "Ou o jogo perdeu todos os interagíveis, ou esta varredura quebrou.");

            Assert.IsEmpty(invisiveis,
                "Interagível(is) fora do alcance do detector:" + Environment.NewLine + "  " +
                string.Join(Environment.NewLine + "  ", invisiveis) + Environment.NewLine +
                "O detector nunca vai encontrá-los: o prompt não aparece e o E não faz nada, " +
                "SEM ERRO NENHUM. Conserto: pôr o objeto numa camada varrida, ou acrescentar a " +
                "camada em DetectorDeInteracao.CamadasPadraoDeInteragiveis.");
        }

        // ── Apoio ─────────────────────────────────────────────────────────────

        /// <summary>
        /// GUID do <c>MonoScript</c> → nome da classe, para cada implementação de
        /// <c>IInteragivel</c>. Sai do <c>TypeCache</c>, não de uma lista: implementação nova
        /// entra sozinha.
        /// </summary>
        private static Dictionary<string, string> GuidsDosInteragiveis()
        {
            var porGuid = new Dictionary<string, string>();

            foreach (var tipo in TypeCache.GetTypesDerivedFrom<IInteragivel>())
            {
                if (tipo.IsAbstract || tipo.IsInterface) continue;

                // A Unity exige nome de arquivo == nome da classe para MonoBehaviour, então o
                // MonoScript é achável pelo nome do tipo.
                foreach (var guid in AssetDatabase.FindAssets($"{tipo.Name} t:MonoScript"))
                {
                    string caminho = AssetDatabase.GUIDToAssetPath(guid);
                    if (Path.GetFileNameWithoutExtension(caminho) != tipo.Name) continue;

                    var script = AssetDatabase.LoadAssetAtPath<MonoScript>(caminho);
                    if (script != null && script.GetClass() == tipo) porGuid[guid] = tipo.Name;
                }
            }

            return porGuid;
        }

        private static IEnumerable<string> ArquivosAutorados()
        {
            foreach (var c in Directory.GetFiles("Assets", "*.unity", SearchOption.AllDirectories))
                yield return c;

            foreach (var c in Directory.GetFiles("Assets", "*.prefab", SearchOption.AllDirectories))
                yield return c;
        }

        /// <summary>
        /// Cada <c>MonoBehaviour</c> do arquivo cujo script está no dicionário, com o nome e a
        /// camada do <c>GameObject</c> dono. Camada <c>-1</c> = dono não declarado neste
        /// documento (instância de prefab).
        /// </summary>
        private static IEnumerable<(string Script, string Dono, int Camada)> Componentes(
            string yaml, Dictionary<string, string> guids)
        {
            var marcadores = Regex.Matches(yaml, @"^--- !u!\d+ &(-?\d+).*$", RegexOptions.Multiline);
            var corpos = new List<(string Id, string Corpo)>();

            for (int i = 0; i < marcadores.Count; i++)
            {
                int inicio = marcadores[i].Index + marcadores[i].Length;
                int fim = i + 1 < marcadores.Count ? marcadores[i + 1].Index : yaml.Length;
                corpos.Add((marcadores[i].Groups[1].Value, yaml.Substring(inicio, fim - inicio)));
            }

            var objetos = corpos
                .Where(c => Regex.IsMatch(c.Corpo, @"^GameObject:\s*$", RegexOptions.Multiline))
                .ToDictionary(c => c.Id, c => c.Corpo);

            foreach (var (_, corpo) in corpos)
            {
                var script = Regex.Match(corpo, @"m_Script:\s*\{fileID:\s*11500000,\s*guid:\s*(\w+)");
                if (!script.Success || !guids.TryGetValue(script.Groups[1].Value, out string nome))
                    continue;

                var dono = Regex.Match(corpo, @"m_GameObject:\s*\{fileID:\s*(-?\d+)\}");
                if (!dono.Success || !objetos.TryGetValue(dono.Groups[1].Value, out string go))
                {
                    yield return (nome, "(fora deste documento)", -1);
                    continue;
                }

                var apelido = Regex.Match(go, @"^  m_Name:\s*(.*)$", RegexOptions.Multiline);
                var camada = Regex.Match(go, @"^  m_Layer:\s*(\d+)\s*$", RegexOptions.Multiline);

                yield return (nome,
                              apelido.Success ? apelido.Groups[1].Value.Trim() : "?",
                              camada.Success ? int.Parse(camada.Groups[1].Value) : -1);
            }
        }
    }
}
