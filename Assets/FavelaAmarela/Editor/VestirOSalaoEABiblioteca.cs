using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FavelaAmarela.EditorTools
{
    /// <summary>
    /// Dá arte aos <b>6 Nobres Fossilizados</b> (Z2, Salão do Banquete) e aos <b>3 Espelhos de
    /// Aldebaran</b> (Z3, Biblioteca) do Castelo de Carcosa.
    ///
    /// <para><b>O que isto conserta.</b> Medido em 2026-09-09 resolvendo cada <c>m_Sprite</c>
    /// por GUID: os nove desenhavam o <b>sprite embutido da Unity</b>
    /// (<c>fileID: 10905</c>, GUID de recursos internos) e <b>nenhum</b> tinha componente que
    /// trocasse isso em runtime. Nove quadrados brancos na última fase do jogo.</para>
    ///
    /// <para><b>Cuidado para não repetir o erro da medição anterior:</b> os 3 Pontos Focais e o
    /// Refúgio da Z1 <i>parecem</i> iguais no YAML e <b>não são</b> —
    /// <c>PontoFocalDeReliquia</c> e <c>RefugioDeLuz</c> escrevem o sprite no <c>Awake</c>. Por
    /// isso esta ferramenta mexe só nos nove que não têm ninguém escrevendo por eles.</para>
    ///
    /// <para><b>Y-sorting escrito uma vez, sem componente.</b> Ganhar arte cria o problema que
    /// o quadrado branco escondia: um vulto de 1,75 unidade que o jogador contorna precisa ser
    /// desenhado atrás dele quando está atrás, e na frente quando está na frente. O
    /// <c>DynamicYSort</c> resolveria, mas ele roda em <c>LateUpdate</c> — e estes nove
    /// <b>não se movem</b>. Gravar o <c>sortingOrder</c> uma vez, com o mesmo fator
    /// <c>-y × 10</c> que o <c>LevelBlockoutGenerator</c> usa para a geometria estática, dá o
    /// mesmo resultado sem nove <c>LateUpdate</c> por quadro.</para>
    ///
    /// <para><b>Três poses para seis Nobres:</b> as instâncias 0–5 recebem os quadros 0,1,2,0,1,2.
    /// Seis desenhos únicos custariam muito mais do que o Salão devolve.</para>
    ///
    /// <para><b>Idempotente:</b> reescreve sprite e ordem. Rodar duas vezes não acumula nada.</para>
    /// </summary>
    public static class VestirOSalaoEABiblioteca
    {
        private const string PastaDaArte = "Assets/FavelaAmarela/Art/Environment/CasteloCarcosa";

        /// <summary>Mesmo fator do <c>LevelBlockoutGenerator</c> e do <c>DynamicYSort</c>.</summary>
        private const float FatorDeProfundidade = 10f;

        [MenuItem("Tools/FavelaAmarela/Cena: vestir o Salão e a Biblioteca do Castelo")]
        public static void Executar()
        {
            if (!Application.isBatchMode &&
                !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                Debug.Log("[VestirCastelo] Cancelado — havia cena modificada por salvar.");
                return;
            }

            var nobres = Carregar("Nobre_Fossilizado", 3);
            var espelhos = Carregar("Espelho_De_Aldebaran", 3);

            if (nobres == null || espelhos == null) return;

            var log = new StringBuilder("[VestirCastelo]\n");
            int vestidos = 0;

            foreach (var entrada in EditorBuildSettings.scenes)
            {
                if (!entrada.enabled || !File.Exists(entrada.path)) continue;

                Scene cena = EditorSceneManager.OpenScene(entrada.path, OpenSceneMode.Single);
                string nomeDaCena = Path.GetFileNameWithoutExtension(entrada.path);
                bool mexeu = false;

                foreach (var t in cena.GetRootGameObjects()
                             .SelectMany(r => r.GetComponentsInChildren<Transform>(true)))
                {
                    Sprite[] banco =
                        t.name.StartsWith("Nobre_Fossilizado") ? nobres :
                        t.name.StartsWith("Espelho_De_Aldebaran") ? espelhos : null;

                    if (banco == null) continue;

                    var sr = t.GetComponent<SpriteRenderer>();
                    if (sr == null)
                    {
                        Debug.LogWarning($"[VestirCastelo] '{t.name}' não tem SpriteRenderer — " +
                                         "pulado. Ele não desenha nada, com ou sem arte.", t);
                        continue;
                    }

                    int indice = IndiceDoNome(t.name) % banco.Length;

                    Undo.RecordObject(sr, "Vestir o Castelo");
                    sr.sprite = banco[indice];
                    sr.sortingOrder = Mathf.RoundToInt(-t.position.y * FatorDeProfundidade);
                    EditorUtility.SetDirty(sr);

                    log.AppendLine($"   {nomeDaCena} / {t.name}  <- {banco[indice].name}" +
                                   $"   ordem {sr.sortingOrder}");
                    vestidos++;
                    mexeu = true;
                }

                if (!mexeu) continue;

                EditorSceneManager.MarkSceneDirty(cena);
                EditorSceneManager.SaveScene(cena);
            }

            log.AppendLine($"   total: {vestidos} objeto(s)");

            if (vestidos == 0)
                log.AppendLine("   NENHUM encontrado — ou foram renomeados, ou saíram da cena.");

            Debug.Log(log.ToString());
        }

        /// <summary>Último número do nome (<c>Nobre_Fossilizado_4</c> → 4). Zero se não houver.</summary>
        private static int IndiceDoNome(string nome)
        {
            int corte = nome.LastIndexOf('_');
            return corte >= 0 && int.TryParse(nome.Substring(corte + 1), out int n) ? n : 0;
        }

        private static Sprite[] Carregar(string prefixo, int quantos)
        {
            var sprites = Enumerable.Range(0, quantos)
                .Select(i => AssetDatabase.LoadAssetAtPath<Sprite>($"{PastaDaArte}/{prefixo}_{i}.png"))
                .ToArray();

            if (!sprites.Any(s => s == null)) return sprites;

            Debug.LogError($"[VestirCastelo] Falta quadro de '{prefixo}' em '{PastaDaArte}'. " +
                           $"Esperados {prefixo}_0..{quantos - 1}.png. Sem eles a ferramenta " +
                           "ligaria referências vazias, que é o mesmo que não rodar — só que " +
                           "em silêncio.");
            return null;
        }
    }
}
