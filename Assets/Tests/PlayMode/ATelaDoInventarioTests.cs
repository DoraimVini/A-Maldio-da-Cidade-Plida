using System.Collections;
using FavelaAmarela.Inventario;
using FavelaAmarela.Runtime.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace FavelaAmarela.Tests.PlayMode
{
    /// <summary>
    /// A tela de inventário do <b>HUD real</b> (plano B, 2026-09-28): escolher um item o descreve,
    /// a recusa diz o porquê, e as três colunas não se atropelam.
    ///
    /// <para><b>O que havia antes:</b> escolher acendia a casa e nada mais; a recusa ao vestir
    /// era muda; e a ficha de atributos ficava pendurada fora da borda da janela, por cima das
    /// linhas do Corpo e da dica de fechar.</para>
    /// </summary>
    public sealed class ATelaDoInventarioTests
    {
        private const string Broquel = "broquel_couro_ressecado";
        private const string Erva = "consumivel_erva_ancoragem";

        private GameObject _manager;
        private GameObject _db;
        private PainelDeInventario _painel;

        [UnitySetUp]
        public IEnumerator Montar()
        {
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

            // O HUD é singleton persistente, e outros testes da suíte o destroem no TearDown.
            HUDController.GarantirInstancia();
            yield return null;

            // Inventário LIMPO em memória: o save real do jogador vem junto com o singleton.
            InventoryManager.Instance.Main.LimparTudo();
            InventoryManager.Instance.Equipment.LimparTudo();

            _painel = Object.FindAnyObjectByType<PainelDeInventario>(FindObjectsInactive.Include);
            Assert.NotNull(_painel, "O HUD não tem PainelDeInventario.");
        }

        [UnityTearDown]
        public IEnumerator Desmontar()
        {
            if (_painel != null) _painel.Fechar();
            if (_manager != null) Object.Destroy(_manager);
            if (_db != null) Object.Destroy(_db);
            yield return null;
        }

        private Button Casa(string caminho)
        {
            var t = _painel.transform.Find(caminho);
            Assert.NotNull(t, $"Sem '{caminho}' no painel — o montador mudou os caminhos.");
            return t.GetComponent<Button>();
        }

        private PainelDoItem Detalhe()
        {
            var d = _painel.GetComponentInChildren<PainelDoItem>(true);
            Assert.NotNull(d, "O painel não tem a coluna de detalhe (PainelDoItem).");
            return d;
        }

        [UnityTest]
        public IEnumerator EscolherUmItem_OMostraNoDetalhe()
        {
            var escudo = new ItemInstance(Broquel, 1);
            Assert.NotNull(escudo.Def, "O ItemDatabase não resolveu o Broquel.");
            InventoryManager.Instance.Main.AddAt(escudo, 0);

            _painel.Abrir();
            yield return null;
            Assert.AreEqual("Damião", Detalhe().TituloAtual, "Sem escolha, o detalhe mostra o Damião.");

            Casa("Janela/Mochila/Slot_0").onClick.Invoke();
            yield return null;

            StringAssert.Contains(escudo.Def.Nome, Detalhe().TituloAtual,
                "Escolher o Broquel não o descreveu na coluna de detalhe.");
        }

        [UnityTest]
        public IEnumerator VestirUmConsumivel_ERecusadoComOPorque()
        {
            var erva = new ItemInstance(Erva, 1);
            Assert.NotNull(erva.Def, "O ItemDatabase não resolveu a Erva de Ancoragem.");
            InventoryManager.Instance.Main.AddAt(erva, 0);

            _painel.Abrir();
            yield return null;

            Casa("Janela/Mochila/Slot_0").onClick.Invoke();
            Casa("Janela/Corpo/Corpo_0").onClick.Invoke();
            yield return null;

            StringAssert.Contains("não se veste", Detalhe().AvisoAtual,
                "A recusa ao vestir um consumível voltou a ser muda.");
            Assert.AreEqual(Erva, InventoryManager.Instance.Main.GetSlot(0)?.ItemDefId,
                "A erva recusada saiu da mochila.");
        }

        [UnityTest]
        public IEnumerator AsTresColunas_NaoSeSobrepoem_EODetalheFicaDentroDaJanela()
        {
            _painel.Abrir();
            yield return null;
            Canvas.ForceUpdateCanvases();

            var janela = (RectTransform)_painel.transform.Find("Janela");
            var mochila = (RectTransform)_painel.transform.Find("Janela/Mochila");
            var corpo = (RectTransform)_painel.transform.Find("Janela/Corpo");
            var detalhe = (RectTransform)Detalhe().transform;

            Rect rJ = Tela(janela), rM = Tela(mochila), rC = Tela(corpo), rD = Tela(detalhe);

            Assert.IsFalse(rM.Overlaps(rC), $"Mochila {rM} e Corpo {rC} se sobrepõem.");
            Assert.IsFalse(rC.Overlaps(rD), $"Corpo {rC} e Detalhe {rD} se sobrepõem.");
            Assert.IsTrue(rJ.Contains(rD.min) && rJ.Contains(rD.max),
                $"O Detalhe {rD} sai da janela {rJ} — o defeito da ficha antiga, pendurada fora da borda.");
        }

        private static Rect Tela(RectTransform rt)
        {
            var c = new Vector3[4];
            rt.GetWorldCorners(c);
            return Rect.MinMaxRect(c[0].x, c[0].y, c[2].x, c[2].y);
        }
    }
}
