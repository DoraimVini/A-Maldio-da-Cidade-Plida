using UnityEngine;

namespace FavelaAmarela.Runtime.Rendering
{
    /// <summary>
    /// Atualiza o <c>sortingOrder</c> do <see cref="SpriteRenderer"/> conforme a
    /// posição Y do ator, para que atores que se movem (Damião, inimigos) sejam
    /// corretamente ocultados pelas paredes/casas à frente e desenhados por cima das
    /// que estão atrás — a base da profundidade isométrica ("fake iso" por Y-sort).
    ///
    /// Usa o MESMO fator do <c>LevelBlockoutGenerator</c> (<c>-y * 10</c>) para sortar
    /// de forma consistente contra a geometria estática já gerada. Roda em
    /// <c>LateUpdate</c> (depois do movimento aplicado em FixedUpdate/Update) e só
    /// escreve quando o valor arredondado muda — sem alocação (regra §1).
    ///
    /// O <see cref="offsetPes"/> permite sortar pela BASE (pés) do sprite em vez do
    /// centro do transform, importante quando a arte final tiver pivot no centro.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    [AddComponentMenu("Favela Amarela/Rendering/Dynamic Y Sort")]
    public sealed class DynamicYSort : MonoBehaviour
    {
        [Tooltip("Fator de conversão Y→sortingOrder. Deve casar com o LevelBlockoutGenerator (10).")]
        [SerializeField] private float fator = 10f;

        [Tooltip("Deslocamento do ponto de referência de sort (pés) relativo ao transform, no eixo Y.")]
        [SerializeField] private float offsetPes = 0f;

        private SpriteRenderer _sr;
        private int _ultimaOrdem = int.MinValue;

        private void Awake()
        {
            _sr = GetComponent<SpriteRenderer>();
        }

        private void LateUpdate()
        {
            int ordem = Mathf.RoundToInt(-(transform.position.y + offsetPes) * fator);
            if (ordem == _ultimaOrdem) return;

            _ultimaOrdem = ordem;
            _sr.sortingOrder = ordem;
        }
    }
}
