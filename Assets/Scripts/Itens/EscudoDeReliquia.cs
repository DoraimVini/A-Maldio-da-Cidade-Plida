using UnityEngine;
using FavelaAmarela.Core.Enemies;

namespace FavelaAmarela.Runtime.Itens
{
    /// <summary>
    /// Camada Runtime (MonoBehaviour). O <b>abrigo</b> que uma relíquia ergue no ponto focal
    /// dela, durante o rito de selamento do Rei em Amarelo.
    ///
    /// <para><b>A mecânica (pedido do Vini, 2026-09-10):</b> <i>"cada artefato gera um escudo
    /// por vez e você tem que se proteger dentro"</i>. A cada ciclo o Rei acende o escudo de
    /// <b>uma</b> relíquia — a <c>ReiEmAmareloFSM.ReliquiaDoCiclo</c> —, e Damião precisa estar
    /// dentro quando ele se desvelar. Um escudo por vez, na ordem em que as relíquias foram
    /// ativadas.</para>
    ///
    /// <para><b>O escudo acende no começo da calmaria</b>, não no desvelo: os 6 s que antes
    /// eram espera viram a corrida até o altar certo. Medido na cena do Trono, a travessia mais
    /// longa entre dois altares é <b>20 unidades</b> — 4,44 s andando (4,5 u/s), 2,67 s
    /// correndo. Cabe, e sobra pouco. O <c>OAbrigoEAlcancavelTests</c> guarda essa conta: mover
    /// um altar para longe quebra o teste, em vez de o jogador descobrir sozinho que não dava
    /// para chegar.</para>
    ///
    /// <para><b>A regra vive no POCO</b> (<see cref="AbrigoDeReliquia"/>); aqui só se lê a
    /// posição real, se liga/desliga o visual, e se responde "está dentro?".</para>
    /// </summary>
    [AddComponentMenu("Favela Amarela/Itens/Escudo de Relíquia")]
    public sealed class EscudoDeReliquia : MonoBehaviour
    {
        [Header("Tamanho do abrigo")]
        [Tooltip("Meia-largura, em unidades. O padrão (2) é a meia-largura DESENHADA da bolha " +
                 "do Escudo_Magico à escala 2,5 — o que se testa é o que se vê.")]
        [Min(0.1f)]
        [SerializeField] private float semiEixoX = AbrigoDeReliquia.SemiEixoXPadrao;

        [Tooltip("Meia-altura, em unidades. Metade da largura: o achatamento isométrico. " +
                 "Abrigo redondo num quadro de 20 x 11,25 lê torto.")]
        [Min(0.1f)]
        [SerializeField] private float semiEixoY = AbrigoDeReliquia.SemiEixoYPadrao;

        [Header("Visual")]
        [Tooltip("Filho com a cúpula (SpriteRenderer + Animador em Laço). Vazio: procurado " +
                 "por nome ao acordar. [CENA]")]
        [SerializeField] private GameObject visual;

        [Tooltip("Nome do filho procurado quando o campo acima está vazio.")]
        [SerializeField] private string nomeDoVisual = "Escudo";

        private bool _aceso;

        /// <summary>Se este escudo está erguido agora.</summary>
        public bool Aceso => _aceso;

        /// <summary>
        /// O id do artefato deste abrigo, lido do <see cref="PontoFocalDeReliquia"/> irmão.
        ///
        /// <para><b>Não é um campo próprio de propósito.</b> Uma segunda cópia do id sairia de
        /// sincronia em silêncio e o escudo acenderia no altar errado — o mesmo buraco que a
        /// propriedade <c>ReiEmAmareloAI.ReliquiasExigidas</c> foi criada para tapar.</para>
        /// </summary>
        public string ArtefatoId
        {
            get
            {
                // Resolvido com preguiça, e nao no Awake: ferramentas de Editor e testes
                // EditMode leem isto com a cena aberta e SEM Awake ter rodado. Este projeto ja
                // se enganou assim antes -- medir o prefab e afirmar sobre o jogo.
                if (_ponto == null) _ponto = GetComponent<PontoFocalDeReliquia>();
                return _ponto != null ? _ponto.ArtefatoId : null;
            }
        }

        private PontoFocalDeReliquia _ponto;

        private void Awake()
        {
            _ponto = GetComponent<PontoFocalDeReliquia>();

            if (_ponto == null)
                Debug.LogError("[EscudoDeReliquia] Sem PontoFocalDeReliquia no mesmo objeto — " +
                               "o escudo não sabe de qual relíquia é.", this);

            if (visual == null && !string.IsNullOrEmpty(nomeDoVisual))
            {
                var achado = transform.Find(nomeDoVisual);
                if (achado != null) visual = achado.gameObject;
            }

            if (visual == null)
                Debug.LogError($"[EscudoDeReliquia] Sem visual ('{nomeDoVisual}') — o abrigo " +
                               "funcionaria invisível, que é pior do que não funcionar.", this);

            Apagar();
        }

        /// <summary>Ergue o escudo. Idempotente.</summary>
        public void Acender()
        {
            _aceso = true;
            if (visual != null) visual.SetActive(true);
        }

        /// <summary>Baixa o escudo. Idempotente.</summary>
        public void Apagar()
        {
            _aceso = false;
            if (visual != null) visual.SetActive(false);
        }

        /// <summary>
        /// Se <paramref name="posicao"/> está abrigada por este escudo. <b>Escudo apagado não
        /// abriga</b> — a checagem de estado vem antes da geometria, senão o jogador
        /// sobreviveria parado num altar que não acendeu.
        /// </summary>
        public bool Protege(Vector2 posicao)
        {
            if (!_aceso) return false;

            return AbrigoDeReliquia.EstaAbrigado(
                posicao, transform.position, semiEixoX, semiEixoY);
        }

        /// <summary>Meia-largura do abrigo, para ferramentas e testes medirem.</summary>
        public float SemiEixoX => semiEixoX;

        /// <summary>Meia-altura do abrigo, para ferramentas e testes medirem.</summary>
        public float SemiEixoY => semiEixoY;

#if UNITY_EDITOR
        /// <summary>Desenha a elipse do abrigo na cena — o que se testa é o que se vê.</summary>
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.95f, 0.85f, 0.3f, 0.9f);

            Vector3 centro = transform.position;
            const int passos = 48;
            Vector3 anterior = centro + new Vector3(semiEixoX, 0f, 0f);

            for (int i = 1; i <= passos; i++)
            {
                float t = i / (float)passos * Mathf.PI * 2f;
                Vector3 p = centro + new Vector3(semiEixoX * Mathf.Cos(t),
                                                 semiEixoY * Mathf.Sin(t), 0f);
                Gizmos.DrawLine(anterior, p);
                anterior = p;
            }
        }
#endif
    }
}
