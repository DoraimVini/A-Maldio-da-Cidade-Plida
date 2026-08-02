using UnityEngine;
using FavelaAmarela.Core.Combat;
using FavelaAmarela.Core.Itens;
using FavelaAmarela.Player;
using FavelaAmarela.Runtime.Combat;

namespace FavelaAmarela.Runtime.Itens
{
    /// <summary>
    /// Camada Runtime (MonoBehaviour). Dono do <see cref="Inventario"/> de Damião e
    /// <b>único ponto</b> onde o efeito de um consumível vira mudança no mundo.
    ///
    /// <para>A divisão é de propósito: o <see cref="Inventario"/> (Core) sabe contar,
    /// empilhar e gastar, mas <b>não sabe o que é Vitalidade nem Resiliência</b> — ele
    /// devolve um <see cref="EfeitoDeUso"/> e esta ponte decide onde aplicar. É o que
    /// mantém o inventário testável sem a Unity rodando.</para>
    /// </summary>
    [RequireComponent(typeof(VitalidadeBridge))]
    [AddComponentMenu("Favela Amarela/Itens/Inventário")]
    public sealed class InventarioBridge : MonoBehaviour
    {
        [Header("Capacidade")]
        [Tooltip("Quantas posições o inventário tem. Enxuto de propósito — escolher o que " +
                 "deixar para trás é parte da tensão.")]
        [Min(1)]
        [SerializeField] private int posicoes = Inventario.PosicoesPadrao;

        [Header("Itens iniciais (opcional)")]
        [Tooltip("O que Damião já carrega ao começar. Vazio = mãos vazias.")]
        [SerializeField] private ItemConfig[] itensIniciais;

        private Inventario _inventario;
        private VitalidadeBridge _vitalidade;
        private ResilienciaMental _resiliencia;
        private MaoFisicaBridge _maoFisica;

        /// <summary>O inventário corrente. Nunca null depois do <c>Awake</c>.</summary>
        public Inventario Inventario => _inventario;

        private void Awake()
        {
            _inventario = new Inventario(posicoes);
            _vitalidade = GetComponent<VitalidadeBridge>();
            _maoFisica = GetComponent<MaoFisicaBridge>();

            if (itensIniciais == null) return;

            foreach (var config in itensIniciais)
            {
                if (config == null) continue;
                _inventario.Adicionar(config.CriarDefinicao());
            }
        }

        /// <summary>
        /// Injeta a Resiliência Mental (criada pelo <c>GameManager</c>). Sem ela, itens de
        /// Ancoragem não têm onde agir — o uso é recusado em vez de sumir com o item.
        /// </summary>
        public void Bind(ResilienciaMental resiliencia) => _resiliencia = resiliencia;

        /// <summary>Guarda um item pelo asset. Devolve quantos não couberam.</summary>
        public int Guardar(ItemConfig config, int quantidade = 1)
            => config == null ? quantidade : _inventario.Adicionar(config.CriarDefinicao(), quantidade);

        /// <summary>
        /// Usa o item de uma posição e aplica o efeito no mundo.
        ///
        /// <para><b>Só consome se o efeito tiver onde agir.</b> Usar uma Ancoragem sem
        /// Resiliência injetada gastaria o item à toa — o pior tipo de bug de inventário,
        /// porque o jogador perde recurso e não vê nada acontecer.</para>
        /// </summary>
        /// <returns>Se o item foi usado de fato.</returns>
        public bool Usar(int indice)
        {
            var pilha = _inventario.Ver(indice);
            if (pilha.Vazia) return false;

            // Arma: empunhar em vez de consumir. Ela continua no inventário — trocar de arma
            // não a destrói, e é isso que permite voltar para a anterior num Refúgio.
            if (pilha.Item.EhEquipavel) return Empunhar(pilha.Item);

            if (!TemOndeAplicar(pilha.Item.Efeito))
            {
                Debug.LogWarning($"[InventarioBridge] '{pilha.Item.Nome}' não tem onde agir " +
                                 "agora — o item não foi gasto.", this);
                return false;
            }

            var efeito = _inventario.Consumir(indice);
            if (!efeito.Houve) return false;

            Aplicar(efeito);
            return true;
        }

        /// <summary>
        /// Empunha a arma na Mão Física. O item <b>permanece</b> no inventário — é o que
        /// permite voltar para a arma anterior depois (troca sob a luz de um Refúgio, ver
        /// <c>systems/abilities.md</c>).
        /// </summary>
        private bool Empunhar(DefinicaoDeItem item)
        {
            if (_maoFisica == null)
            {
                Debug.LogError("[InventarioBridge] Não há MaoFisicaBridge neste objeto — " +
                               $"'{item.Nome}' não pode ser empunhada.", this);
                return false;
            }

            _maoFisica.EquiparArma(item.ArmaEquipavel.Value);
            return true;
        }

        private bool TemOndeAplicar(TipoDeEfeito tipo) => tipo switch
        {
            TipoDeEfeito.Ancorar => _resiliencia != null,
            TipoDeEfeito.Estabilizar => _vitalidade != null && _vitalidade.Vitalidade != null,
            // Damião ainda não tem uma Ferida de Aklo própria (só inimigos sangram), então
            // estancar não teria alvo. Recusar evita gastar o item por nada.
            TipoDeEfeito.EstancarFeridas => false,
            _ => false,
        };

        private void Aplicar(EfeitoDeUso efeito)
        {
            switch (efeito.Tipo)
            {
                case TipoDeEfeito.Ancorar:
                    _resiliencia.Ancorar(efeito.Potencia);
                    break;

                case TipoDeEfeito.Estabilizar:
                    _vitalidade.Vitalidade.Curar(efeito.Potencia);
                    break;
            }
        }
    }
}
