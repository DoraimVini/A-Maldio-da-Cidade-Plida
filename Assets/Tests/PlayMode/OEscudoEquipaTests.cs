using System.Collections;
using FavelaAmarela.Inventario;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace FavelaAmarela.Tests.PlayMode
{
    /// <summary>
    /// O Broquel de Couro <b>equipa</b> — na Mão Secundária, pelo caminho que a UI usa.
    ///
    /// <para><b>O defeito que isto pega (2026-09-10).</b> O Vini: <i>"o escudo não dá para ser
    /// equipado"</i>. Reproduzido no Editor vivo: <c>IndiceDoSlot(MaoSecundaria) == -1</c>. O
    /// código do <c>InventoryManager</c> lista sete slots na <c>anatomia</c>, mas o
    /// <c>InventoryManager.prefab</c> tinha <b>seis</b> serializados — o sétimo entrou no default
    /// do C# quando a Mão Secundária foi criada, e o prefab, que é quem vale, ficou para trás.
    /// A UI tinha o <c>Corpo_6</c>; o modelo por baixo não. O escudo nunca pôde ser equipado,
    /// por ninguém, desde que existe — e a suíte estava verde, porque nenhum teste equipava
    /// um escudo.</para>
    ///
    /// <para>É o modo de falha dominante deste projeto pela enésima vez: código que existe e não
    /// está ligado, com o valor serializado mandando sobre o default. Este teste instancia o
    /// <b>prefab</b>, não o código.</para>
    /// </summary>
    public sealed class OEscudoEquipaTests
    {
        private const string Broquel = "broquel_couro_ressecado";
        private GameObject _manager;
        private GameObject _db;

        [UnitySetUp]
        public IEnumerator Montar()
        {
            // O ItemDatabase resolve os ItemDef por id; o InventoryManager é o prefab real.
            if (ItemDatabase.Instance == null)
            {
                var dbPrefab = Resources.Load<GameObject>("ItemDatabase");
                if (dbPrefab != null) _db = Object.Instantiate(dbPrefab);
            }

            if (InventoryManager.Instance == null)
            {
                var prefab = Resources.Load<GameObject>("InventoryManager");
                Assert.NotNull(prefab, "InventoryManager.prefab não está em Resources.");
                _manager = Object.Instantiate(prefab);
            }

            yield return null;
            Assert.NotNull(InventoryManager.Instance, "InventoryManager.Instance não subiu.");

            // Inventário LIMPO (em memória; nenhum teste grava em disco). O GerenciadorDeSave
            // carrega o save real do jogador no Awake, e o InventoryManager persistente o
            // aplica: na suíte completa este teste herdava o Alfanje de duas mãos do Vini na
            // Mão principal — e a Mão Secundária recusa escudo com arma de duas mãos, por
            // regra. Reprovava dizendo "nenhum slot aceita", que é outro defeito, inexistente.
            InventoryManager.Instance.Main.LimparTudo();
            InventoryManager.Instance.Equipment.LimparTudo();
        }

        [UnityTearDown]
        public IEnumerator Desmontar()
        {
            if (_manager != null) Object.Destroy(_manager);
            if (_db != null) Object.Destroy(_db);
            yield return null;
        }

        /// <summary>
        /// O modelo tem a Mão Secundária — é a asserção que teria reprovado o prefab de seis.
        /// </summary>
        [Test]
        public void OEquipamento_TemSlotDeMaoSecundaria()
        {
            int indice = InventoryManager.Instance.Equipment.IndiceDoSlot(EquipmentSlot.MaoSecundaria);

            Assert.GreaterOrEqual(indice, 0,
                "O EquipmentInventory em execução não tem slot de MaoSecundaria. Confira a " +
                "'anatomia' serializada no InventoryManager.prefab — o default do C# não vale " +
                "nada se o prefab tiver a lista antiga.");
        }

        /// <summary>
        /// O caminho inteiro que a UI percorre: item na mochila → <c>Equipar</c> → Mão Secundária.
        /// </summary>
        [Test]
        public void OBroquel_EquipaNaMaoSecundaria()
        {
            var inv = InventoryManager.Instance;
            var escudo = new ItemInstance(Broquel, 1);

            Assert.NotNull(escudo.Def, $"ItemDef '{Broquel}' não resolvido — o ItemDatabase não subiu.");
            Assert.AreEqual(EquipmentSlot.MaoSecundaria, escudo.Def.SlotEquipamento,
                "O Broquel deixou de ser item de Mão Secundária.");

            Assert.IsTrue(inv.Main.Add(escudo), "A mochila recusou o Broquel.");

            int naMochila = -1;
            for (int i = 0; i < inv.Main.Capacidade; i++)
            {
                var s = inv.Main.GetSlot(i);
                if (s != null && s.ItemDefId == Broquel) { naMochila = i; break; }
            }
            Assert.GreaterOrEqual(naMochila, 0, "O Broquel não apareceu na mochila depois do Add.");

            Assert.IsTrue(inv.Equipment.CanAddAny(escudo),
                "CanAddAny recusou o escudo: nenhum slot o aceita — o sintoma exato que o Vini viu.");

            Assert.IsTrue(inv.Equipar(naMochila), "Equipar devolveu false para o Broquel.");

            int maoSecundaria = inv.Equipment.IndiceDoSlot(EquipmentSlot.MaoSecundaria);
            var equipado = inv.Equipment.GetSlot(maoSecundaria);

            Assert.NotNull(equipado, "Equipar devolveu true, mas a Mão Secundária está vazia.");
            Assert.AreEqual(Broquel, equipado.ItemDefId, "Outra coisa foi parar na Mão Secundária.");
            Assert.IsNull(inv.Main.GetSlot(naMochila), "O Broquel continua na mochila depois de equipado.");
        }
    }
}
