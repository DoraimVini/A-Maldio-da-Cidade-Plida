using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using FavelaAmarela.Core.Navegacao;

namespace FavelaAmarela.Runtime.Navegacao
{
    /// <summary>
    /// A ponte entre o mundo desenhado e a <see cref="BuscaDeCaminho"/>: responde <b>onde dá
    /// para pisar</b>.
    ///
    /// <para><b>Pergunta à física, não ao tilemap.</b> Foi a decisão de arquitetura desta peça,
    /// e ela veio de uma medição: <b>tudo que bloqueia neste jogo está na layer
    /// <c>Obstacle</c></b> — o tilemap <c>Colisao</c>, as paredes do Santuário, os nobres
    /// fossilizados do Castelo, e o <b>Lago de Hali</b>, que é um <c>PolygonCollider2D</c> solto
    /// e não pertence a tilemap nenhum.</para>
    ///
    /// <para>Ler o tilemap teria criado uma <b>segunda representação do mundo</b>: o inimigo
    /// contornaria paredes de tilemap e atravessaria o lago, porque a navegação não saberia
    /// dele. Perguntando à física, navegação e colisão concordam <i>por construção</i> — e este
    /// repositório já tem cicatrizes de duas fontes da verdade para a mesma coisa (dois números
    /// de dano por inimigo, dois zooms de câmera, sete listas de cena).</para>
    ///
    /// <para><b>Sonda preguiçosa, com cache.</b> Assar o Deserto inteiro no arranque seria
    /// milhares de consultas para células que ninguém visita. Cada célula é consultada na
    /// primeira vez que a busca a alcança e guardada depois — o custo se espalha e some.</para>
    /// </summary>
    [AddComponentMenu("Favela Amarela/Navegação/Navegação do Mundo")]
    public sealed class NavegacaoDoMundo : MonoBehaviour, IMapaDeNavegacao
    {
        private static NavegacaoDoMundo _instancia;

        /// <summary>Instância única. Nula fora de Play — todo chamador deve tolerar isso.</summary>
        public static NavegacaoDoMundo Instancia => _instancia;

        [Header("O que bloqueia")]
        [Tooltip("Camadas que impedem passagem. Padrão: Obstacle — onde vivem o tilemap de " +
                 "colisão, as paredes e o Lago de Hali.")]
        [SerializeField] private LayerMask camadasQueBloqueiam;

        [Tooltip("Fração da célula usada como sonda. Menor que 1 para o ator caber na " +
                 "passagem; grande demais e corredores de uma célula viram parede.")]
        [Range(0.3f, 1f)]
        [SerializeField] private float fracaoDaSonda = 0.8f;

        private Grid _grade;
        private readonly Dictionary<Celula, bool> _cache = new Dictionary<Celula, bool>(2048);

        /// <summary>Nasce antes de qualquer cena, como as outras pontes do projeto.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void GarantirInstancia()
        {
            if (_instancia != null) return;

            var go = new GameObject("NavegacaoDoMundo (automático)");
            go.AddComponent<NavegacaoDoMundo>();
        }

        private void Awake()
        {
            if (_instancia != null && _instancia != this) { Destroy(gameObject); return; }

            _instancia = this;
            DontDestroyOnLoad(gameObject);

            // LayerMask serializada nasce ZERO num objeto criado por código -- e máscara zero
            // significa "nada bloqueia", ou seja, navegação que atravessa parede. É o mesmo
            // defeito que deixou o EnemyCombat sem alvo, então aqui ele é corrigido de saída.
            if (camadasQueBloqueiam.value == 0)
                camadasQueBloqueiam = LayerMask.GetMask("Obstacle");

            SceneManager.sceneLoaded += HandleCenaCarregada;
            ResolverGrade();
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= HandleCenaCarregada;
            if (_instancia == this) _instancia = null;
        }

        private void HandleCenaCarregada(Scene cena, LoadSceneMode modo)
        {
            // O mundo mudou por inteiro: o cache descreve a cena anterior e mentiria sobre esta.
            _cache.Clear();
            ResolverGrade();
        }

