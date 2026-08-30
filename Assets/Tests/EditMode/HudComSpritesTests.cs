using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace FavelaAmarela.Tests.EditMode
{
    /// <summary>
    /// Guarda o requisito que faz uma barra de HUD <b>encolher</b>.
    ///
    /// <para><b>O defeito (relatado pelo Vini em 2026-08-29):</b> <i>"as UI de vida e
    /// resiliência não parecem estar diminuindo."</i> A lógica das barras estava toda correta —
    /// <c>Bind</c>, evento, <c>fillAmount</c>, tudo. O que faltava era <b>sprite</b>.</para>
    ///
    /// <para>Da fonte do uGUI desta Unity (<c>Image.cs:883</c>):</para>
    /// <code>
    /// if (activeSprite == null)
    /// {
    ///     base.OnPopulateMesh(toFill);   // Graphic: quad INTEIRO, sempre
    ///     return;                        // o 'type' nunca é consultado
    /// }
    /// </code>
    ///
    /// <para>Com sprite nulo o <c>fillAmount</c> muda no código e <b>nada muda na tela</b> — um
    /// sintoma que se lê como "o dano não está sendo aplicado", e que mandou a investigação
    /// para o lado errado (dano, mitigação, binding) antes de chegar aqui.</para>
    ///
    /// <para><b>Por que um teste e não só o conserto.</b> Isto já foi consertado uma vez, em
    /// 2026-08-02, por uma ferramenta de Editor. A ferramenta varria a <b>cena ativa</b>, e a
    /// migração do HUD para prefab persistente tirou o HUD de todas as cenas — o conserto virou
    /// um <c>no-op</c> que relatava sucesso. Ferramenta de conserto sem guarda apodrece em
    /// silêncio; o guarda é o que faz o conserto durar.</para>
    /// </summary>
    public sealed class HudComSpritesTests
    {
        /// <summary>O HUD que o jogo carrega em runtime — não o prefab de arte legado.</summary>
        private const string HudVivo = "Assets/FavelaAmarela/Resources/HUD_Gameplay.prefab";

        private static GameObject CarregarHud()
        {
            var hud = AssetDatabase.LoadAssetAtPath<GameObject>(HudVivo);

            Assert.IsNotNull(hud,
                $"{HudVivo} não existe. É o prefab que HUDController.GarantirInstancia carrega " +
                "por Resources.Load — sem ele o jogo roda sem HUD nenhum.");

            return hud;
        }

        /// <summary>
        /// <b>O guarda principal.</b> Toda <c>Image</c> do tipo <c>Filled</c> precisa de sprite,
        /// senão o preenchimento é decorativo.
        /// </summary>
        [Test]
        public void NenhumaImagemFilled_FicaSemSprite()
        {
            var mudas = new List<string>();

            foreach (var img in CarregarHud().GetComponentsInChildren<Image>(includeInactive: true))
            {
                if (img.type != Image.Type.Filled) continue;
                if (img.sprite != null) continue;

                mudas.Add($"{Caminho(img.transform)} — Filled sem sprite: o fillAmount muda e a " +
                          "tela não");
            }

            Assert.IsEmpty(mudas,
                "Image(s) de preenchimento sem sprite:" + Environment.NewLine + "  " +
                string.Join(Environment.NewLine + "  ", mudas) + Environment.NewLine +
                "Conserto: 'Tools/FavelaAmarela/HUD: restaurar os sprites das barras'.");
        }

        /// <summary>
        /// As barras de recurso são horizontais. <c>Radial360</c> numa barra de vida seria um
        /// erro de Inspector que passa despercebido — a barra encolheria, só que girando.
        /// </summary>
        [Test]
        public void AsBarrasDeRecurso_PreenchemNaHorizontal()
        {
            var tortas = new List<string>();

            foreach (var barra in new[] { "Barra_Vitalidade", "Barra_ResilienciaMental",
                                          "Barra_Vigor", "Barra_Companheiro" })
            {
                var alvo = CarregarHud().transform.Find($"{barra}/Preenchimento");

                if (alvo == null)
                {
                    tortas.Add($"{barra}/Preenchimento não existe no HUD");
                    continue;
                }

                var img = alvo.GetComponent<Image>();
                if (img == null) { tortas.Add($"{barra}/Preenchimento sem Image"); continue; }

                if (img.type != Image.Type.Filled)
                    tortas.Add($"{barra}: tipo {img.type}, deveria ser Filled");

                else if (img.fillMethod != Image.FillMethod.Horizontal)
                    tortas.Add($"{barra}: fillMethod {img.fillMethod}, deveria ser Horizontal");
            }

            Assert.IsEmpty(tortas,
                "Barra(s) de recurso mal configuradas:" + Environment.NewLine + "  " +
                string.Join(Environment.NewLine + "  ", tortas));
        }

        /// <summary>
        /// Cada barra de recurso tem de ter as <b>duas</b> referências ligadas — o trilho tanto
        /// quanto o preenchimento. Trilho nulo não trava a barra, mas deixa o preenchimento
        /// flutuando sem moldura, e é sinal de prefab montado pela metade.
        /// </summary>
        [Test]
        public void CadaBarra_TemTrilhoEPreenchimentoLigados()
        {
            var incompletas = new List<string>();
            var hud = CarregarHud();

            foreach (var barra in hud.GetComponentsInChildren<MonoBehaviour>(true)
                         .Where(m => m != null && EhBarraAnimada(m.GetType())))
            {
                var tipo = m_TipoBase(barra.GetType());

                foreach (var campo in new[] { "fillImage", "backgroundImage" })
                {
                    var info = tipo.GetField(campo,
                        System.Reflection.BindingFlags.NonPublic |
                        System.Reflection.BindingFlags.Instance);

                    if (info == null)
                    {
                        incompletas.Add($"{barra.GetType().Name}: campo '{campo}' sumiu da " +
                                        "BarraAnimada");
                        continue;
                    }

                    if (info.GetValue(barra) as Image == null)
                        incompletas.Add($"{barra.GetType().Name}.{campo} está NULO no prefab");
                }
            }

            Assert.IsEmpty(incompletas,
                "Barra(s) com referência solta:" + Environment.NewLine + "  " +
                string.Join(Environment.NewLine + "  ", incompletas));
        }

        /// <summary>
        /// A ferramenta de conserto tem de mirar o <b>HUD vivo</b>. Ela já apontou para a cena
        /// ativa — e, depois da migração para prefab persistente, passou a relatar sucesso sem
        /// tocar em nada por meses.
        /// </summary>
        [Test]
        public void AFerramentaDeConserto_MiraOHudQueOJogoCarrega()
        {
            string fonte = System.IO.File.ReadAllText(
                "Assets/FavelaAmarela/Editor/CorrigirSpritesDoHUD.cs");

            StringAssert.Contains("Resources/HUD_Gameplay.prefab", fonte,
                "A ferramenta de sprites deixou de mirar o HUD que HUDController carrega por " +
                "Resources.Load. Se ela voltar a varrer a cena, vai relatar 'nada a fazer' para " +
                "sempre — o HUD não está em cena nenhuma.");

            StringAssert.Contains("LoadAllAssetsAtPath", fonte,
                "A ferramenta voltou a carregar o sprite só por LoadAssetAtPath<Sprite>. Os " +
                "PNGs das barras estão em spriteMode Multiple, onde o Sprite é SUB-ASSET e " +
                "aquela chamada devolve null — a ferramenta abortaria no guarda de nulo, como " +
                "fazia antes.");
        }

        // ── Apoio ─────────────────────────────────────────────────────────────

        private static bool EhBarraAnimada(Type t) => m_TipoBase(t) != null;

        /// <summary>Sobe a hierarquia até achar <c>BarraAnimada&lt;&gt;</c>, ou nulo.</summary>
        private static Type m_TipoBase(Type t)
        {
            for (var atual = t; atual != null; atual = atual.BaseType)
            {
                if (atual.IsGenericType &&
                    atual.GetGenericTypeDefinition().Name.StartsWith("BarraAnimada"))
                    return atual;
            }
            return null;
        }

        private static string Caminho(Transform t)
        {
            var partes = new List<string>();
            for (var atual = t; atual != null; atual = atual.parent) partes.Add(atual.name);
            partes.Reverse();
            return string.Join("/", partes);
        }
    }
}
