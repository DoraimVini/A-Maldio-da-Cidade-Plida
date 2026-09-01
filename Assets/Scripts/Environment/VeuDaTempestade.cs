using UnityEngine;
using FavelaAmarela.Core.Environment;
using FavelaAmarela.Inventario;
using FavelaAmarela.Runtime.UI;

namespace FavelaAmarela.Runtime.Environment
{
    /// <summary>
    /// A área que <b>esconde o Templo do Povo Serpente</b>. Quem entra sem a carta é arremessado
    /// para outro canto do Deserto pela Tempestade de Memória.
    ///
    /// <para><b>Adaptador puro:</b> a regra de para onde arremessar vive em
    /// <see cref="DesorientacaoDaTempestade"/>, que é POCO e testável sem cena. Aqui só se lê o
    /// inventário, se move o corpo e se conta ao jogador o que aconteceu.</para>
    ///
    /// <para><b>Contar é metade da mecânica.</b> Ser teleportado sem explicação é
    /// indistinguível de bug — e este projeto já tem histórico de defeitos que o jogador lê como
    /// a coisa errada. A primeira vez diz que <i>falta</i> algo; as seguintes são mais secas,
    /// porque repetir a lição inteira vira ruído.</para>
    /// </summary>
    [AddComponentMenu("Favela Amarela/Ambiente/Véu da Tempestade")]
    [RequireComponent(typeof(Collider2D))]
    public sealed class VeuDaTempestade : MonoBehaviour
    {
        [Header("O que atravessa o véu")]
        [Tooltip("Id do ItemDef que permite passar. Sem ele, a tempestade arremessa. [ASSET]")]
        [SerializeField] private string idDaCarta = "Item_Chave_CartaDasAreias";

        [Header("Para onde arremessa")]
        [Tooltip("Os cantos do Deserto. Vazio = derivados dos Limite_* da cena no Awake.")]
        [SerializeField] private Transform[] cantos;

        [Tooltip("Margem para dentro dos limites, ao derivar os cantos automaticamente. " +
                 "Zero cuspiria o jogador dentro da parede.")]
        [Min(1f)]
        [SerializeField] private float margemDaBorda = 6f;

        [Header("O que o jogador lê")]
        [TextArea]
        [SerializeField]
        private string primeiraVez =
            "A tempestade te devolve. As areias reescrevem o caminho a cada passo — " +
            "sem uma carta, o leste não existe.";

        [TextArea]
        [SerializeField] private string demaisVezes = "A tempestade te devolve.";

        private DesorientacaoDaTempestade _regra;
        private bool _jaAvisou;

        private void Awake()
        {
            var col = GetComponent<Collider2D>();

            // Trigger, sempre: um véu sólido seria uma parede, e o ponto é justamente deixar
            // entrar para então devolver.
            if (!col.isTrigger) col.isTrigger = true;

            _regra = new DesorientacaoDaTempestade(MontarCantos());
        }

        /// <summary>
        /// Os cantos: os autorados, ou — quando não há — os quatro cantos do mapa, derivados dos
        /// <c>Limite_*</c> da cena.
        ///
        /// <para>Derivar é o padrão de propósito: o mapa <b>dobrou de tamanho</b> em 2026-09-01,
        /// e quatro posições escritas à mão teriam ficado no meio do mapa novo, em silêncio. Um
        /// array autorado é mais uma lista para envelhecer — este repositório já catalogou
        /// oito.</para>
        /// </summary>
        private DesorientacaoDaTempestade.Ponto[] MontarCantos()
        {
            if (cantos != null && cantos.Length >= 2)
            {
                var autorados = new DesorientacaoDaTempestade.Ponto[cantos.Length];

                for (int i = 0; i < cantos.Length; i++)
                    autorados[i] = cantos[i] == null
                        ? default
                        : new DesorientacaoDaTempestade.Ponto(cantos[i].position.x,
                                                              cantos[i].position.y);

                return autorados;
            }

            float maxX = 0f, maxY = 0f;

            foreach (var t in FindObjectsByType<Transform>(FindObjectsInactive.Include,
                                                           FindObjectsSortMode.None))
            {
                if (!t.name.StartsWith("Limite_")) continue;

                maxX = Mathf.Max(maxX, Mathf.Abs(t.position.x));
                maxY = Mathf.Max(maxY, Mathf.Abs(t.position.y));
            }

            if (maxX < 1f || maxY < 1f)
            {
                Debug.LogError("[VeuDaTempestade] Nenhum 'Limite_*' na cena — não dá para saber " +
                               "onde ficam os cantos, e o véu não vai arremessar ninguém.", this);

                // Dois pontos degenerados para o construtor não estourar; o LogError acima é o
                // sintoma que importa.
                return new[]
                {
                    new DesorientacaoDaTempestade.Ponto(0f, 0f),
                    new DesorientacaoDaTempestade.Ponto(1f, 1f),
                };
            }

            float x = maxX - margemDaBorda;
            float y = maxY - margemDaBorda;

            return new[]
            {
                new DesorientacaoDaTempestade.Ponto(-x, -y),
                new DesorientacaoDaTempestade.Ponto(-x, y),
                new DesorientacaoDaTempestade.Ponto(x, -y),
                new DesorientacaoDaTempestade.Ponto(x, y),
            };
        }

        private void OnTriggerEnter2D(Collider2D outro)
        {
            if (!outro.CompareTag("Player")) return;

            var inventario = InventoryManager.Instance;

            bool temACarta = inventario != null &&
                             inventario.PossuiItemNaMochila(idDaCarta);

            if (!DesorientacaoDaTempestade.DeveArremessar(temACarta)) return;

            var corpo = outro.attachedRigidbody != null
                ? outro.attachedRigidbody.transform
                : outro.transform;

            var destino = _regra.Arremessar(
                new DesorientacaoDaTempestade.Ponto(corpo.position.x, corpo.position.y));

            // Zera a velocidade junto: arremessar sem parar o corpo faria o jogador chegar ao
            // canto novo já correndo na direção antiga, o que se lê como perda de controle.
            if (outro.attachedRigidbody != null)
                outro.attachedRigidbody.linearVelocity = Vector2.zero;

            corpo.position = new Vector3(destino.X, destino.Y, corpo.position.z);

            Contar();
        }

        private void Contar()
        {
            var caixa = TutorialHintUI.Instancia;

            if (caixa == null)
            {
                // Sem caixa o jogador é teleportado sem explicação — indistinguível de bug.
                Debug.LogWarning("[VeuDaTempestade] Sem TutorialHintUI: o jogador foi arremessado " +
                                 "e não vai entender por quê.", this);
                return;
            }

            caixa.Mostrar(_jaAvisou ? demaisVezes : primeiraVez);
            _jaAvisou = true;
        }

        private void OnDrawGizmosSelected()
        {
            var col = GetComponent<Collider2D>();
            if (col == null) return;

            Gizmos.color = new Color(0.83f, 0.70f, 0.24f, 0.25f);
            Gizmos.DrawCube(col.bounds.center, col.bounds.size);
        }
    }
}
