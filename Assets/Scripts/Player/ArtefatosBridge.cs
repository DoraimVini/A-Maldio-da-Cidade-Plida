using System.Collections.Generic;
using UnityEngine;
using FavelaAmarela.Core.Artefatos;
using FavelaAmarela.Inventario;
using FavelaAmarela.Runtime.Enemies;
using FavelaAmarela.Runtime.GameLoop;
using FavelaAmarela.Runtime.Itens;

namespace FavelaAmarela.Player
{
    /// <summary>
    /// Camada Runtime. Bridge dos Artefatos de Damião: dona do inventário de quatro slots,
    /// dos quatro cooldowns independentes e da tradução id→asset.
    ///
    /// <para>É separada da <see cref="MaoFisicaBridge"/> de propósito: aquela guarda um único
    /// relógio de habilidade, e aqui são quatro habilidades que recarregam sozinhas.</para>
    ///
    /// <para>Um Artefato só vale — passiva <b>e</b> habilidade — enquanto estiver num slot.
    /// Coletar não basta; é preciso escolher o que carregar.</para>
    /// </summary>
    [AddComponentMenu("Favela Amarela/Artefatos Bridge")]
    public sealed class ArtefatosBridge : MonoBehaviour
    {
        [Header("Catálogo")]
        [Tooltip("Todos os ArtefatoDef do jogo. Vazio = carrega de Resources/Artefatos. [ASSET]")]
        [SerializeField] private ArtefatoDef[] catalogo;

        [Header("Revelação (Recitar o Aklo)")]
        [Tooltip("Arte do sinal que paira sobre a entidade revelada. Vazio = quadrado procedural. [ASSET]")]
        [SerializeField] private Sprite spriteDeRevelacao;

        [Tooltip("Cor do sinal de revelação.")]
        [SerializeField] private Color corDeRevelacao = new Color(1f, 0.92f, 0.016f, 0.85f);

        [Tooltip("Altura do sinal acima da entidade revelada.")]
        [SerializeField] private float alturaDoSinal = 1.2f;

        [Header("Aplacamento (Sibilo de Yig)")]
        [Tooltip("Camadas consideradas na varredura de entidades. Vazio = todas.")]
        [SerializeField] private LayerMask camadasDeEntidade = ~0;

        private readonly InventarioDeArtefatos _inventario = new InventarioDeArtefatos();
        private readonly Dictionary<string, ArtefatoDef> _porId = new Dictionary<string, ArtefatoDef>();
        private readonly float[] _ultimoUso = new float[InventarioDeArtefatos.TotalDeSlots];

        private IContextoDeArtefato _contexto;
        private PlayerMovement _movimento;

        /// <summary>Disparado quando a composição dos slots muda — a UI redesenha por aqui.</summary>
        public event System.Action OnArtefatosMudaram;

        /// <summary>Os quatro slots de Artefato de Damião.</summary>
        public InventarioDeArtefatos Inventario => _inventario;

        private void Awake()
        {
            _movimento = GetComponent<PlayerMovement>();
            if (_movimento == null)
                Debug.LogError($"[ArtefatosBridge] '{name}' não tem PlayerMovement — o Resguardo do Sinal não vai calar nada.", this);

            _contexto = new ContextoDeArtefatoUnity(this);

            ConstruirCatalogo();

            for (int i = 0; i < _ultimoUso.Length; i++)
                _ultimoUso[i] = float.NegativeInfinity;

            _inventario.OnMudou += NotificarMudanca;
        }

        private void OnDestroy() => _inventario.OnMudou -= NotificarMudanca;

        private void NotificarMudanca() => OnArtefatosMudaram?.Invoke();

        private void ConstruirCatalogo()
        {
            var fonte = catalogo != null && catalogo.Length > 0
                ? catalogo
                : Resources.LoadAll<ArtefatoDef>("Artefatos");

            foreach (var def in fonte)
            {
                if (def == null || string.IsNullOrEmpty(def.Id)) continue;
                _porId[def.Id] = def;
            }
        }

        // ── Consulta (a UI lê por aqui) ───────────────────────────────────────

        /// <summary>O <c>ArtefatoDef</c> encaixado no slot, ou <c>null</c>.</summary>
        public ArtefatoDef DefNoSlot(int slot)
        {
            string id = _inventario.IdNoSlot(slot);
            if (string.IsNullOrEmpty(id)) return null;
            return _porId.TryGetValue(id, out var def) ? def : null;
        }

        /// <summary>Nome diegético da habilidade no slot, ou vazio.</summary>
        public string NomeDaHabilidade(int slot) => DefNoSlot(slot)?.NomeDaHabilidade ?? "";

