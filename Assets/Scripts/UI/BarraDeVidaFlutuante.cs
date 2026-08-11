using UnityEngine;
using FavelaAmarela.Core.Combat;

namespace FavelaAmarela.Runtime.UI
{
    /// <summary>
    /// Camada Runtime (MonoBehaviour). Barra de vida <b>no mundo</b>, flutuando sobre a cabeça
    /// de um ator. Feita para aliados (Yug-Neth), mas serve a qualquer coisa com
    /// <see cref="Vitalidade"/>.
    ///
    /// <para><b>Discrição é requisito, não enfeite</b> (pedido do Vini: "sem poluir tanto a
    /// tela"). Três regras cuidam disso:</para>
    /// <list type="number">
    ///   <item>Some quando a vida está cheia — barra cheia não informa nada.</item>
    ///   <item>Aparece ao levar dano e <b>desaparece sozinha</b> depois de alguns segundos.</item>
    ///   <item>Fica visível de forma permanente só quando a vida está crítica, que é
    ///   justamente quando o jogador precisa decidir se socorre ou recua.</item>
    /// </list>
    ///
    /// <para>Usa <c>SpriteRenderer</c> e não Canvas: o jogo é 2D com Y-sorting, e um Canvas
    /// em world space traria layout, raycast e batching que esta barra não precisa.</para>
    /// </summary>
    [AddComponentMenu("FavelaAmarela/UI/Barra de Vida Flutuante")]
    public sealed class BarraDeVidaFlutuante : MonoBehaviour
    {
        [Header("Peças")]
        [Tooltip("Fundo da barra (o trilho escuro). [ASSET]")]
        [SerializeField] private SpriteRenderer fundo;

        [Tooltip("Preenchimento — é ele que encolhe conforme a vida cai. [ASSET]")]
        [SerializeField] private SpriteRenderer preenchimento;

        [Header("Posição")]
        [Tooltip("Altura acima do pivô do ator.")]
        [SerializeField] private float alturaAcimaDaCabeca = 0.9f;

        [Header("Discrição")]
        [Tooltip("Segundos visível após o último dano antes de sumir.")]
        [Min(0f)]
        [SerializeField] private float segundosVisivelAposDano = 4f;

        [Tooltip("Abaixo desta fração a barra fica sempre visível (momento de socorrer ou recuar).")]
        [Range(0f, 1f)]
        [SerializeField] private float fracaoCritica = 0.35f;

        [Header("Cores")]
        [SerializeField] private Color corSaudavel = new Color(0.75f, 0.72f, 0.45f, 0.85f);
        [SerializeField] private Color corCritica = new Color(0.7f, 0.15f, 0.12f, 0.95f);

        private Vitalidade _vitalidade;
        private float _larguraCheia = 1f;
        private float _tempoAteSumir;

        // Guardados para esvaziar a barra por uma ponta só. O sprite tem pivô central: escalar
        // sem compensar a posição faria a barra encolher para o meio, como um acordeão, em vez
        // de drenar da direita para a esquerda.
        private Vector3 _posicaoCheia;
        private float _larguraLocalCheia;

        /// <summary>
        /// Liga a barra a uma Vitalidade. Idempotente: re-bind troca a fonte com segurança.
        /// </summary>
        public void Bind(Vitalidade vitalidade)
        {
            if (vitalidade == null)
            {
                Debug.LogWarning("[BarraDeVidaFlutuante] Bind recebeu Vitalidade nula.", this);
                return;
            }

            Unbind();

            _vitalidade = vitalidade;
            _vitalidade.OnChanged += HandleMudanca;

            Redesenhar(_vitalidade.Percentual, mostrar: false);
        }

        /// <summary>Desliga do evento. Seguro chamar sem bind ativo.</summary>
        public void Unbind()
        {
            if (_vitalidade != null) _vitalidade.OnChanged -= HandleMudanca;
            _vitalidade = null;
        }

        private void Awake()
        {
            if (preenchimento != null)
            {
                _larguraCheia = preenchimento.transform.localScale.x;
                _posicaoCheia = preenchimento.transform.localPosition;

                var sprite = preenchimento.sprite;
                _larguraLocalCheia = sprite != null ? sprite.bounds.size.x * _larguraCheia : 0f;
            }

            Mostrar(false);
        }

        private void OnDestroy() => Unbind();

        private void HandleMudanca(VitalidadeChangedArgs args)
        {
            // Só reinicia o relógio quando de fato perdeu vida. Cura não precisa chamar
            // atenção — e se chamasse, a barra piscaria a cada tique de regeneração.
            bool perdeuVida = args.ValorAtual < args.ValorAnterior;
            if (perdeuVida) _tempoAteSumir = segundosVisivelAposDano;

            Redesenhar(args.Percentual, mostrar: perdeuVida);
        }

        private void LateUpdate()
        {
            // LateUpdate: o ator já se moveu neste frame, então a barra não fica um frame atrás.
            transform.localPosition = new Vector3(0f, alturaAcimaDaCabeca, 0f);

            // A barra não gira com o ator — sempre na horizontal, legível.
            transform.rotation = Quaternion.identity;

            if (_tempoAteSumir <= 0f) return;

            _tempoAteSumir -= Time.deltaTime;
            if (_tempoAteSumir <= 0f && _vitalidade != null)
                Redesenhar(_vitalidade.Percentual, mostrar: false);
        }

        private void Redesenhar(float percentual, bool mostrar)
        {
            percentual = Mathf.Clamp01(percentual);

            bool critico = percentual > 0f && percentual <= fracaoCritica;
            bool cheio = percentual >= 0.999f;
            bool abatido = percentual <= 0f;

            // Cheia não informa nada; abatido é sinalizado pelo próprio ator (ele cai e muda
            // de cor). Nos dois casos a barra só ocuparia tela.
            bool visivel = !cheio && !abatido && (mostrar || critico || _tempoAteSumir > 0f);
            Mostrar(visivel);

            if (preenchimento == null) return;

            var escala = preenchimento.transform.localScale;
            escala.x = _larguraCheia * percentual;
            preenchimento.transform.localScale = escala;

            // Compensa o pivô central: desloca meia-largura perdida para a esquerda, de modo
            // que a ponta esquerda fique parada e a barra drene da direita.
            var posicao = _posicaoCheia;
            posicao.x -= _larguraLocalCheia * (1f - percentual) * 0.5f;
            preenchimento.transform.localPosition = posicao;

            preenchimento.color = critico ? corCritica : corSaudavel;
        }

        private void Mostrar(bool visivel)
        {
            if (fundo != null) fundo.enabled = visivel;
            if (preenchimento != null) preenchimento.enabled = visivel;
        }
    }
}
