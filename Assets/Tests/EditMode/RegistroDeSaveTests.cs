using NUnit.Framework;
using FavelaAmarela.Core.Persistencia;

namespace FavelaAmarela.Tests.EditMode
{
    /// <summary>
    /// Testes do <see cref="RegistroDeSave"/>. Boa parte cobre <b>degradação graciosa</b>:
    /// um save parcialmente corrompido não pode impedir a partida de carregar, porque a
    /// alternativa (exceção no load) custa a run inteira do jogador.
    /// </summary>
    public sealed class RegistroDeSaveTests
    {
        [Test]
        public void RegistroNovo_ComecaVazio()
        {
            var registro = new RegistroDeSave();
            Assert.AreEqual(0, registro.Contagem);
        }

        [Test]
        public void Definir_GravaEPodeSerLido()
        {
            var registro = new RegistroDeSave();
            registro.Definir(ChavesDeSave.ArmaEquipada, "EstileteDeIrem");

            Assert.IsTrue(registro.TentarObter(ChavesDeSave.ArmaEquipada, out var valor));
            Assert.AreEqual("EstileteDeIrem", valor);
        }

        [Test]
        public void Definir_DuasVezes_UltimoValorVence()
        {
            var registro = new RegistroDeSave();
            registro.Definir("k", "primeiro");
            registro.Definir("k", "segundo");

            Assert.AreEqual("segundo", registro.ObterOuPadrao("k"));
            Assert.AreEqual(1, registro.Contagem, "sobrescrever não pode duplicar a chave");
        }

        [Test]
        public void ChaveNulaOuVazia_EIgnoradaSemLancar()
        {
            var registro = new RegistroDeSave();

            Assert.DoesNotThrow(() => registro.Definir(null, "x"));
            Assert.DoesNotThrow(() => registro.Definir("", "x"));
            Assert.DoesNotThrow(() => registro.Definir("   ", "x"));

            Assert.AreEqual(0, registro.Contagem);
        }

        [Test]
        public void ChaveAusente_NaoQuebra_EDevolveOPadrao()
        {
            var registro = new RegistroDeSave();

            Assert.IsFalse(registro.TentarObter("nunca.gravada", out _));
            Assert.AreEqual("padrao", registro.ObterOuPadrao("nunca.gravada", "padrao"));
            Assert.IsFalse(registro.Contem("nunca.gravada"));
        }

        [Test]
        public void Remover_ApagaAChave()
        {
            var registro = new RegistroDeSave();
            registro.Definir("k", "v");

            Assert.IsTrue(registro.Remover("k"));
            Assert.IsFalse(registro.Contem("k"));
            Assert.IsFalse(registro.Remover("k"), "remover de novo não encontra nada");
        }

        [Test]
        public void Limpar_EsvaziaTudo()
        {
            var registro = new RegistroDeSave();
            registro.Definir("a", "1");
            registro.Definir("b", "2");

            registro.Limpar();
            Assert.AreEqual(0, registro.Contagem);
        }

        [Test]
        public void IdaEVolta_PreservaTodasAsChaves()
        {
            var original = new RegistroDeSave();
            original.Definir(ChavesDeSave.ArmaEquipada, "CravoDeAklo");
            original.Definir(ChavesDeSave.VitalidadeAtual, "42.5");
            original.Definir(ChavesDeSave.YugNethLibertado, "true");

            var recuperado = RegistroDeSave.DeEstado(original.ParaEstado());

            Assert.AreEqual(3, recuperado.Contagem);
            Assert.AreEqual("CravoDeAklo", recuperado.ObterOuPadrao(ChavesDeSave.ArmaEquipada));
            Assert.AreEqual("42.5", recuperado.ObterOuPadrao(ChavesDeSave.VitalidadeAtual));
            Assert.AreEqual("true", recuperado.ObterOuPadrao(ChavesDeSave.YugNethLibertado));
        }

        [Test]
        public void DeEstado_ComEstadoNulo_DevolveRegistroVazio()
        {
            var registro = RegistroDeSave.DeEstado(null);

            Assert.IsNotNull(registro);
            Assert.AreEqual(0, registro.Contagem);
        }

        [Test]
        public void DeEstado_ComEntradaNula_IgnoraEmVezDeLancar()
        {
            var estado = new EstadoDeSave();
            estado.Entradas.Add(null);
            estado.Entradas.Add(new EntradaDeSave("boa", "v"));

            RegistroDeSave registro = null;
            Assert.DoesNotThrow(() => registro = RegistroDeSave.DeEstado(estado));
            Assert.AreEqual(1, registro.Contagem);
            Assert.AreEqual("v", registro.ObterOuPadrao("boa"));
        }

        [Test]
        public void DeEstado_ComChaveRepetida_NaoDuplica()
        {
            var estado = new EstadoDeSave();
            estado.Entradas.Add(new EntradaDeSave("k", "antigo"));
            estado.Entradas.Add(new EntradaDeSave("k", "novo"));

            var registro = RegistroDeSave.DeEstado(estado);

            Assert.AreEqual(1, registro.Contagem);
            Assert.AreEqual("novo", registro.ObterOuPadrao("k"));
        }

        [Test]
        public void ChaveOrfaNoSave_NaoAtrapalhaAsDemais()
        {
            // Objeto removido do jogo pelo level designer: a chave sobra no arquivo.
            var estado = new EstadoDeSave();
            estado.Entradas.Add(new EntradaDeSave("objeto.que.nao.existe.mais", "x"));
            estado.Entradas.Add(new EntradaDeSave(ChavesDeSave.ArmaEquipada, "AlfanjeDeAlhazred"));

            var registro = RegistroDeSave.DeEstado(estado);

            Assert.AreEqual("AlfanjeDeAlhazred", registro.ObterOuPadrao(ChavesDeSave.ArmaEquipada));
        }

        [Test]
        public void ValorNulo_EGravadoSemQuebrar()
        {
            var registro = new RegistroDeSave();
            registro.Definir("k", null);

            Assert.IsTrue(registro.Contem("k"), "a chave existe mesmo com valor nulo");
            Assert.IsNull(registro.ObterOuPadrao("k", "padrao-nao-usado"));
        }

        [Test]
        public void ChavesDeSave_SeguemAConvencaoHierarquica()
        {
            // A convenção existe para o save ser legível ao depurar; um literal solto
            // como "chefe_morto" quebra isso e não é pego por nenhum compilador.
            Assert.That(ChavesDeSave.ArmaEquipada, Does.Contain("."));
            Assert.That(ChavesDeSave.AbdulResolvido, Does.StartWith("Quest."));
            Assert.That(ChavesDeSave.VitalidadeAtual, Does.StartWith("Jogador."));
        }
    }
}
