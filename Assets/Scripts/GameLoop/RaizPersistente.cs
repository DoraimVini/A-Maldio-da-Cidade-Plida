using UnityEngine;
using FavelaAmarela.Inventario;
using FavelaAmarela.Runtime.Persistencia;
using FavelaAmarela.Runtime.Progression;

namespace FavelaAmarela.Runtime.GameLoop
{
    /// <summary>
    /// Casa única do que <b>sobrevive à troca de cena</b>. Cria os quatro serviços persistentes
    /// sob um mesmo GameObject, em ordem explícita.
    ///
    /// <para><b>O que ela NÃO resolve, para não vender o que não entrega:</b> hoje <b>não existe
    /// dependência de ordem</b> entre os quatro — o <c>InventoryManager</c> só instancia um
    /// prefab, o <c>ItemDatabase</c> só lê <c>Resources</c>, e nenhum toca o outro na
    /// inicialização. A ordem aqui é um <b>gancho pronto</b> para quando houver, não a correção
    /// de um bug atual.</para>
    ///
    /// <para><b>O que ela resolve de fato:</b> antes eram quatro <c>GameObject</c> soltos na cena
    /// <c>DontDestroyOnLoad</c>, cada um se criando por conta própria, e nenhum lugar do código
    /// respondia "o que atravessa as cenas?". Agora há um só, com a lista à vista.</para>
    ///
    /// <para><b>Por que não é um "GameManager".</b> Ela não intermedia chamada nenhuma: ninguém
    /// pede nada <i>a ela</i>. Só garante existência e parentesco, e sai da frente. Um componente
    /// que encaminha acessos vira poço gravitacional — foi exatamente assim que o
    /// <c>GameManager</c> acumulou seis responsabilidades antes de ser desmontado.</para>
    ///
    /// <para><b>Tolerante à ordem de arranque:</b> a Unity não garante ordem entre métodos
    /// <c>[RuntimeInitializeOnLoadMethod]</c> do mesmo tipo de carga. Se um dos serviços nascer
    /// antes desta raiz, ele é simplesmente adotado — cada um continua idempotente e ninguém é
    /// duplicado.</para>
    /// </summary>
    public static class RaizPersistente
    {
        /// <summary>Nome do GameObject, com marcadores para destacá-lo na Hierarchy.</summary>
        private const string NomeDoObjeto = "— Persistente —";

        private static GameObject _raiz;

        /// <summary>O GameObject que hospeda os serviços persistentes, ou <c>null</c> fora de play.</summary>
        public static GameObject Raiz => _raiz;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Montar()
        {
            if (_raiz != null) return;

            _raiz = new GameObject(NomeDoObjeto);
            Object.DontDestroyOnLoad(_raiz);

            // Ordem explícita. Nenhuma dependência real hoje — ver o resumo da classe. Está
            // escrita do mais básico para o mais dependente, que é a ordem que faria sentido se
            // alguma surgir: o catálogo antes de quem resolve itens por ele, e a progressão
            // depois do save, de quem ela lê o estado.
            ItemDatabase.GarantirInstancia();
            InventoryManager.GarantirInstancia();
            GerenciadorDeSave.GarantirInstancia();
            ProgressionBridge.GarantirInstancia();

            Adotar(ItemDatabase.Instance);
            Adotar(InventoryManager.Instance);
            Adotar(GerenciadorDeSave.Instancia);
            Adotar(ProgressionBridge.Instancia);
        }

        /// <summary>
        /// Traz um serviço para debaixo da raiz. Aceita nulo — um serviço que falhou em nascer
        /// (prefab ausente, por exemplo) já registra o próprio erro; não cabe a esta classe
        /// repetir o aviso.
        /// </summary>
        private static void Adotar(Component servico)
        {
            if (servico == null || _raiz == null) return;

            var go = servico.gameObject;
            if (go == _raiz || go.transform.parent == _raiz.transform) return;

            go.transform.SetParent(_raiz.transform, worldPositionStays: false);
        }
    }
}
