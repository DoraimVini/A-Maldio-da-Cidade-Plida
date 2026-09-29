using UnityEngine;
using FavelaAmarela.Core.Enemies;

namespace FavelaAmarela.Runtime.Enemies
{
    /// <summary>
    /// Um Nobre Fossilizado no Trono de Aldebaran: a <b>sombra</b> do Rito do Olhar. Enquanto está
    /// de pé, a pegada dele (o <c>BoxCollider2D</c>) corta a linha de visão do Rei; atrás dele o
    /// Damião não é visto e a mente se ancora.
    ///
    /// <para><b>"Atrás", aqui, é na frente da câmera.</b> O Rei fica ao fundo da sala; a sombra de
    /// um Nobre cai para o lado da entrada, que é o lado de y menor — e o Y-sort desenha o Damião
    /// por cima da estátua. Esconder-se não esconde o jogador da tela, só do Rei.</para>
    ///
    /// <para><b>Silhueta com o Damião do outro lado.</b> Quando ele passa por trás da estátua
    /// (y maior, o lado exposto), o Nobre de 3,5 un o cobriria inteiro. Quem abre os buracos é o
    /// <c>OcclusaoDitherFade</c> com o material <c>OcclusionDither</c> (skill de isometria, regra
    /// 6) — a ferramenta do Trono os põe em cada Nobre. Este componente não mexe no alpha por
    /// isso: alpha liso é justamente o que a regra proíbe.</para>
    ///
    /// <para><b>O Verbo o desfaz</b> (Fase 4): <see cref="Desfazer"/> tira a sombra na hora — no
    /// instante do pulso — e apaga o sprite em meio segundo.</para>
    /// </summary>
    [AddComponentMenu("Favela Amarela/Enemies/Rito/Cobertura do Trono")]
    [RequireComponent(typeof(SpriteRenderer), typeof(BoxCollider2D))]
    public sealed class CoberturaDoTrono : MonoBehaviour
    {
        [Tooltip("Segundos para o Nobre se desfazer depois do pulso.")]
        [Min(0.05f)]
        [SerializeField] private float duracaoDoDesfazimento = 0.5f;

        private SpriteRenderer _sprite;
        private BoxCollider2D _pegada;
        private float _desfazendo = -1f;

        /// <summary>Se ainda corta o olhar do Rei.</summary>
        public bool DePe => _desfazendo < 0f && isActiveAndEnabled;

        /// <summary>
        /// A pegada no chão, em mundo, calculada do <c>BoxCollider2D</c> e da escala — e não de
        /// <c>Collider2D.bounds</c>, que em EditMode depende de a física ter sincronizado. Assim o
        /// guarda de geometria lê a cena parada e recebe a mesma caixa que o jogo usa.
        /// </summary>
        public CaixaDeCobertura Caixa
        {
            get
            {
                var box = _pegada != null ? _pegada : GetComponent<BoxCollider2D>();
                if (box == null) return default;
                Vector2 escala = transform.lossyScale;
                Vector2 centro = (Vector2)transform.position + Vector2.Scale(box.offset, escala);
                Vector2 tamanho = Vector2.Scale(box.size, new Vector2(Mathf.Abs(escala.x), Mathf.Abs(escala.y)));
                return CaixaDeCobertura.DoCentro(centro, tamanho);
            }
        }

        private void Awake()
        {
            _sprite = GetComponent<SpriteRenderer>();
            _pegada = GetComponent<BoxCollider2D>();
        }

        /// <summary>O Verbo alcançou este Nobre: a sombra some agora; o corpo, em meio segundo.</summary>
        public void Desfazer()
        {
            if (_desfazendo >= 0f) return;
            _desfazendo = 0f;
            if (_pegada != null) _pegada.enabled = false;
        }

        private void Update()
        {
            if (_desfazendo < 0f || _sprite == null) return;

            _desfazendo += Time.deltaTime;
            float a = Mathf.Clamp01(1f - _desfazendo / duracaoDoDesfazimento);
            _sprite.color = new Color(1f, 1f, 1f, a);
            if (a <= 0f) enabled = false;
        }
    }
}
