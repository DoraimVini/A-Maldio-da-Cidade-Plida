using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace FavelaAmarela.Tests.PlayMode
{
    /// <summary>
    /// Guarda que <b>ligar o jogo não produz aviso nem erro nosso</b>.
    ///
    /// <para><b>O que motivou (2026-09-02).</b> O Vini mandou o log de uma sessão e ele tinha
    /// cinco avisos <b>na inicialização normal</b> — três deles escritos por mim na madrugada
    /// anterior. Eu havia trocado <c>LogError</c> por <c>LogWarning</c> em campos que
    /// <b>nascem vazios por construção</b> (o HUD é um prefab-asset e não referencia objeto de
    /// cena; quem preenche é o <c>GameLoopBootstrap</c>, depois do <c>Awake</c>).</para>
    ///
    /// <para><b>Aviso no caso normal tem a mesma doença do erro no caso normal:</b> ensina a
    /// ignorar o log. E o log é o <b>único canal de runtime</b> que este projeto tem — foi a
    /// descoberta da mesma auditoria que eu nunca tinha aberto um <c>Editor.log</c>. Poluí-lo é
    /// destruir a ferramenta que acabei de adotar.</para>
    ///
    /// <para>Só olha as mensagens com <b>marcador do projeto</b> (<c>[Assim]</c>): ruído da
    /// própria Unity ou de pacote não é nosso para consertar.</para>
    /// </summary>
    public sealed class NasceEmSilencioTests
    {
        private readonly List<string> _reclamacoes = new List<string>();
        private GameObject _hud;

        [SetUp]
        public void SetUp()
        {
            // Eu faço a contabilidade; o framework não deve derrubar o teste por mim.
            LogAssert.ignoreFailingMessages = true;

            _reclamacoes.Clear();
            Application.logMessageReceived += Registrar;
        }

        [TearDown]
        public void TearDown()
        {
            Application.logMessageReceived -= Registrar;

            if (_hud != null) Object.DestroyImmediate(_hud);
            LogAssert.ignoreFailingMessages = false;
        }

        private void Registrar(string mensagem, string pilha, LogType tipo)
        {
            if (tipo != LogType.Warning && tipo != LogType.Error && tipo != LogType.Exception)
                return;

            // Marcador do projeto: "[Alguma Coisa] ...". Sem ele, não é nosso.
            if (!mensagem.StartsWith("[")) return;

            _reclamacoes.Add($"[{tipo}] {mensagem.Split('\n')[0]}");
        }

        [UnityTest]
        public IEnumerator OHudNasceSemReclamar()
        {
            FavelaAmarela.Runtime.UI.HUDController.GarantirInstancia();
            yield return null;
            yield return null;

            var controlador = FavelaAmarela.Runtime.UI.HUDController.Instancia;
            Assert.IsNotNull(controlador, "O HUD não nasceu.");
            _hud = controlador.gameObject;

            Assert.IsEmpty(_reclamacoes,
                $"Ligar o HUD produziu {_reclamacoes.Count} reclamação(ões) nossa(s):" +
                System.Environment.NewLine + "  " +
                string.Join(System.Environment.NewLine + "  ", _reclamacoes.Distinct()) +
                System.Environment.NewLine +
                "Campo que nasce vazio POR CONSTRUÇÃO não é defeito — o HUD é um prefab-asset e " +
                "não referencia objeto de cena. Avise onde a falta DÓI (no uso), não onde o " +
                "campo está vazio. Aviso no caso normal ensina a ignorar o log, que é o único " +
                "canal de runtime que temos.");
        }
    }
}
