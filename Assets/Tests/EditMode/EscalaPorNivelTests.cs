using System.Linq;
using NUnit.Framework;
using UnityEditor;
using FavelaAmarela.Core.Progression;
using FavelaAmarela.Core.Combat;

namespace FavelaAmarela.Tests.EditMode
{
    /// <summary>
    /// Guarda a <b>lei única de escala</b>: um número só cresce de um jeito, e o nível 1 é
    /// sempre o valor autorado.
    ///
    /// <para><b>Por que uma lei só (2026-08-28).</b> O Vini pediu que a escala cresça com o jogo
    /// e com o personagem — "saber que ele no nível 2 está mais forte e com mais defesa". Sem
    /// uma lei central, cada sistema inventa a sua: foi assim que sete ferramentas de Editor
    /// acabaram com sete zooms de câmera diferentes, e que o dano da arma e o dano da ficha
    /// viraram dois números independentes mantidos à mão.</para>
    /// </summary>
    public sealed class EscalaPorNivelTests
    {
        /// <summary>
        /// <b>A condição que permitiu ligar a escala sem rebalancear o jogo.</b> Se nível 1
        /// mexesse em qualquer número, todo <c>.asset</c> de ficha do projeto teria de ser
        /// reescrito no mesmo commit — e o balanceamento de todo encontro mudaria junto, sem
        /// ninguém conseguir separar o que foi intenção do que foi efeito colateral.
        /// </summary>
        [Test]
        public void ONivel1_NaoMexeEmNumeroNenhum()
        {
            foreach (var ficha in Fichas())
            {
                var nivel1 = ficha.CriarFicha(1);

                Assert.AreEqual(ficha.VitalidadeMax, nivel1.VitalidadeMax, 0.001f,
                    $"{ficha.name}: Vitalidade mudou no nível 1.");
                Assert.AreEqual(ficha.Ataque, nivel1.Ataque, 0.001f,
                    $"{ficha.name}: Ataque mudou no nível 1.");
                Assert.AreEqual(ficha.Defesa, nivel1.Defesa, 0.001f,
                    $"{ficha.name}: Defesa mudou no nível 1.");
            }
        }

        [Test]
        public void ONivel2_EhMaisForteEMaisDefendido()
        {
            foreach (var ficha in Fichas())
            {
                var n1 = ficha.CriarFicha(1);
                var n2 = ficha.CriarFicha(2);

                // Só afirma crescimento onde há valor base: uma ficha com Ataque 0 (o Damião
                // desarmado, o Yug-Neth) continua em 0 -- 25% de zero é zero, e isso é certo.
                if (n1.VitalidadeMax > 0f)
                    Assert.Greater(n2.VitalidadeMax, n1.VitalidadeMax, $"{ficha.name}: Vitalidade");

                if (n1.Ataque > 0f)
                    Assert.Greater(n2.Ataque, n1.Ataque, $"{ficha.name}: Ataque");

                if (n1.Defesa > 0f)
                    Assert.Greater(n2.Defesa, n1.Defesa, $"{ficha.name}: Defesa");
            }
        }

        /// <summary>
        /// Defesa <b>subtrai</b> e ataque <b>multiplica</b>. Casar os dois no mesmo passo faz o
        /// combate travar no meio da curva — chega um nível em que a defesa alcança o ataque e
        /// todo golpe passa a bater no piso de 15% da mitigação.
        /// </summary>
        [Test]
        public void ADefesa_CresceMaisDevagarQueOAtaque()
        {
            foreach (var ficha in Fichas())
            {
                if (ficha.Ataque <= 0f || ficha.Defesa <= 0f) continue;

                Assert.Less(ficha.DefesaPorNivel, ficha.AtaquePorNivel,
                    $"{ficha.name}: a Defesa cresce igual ou mais que o Ataque. No teto da " +
                    "curva isso faz todo golpe bater no piso da mitigação e o combate para.");
            }
        }

        [Test]
        public void ATabelaDaArma_UsaAMesmaLeiDaFicha()
        {
            // Não é coincidência a ser verificada por olho: é a mesma função.
            Assert.AreEqual(EscalaDeNivel.Fator(4, EscalaDeNivel.GanhoPorNivel),
                            EscalaDeNivel.FatorDeDano(4), 0.0001f,
                "O fator de dano da arma deixou de sair da lei única. Duas curvas divergem, e " +
                "divergem em silêncio — foi assim que a ficha do Byakhee e o danoDasGarras " +
                "viraram dois 26 mantidos à mão.");
        }

        [Test]
        public void ONivelInvalido_EhTratadoComoUm()
        {
            Assert.AreEqual(1f, EscalaDeNivel.Fator(0, 0.25f), 0.0001f);
            Assert.AreEqual(1f, EscalaDeNivel.Fator(-5, 0.25f), 0.0001f,
                "Nível zero ou negativo é dado não serializado ou erro de autoria, nunca uma " +
                "unidade 'mais fraca que o começo'. Um Cultista com metade da vida por causa " +
                "de um campo ausente seria invisível em jogo.");
        }

        private static FichaAtributosConfig[] Fichas() =>
            AssetDatabase.FindAssets("t:FichaAtributosConfig")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<FichaAtributosConfig>)
                .Where(f => f != null)
                .ToArray();
    }
}
