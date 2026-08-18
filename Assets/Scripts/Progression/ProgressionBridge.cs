using System.Collections.Generic;
using UnityEngine;
using FavelaAmarela.Core.Progression;
using FavelaAmarela.Progression;

namespace FavelaAmarela.Runtime.Progression
{
    /// <summary>
    /// Camada Runtime. Adaptador do POCO <see cref="Progressao"/>: traduz <c>EcoDef</c> (asset)
    /// para os ids que o Core manipula, e faz a progressão <b>existir de verdade em runtime</b>.
    ///
    /// <para><b>O buraco que isto fecha (auditoria 2026-08-14):</b> o antigo
    /// <c>ProgressionManager</c> era um <c>MonoBehaviour</c> que <b>não estava em cena nenhuma</b>.
    /// <c>Instance</c> era sempre <c>null</c>, e os 4 consumidores rodavam permanentemente no
    /// fallback — nível 1 fixo, sem Ecos, progressão nunca salva. Pior: o loot libera tiers
    /// comparando <c>NivelMinimo</c> com o nível atual, então <b>nenhum item de tier acima de 1
    /// podia cair no jogo</b>. Não era bug de sorteio; era o manager não existir.</para>
    ///
    /// <para><b>Por que auto-instanciação e não wiring de cena:</b> mesmo padrão do
    /// <c>GerenciadorDeSave</c> — <c>[RuntimeInitializeOnLoadMethod(BeforeSceneLoad)]</c> +
    /// <c>DontDestroyOnLoad</c>. Ele nasce antes de qualquer cena, inclusive as que ainda não
    /// existem, e sobrevive às trocas. Depender de alguém lembrar de arrastar o componente para
    /// cada cena nova é exatamente como o sistema morreu da primeira vez. Progressão que zera ao
    /// trocar de cena seria regressão silenciosa.</para>
    ///
    /// <para>Um bridge colocado à mão numa cena continua funcionando: o guarda de instância única
    /// no <c>Awake</c> faz o duplicado se destruir.</para>
    /// </summary>
    [AddComponentMenu("Favela Amarela/Progressão/Bridge de Progressão")]
    [DefaultExecutionOrder(-150)]
    public sealed class ProgressionBridge : MonoBehaviour
    {
        private static ProgressionBridge _instancia;

        /// <summary>
        /// Acesso ao bridge vivo. Nunca é <c>null</c> em runtime — a auto-instanciação garante.
        /// </summary>
        public static ProgressionBridge Instancia => _instancia;

        [Header("Curva de Exposição")]
        [Tooltip("Exposição acumulada por nível. O tamanho do vetor É o teto de nível. " +
                 "Num jogo de ~4h a curva é fechada de propósito.")]
        [SerializeField]
        private int[] curvaDeExposicao =
        {
            0,      // Nível 1
            100,    // Nível 2
            300,    // Nível 3
            600,    // Nível 4
            1000,   // Nível 5
            1500,   // Nível 6
            2100,   // Nível 7
            2800,   // Nível 8
            3600,   // Nível 9
            4500,   // Nível 10
            5500,   // Nível 11
            6600    // Nível 12 (teto)
        };

        [Header("Catálogo")]
        [Tooltip("Todos os EcoDef do jogo, para resolver id→asset. " +
                 "Vazio = carrega de Resources/Ecos. [ASSET]")]
        [SerializeField] private EcoDef[] catalogoDeEcos;

        private Progressao _progressao;
        private Dictionary<string, EcoDef> _porId;

        /// <summary>O POCO com a regra. Criado no <c>Awake</c>, nunca nulo depois disso.</summary>
        public Progressao Progressao => _progressao;

        /// <summary>Nível de Exposição corrente.</summary>
        public int NivelAtual => _progressao?.NivelAtual ?? 1;

        /// <summary>Exposição acumulada.</summary>
        public int ExposicaoAtual => _progressao?.ExposicaoAtual ?? 0;

        /// <summary>Pontos de Eco por gastar.</summary>
        public int PontosDeEcoDisponiveis => _progressao?.PontosDeEcoDisponiveis ?? 0;

        /// <summary>Disparado ao destrancar um Eco, já resolvido para o asset.</summary>
        public event System.Action<EcoDef> OnEcoDesbloqueado;

        /// <summary>Disparado a cada nível ganho.</summary>
        public event System.Action<int> OnLevelUp;

        /// <summary>Disparado sempre que Exposição é somada.</summary>
        public event System.Action OnExposicaoGanha;

        /// <summary>
        /// Nasce antes de qualquer cena carregar. Sem isto, a progressão só existiria nas cenas
        /// onde alguém lembrou de colocar o componente — que é como ela morreu antes.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void GarantirInstancia()
        {
            if (_instancia != null) return;

            var go = new GameObject("ProgressionBridge (automático)");
            go.AddComponent<ProgressionBridge>(); // o Awake faz o DontDestroyOnLoad
        }

