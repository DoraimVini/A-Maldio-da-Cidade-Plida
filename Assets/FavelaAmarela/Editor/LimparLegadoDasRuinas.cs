using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace FavelaAmarela.EditorTools
{
    /// <summary>
    /// Utilitário de Editor: remove da cena aberta a lógica herdada das **Ruínas Pálidas**
    /// que não faz sentido dentro da Tumba de Alhazred, agora que ela é uma **dungeon
    /// única e fechada** (decisão do Vini, 2026-07-30).
    ///
    /// <para>O que sai e por quê:</para>
    /// <list type="bullet">
    ///   <item><b>Tempestade de areia</b> — é fenômeno do deserto a céu aberto; não ocorre dentro de uma cripta.</item>
    ///   <item><b>Queda Z4→Z5 e o Cerco</b> — pressupunham a travessia entre zonas das Ruínas; a dungeon não tem esse trecho.</item>
    /// </list>
    ///
    /// <para><b>Não</b> mexe em: Cultistas, baú, Abdul, Pedras de Poder, HUD, GameManager,
    /// câmera, colapso (morte) e o chão/colisão isométricos. Também <b>não</b> toca no
    /// patuá — ele foi revisto e ganhará outro propósito.</para>
    ///
    /// <para>Só desativa o que encontra? Não: <b>destrói</b>, porque é remoção definitiva de
    /// escopo. Faça commit antes se quiser um ponto de retorno. O relatório no Console
    /// lista exatamente o que saiu.</para>
    /// </summary>
    public static class LimparLegadoDasRuinas
    {
        /// <summary>
        /// Componentes cuja presença marca um objeto como legado das Ruínas. Resolvidos por
        /// nome de tipo (padrão dos outros builders desta pasta) para não acoplar o Editor
        /// aos assemblies de Runtime.
        /// </summary>
        private static readonly string[] TiposALimpar =
        {
            // Tempestade de areia — não existe dentro da cripta.
            "FavelaAmarela.Runtime.Environment.TempestadeAmbiente",
            "FavelaAmarela.Runtime.Environment.TempestadeRajadaAleatoria",
            "FavelaAmarela.Runtime.UI.TempestadeVisualOverlay",
            "FavelaAmarela.Runtime.GameLoop.TempestadeZonaTrigger",

            // Travessia entre zonas das Ruínas — a dungeon é uma coisa só.
            "FavelaAmarela.Runtime.GameLoop.QuedaZ4Z5Trigger",
            "FavelaAmarela.Runtime.GameLoop.CercoZ4Cutscene",
        };

        [MenuItem("Tools/FavelaAmarela/Limpar Legado das Ruínas (cena aberta)")]
        public static void Limpar()
        {
            var alvos = new List<GameObject>();
            var relatorio = new StringBuilder();

            foreach (var nomeTipo in TiposALimpar)
            {
                var tipo = ResolverTipo(nomeTipo);
                if (tipo == null)
                {
                    relatorio.AppendLine($"  · {Curto(nomeTipo)}: tipo não encontrado (script removido?)");
                    continue;
                }

                var encontrados = Object.FindObjectsByType(tipo, FindObjectsInactive.Include)
                    .OfType<Component>()
                    .Select(c => c.gameObject)
                    .Distinct()
                    .ToList();

                if (encontrados.Count == 0)
                {
                    relatorio.AppendLine($"  · {Curto(nomeTipo)}: nada na cena");
                    continue;
                }

                foreach (var go in encontrados)
                {
                    relatorio.AppendLine($"  · {Curto(nomeTipo)}: removendo '{CaminhoNaHierarquia(go)}'");
                    if (!alvos.Contains(go)) alvos.Add(go);
                }
            }

            if (alvos.Count == 0)
            {
                Debug.Log("[LimparRuinas] Nada a remover — a cena já está limpa.\n" + relatorio);
                return;
            }

            foreach (var go in alvos)
                Object.DestroyImmediate(go);

            var cena = EditorSceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(cena);

            Debug.Log($"[LimparRuinas] {alvos.Count} objeto(s) removido(s) da cena '{cena.name}'.\n" +
                      relatorio +
                      "\nPreservados de propósito: Cultistas, baú, Abdul, Pedras de Poder, HUD, " +
                      "GameManager, câmera, colapso, patuá e o chão/colisão isométricos.\n" +
                      "A cena NÃO foi salva — confira no Editor antes de salvar.");
        }

        private static string Curto(string nomeCompleto)
        {
            int i = nomeCompleto.LastIndexOf('.');
            return i >= 0 ? nomeCompleto.Substring(i + 1) : nomeCompleto;
        }

        private static string CaminhoNaHierarquia(GameObject go)
        {
            var partes = new List<string>();
            for (var t = go.transform; t != null; t = t.parent) partes.Insert(0, t.name);
            return string.Join("/", partes);
        }

        private static System.Type ResolverTipo(string nomeCompleto)
            => System.AppDomain.CurrentDomain.GetAssemblies()
                .Select(a => a.GetType(nomeCompleto))
                .FirstOrDefault(t => t != null);
    }
}
