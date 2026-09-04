using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace FavelaAmarela.Tests.EditMode
{
    /// <summary>
    /// Guarda que o <c>VisualizadorDeGolpes</c> <b>funciona fora do Play</b>.
    ///
    /// <para><b>Por que este teste existe.</b> O componente ganhou suporte a Edit mode em
    /// 2026-09-03, e as duas suítes ficaram verdes sem exercitar uma linha do caminho novo —
    /// <c>[ExecuteAlways]</c>, o relógio do Editor e a poda no desenho não são tocados por
    /// nenhum outro teste. Afirmar "funciona nos dois modos" com base em suíte verde seria
    /// exatamente o tipo de conclusão que este projeto já pagou caro para aprender a não
    /// tirar.</para>
    ///
    /// <para><b>O que ele mede, e o que não mede.</b> Mede o que roda sem Play: registro,
    /// armazenamento e o relógio. <b>Não</b> mede o desenho — <c>OnDrawGizmos</c> é chamado
    /// pelo Editor durante o repaint do Scene view, e um teste não tem como forçá-lo nem como
    /// ler o que foi desenhado. Essa parte continua sendo verificação a olho.</para>
    ///
    /// <para>Um teste EditMode roda com <c>UNITY_EDITOR</c> definido, então o
    /// <c>ConditionalAttribute</c> dos métodos de registro <b>não</b> apaga as chamadas daqui —
    /// que é justamente o que permite medi-los.</para>
    /// </summary>
    public sealed class VisualizadorForaDoPlayTests
    {
        private const string Tipo =
            "FavelaAmarela.Runtime.Diagnostico.VisualizadorDeGolpes";

        private static System.Type Alvo()
        {
            var t = System.Type.GetType(Tipo + ", FavelaAmarela.Runtime")
                    ?? System.AppDomain.CurrentDomain.GetAssemblies()
                        .Select(a => a.GetType(Tipo))
                        .FirstOrDefault(x => x != null);

            Assert.IsNotNull(t, $"Não achei o tipo {Tipo}. Ele foi renomeado ou saiu do assembly.");
            return t;
        }

        /// <summary>A lista privada de marcas, lida por reflexão para não criar API só de teste.</summary>
        private static IList Marcas()
        {
            var campo = Alvo().GetField("_marcas",
                BindingFlags.NonPublic | BindingFlags.Static);

            Assert.IsNotNull(campo,
                "O campo estático '_marcas' sumiu. Este teste lê a lista por reflexão de " +
                "propósito: expor um contador público só para o teste criaria superfície que a " +
                "auditoria de ligação acusaria como sem chamador no jogo.");

            return (IList)campo.GetValue(null);
        }

        private static void DefinirMostrar(bool valor)
        {
            var p = Alvo().GetField("Mostrar", BindingFlags.Public | BindingFlags.Static);
            Assert.IsNotNull(p, "O campo estático público 'Mostrar' sumiu.");
            p.SetValue(null, valor);
        }

        [SetUp]
        public void SetUp()
        {
            Marcas().Clear();
            DefinirMostrar(false);
        }

        [TearDown]
        public void TearDown()
        {
            Marcas().Clear();
            DefinirMostrar(false);
        }

        [Test]
        public void ForaDoPlay_ORelogioNaoEhOTimeTime()
        {
            Assert.IsFalse(Application.isPlaying,
                "Este teste só faz sentido fora do Play — se está rodando em PlayMode, ele foi " +
                "posto na pasta errada.");

            var prop = Alvo().GetProperty("Agora",
                BindingFlags.NonPublic | BindingFlags.Static);

            Assert.IsNotNull(prop,
                "A propriedade 'Agora' sumiu. É ela que troca Time.time pelo relógio do Editor " +
                "fora do Play — sem ela, uma marca registrada em Edit mode nunca expira ou " +
                "expira no mesmo quadro.");

            float agora = (float)prop.GetValue(null);

            Assert.Greater(agora, 0f,
                $"O relógio devolveu {agora}. Fora do Play ele deveria ser " +
                "EditorApplication.timeSinceStartup, que conta desde que o Editor abriu e " +
                "portanto nunca é zero numa sessão real.");

            Assert.AreNotEqual(Time.time, agora,
                "O relógio devolveu exatamente Time.time fora do Play — ou seja, ainda é o " +
                "relógio errado. Em Edit mode ele não avança de forma útil.");
        }

        [Test]
        public void ForaDoPlay_RegistrarGuardaAMarca_ComExpiracaoNoFuturo()
        {
            DefinirMostrar(true);

            var registrar = Alvo().GetMethod("RegistrarCirculo",
                BindingFlags.Public | BindingFlags.Static);

            Assert.IsNotNull(registrar, "RegistrarCirculo sumiu ou mudou de assinatura.");

            registrar.Invoke(null, new object[]
            {
                new Vector2(3f, 4f), 1.5f, Color.red, 2f
            });

            var marcas = Marcas();

            Assert.AreEqual(1, marcas.Count,
                "Registrar fora do Play não guardou nada. Como um teste EditMode roda com " +
                "UNITY_EDITOR definido, o ConditionalAttribute NÃO apagou a chamada — então " +
                "o método rodou e descartou, o que só acontece se 'Mostrar' estiver falso.");

            // A expiração tem de estar no FUTURO pelo relógio que o próprio componente usa.
            var marca = marcas[0];
            var expira = (float)marca.GetType()
                .GetField("Expira", BindingFlags.Public | BindingFlags.Instance)
                .GetValue(marca);

            var agora = (float)Alvo()
                .GetProperty("Agora", BindingFlags.NonPublic | BindingFlags.Static)
                .GetValue(null);

            Assert.Greater(expira, agora,
                $"A marca já nasceu vencida: expira em {expira:0.###} e o relógio está em " +
                $"{agora:0.###}. É o sintoma de registrar com um relógio e podar com outro.");
        }

        [Test]
        public void ComMostrarDesligado_NadaEhGuardado()
        {
            DefinirMostrar(false);

            Alvo().GetMethod("RegistrarCirculo", BindingFlags.Public | BindingFlags.Static)
                .Invoke(null, new object[] { Vector2.zero, 1f, Color.red, 1f });

            Assert.AreEqual(0, Marcas().Count,
                "Com o visualizador desligado a lista cresceu. Esse é o vazamento que a guarda " +
                "de 'Mostrar' existe para impedir: o código de combate chama Registrar a cada " +
                "FixedUpdate de janela aberta, e sem o portão a lista cresceria a jogo inteiro.");
        }
    }
}