        private void Awake()
        {
            if (_instancia != null && _instancia != this)
            {
                Destroy(gameObject);
                return;
            }

            _instancia = this;
            DontDestroyOnLoad(gameObject);

            _progressao = new Progressao(curvaDeExposicao);
            _progressao.OnEcoDesbloqueado += HandleEcoDesbloqueado;
            _progressao.OnLevelUp += HandleLevelUp;
            _progressao.OnExposicaoGanha += HandleExposicaoGanha;
        }

        private void OnDestroy()
        {
            if (_instancia != this) return;

            if (_progressao != null)
            {
                _progressao.OnEcoDesbloqueado -= HandleEcoDesbloqueado;
                _progressao.OnLevelUp -= HandleLevelUp;
                _progressao.OnExposicaoGanha -= HandleExposicaoGanha;
            }

            _instancia = null;
        }

        private void HandleEcoDesbloqueado(string id)
        {
            var def = ResolverEco(id);
            if (def != null) OnEcoDesbloqueado?.Invoke(def);
        }

        private void HandleLevelUp(int nivel) => OnLevelUp?.Invoke(nivel);
        private void HandleExposicaoGanha() => OnExposicaoGanha?.Invoke();

        // ── API para o mundo ─────────────────────────────────────────────────

        /// <summary>Soma Exposição por explorar ou por evento narrativo.</summary>
        public void AdicionarExposicao(int valor) => _progressao?.AdicionarExposicao(valor);

        /// <summary>
        /// Tenta destrancar um Eco, traduzindo os pré-requisitos do asset para ids antes de
        /// consultar o POCO.
        /// </summary>
        public bool TryDesbloquearEco(EcoDef eco)
        {
            if (eco == null || _progressao == null) return false;

            List<string> preRequisitos = null;
            if (eco.PreRequisitos != null && eco.PreRequisitos.Count > 0)
            {
                preRequisitos = new List<string>(eco.PreRequisitos.Count);
                foreach (var pr in eco.PreRequisitos)
                {
                    if (pr != null && !string.IsNullOrEmpty(pr.Id)) preRequisitos.Add(pr.Id);
                }
            }

            if (_progressao.TryDesbloquearEco(eco.Id, preRequisitos, out string motivo))
            {
                Debug.Log($"[ProgressionBridge] Eco desbloqueado: {eco.NomeDoEco} ({eco.Caminho})", this);
                return true;
            }

            Debug.LogWarning($"[ProgressionBridge] '{eco.NomeDoEco}' não desbloqueado: {motivo}", this);
            return false;
        }

        /// <summary>Ecos destrancados, já resolvidos para asset. Ids órfãos são ignorados.</summary>
        public IEnumerable<EcoDef> EcosDesbloqueados()
        {
            if (_progressao == null) yield break;

            foreach (var id in _progressao.EcosDesbloqueados)
            {
                var def = ResolverEco(id);
                if (def != null) yield return def;
            }
        }

        // ── Persistência ─────────────────────────────────────────────────────

        /// <summary>Estado serializável para o save.</summary>
        public ProgressionSaveData CapturarSaveData()
        {
            if (_progressao == null) return new ProgressionSaveData();

            return new ProgressionSaveData(
                _progressao.NivelAtual,
                _progressao.ExposicaoAtual,
                _progressao.PontosDeEcoDisponiveis,
                new List<string>(_progressao.EcosDesbloqueados));
        }

        /// <summary>
        /// Restaura do save, descartando ids de Ecos que não existem mais no catálogo — um asset
        /// removido entre versões não deve travar o carregamento.
        /// </summary>
        public void RestaurarSaveData(ProgressionSaveData dados)
        {
            if (dados == null || _progressao == null) return;

            var validos = new List<string>();
            if (dados.ecosDesbloqueadosIds != null)
            {
                foreach (var id in dados.ecosDesbloqueadosIds)
                {
                    if (ResolverEco(id) != null) validos.Add(id);
                }
            }

            _progressao.Restaurar(dados.nivelAtual, dados.exposicaoAtual,
                                  dados.pontosDeEcoDisponiveis, validos);
        }

        // ── Catálogo ─────────────────────────────────────────────────────────

        private EcoDef ResolverEco(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;

            _porId ??= MontarCatalogo();
            return _porId.TryGetValue(id, out var def) ? def : null;
        }

        private Dictionary<string, EcoDef> MontarCatalogo()
        {
            var fonte = catalogoDeEcos != null && catalogoDeEcos.Length > 0
                ? catalogoDeEcos
                : Resources.LoadAll<EcoDef>("Ecos");

            var mapa = new Dictionary<string, EcoDef>();
            foreach (var eco in fonte)
            {
                if (eco == null || string.IsNullOrEmpty(eco.Id)) continue;
                mapa[eco.Id] = eco;
            }

            return mapa;
        }
    }
}
