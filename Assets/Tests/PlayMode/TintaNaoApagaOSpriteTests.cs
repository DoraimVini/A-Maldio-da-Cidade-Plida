using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace FavelaAmarela.Tests.PlayMode
{
    /// <summary>
    /// Guarda que nenhuma <see cref="Image"/> do HUD tem uma <b>cor que apaga o próprio sprite</b>.
    ///
    /// <para><b>O que motivou (2026-09-02).</b> O Vini mandou print atrás de print de painel que
    /// "está sem a UI": a tela de Colapso, a barra de interação, o inventário. O sprite estava
    /// atribuído em todos — o que os apagava era a <b>tinta</b>.</para>
    ///
    /// <para><c>Image.color</c> <b>multiplica</b> a textura, não a substitui. E o
    /// <c>painel_ornado</c> do pacote já é, por desenho, um painel escuro com borda dourada:</para>
    ///
    /// <code>
    /// miolo  (46, 50, 42)    luminância 0,19
    /// borda  (163, 126, 55)  o ouro da moldura
    /// </code>
    ///
    /// <para>Tingir isso com <c>(0.05, 0.04, 0.02)</c> leva o miolo a <c>(2, 2, 0)</c> e o ouro a
    /// <c>(8, 5, 1)</c>. <b>Tudo preto.</b> Era eu escurecendo à mão um sprite que já vinha
    /// escuro — dez vezes, no mesmo prefab.</para>
    ///
    /// <para><b>Duas isenções, as duas medidas.</b> O critério é o canal <b>MÁXIMO</b>, e não a
    /// luminância: medir por luminância acusava a barra de Vitalidade, vermelha
    /// <c>(0.72, 0.18, 0.18)</c> sobre um preenchimento claro — tinta intencional, e o vermelho
    /// sobrevive inteiro. O que apaga um sprite é <b>nenhum</b> canal sobrar, não o resultado ser
    /// escuro. E <c>Image.Type.Filled</c> fica de fora inteiro: ali o sprite é forma para o
    /// <c>fillAmount</c> recortar, não arte.</para>
    ///
    /// <para><b>O critério antigo, para o registro.</b> Medir por luminância acusava a
    /// barra de Vitalidade, que é vermelha <c>(0.72, 0.18, 0.18)</c> sobre um preenchimento claro
    /// — tinta intencional, e o vermelho sobrevive inteiro. O que apaga um sprite é <b>nenhum</b>
    /// canal sobrar, não o resultado ser escuro.</para>
    /// </summary>
    public sealed class TintaNaoApagaOSpriteTests
    {
        private GameObject _hud;

        /// <summary>
        /// Abaixo disto, o sprite mais claro possível (255) ainda sai como 63 — preto na prática.
        /// Acima, a tinta ainda é uma cor: a barra de Vitalidade passa com 0,72.
        /// </summary>
        private const float CanalMinimo = 0.25f;

        [TearDown]
        public void TearDown()
        {
            if (_hud != null) Object.DestroyImmediate(_hud);
        }

        [UnityTest]
        public IEnumerator NenhumaTintaApagaOSpriteQueVeste()
        {
            FavelaAmarela.Runtime.UI.HUDController.GarantirInstancia();
            yield return null;

            var controlador = Object.FindAnyObjectByType<FavelaAmarela.Runtime.UI.HUDController>(
                FindObjectsInactive.Include);

            Assert.IsNotNull(controlador, "O HUD_Gameplay não subiu — nada a medir.");
            _hud = controlador.gameObject;

            var todas = _hud.GetComponentsInChildren<Image>(true);

            // REGRA DURA: um HUD sem Image nenhuma passaria vazio e verde.
            Assert.Greater(todas.Length, 20,
                $"Só achei {todas.Length} Image(s) no HUD — este teste não está medindo o prefab.");

            // ISENÇÃO: Image.Type.Filled. Ali o sprite é uma FORMA a ser recortada pelo
            // fillAmount, não arte a ser exibida, e a cor é a do próprio véu -- o retângulo preto
            // que cobre o ícone enquanto o artefato recarrega. Escurecer é a função dele.
            //
            // Descobri isto errando: tirei o sprite das quatro Recarga da Barra_Artefatos, e o
            // HudComSpritesTests.NenhumaImagemFilled_FicaSemSprite -- que já existia -- pegou na
            // hora: "Filled sem sprite: o fillAmount muda e a tela não".
            var comSprite = todas
                .Where(i => i.sprite != null && i.type != Image.Type.Filled)
                .ToArray();

            Assert.Greater(comSprite.Length, 10,
                $"Só {comSprite.Length} Image(s) têm sprite. Ou o pacote de UI saiu do prefab, " +
                "ou este teste está olhando para outra coisa.");

            var apagadas = comSprite
                .Where(i => Mathf.Max(i.color.r, Mathf.Max(i.color.g, i.color.b)) < CanalMinimo)
                .Select(i => $"  {Caminho(i.transform)}" + System.Environment.NewLine +
                             $"      sprite '{i.sprite.name}' tingido de " +
                             $"({i.color.r:0.00}, {i.color.g:0.00}, {i.color.b:0.00})")
                .ToArray();

            Assert.IsEmpty(apagadas,
                $"{apagadas.Length} Image(s) com uma tinta que apaga o próprio sprite — o painel " +
                "aparece como um retângulo preto vazio, que é o 'está sem a UI' do Vini:" +
                System.Environment.NewLine +
                string.Join(System.Environment.NewLine, apagadas));
        }

        private static string Caminho(Transform t)
        {
            var partes = new System.Collections.Generic.List<string>();
            for (var a = t; a != null; a = a.parent) partes.Add(a.name);
            partes.Reverse();
            return string.Join("/", partes);
        }
    }
}
