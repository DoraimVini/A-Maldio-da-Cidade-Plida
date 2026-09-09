using UnityEngine;

namespace FavelaAmarela.Runtime.Rendering
{
    /// <summary>
    /// Projeta uma sombra elíptica no chão sob o ator — a âncora que diz <b>onde ele pisa</b>.
    ///
    /// <para><b>Por que isto é a maior falta de profundidade do projeto (2026-09-09).</b> Não
    /// havia <b>nenhum</b> sistema de sombra: varredura por "Sombra"/"Shadow" em
    /// <c>Assets/Scripts</c> devolveu zero. Num 2D sem sombra todo sprite parece flutuar, porque
    /// nada liga a arte ao solo — o Y-sort resolve <i>quem está na frente</i>, e não <i>a que
    /// altura</i>.</para>
    ///
    /// <para><b>E não é só estética: é leitura de combate.</b> O Byakhee voa, e hoje a única
    /// diferença visual entre ele voando e pousado é a <b>cor</b> (<c>corPousado</c>,
    /// <c>corFrenesi</c>) — ele não sobe, não escala, não projeta nada. O Vini relatou que "a
    /// hurtbox dela é muito difícil de atingir"; parte disso é não existir nada na tela dizendo
    /// onde o bicho está <i>no chão</i>. A sombra é esse indicador.</para>
    ///
    /// <para><b>A sombra não é filha do movimento visual.</b> Ela é posicionada pelo
    /// <c>transform</c> do ator, que é a posição de <b>solo</b> — se um dia o visual do ator
    /// subir para representar voo, a sombra fica embaixo, que é o que dá a leitura de altura.
    /// Usar <see cref="Altura"/> encolhe e clareia a sombra conforme ele sobe.</para>
    /// </summary>
    [AddComponentMenu("Favela Amarela/Rendering/Sombra De Chão")]
    public sealed class SombraDeChao : MonoBehaviour
    {
        [Tooltip("A elipse. Vazio = procura 'sombra_chao' nos Resources do projeto.")]
        [SerializeField] private Sprite sprite;

        [Tooltip("Largura da sombra em unidades de mundo. A altura sai da proporção 2:1 da célula.")]
        [SerializeField] private float largura = 0.9f;

        [Tooltip("Opacidade com o ator no chão.")]
        [Range(0f, 1f)]
        [SerializeField] private float opacidade = 0.5f;

        [Tooltip("Ajuste fino dos pés em relação ao transform, em unidades.")]
        [SerializeField] private Vector2 deslocamento = Vector2.zero;

        [Tooltip("Altura em que a sombra some de vez. Só importa para quem voa.")]
        [SerializeField] private float alturaDeDesaparecimento = 4f;

        private Vector3 _escalaBase = Vector3.one;
        private SpriteRenderer _doDono;
        private SpriteRenderer _daSombra;
        private Transform _sombra;

        /// <summary>
        /// Altura do ator acima do solo, em unidades. Zero = pisando.
        ///
        /// <para>Quem voa escreve aqui; a sombra encolhe e clareia conforme sobe, que é como o
        /// olho lê altura num jogo sem eixo vertical de verdade.</para>
        /// </summary>
        public float Altura { get; set; }

        private void Awake()
        {
            _doDono = GetComponent<SpriteRenderer>();

            if (_doDono == null)
                Debug.LogError("[SombraDeChao] Sem SpriteRenderer no ator — a sombra não tem " +
                               "como saber em que ordem se desenhar.", this);

            GarantirSombra();
        }

        /// <summary>
        /// Cria o objeto da sombra se ele ainda não existe.
        ///
        /// <para>Mesmo padrão do <c>EcoDeCarcosa.GarantirVisual</c>: o componente monta o
        /// próprio visual em vez de depender de alguém ter arrastado um filho na cena — que é o
        /// modo de falha dominante deste projeto (código existe, não está ligado).</para>
        /// </summary>
        private void GarantirSombra()
        {
            Transform achada = transform.Find(NomeDoObjeto);

            if (achada == null)
            {
                var go = new GameObject(NomeDoObjeto);
                go.transform.SetParent(transform, false);
                achada = go.transform;
            }

            _sombra = achada;
            _daSombra = _sombra.GetComponent<SpriteRenderer>();

            if (_daSombra == null) _daSombra = _sombra.gameObject.AddComponent<SpriteRenderer>();

            if (sprite != null) _daSombra.sprite = sprite;

            if (_daSombra.sprite == null)
            {
                Debug.LogWarning("[SombraDeChao] Sem sprite de sombra — o componente não vai " +
                                 "desenhar nada. Atribua 'sombra_chao' no Inspector.", this);
                enabled = false;
                return;
            }

            if (_doDono != null) _daSombra.sortingLayerID = _doDono.sortingLayerID;

            AplicarEscala();
        }

        /// <summary>
        /// A escala que faz a sprite bater com <see cref="largura"/>. Calculada <b>uma vez</b> —
        /// ler <c>sprite.bounds</c> e escrever <c>localScale</c> a cada quadro seria trabalho
        /// repetido em hot path (Regra de Ouro §1).
        /// </summary>
        private void AplicarEscala()
        {
            var s = _daSombra.sprite;
            if (s == null || s.bounds.size.x <= 0f) return;

            float k = largura / s.bounds.size.x;
            _escalaBase = new Vector3(k, k, 1f);
            _sombra.localScale = _escalaBase;
        }

        private void LateUpdate()
        {
            if (_daSombra == null || _doDono == null) return;

            // Posição de SOLO: o transform do ator, não o visual dele.
            Vector3 pe = transform.position;
            _sombra.position = new Vector3(pe.x + deslocamento.x, pe.y + deslocamento.y, pe.z);

            // Um degrau atrás do dono, na mesma camada. Lê a ordem já resolvida em vez de
            // recalcular -y*10: assim um `offsetPes` no DynamicYSort continua valendo, e não há
            // duas contas de profundidade para divergirem.
            _daSombra.sortingLayerID = _doDono.sortingLayerID;
            _daSombra.sortingOrder = _doDono.sortingOrder - 1;

            float fracao = alturaDeDesaparecimento > 0f
                ? Mathf.Clamp01(Altura / alturaDeDesaparecimento)
                : 0f;

            // Sobe -> encolhe e clareia. As duas coisas juntas, porque só uma lê como bug.
            _sombra.localScale = _escalaBase * Mathf.Lerp(1f, 0.45f, fracao);

            var c = _daSombra.color;
            c.a = opacidade * (1f - fracao);
            _daSombra.color = c;
        }

        private const string NomeDoObjeto = "Sombra";
    }
}
