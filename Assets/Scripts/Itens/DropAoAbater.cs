using UnityEngine;
using FavelaAmarela.Core.Loot;
using FavelaAmarela.Inventario;
using FavelaAmarela.Progression;
using FavelaAmarela.Runtime.Enemies;

namespace FavelaAmarela.Runtime.Itens
{
    /// <summary>
    /// Camada Runtime: larga o espólio no chão quando a unidade é abatida. Adaptador puro —
    /// a regra do sorteio vive no POCO <see cref="SorteioDeDrop"/>; aqui só se lê o nível de
    /// Exposição, materializa o resultado e some.
    ///
    /// <para>O item cai no chão e é recolhido com E, como todo o resto — nada de coleta
    /// automática por toque.</para>
    /// </summary>
    [RequireComponent(typeof(EnemyBase))]
    [AddComponentMenu("Favela Amarela/Itens/Drop ao Abater")]
    public sealed class DropAoAbater : MonoBehaviour
    {
        [Header("Espólio")]
        [Tooltip("Tabela de drop deste arquétipo. [ASSET]")]
        [SerializeField] private TabelaDeDrop tabela;

        [Header("Materialização")]
        [Tooltip("Prefab de coletável usado para cada item. Vazio = um objeto mínimo é montado em runtime.")]
        [SerializeField] private ColetavelDeItem prefabColetavel;

        [Tooltip("Espalhamento em torno do corpo, para dois itens não caírem sobrepostos.")]
        [Min(0f)]
        [SerializeField] private float raioDeEspalhamento = 0.4f;

        private EnemyBase _enemyBase;
        private SorteioDeDrop _sorteio;
        private IFonteDeAleatoriedade _fonte;

        private void Awake()
        {
            _sorteio = new SorteioDeDrop();
            _fonte = new FonteDeAleatoriedadeUnity();

            _enemyBase = GetComponent<EnemyBase>();
            if (_enemyBase == null)
            {
                Debug.LogError($"[DropAoAbater] '{name}' não tem EnemyBase — nada será largado.", this);
                return;
            }

            _enemyBase.OnAbatido += HandleAbatido;
        }

        private void OnDestroy()
        {
            if (_enemyBase != null) _enemyBase.OnAbatido -= HandleAbatido;
        }

        private void HandleAbatido()
        {
            if (tabela == null) return;

            var banco = ItemDatabase.Instance;
            if (banco == null)
            {
                Debug.LogError($"[DropAoAbater] ItemDatabase.Instance ausente — espólio de '{name}' perdido.", this);
                return;
            }

            int nivel = ProgressionManager.Instance != null ? ProgressionManager.Instance.NivelAtual : 1;
            var sorteados = _sorteio.Sortear(tabela.ProjetarCandidatos(), nivel, _fonte, tabela.TetoDeItens);

            for (int i = 0; i < sorteados.Count; i++)
            {
                var def = banco.Get(sorteados[i].ItemDefId);
                if (def == null)
                {
                    Debug.LogWarning($"[DropAoAbater] Item '{sorteados[i].ItemDefId}' não existe no ItemDatabase.", this);
                    continue;
                }

                Materializar(def, sorteados[i].Quantidade);
            }
        }

        private void Materializar(ItemDef def, int quantidade)
        {
            Vector3 posicao = transform.position + Deslocamento();

            var coletavel = prefabColetavel != null
                ? Instantiate(prefabColetavel, posicao, Quaternion.identity)
                : MontarColetavelMinimo(def, posicao);

            // Espólio de inimigo nasce sem chave de save: quem persiste é o abate do inimigo.
            coletavel.Configurar(def, quantidade);
            coletavel.name = $"Drop_{def.Nome}";

            var sr = coletavel.GetComponent<SpriteRenderer>();
            if (sr != null) sr.sortingOrder = Mathf.RoundToInt(-posicao.y * 10f);

            // Só agora o objeto entra em cena. O Awake do ColetavelDeItem exige um ItemDef, e
            // AddComponent o dispararia ANTES do Configurar acima — o item era entregue certo,
            // mas cada drop cuspia um LogError de "está sem ItemDef".
            if (!coletavel.gameObject.activeSelf) coletavel.gameObject.SetActive(true);
        }

        private Vector3 Deslocamento()
        {
            if (raioDeEspalhamento <= 0f) return Vector3.zero;

            float angulo = _fonte.ProximoValor() * Mathf.PI * 2f;
            float raio = _fonte.ProximoValor() * raioDeEspalhamento;
            return new Vector3(Mathf.Cos(angulo) * raio, Mathf.Sin(angulo) * raio, 0f);
        }

        private static ColetavelDeItem MontarColetavelMinimo(ItemDef def, Vector3 posicao)
        {
            var go = new GameObject($"Drop_{def.Nome}", typeof(SpriteRenderer), typeof(BoxCollider2D));
            go.transform.position = posicao;

            // Nasce inativo: assim o Awake do ColetavelDeItem só roda depois do Configurar,
            // que é quem entrega o ItemDef. Quem reativa é o Materializar.
            go.SetActive(false);

            var col = go.GetComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = Vector2.one;

            return go.AddComponent<ColetavelDeItem>();
        }
    }
}
