using System.IO;
using System.Linq;
using FavelaAmarela.Core.Camera;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FavelaAmarela.Tests.EditMode
{
    /// <summary>
    /// Guarda o canal de tremores do <b>mundo</b> e o acabamento de câmera que veio junto.
    ///
    /// <para><b>Por que o canal existe.</b> O <c>AcrescentarTrauma</c> da câmera era público e
    /// <b>não tinha um chamador sequer</b>. Golpe já sacota a tela por <c>HitStop.OnImpacto</c>;
    /// faltava a porta do que não é golpe — chefe pousando, portão batendo, chão desmoronando.
    /// As duas alternativas óbvias falhavam: campo serializado num prefab não referencia objeto
    /// de cena, e <c>FindObjectOfType</c> é proibido em produção.</para>
    /// </summary>
    public sealed class TremorDoMundoTests
    {
        [Test]
        public void Sacudir_ChegaAQuemAssina()
        {
            float recebido = 0f;
            System.Action<float> ouvinte = t => recebido = t;

            TremorDoMundo.OnTremor += ouvinte;
            try { TremorDoMundo.Sacudir(0.5f); }
            finally { TremorDoMundo.OnTremor -= ouvinte; }

            Assert.AreEqual(0.5f, recebido, 0.0001f);
        }

        /// <summary>
        /// Trauma zero ou negativo não dispara nada. Sem esta guarda, um evento que calcula o
        /// próprio trauma e chega a zero acordaria a câmera à toa a cada quadro.
        /// </summary>
        [TestCase(0f)]
        [TestCase(-1f)]
        public void SacudirComTraumaNaoPositivo_NaoDisparaNada(float trauma)
        {
            int vezes = 0;
            System.Action<float> ouvinte = _ => vezes++;

            TremorDoMundo.OnTremor += ouvinte;
            try { TremorDoMundo.Sacudir(trauma); }
            finally { TremorDoMundo.OnTremor -= ouvinte; }

            Assert.AreEqual(0, vezes);
        }

        /// <summary>
        /// Sem assinante, sacudir é inofensivo — e precisa ser: a Cena_Menu não tem câmera de
        /// gameplay, e um <c>NullReferenceException</c> ali seria um crash por causa de um
        /// efeito visual.
        /// </summary>
        [Test]
        public void SemAssinante_SacudirNaoExplode()
        {
            Assert.DoesNotThrow(() => TremorDoMundo.Sacudir(1f));
        }

        // ── acabamento de câmera ─────────────────────────────────────────────

        /// <summary>
        /// Toda câmera de gameplay limpa com <b>cor sólida</b>.
        ///
        /// <para><b>Por que isto virou guarda (2026-09-09).</b> Três das cinco estavam em
        /// <c>Skybox</c> <i>sem material de skybox atribuído</i>, num jogo 2D ortográfico — e a
        /// <c>m_BackGroundColor</c> cuidadosamente escolhida de cada cena (o quase-preto
        /// 0,04/0,03/0,05 do Castelo) só governa com certeza em <c>SolidColor</c>. Ter duas
        /// cenas num modo e três no outro não foi decisão de ninguém.</para>
        /// </summary>
        [Test]
        public void AsCamerasDeGameplay_LimpamComCorSolida()
        {
            var fora = EditorBuildSettings.scenes
                .Where(e => e.enabled && File.Exists(e.path))
                .SelectMany(e =>
                {
                    Scene c = EditorSceneManager.OpenScene(e.path, OpenSceneMode.Single);
                    string nome = Path.GetFileNameWithoutExtension(e.path);

                    return c.GetRootGameObjects()
                        .SelectMany(g => g.GetComponentsInChildren<Camera>(true))
                        .Where(cam => cam.orthographic)
                        .Where(cam => cam.clearFlags != CameraClearFlags.SolidColor)
                        .Select(cam => $"{nome}/{cam.name} = {cam.clearFlags}");
                })
                .ToArray();

            Assert.IsEmpty(fora,
                "Câmeras de gameplay fora de SolidColor: " + string.Join(", ", fora) +
                ". Num 2D ortográfico o fundo é a cor autorada da cena, e ela só governa com " +
                "certeza nesse modo.");
        }

        /// <summary>
        /// O arquivo do controlador de câmera precisa ter o nome da classe.
        ///
        /// <para>Funcionava por GUID — a cena referencia o <c>.meta</c>, não o nome —, mas
        /// arquivo com nome diferente da classe é a espécie de divergência que custa uma busca
        /// frustrada a cada vez. Renomeado em 2026-09-09 com o <c>.meta</c> junto, que é o que
        /// preserva o GUID e mantém as seis cenas ligadas.</para>
        /// </summary>
        [Test]
        public void OArquivoDaCamera_TemONomeDaClasse()
        {
            Assert.IsTrue(File.Exists("Assets/Scripts/Camera/IsometricCameraController.cs"),
                "IsometricCameraController.cs não está onde deveria.");

            Assert.IsFalse(File.Exists("Assets/Scripts/Camera/CameraController.cs"),
                "O nome antigo voltou — dois arquivos para a mesma classe é pior que um com o " +
                "nome errado.");
        }
    }
}