        /// <summary>Se a habilidade do slot pode disparar agora.</summary>
        public bool EstaPronto(int slot)
        {
            var def = DefNoSlot(slot);
            if (def == null) return false;

            return def.CriarAtivo().PodeAtivar(ResilienciaAtual(), Time.time - _ultimoUso[slot]);
        }

        /// <summary>Progresso da recarga em [0, 1]; 1 = pronta.</summary>
        public float ProgressoCooldown(int slot)
        {
            var def = DefNoSlot(slot);
            if (def == null || def.Cooldown <= 0f) return 1f;

            return Mathf.Clamp01((Time.time - _ultimoUso[slot]) / def.Cooldown);
        }

        // ── Ações ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Encaixa um Artefato no primeiro slot livre. Chamado ao coletar o item
        /// correspondente. Sem slot livre, o Artefato fica de fora — é escolha do jogador
        /// trocar depois.
        /// </summary>
        /// <returns>O slot ocupado, ou -1 se não coube ou o id não existe.</returns>
        public int EquiparNoPrimeiroSlotLivre(string artefatoId)
        {
            if (!_porId.ContainsKey(artefatoId) || _inventario.Contem(artefatoId)) return -1;

            int slot = _inventario.PrimeiroSlotLivre();
            if (slot < 0) return -1;

            _inventario.Equipar(artefatoId, slot);
            _ultimoUso[slot] = float.NegativeInfinity; // entra pronto para uso
            return slot;
        }

        /// <summary>Encaixa um Artefato num slot específico, trocando com o que estiver lá.</summary>
        public bool Equipar(string artefatoId, int slot)
        {
            if (!_porId.ContainsKey(artefatoId)) return false;
            if (slot < 0 || slot >= InventarioDeArtefatos.TotalDeSlots) return false;
            if (_inventario.Contem(artefatoId)) return false;

            _inventario.Equipar(artefatoId, slot);
            _ultimoUso[slot] = float.NegativeInfinity;
            return true;
        }

        /// <summary>Retira o Artefato do slot.</summary>
        public bool Desequipar(int slot) => _inventario.Desequipar(slot) != null;

        /// <summary>
        /// Dispara a habilidade do slot, se houver Artefato, RM suficiente e recarga pronta.
        /// O POCO decide e aplica; aqui só se cobra o custo e se arma o relógio.
        /// </summary>
        public void TryUsarArtefato(int slot)
        {
            var def = DefNoSlot(slot);
            if (def == null) return;

            var ativo = def.CriarAtivo();
            if (!ativo.PodeAtivar(ResilienciaAtual(), Time.time - _ultimoUso[slot])) return;

            var resultado = ativo.Ativar(_contexto);
            if (!resultado.Sucesso) return;

            _ultimoUso[slot] = Time.time;

            if (resultado.CustoRM > 0f)
                GameManager.Instance?.Resiliencia?.SofrerTrauma(resultado.CustoRM);
        }

        private static float ResilienciaAtual()
            => GameManager.Instance?.Resiliencia?.Atual ?? 0f;

        // ── Contexto concreto ─────────────────────────────────────────────────

        /// <summary>
        /// Implementação Unity do <see cref="IContextoDeArtefato"/>. Aninhada porque só a
        /// bridge a usa e ela precisa dos campos serializados dela.
        /// </summary>
        private sealed class ContextoDeArtefatoUnity : IContextoDeArtefato
        {
            private readonly ArtefatosBridge _bridge;

            public ContextoDeArtefatoUnity(ArtefatosBridge bridge) => _bridge = bridge;

            /// <inheritdoc />
            public void RevelarEntidades(float raio, float duracao)
            {
                var achados = Physics2D.OverlapCircleAll(_bridge.transform.position, raio, _bridge.camadasDeEntidade);
                foreach (var col in achados)
                {
                    var inimigo = col.GetComponentInParent<EnemyBase>();
                    if (inimigo == null) continue;

                    MarcadorDeRevelacao.Marcar(inimigo.gameObject, duracao,
                        _bridge.spriteDeRevelacao, _bridge.corDeRevelacao, _bridge.alturaDoSinal);
                }
            }

            /// <inheritdoc />
            public void AncorarJogador(float valor)
                => GameManager.Instance?.Resiliencia?.Ancorar(valor);

            /// <inheritdoc />
            public void SilenciarPassos(float duracao)
                => _bridge._movimento?.SilenciarPassos(duracao);

            /// <inheritdoc />
            public void AplacarSerpentes(float raio, float duracao)
            {
                var achados = Physics2D.OverlapCircleAll(_bridge.transform.position, raio, _bridge.camadasDeEntidade);
                foreach (var col in achados)
                {
                    var fsm = col.GetComponentInParent<EnemyStateMachine>();
                    if (fsm == null) continue;

                    fsm.Atordoar(duracao);
                }
            }
        }
    }
}
