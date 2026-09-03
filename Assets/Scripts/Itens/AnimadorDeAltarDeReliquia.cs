using UnityEngine;

namespace FavelaAmarela.Runtime.Itens
{
    /// <summary>
    /// Camada Runtime. Toca o feixe de um Ponto Focal depois que a Relíquia é ativada — mesmo
    /// padrão dos <c>AnimadorDo*</c> do elenco: um <see cref="MonoBehaviour"/> que escreve
    /// <c>SpriteRenderer.sprite</c> direto, sem <c>AnimatorController</c>.
    ///
    /// <para><b>O buraco que isto fecha (2026-09-03).</b> Os três <c>Ponto_Focal_*</c> do
    /// Castelo estavam com <c>spriteInativo</c> e <c>spriteAtivo</c> <b>vazios</b>. O próprio
    /// doc do <see cref="PontoFocalDeReliquia"/> já registrava: <i>"o terceiro trocava um sprite
    /// que não está autorado"</i>. Ativar uma relíquia não mudava um pixel — e foi por isso que
    /// o Vini concluiu que os altares estavam quebrados. Estavam <b>mudos</b>.</para>
    ///
    /// <para><b>Por que fica parado até acender.</b> Um altar inativo é pedra: não tem o que
    /// animar, e um <c>Update</c> girando à toa em três objetos de cenário é desperdício sem
    /// nada em troca. O componente só entra em <c>Update</c> depois de <see cref="Acender"/>,
    /// e o estado aceso é <b>terminal</b> — uma relíquia ativada não volta atrás dentro da
    /// mesma luta.</para>
    ///
    /// <para>Não há um POCO por trás porque não há regra por trás: isto é cadência de quadro,
    /// não decisão de jogo. Quem decide se o altar acende é o <see cref="PontoFocalDeReliquia"/>,
    /// que por sua vez pergunta ao <c>ReiEmAmareloAI</c>.</para>
    /// </summary>
    [AddComponentMenu("Favela Amarela/Itens/Animador de Altar de Relíquia")]
    public sealed class AnimadorDeAltarDeReliquia : MonoBehaviour
    {
        [Header("Quadros do feixe aceso")]
        [Tooltip("Os 10 quadros de Art/Props/AltarDeReliquia, em ordem. [ASSET pixel art]")]
        [SerializeField] private Sprite[] quadros;

        [Header("Cadência")]
        [Min(1f)]
        [Tooltip("O pacote de origem foi desenhado para uma volta rápida; 12 fecha o ciclo em " +
                 "pouco menos de um segundo.")]
        [SerializeField] private float quadrosPorSegundo = 12f;

        private SpriteRenderer _alvo;
        private float _relogio;
        private int _quadro;

        /// <summary>
        /// Acende o feixe e começa o ciclo.
        ///
        /// <para>O <see cref="SpriteRenderer"/> vem de fora, e não de um <c>GetComponent</c>,
        /// porque quem manda no visual do ponto focal é o campo <c>spriteDoPonto</c> dele — que
        /// pode apontar para um filho. Receber o alvo pronto faz este componente funcionar nos
        /// dois arranjos sem nenhuma ligação nova para preencher na cena.</para>
        /// </summary>
        /// <param name="alvo">Renderer onde o feixe é desenhado.</param>
        public void Acender(SpriteRenderer alvo)
        {
            if (alvo == null || quadros == null || quadros.Length == 0) return;

            _alvo = alvo;
            _quadro = 0;
            _relogio = 0f;
            _alvo.sprite = quadros[0];
        }

        /// <summary>Apaga o feixe. Usado no <c>Start</c>, para o altar nascer de pedra.</summary>
        public void Apagar() => _alvo = null;

        private void Update()
        {
            if (_alvo == null) return;

            _relogio += Time.deltaTime * quadrosPorSegundo;
            if (_relogio < 1f) return;

            _relogio -= 1f;
            _quadro = (_quadro + 1) % quadros.Length;
            _alvo.sprite = quadros[_quadro];
        }
    }
}
