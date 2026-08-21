using UnityEngine;
using FavelaAmarela.Runtime.Interaction;
using FavelaAmarela.Runtime.UI;

namespace FavelaAmarela.Runtime.Quests
{
    /// <summary>
    /// Camada Runtime. <b>Yug-Neth já no Castelo</b>, aposentado de companheiro e virado NPC: é
    /// dele que Damião vai aprender o artesanato.
    ///
    /// <para><b>O artesanato em si é conteúdo pós-Vertical Slice</b> (decisão do Vini,
    /// 2026-08-20), e este componente <b>não o implementa</b>. Ele existe para que a virada de
    /// papel aconteça em jogo — o companheiro que te seguiu o deserto inteiro para de te seguir
    /// e passa a ser alguém com quem se fala — sem que isso dependa de um sistema que ainda não
    /// existe. Quando o artesanato entrar, é aqui que ele pendura.</para>
    ///
    /// <para><b>Sobre o texto:</b> as falas são <b>provisórias</b> e ficam serializadas, para o
    /// Vini reescrever no Inspector sem recompilar. Yug-Neth se comunica por bioluminescência
    /// (GDD, <c>lore/migo_companion.md</c>), não por palavras — a fala aqui é a leitura que
    /// Damião faz dos pontos dourados, e o texto final precisa dessa voz.</para>
    /// </summary>
    [AddComponentMenu("Favela Amarela/Quests/Yug-Neth Artesão")]
    public sealed class YugNethArtesao : MonoBehaviour, IInteragivel
    {
        [Header("Diálogo")]
        [Tooltip("Caixa de texto da cena. Sem ela a interação acontece muda.")]
        [SerializeField] private TutorialHintUI caixaDeTexto;

        [Tooltip("Rótulo do prompt. Infinitivo e diegético (favela-lore-enforcer).")]
        [SerializeField] private string rotulo = "Ouvir Yug-Neth";

        // TEXTO PROVISÓRIO — a ser escrito pelo Vini. Yug-Neth não fala: pisca. Estas linhas são
        // a leitura que Damião faz da bioluminescência, e é essa voz que o texto final precisa.
        [Tooltip("Falas, em ordem. Repetem a última depois de esgotadas. PROVISÓRIAS.")]
        [TextArea(2, 4)]
        [SerializeField] private string[] falas =
        {
            "Os pontos dourados desenham formas que você quase reconhece: um cabo, uma lâmina, " +
            "uma juntura.",
            "Ele repete o desenho, mais devagar. Não é agora — mas você vai precisar disto.",
        };

        [Tooltip("Segundos que cada fala fica na tela.")]
        [Min(0.5f)]
        [SerializeField] private float duracaoDaFala = 4f;

        private int _proxima;

        /// <inheritdoc />
        public string RotuloDeInteracao => rotulo;

        /// <summary>Sempre disponível: é um NPC, não um baú que esvazia.</summary>
        public bool PodeInteragir => falas != null && falas.Length > 0;

        /// <inheritdoc />
        public int PrioridadeDeInteracao => 0;

        /// <inheritdoc />
        public Vector2 PosicaoDeInteracao => transform.position;

        /// <summary>
        /// Entrega a caixa de texto. Existe porque este componente é <b>acrescentado em
        /// runtime</b> pela <c>TravessiaDoCompanheiro</c> — não dá para ligar a referência no
        /// Inspector de um componente que ainda não existe quando a cena é montada.
        ///
        /// <para>E é por isso que <b>não</b> há um <c>FindAnyObjectByType</c> aqui: o
        /// <c>Scripts/UI/CLAUDE.md</c> proíbe localizar elemento de UI por busca. A dependência
        /// entra por injeção, como no resto do Runtime.</para>
        /// </summary>
        public void Configurar(TutorialHintUI caixa)
        {
            if (caixa != null) caixaDeTexto = caixa;
        }

        private void Awake()
        {
            if (caixaDeTexto == null)
                Debug.LogError("[YugNethArtesao] Sem caixa de texto — falar com ele não mostraria " +
                               "nada, e a interação pareceria quebrada. Quem entrega é a " +
                               "TravessiaDoCompanheiro, por Configurar().", this);
        }

        /// <inheritdoc />
        public void Interagir(GameObject quemInterage)
        {
            if (!PodeInteragir || caixaDeTexto == null) return;

            caixaDeTexto.Mostrar(falas[_proxima], duracaoDaFala);

            // Trava na última em vez de voltar ao começo: reler o fim é menos estranho do que o
            // NPC recomeçar a explicação sozinho toda vez que se aperta o botão.
            if (_proxima < falas.Length - 1) _proxima++;
        }
    }
}
