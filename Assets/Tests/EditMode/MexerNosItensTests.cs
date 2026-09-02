using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using FavelaAmarela.Inventario;

namespace FavelaAmarela.Tests.EditMode
{
    /// <summary>
    /// Guarda que o jogador <b>consegue mexer nos itens</b>.
    ///
    /// <para><b>O que motivou (2026-09-02).</b> O Vini relatou <i>"não dá para mexer nos itens
    /// dentro do inventário"</i>, e as três camadas estavam faltando ao mesmo tempo: o prefab
    /// não tinha <c>Button</c> nas casas, o painel não tinha handler de ponteiro, e o modelo não
    /// tinha <c>Mover</c> nem <c>Descartar</c>.</para>
    ///
    /// <para>O mais revelador: o <c>BaseInventory.Swap</c> <b>já existia</b>, com um comentário
    /// dizendo que servia para "o arrastar da UI", e tinha <b>zero chamadores</b> no projeto
    /// inteiro. O <c>Desequipar</c> idem. As peças estavam prontas e nada as chamava — o modo de
    /// falha assinado deste repositório.</para>
    /// </summary>
    public sealed class MexerNosItensTests
    {
        private GameObject _objetoDoBanco;
        private GameObject _objetoDoGerente;
        private ItemDatabase _banco;
        private InventoryManager _gerente;
        private ItemDef _erva;
        private ItemDef _agua;

        [SetUp]
        public void SetUp()
        {
            _objetoDoBanco = new GameObject("BancoDeTeste");
            _banco = _objetoDoBanco.AddComponent<ItemDatabase>();
            _banco.InitializeForTesting();

            _erva = ScriptableObject.CreateInstance<ItemDef>();
            _erva.Id = "erva";
            _erva.Nome = "Erva";
            _erva.Tipo = ItemType.Consumivel;
            _erva.EmpilhamentoMaximo = 5;
            _erva.Modificadores = new List<ModificadorFixo>();
            _banco.Registrar(_erva);

            _agua = ScriptableObject.CreateInstance<ItemDef>();
            _agua.Id = "agua";
            _agua.Nome = "Água";
            _agua.Tipo = ItemType.Consumivel;
            _agua.EmpilhamentoMaximo = 5;
            _agua.Modificadores = new List<ModificadorFixo>();
            _banco.Registrar(_agua);

            _objetoDoGerente = new GameObject("GerenteDeTeste");
            _gerente = _objetoDoGerente.AddComponent<InventoryManager>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_objetoDoGerente != null) Object.DestroyImmediate(_objetoDoGerente);
            if (_objetoDoBanco != null) Object.DestroyImmediate(_objetoDoBanco);
            if (_erva != null) Object.DestroyImmediate(_erva);
            if (_agua != null) Object.DestroyImmediate(_agua);
        }

        private void Por(string id, int slot)
        {
            _gerente.Main.AddAt(new ItemInstance(id, 1), slot);
        }

        [Test]
        public void Mover_LevaOItemParaACasaVazia()
        {
            Por("erva", 0);

            Assert.IsTrue(_gerente.Mover(0, 3));

            Assert.IsNull(_gerente.Main.GetSlot(0), "A casa de origem não esvaziou.");
            Assert.AreEqual("erva", _gerente.Main.GetSlot(3)?.ItemDefId);
        }

        [Test]
        public void Mover_TrocaQuandoODestinoTemOutroItem()
        {
            Por("erva", 0);
            Por("agua", 1);

            Assert.IsTrue(_gerente.Mover(0, 1));

            Assert.AreEqual("agua", _gerente.Main.GetSlot(0)?.ItemDefId);
            Assert.AreEqual("erva", _gerente.Main.GetSlot(1)?.ItemDefId);
        }

        /// <summary>
        /// Mover para a mesma casa, ou entre duas casas vazias, não é movimento — e devolver
        /// <c>true</c> faria a UI redesenhar à toa a cada clique errado.
        /// </summary>
        [Test]
        public void Mover_NaoFazNadaQuandoNaoHaOQueMover()
        {
            Por("erva", 0);

            Assert.IsFalse(_gerente.Mover(0, 0), "Mover para a própria casa contou como movimento.");
            Assert.IsFalse(_gerente.Mover(4, 5), "Trocar dois vazios contou como movimento.");
        }

