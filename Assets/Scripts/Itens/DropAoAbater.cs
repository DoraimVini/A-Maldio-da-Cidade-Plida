using UnityEngine;
using FavelaAmarela.Core.Loot;
using FavelaAmarela.Inventario;
using FavelaAmarela.Progression;
using FavelaAmarela.Runtime.Progression;
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
    // SEM [RequireComponent(typeof(EnemyBase))] -- ele ficou aqui por um commit depois de o
    // binding virar interface, e o estrago foi imediato: acrescentar este componente ao Abdul
    // arrastou um EnemyBase inteiro (e o Rigidbody2D que ELE exige) para um ator que é
    // deliberadamente sem corpo físico. Os guardas de física pegaram na mesma rodada --
    // gravidade 1, sem FreezeRotation, detecção Discrete.
    //
    // Exigir a classe contradiz o motivo de existir a interface: quem larga espólio é quem
    // sabe avisar que foi derrotado, não quem herda de uma classe específica.
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

        private IFonteDeEspolio _fonteDeEspolio;
        private SorteioDeDrop _sorteio;
        private IFonteDeAleatoriedade _fonte;

        private void Awake()
        {
            _sorteio = new SorteioDeDrop();
            _fonte = new FonteDeAleatoriedadeUnity();

            // Liga na INTERFACE, não na classe: o Abdul não é EnemyBase (implementa
            // IDanificavel direto) e ficava de fora do espólio por construção -- abater o
            // primeiro chefe do jogo nunca largou uma peça de equipamento.
            _fonteDeEspolio = GetComponent<IFonteDeEspolio>();
            if (_fonteDeEspolio == null)
            {
                Debug.LogError($"[DropAoAbater] '{name}' não implementa IFonteDeEspolio — " +
                               "nada será largado.", this);
                return;
            }

            _fonteDeEspolio.OnAbatido += HandleAbatido;
        }

        private void OnDestroy()
        {
            if (_fonteDeEspolio != null) _fonteDeEspolio.OnAbatido -= HandleAbatido;
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

            int nivel = ProgressionBridge.Instancia != null ? ProgressionBridge.Instancia.NivelAtual : 1;
            var sorteados = _sorteio.Sortear(tabela.ProjetarCandidatos(), nivel, _fonte, tabela.TetoDeItens);

            for (int i = 0; i < sorteados.Count; i++)
            {
                var def = banco.Get(sorteados[i].ItemDefId);
                if (def == null)
                {
                    Debug.LogWarning($"[DropAoAbater] Item '{sorteados[i].ItemDefId}' não existe no ItemDatabase.", this);
                    continue;
                }

                // O item que cai é um EXEMPLAR: base + grau + afixos rolados. Sem esta linha,
                // o gerador existiria e não estaria ligado a nada -- o modo de falha que este
                // repositório já catalogou nove vezes.
                //
                // O GRAU é sorteado pela curva (2026-08-28), com o valor autorado na entrada
                // servindo de PISO: um chefe que declara Impregnado nunca larga Inerte por
                // azar, mas um Cultista que declara Inerte pode surpreender. É o que faz o
                // loot da primeira fase deixar de ser sempre igual.
                var grau = CurvaDeGrau.Sortear(nivel, sorteados[i].Grau, _fonte);

                // E o NÍVEL do item acompanha o jogador, com o piso da tabela: chefe nunca
                // larga item de nível 1, e o Deserto deixa de entregar tier 1 no endgame.
                int nivelDoItem = Mathf.Max(tabela.NivelDoItem, nivel);

                var exemplar = _gerador.Gerar(def, grau, nivelDoItem,
                                              CatalogoDeAfixos.Todos, _fonte);

                if (exemplar != null) exemplar.Quantidade = sorteados[i].Quantidade;

                Materializar(def, exemplar, sorteados[i].Quantidade);
            }
        }

        /// <summary>Gera o exemplar rolado. POCO sem estado — uma instância basta.</summary>
        private readonly GeradorDeItem _gerador = new GeradorDeItem();

        private void Materializar(ItemDef def, ItemInstance exemplar, int quantidade)
        {
            Vector3 posicao = transform.position + Deslocamento();

            var coletavel = prefabColetavel != null
                ? Instantiate(prefabColetavel, posicao, Quaternion.identity)
                : MontarColetavelMinimo(def, posicao);

            // Espólio de inimigo nasce sem chave de save: quem persiste é o abate do inimigo.
            if (exemplar != null) coletavel.Configurar(exemplar, def);
            else coletavel.Configurar(def, quantidade);
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
