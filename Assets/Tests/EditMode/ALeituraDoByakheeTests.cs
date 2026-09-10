using System.Linq;
using System.Reflection;
using FavelaAmarela.Core.Enemies;
using FavelaAmarela.Runtime.Enemies;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace FavelaAmarela.Tests.EditMode
{
    /// <summary>
    /// Guarda o que o jogador <b>consegue ler</b> na luta contra o Byakhee.
    ///
    /// <para><b>A queixa que originou:</b> <i>"a hurtbox dela é muito difícil de atingir"</i>.
    /// A geometria foi corrigida antes; o que sobrava era visual, e medido era pior do que
    /// parecia.</para>
    ///
    /// <para><b>Estava invertido.</b> A tinta de voo <c>(0.35, 0.30, 0.45)</c> deixava o chefe a
    /// <b>32% do brilho</b> durante a maior parte da luta — e a tinta da janela de dano,
    /// <c>(0.85, 0.75, 0.25)</c>, aplicada a uma arte que <b>já é dourada</b>, produzia quase a
    /// mesma imagem. O estado imune era o marcante; a única janela de dano não tinha sinal.</para>
    ///
    /// <para><b>E não dava para consertar com cor.</b> Tinta multiplicativa só escurece: era
    /// possível apagar o chefe, nunca destacá-lo. O indicador passou a ser a <b>sombra</b> —
    /// <c>Pousado</c> é o único dos oito estados em que ele está no chão (<c>Espreita</c> é
    /// "pousado no topo do arco", que é alto).</para>
    /// </summary>
    public sealed class ALeituraDoByakheeTests
    {
        private static float Altura(ByakheeState estado)
        {
            var m = typeof(ByakheeAI).GetMethod("AlturaDoEstado",
                BindingFlags.Static | BindingFlags.NonPublic);

            Assert.NotNull(m, "AlturaDoEstado sumiu do ByakheeAI — é ela que dá a leitura de " +
                              "altura pela sombra.");

            return (float)m.Invoke(null, new object[] { estado });
        }

        /// <summary>
        /// A única janela de dano é a única com a sombra fechada no chão. É a regra inteira.
        /// </summary>
        [Test]
        public void SoAJanelaDeDano_TemASombraNoChao()
        {
            Assert.AreEqual(0f, Altura(ByakheeState.Pousado), 0.001f,
                "Pousado é 'a única janela de dano' e precisa da sombra fechada no chão — é o " +
                "sinal de que dá para bater.");

            foreach (var estado in new[] { ByakheeState.Espreita, ByakheeState.Rasante,
                                           ByakheeState.MergulhoDeGarras,
                                           ByakheeState.GritoDirecionado,
                                           ByakheeState.Circundando, ByakheeState.Frenesi })
                Assert.Greater(Altura(estado), 0f,
                    $"{estado} é estado de voo e não pode mostrar a sombra no chão — seria " +
                    "dizer ao jogador que dá para bater quando não dá.");
        }

        /// <summary>
        /// O mergulho desce: a sombra apertando é o telégrafo de onde ele vai cair. Precisa
        /// ficar <b>abaixo</b> do voo normal e <b>acima</b> do chão.
        /// </summary>
        [Test]
        public void OMergulho_MostraQueEleEstaDescendo()
        {
            float mergulho = Altura(ByakheeState.MergulhoDeGarras);
            float rasante = Altura(ByakheeState.Rasante);

            Assert.Less(mergulho, rasante,
                $"O mergulho ({mergulho}) precisa ler mais baixo que o rasante ({rasante}).");

            Assert.Greater(mergulho, 0f,
                "Mas não pode chegar a zero: ele continua IMUNE durante o mergulho, e sombra " +
                "no chão significa que dá para bater.");
        }

        /// <summary>
        /// <c>Espreita</c> é "pousado no topo do arco" — o ponto mais alto da luta, e imune.
        /// </summary>
        [Test]
        public void Espreita_EhOPontoMaisAlto()
        {
            float espreita = Altura(ByakheeState.Espreita);

            foreach (var estado in new[] { ByakheeState.Rasante, ByakheeState.MergulhoDeGarras,
                                           ByakheeState.GritoDirecionado,
                                           ByakheeState.Circundando, ByakheeState.Frenesi })
                Assert.GreaterOrEqual(espreita, Altura(estado),
                    $"Espreita é o topo do arco e deveria ler mais alto que {estado}.");
        }

        /// <summary>
        /// A arte animada <b>não se tinge</b> nos estados de voo.
        ///
        /// <para>O <c>AnimadorDoByakhee</c> traz 26 quadros por estado e a documentação dele diz
        /// que substitui o tingimento. Os dois ficaram rodando juntos e o placeholder venceu:
        /// o chefe passou meses a 32% do brilho.</para>
        /// </summary>
        [Test]
        public void ACorDeVoo_NaoEscureceAArte()
        {
            string caminho = AssetDatabase.FindAssets("t:Prefab Byakhee")
                .Select(AssetDatabase.GUIDToAssetPath)
                .FirstOrDefault(p => System.IO.Path.GetFileNameWithoutExtension(p) == "Byakhee");

            Assert.NotNull(caminho, "Prefab do Byakhee não encontrado.");

            var ai = AssetDatabase.LoadAssetAtPath<GameObject>(caminho).GetComponent<ByakheeAI>();
            Assert.NotNull(ai, "O prefab do Byakhee perdeu o ByakheeAI.");

            var cor = (Color)new SerializedObject(ai).FindProperty("corNoAr").colorValue;
            float brilho = 0.2126f * cor.r + 0.7152f * cor.g + 0.0722f * cor.b;

            Assert.GreaterOrEqual(brilho, 0.95f,
                $"corNoAr está em {brilho:P0} de brilho. Tinta multiplicativa só escurece, e " +
                "esta cobre a maior parte da luta — o chefe fica difícil de acompanhar. " +
                "A arte animada não se tinge.");
        }
    }
}
