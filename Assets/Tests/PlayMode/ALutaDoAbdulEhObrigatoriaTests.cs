using System.Collections;
using FavelaAmarela.Runtime.Enemies;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace FavelaAmarela.Tests.PlayMode
{
    /// <summary>
    /// A conversa com o Abdul <b>termina em luta</b> — sempre.
    ///
    /// <para><b>Por que (decisão do Vini, 2026-09-28).</b> A escolha "Concordar — poupar Abdul"
    /// libertava o Yug-Neth sem entregar o Necronomicon, e o Rei em Amarelo não se sela sem ele:
    /// a escolha pacífica levava o jogador a um final impossível. A trégua saiu; a conversa acaba
    /// com o Damião ofendendo o Abdul, e o aperto seguinte desperta a luta.</para>
    ///
    /// <para>Instancia o <b>prefab real</b>: as falas moram serializadas nele, e um default de C#
    /// não vale nada se o prefab guardar a lista antiga (o modo de falha do escudo que não
    /// equipava).</para>
    /// </summary>
    public sealed class ALutaDoAbdulEhObrigatoriaTests
    {
        private const string Prefab = "Assets/FavelaAmarela/Art/Enemies/Abdul_Alhazred.prefab";
        private GameObject _abdul;

        [UnityTearDown]
        public IEnumerator Desmontar()
        {
            if (_abdul != null) Object.Destroy(_abdul);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ApertarAteOFimDaConversa_DespertaALuta()
        {
#if UNITY_EDITOR
            var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(Prefab);
#else
            GameObject prefab = null;
#endif
            Assert.NotNull(prefab, $"Prefab do Abdul ausente: {Prefab}");

            // Log de erro de peças de cena ausentes (arena, Yug-Neth) não interessa aqui.
            LogAssert.ignoreFailingMessages = true;
            _abdul = Object.Instantiate(prefab);
            yield return null;

            var abdul = _abdul.GetComponent<AbdulAlhazredAI>();
            Assert.NotNull(abdul, "O prefab perdeu o AbdulAlhazredAI.");
            Assert.IsTrue(abdul.PodeInteragir, "O Abdul devia nascer em Transe, esperando a conversa.");

            int falas = AbdulAlhazredAI.FalasPadrao.Length;
            for (int i = 0; i < falas; i++)
            {
                Assert.IsTrue(abdul.PodeInteragir,
                    $"A conversa acabou antes da hora: na fala {i + 1} de {falas} o Abdul já não aceita interação.");
                abdul.Interagir(null);
                yield return null;
            }

            Assert.IsTrue(abdul.PodeInteragir,
                "Depois da última fala o Abdul ainda precisa aceitar o aperto que desperta a luta.");

            abdul.Interagir(null);
            yield return null;
            LogAssert.ignoreFailingMessages = false;

            Assert.IsFalse(abdul.PodeInteragir,
                "O aperto depois da última fala não despertou a luta: o Abdul continua em Transe. " +
                "Sem luta não há Necronomicon, e sem Necronomicon o Rei não se sela.");
        }
    }
}