        [Test]
        public void Mover_RecusaIndiceForaDaMochila()
        {
            Por("erva", 0);

            Assert.IsFalse(_gerente.Mover(0, -1));
            Assert.IsFalse(_gerente.Mover(0, _gerente.Main.Capacidade));
            Assert.IsFalse(_gerente.Mover(-1, 0));

            Assert.AreEqual("erva", _gerente.Main.GetSlot(0)?.ItemDefId,
                "Um índice inválido mexeu no item que estava lá.");
        }

        [Test]
        public void Descartar_EsvaziaACasa()
        {
            Por("erva", 0);

            Assert.IsTrue(_gerente.Descartar(0));
            Assert.IsNull(_gerente.Main.GetSlot(0));
        }

        [Test]
        public void Descartar_CasaVaziaNaoFazNada()
        {
            Assert.IsFalse(_gerente.Descartar(2));
        }

        // ── A camada de cima: o clique tem onde chegar? ───────────────────────

        /// <summary>
        /// Toda casa do inventário precisa de um <c>Button</c> ligado ao painel, e a moldura
        /// precisa <b>aceitar raycast</b>.
        ///
        /// <para>Sem o <c>Button</c>, o inventário é uma vitrine — foi o estado até 2026-09-02.
        /// Com o <c>Button</c> e <c>raycastTarget = false</c>, é pior: o botão existe, parece
        /// certo no Inspector, e o clique <b>atravessa</b> a casa sem disparar nada.</para>
        /// </summary>
        [Test]
        public void TodaCasaTemBotaoLigadoEAceitaClique()
        {
            const string Hud = "Assets/FavelaAmarela/Resources/HUD_Gameplay.prefab";

            var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(Hud);
            Assert.IsNotNull(prefab, $"Prefab ausente: {Hud}");

            var painel = prefab.GetComponentInChildren<
                FavelaAmarela.Runtime.UI.PainelDeInventario>(true);

            Assert.IsNotNull(painel, "PainelDeInventario não está no HUD.");

            var so = new UnityEditor.SerializedObject(painel);
            var problemas = new List<string>();
            int total = 0;

            foreach (var nome in new[] { "slotsDaMochila", "slotsDoCorpo" })
            {
                var array = so.FindProperty(nome);
                Assert.IsNotNull(array, $"Campo '{nome}' não existe mais no PainelDeInventario.");

                for (int i = 0; i < array.arraySize; i++)
                {
                    total++;
                    var entrada = array.GetArrayElementAtIndex(i);

                    var botao = entrada.FindPropertyRelative("botao")?.objectReferenceValue
                        as UnityEngine.UI.Button;

                    if (botao == null)
                    {
                        problemas.Add($"{nome}[{i}]: sem Button — a casa não responde a clique");
                        continue;
                    }

                    if (!botao.interactable)
                        problemas.Add($"{nome}[{i}]: Button não-interativo");

                    var alvo = botao.targetGraphic;

                    if (alvo == null)
                        problemas.Add($"{nome}[{i}]: Button sem targetGraphic");
                    else if (!alvo.raycastTarget)
                        problemas.Add($"{nome}[{i}]: a moldura não aceita raycast — o clique " +
                                      "atravessa a casa e o Button nunca dispara");
                }
            }

            Assert.Greater(total, 0, "Nenhuma casa encontrada — a estrutura mudou.");

            Assert.IsEmpty(problemas,
                $"{problemas.Count} de {total} casa(s) não recebem clique:" +
                System.Environment.NewLine + "  " +
                string.Join(System.Environment.NewLine + "  ", problemas) +
                System.Environment.NewLine +
                "Conserto: 'Tools/FavelaAmarela/UI: tornar os slots do inventário clicáveis'.");
        }
    }
}
