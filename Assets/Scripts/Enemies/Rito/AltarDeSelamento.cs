using UnityEngine;
using FavelaAmarela.Core.Enemies;

namespace FavelaAmarela.Runtime.Enemies
{
    /// <summary>
    /// O Altar de Selamento, no centro do Trono: onde o selo avança — se o Damião estiver
    /// <b>exposto</b> ao olhar do Rei.
    ///
    /// <para>A área é uma elipse de 4 × 2 un centrada <b>neste objeto</b> (a geometria testada do
    /// <see cref="AbrigoDeReliquia"/>, que era o escudo da luta antiga e agora é o altar da nova):
    /// num quadro largo, espaço de jogo redondo lê torto. A pedra do altar é um filho, posto na
    /// borda do fundo da elipse, para o Damião ficar <b>na frente</b> dela — e não sumir atrás do
    /// próprio altar pelo Y-sort.</para>
    ///
    /// <para><b>Dois sinais quando o selo avança:</b> o feixe da pedra acende (o
    /// <c>AnimadorDeAltarDeReliquia</c> dos altares de relíquia) e o círculo no chão clareia. O
    /// círculo existe também para mostrar onde a área começa — "estar no altar" não pode ser
    /// adivinhação.</para>
    /// </summary>
    [AddComponentMenu("Favela Amarela/Enemies/Rito/Altar de Selamento")]
    public sealed class AltarDeSelamento : MonoBehaviour
    {
        [Tooltip("Meia-largura da área do altar, em unidades.")]
        [Min(0.1f)]
        [SerializeField] private float semiEixoX = AbrigoDeReliquia.SemiEixoXPadrao;

        [Tooltip("Meia-altura da área do altar, em unidades (metade da largura: o achatamento isométrico).")]
        [Min(0.1f)]
        [SerializeField] private float semiEixoY = AbrigoDeReliquia.SemiEixoYPadrao;

        [Tooltip("A pedra do altar; o feixe a assume enquanto o selo avança. [CENA]")]
        [SerializeField] private SpriteRenderer pedra;

        [Tooltip("Sprite da pedra apagada. [ASSET pixel art]")]
        [SerializeField] private Sprite spriteApagado;

        [Tooltip("O círculo no chão que marca a área. [CENA]")]
        [SerializeField] private LineRenderer circulo;

        [SerializeField] private Color corDoCirculo = new Color(0.9f, 0.78f, 0.3f, 0.35f);
        [SerializeField] private Color corDoCirculoAceso = new Color(1f, 0.9f, 0.4f, 0.95f);

        private FavelaAmarela.Runtime.Itens.AnimadorDeAltarDeReliquia _feixe;
        private bool _aceso;

        /// <summary>Meia-largura, para guardas de geometria.</summary>
        public float SemiEixoX => semiEixoX;

        /// <summary>Meia-altura, para guardas de geometria.</summary>
        public float SemiEixoY => semiEixoY;

        /// <summary>Se o selo avançou no último quadro (o feixe está aceso).</summary>
        public bool Aceso => _aceso;

        private void Awake()
        {
            _feixe = GetComponent<FavelaAmarela.Runtime.Itens.AnimadorDeAltarDeReliquia>();
            if (pedra == null)
                Debug.LogError("[AltarDeSelamento] Sem a pedra do altar — o selo avança sem nada " +
                               "acender na tela.", this);
            PintarCirculo(false);
        }

        /// <summary>Se uma posição de mundo está dentro do altar.</summary>
        public bool Contem(Vector2 posicao)
            => AbrigoDeReliquia.EstaAbrigado(posicao, transform.position, semiEixoX, semiEixoY);

        /// <summary>Liga ou desliga o feixe e o círculo conforme o selo avança.</summary>
        public void MostrarAvanco(bool avancando)
        {
            if (avancando == _aceso) return;
            _aceso = avancando;
            PintarCirculo(avancando);

            if (_feixe == null || pedra == null) return;
            if (avancando)
            {
                _feixe.Acender(pedra);
                return;
            }

            _feixe.Apagar();
            if (spriteApagado != null) pedra.sprite = spriteApagado;
        }

        private void PintarCirculo(bool aceso)
        {
            if (circulo == null) return;
            circulo.startColor = circulo.endColor = aceso ? corDoCirculoAceso : corDoCirculo;
        }
    }
}
