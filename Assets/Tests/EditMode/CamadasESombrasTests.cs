using System.IO;
using System.Linq;
using FavelaAmarela.Runtime.Rendering;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace FavelaAmarela.Tests.EditMode
{
    /// <summary>
    /// Guarda as camadas de ordenação e as sombras de chão.
    ///
    /// <para><b>O que isto protege (2026-09-09).</b> O jogo inteiro vivia numa única sorting
    /// layer, com o chão em <c>sortingOrder −1000</c> escolhido à mão e os atores em
    /// <c>−y × 10</c>. O Castelo já alcançava <b>−710</b> — 290 de folga antes de um ator
    /// começar a desenhar atrás do chão e sumir da tela, sem erro nenhum. E este projeto já
    /// dobrou um mapa de tamanho uma vez.</para>
    /// </summary>
    public sealed class CamadasESombrasTests
    {
        /// <summary>Da mais atrás para a mais à frente.</summary>
        private static readonly string[] Ordem = { "Fundo", "Chao", "Default", "Frente" };

        [Test]
        public void AsCamadasExistem_NaOrdemDeDesenho()
        {
            var nomes = SortingLayer.layers.Select(l => l.name).ToArray();

            CollectionAssert.AreEqual(Ordem, nomes,
                "A ordem das sorting layers É a ordem de desenho — trocá-la reordena o jogo " +
                "inteiro.\n  esperado: " + string.Join(", ", Ordem) +
                "\n  medido:   " + string.Join(", ", nomes));
        }

        /// <summary>
        /// <c>Default</c> precisa manter o <c>uniqueID</c> 0. Todo <c>SpriteRenderer</c> já
        /// gravado nas cenas referencia esse número; mudá-lo realocaria o projeto inteiro.
        /// </summary>
        [Test]
        public void ACamadaDefault_MantemOIdZero()
        {
            var padrao = SortingLayer.layers.FirstOrDefault(l => l.name == "Default");

            Assert.AreEqual(0, padrao.id,
                "O uniqueID da camada Default mudou. Os 78 renderers das cenas apontam para 0.");
        }

        [Test]
        public void OChaoFicaNaCamadaChao_ForaDaDisputaComOsAtores()
        {
            var fora = EditorBuildSettings.scenes
                .Where(e => e.enabled && File.Exists(e.path))
                .SelectMany(e =>
                {
                    Scene c = EditorSceneManager.OpenScene(e.path, OpenSceneMode.Single);
                    string nome = Path.GetFileNameWithoutExtension(e.path);

                    return c.GetRootGameObjects()
                        .SelectMany(g => g.GetComponentsInChildren<TilemapRenderer>(true))
                        .Where(t => t.sortingLayerName == "Default" && t.sortingOrder < -500)
                        .Select(t => nome + "/" + t.name);
                })
                .ToArray();

            Assert.IsEmpty(fora,
                "Estes tilemaps de chão continuam na camada Default, com a profundidade " +
                "dependendo de um sortingOrder muito negativo: " + string.Join(", ", fora));
        }

        /// <summary>
        /// Os atores que pisam no chão precisam ter sombra — e ela precisa ter sprite. Um
        /// <c>SombraDeChao</c> sem sprite se desliga sozinho no <c>Awake</c> e não desenha nada,
        /// que é o modo de falha silencioso deste projeto.
        /// </summary>
        [TestCase("Player_Damiao")]
        [TestCase("Cassilda")]
        [TestCase("YugNeth")]
        [TestCase("Abdul_Alhazred")]
        [TestCase("Byakhee")]
        [TestCase("CoisaDoCemiterio")]
        [TestCase("Cultista")]
        [TestCase("EsqueletoInvocado")]
        [TestCase("ReiEmAmarelo")]
        public void OAtorTemSombraComSprite(string prefab)
        {
            string caminho = AssetDatabase.FindAssets("t:Prefab " + prefab)
                .Select(AssetDatabase.GUIDToAssetPath)
                .FirstOrDefault(p => Path.GetFileNameWithoutExtension(p) == prefab);

            Assert.NotNull(caminho, $"Prefab {prefab} não encontrado.");

            var raiz = AssetDatabase.LoadAssetAtPath<GameObject>(caminho);
            var sombra = raiz.GetComponent<SombraDeChao>();

            Assert.NotNull(sombra, $"{prefab} não tem SombraDeChao. Sem ela o sprite flutua — " +
                                   "nada liga a arte ao solo.");

            var so = new SerializedObject(sombra);

            Assert.NotNull(so.FindProperty("sprite").objectReferenceValue,
                $"{prefab} tem SombraDeChao SEM sprite. O componente se desliga sozinho no " +
                "Awake e não desenha nada — existe e não está ligado.");
        }

        /// <summary>
        /// O Espectro de Hali fica <b>de fora</b> de propósito: espectro que projeta sombra
        /// sólida contradiz a ficção. Se alguém puser, que seja por decisão e não por varredura.
        /// </summary>
        [Test]
        public void OEspectro_NaoProjetaSombra()
        {
            string caminho = AssetDatabase.FindAssets("t:Prefab EspectroHali")
                .Select(AssetDatabase.GUIDToAssetPath)
                .FirstOrDefault(p => Path.GetFileNameWithoutExtension(p) == "EspectroHali");

            if (caminho == null) Assert.Ignore("EspectroHali não existe mais.");

            var raiz = AssetDatabase.LoadAssetAtPath<GameObject>(caminho);

            Assert.IsNull(raiz.GetComponent<SombraDeChao>(),
                "O Espectro de Hali ganhou sombra sólida. É uma decisão de ficção, não de " +
                "componente — se foi deliberada, tire este teste.");
        }
    }
}