        /// <summary>
        /// Se já reclamamos da falta de malha <b>nesta cena</b>. Reiniciado a cada carga.
        /// </summary>
        private bool _jaAvisouSemMalha;

        private void ResolverGrade()
        {
            _grade = FindAnyObjectByType<Grid>();
            _jaAvisouSemMalha = false;

            // SEM AVISO AQUI (2026-09-02). Este método roda duas vezes onde não pode haver
            // malha: no Awake, que acontece em BeforeSceneLoad -- não existe cena ainda --, e
            // ao carregar o menu, que legitimamente não tem Grid nenhum. Avisar nos dois casos
            // NORMAIS é ensinar a ignorar o log, e o log é o único canal de runtime que temos.
            //
            // Quem avisa é o primeiro uso real: ver AvisarSeSemMalha.
        }

        /// <summary>
        /// Reclama da falta de malha <b>uma vez por cena</b>, e só quando alguém realmente tenta
        /// navegar. Cena sem Grid e sem ninguém navegando — o menu — é caso legítimo.
        /// </summary>
        private void AvisarSeSemMalha()
        {
            if (_grade != null || _jaAvisouSemMalha) return;

            _jaAvisouSemMalha = true;

            Debug.LogWarning("[Navegacao] Pediram caminho e não há Grid nesta cena — a busca não " +
                             "tem malha para percorrer, e quem depender dela vai andar em linha " +
                             "reta.", this);
        }

        /// <summary>Se a navegação está utilizável nesta cena.</summary>
        public bool Pronta => _grade != null;

        // ── Conversões ────────────────────────────────────────────────────────

        /// <summary>
        /// Posição do mundo → célula. Usa o <c>Grid</c> da cena de propósito: é ele que sabe
        /// que a malha é isométrica 1×0,5, e reimplementar essa conta aqui seria inventar a
        /// oitava lista de constantes que este projeto mantém à mão.
        /// </summary>
        public Celula ParaCelula(Vector3 mundo)
        {
            if (_grade == null)
            {
                AvisarSeSemMalha();
                return new Celula(0, 0);
            }


            var c = _grade.WorldToCell(mundo);
            return new Celula(c.x, c.y);
        }

        /// <summary>Célula → centro dela no mundo.</summary>
        public Vector3 ParaMundo(Celula c)
        {
            if (_grade == null) return Vector3.zero;

            return _grade.GetCellCenterWorld(new Vector3Int(c.X, c.Y, 0));
        }

        // ── A pergunta que a busca faz ────────────────────────────────────────

        /// <inheritdoc />
        public bool EhCaminhavel(Celula c)
        {
            if (_grade == null) return true;   // sem malha, nada bloqueia: degrada para o antigo

            if (_cache.TryGetValue(c, out bool livre)) return livre;

            Vector3 centro = ParaMundo(c);
            Vector2 tamanho = (Vector2)_grade.cellSize * fracaoDaSonda;

            livre = Physics2D.OverlapBox(centro, tamanho, 0f, camadasQueBloqueiam) == null;

            _cache[c] = livre;
            return livre;
        }

        /// <summary>
        /// Esquece o que foi medido. Necessário quando a geometria muda em runtime — uma porta
        /// que abre, um muro que cai. Sem isto o mundo mudaria e a navegação continuaria
        /// respondendo sobre o mundo anterior.
        /// </summary>
        public void Reavaliar() => _cache.Clear();

        /// <summary>
        /// Esquece uma região só — mais barato que <see cref="Reavaliar"/> quando se sabe o que
        /// mudou.
        /// </summary>
        public void Reavaliar(Vector3 centro, float raioEmCelulas)
        {
            if (_grade == null) return;

            var meio = ParaCelula(centro);
            int r = Mathf.CeilToInt(raioEmCelulas);

            for (int x = -r; x <= r; x++)
            for (int y = -r; y <= r; y++)
                _cache.Remove(new Celula(meio.X + x, meio.Y + y));
        }

        /// <summary>Quantas células já foram medidas. Para o console de diagnóstico.</summary>
        public int CelulasConhecidas => _cache.Count;
    }
}
