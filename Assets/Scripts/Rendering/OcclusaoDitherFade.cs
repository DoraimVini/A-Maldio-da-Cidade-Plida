using UnityEngine;

namespace FavelaAmarela.Runtime.Rendering
{
    /// <summary>
    /// Camada Runtime (MonoBehaviour). Vai numa parede/casa alta cujo sprite o Y-sort
    /// desenha por cima do Damião. Quando o jogador está ATRÁS da parede (mais "longe"
    /// na tela, ou seja, com Y maior que a base da parede), sobe o <c>_DitherAmount</c>
    /// do material via <see cref="MaterialPropertyBlock"/> — o shader
    /// <c>FavelaAmarela/SpriteDitherOcclusion</c> abre buracos e a silhueta do boneco
    /// aparece por eles. Sai de trás → volta ao opaco.
    ///
    /// Requer um <see cref="Collider2D"/> marcado como Trigger cobrindo a área onde o
    /// jogador ficaria oculto. Event-driven (regra §8): a presença do jogador vem de
    /// OnTriggerEnter/Exit, não de polling de cena.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    [AddComponentMenu("Favela Amarela/Rendering/Oclusão Dither Fade")]
    public sealed class OcclusaoDitherFade : MonoBehaviour
    {
        [Tooltip("Quanto de dither (0..1) aplicar quando o jogador está atrás. 0.6 ≈ silhueta clara.")]
        [Range(0f, 1f)]
        [SerializeField] private float ditherAlvo = 0.6f;

        [Tooltip("Velocidade do fade do dither (unidades de amount por segundo).")]
        [SerializeField] private float velocidadeFade = 6f;

        private static readonly int _DitherAmountId = Shader.PropertyToID("_DitherAmount");

        private SpriteRenderer _sr;
        private MaterialPropertyBlock _mpb;
        private Transform _jogador;
        private float _atual;

        private void Awake()
        {
            _sr = GetComponent<SpriteRenderer>();
            _mpb = new MaterialPropertyBlock();
            AplicarDither(0f);
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (!collision.CompareTag("Player")) return;
            _jogador = collision.transform;
        }

        private void OnTriggerExit2D(Collider2D collision)
        {
            if (!collision.CompareTag("Player")) return;
            if (_jogador == collision.transform) _jogador = null;
        }

        private void Update()
        {
            // A parede oclui o jogador só quando ele está "atrás" dela — isto é, com Y
            // maior (mais longe na tela), pois o Y-sort desenha a base mais baixa por cima.
            bool ocluindo = _jogador != null && _jogador.position.y > transform.position.y;
            float alvo = ocluindo ? ditherAlvo : 0f;

            if (Mathf.Approximately(_atual, alvo)) return;

            _atual = Mathf.MoveTowards(_atual, alvo, velocidadeFade * Time.deltaTime);
            AplicarDither(_atual);
        }

        private void AplicarDither(float valor)
        {
            _atual = valor;
            _sr.GetPropertyBlock(_mpb);
            _mpb.SetFloat(_DitherAmountId, valor);
            _sr.SetPropertyBlock(_mpb);
        }
    }
}
