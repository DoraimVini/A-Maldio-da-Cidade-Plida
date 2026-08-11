using UnityEngine;

namespace FavelaAmarela.Runtime.Itens
{
    /// <summary>
    /// Camada Runtime. O sinal que fica pairando sobre uma entidade revelada pelo Aklo.
    ///
    /// <para>É um <b>objeto próprio</b>, filho da entidade, com o próprio
    /// <c>SpriteRenderer</c> em ordem altíssima — e não uma alteração no renderer do inimigo.
    /// Assim ele atravessa parede sem brigar com o <c>DynamicYSort</c>, que reescreve o
    /// <c>sortingOrder</c> do inimigo todo <c>LateUpdate</c>.</para>
    ///
    /// <para>Idempotente: revelar de novo quem já está marcado só renova o tempo.</para>
    /// </summary>
    [AddComponentMenu("")]
    public sealed class MarcadorDeRevelacao : MonoBehaviour
    {
        /// <summary>Ordem de desenho bem acima de qualquer geometria do blockout.</summary>
        private const int OrdemAcimaDeTudo = 30000;

        private float _restante;

        /// <summary>
        /// Marca a entidade (ou renova a marca) por <paramref name="duracao"/> segundos.
        /// </summary>
        /// <param name="alvo">Entidade revelada.</param>
        /// <param name="duracao">Tempo de permanência do sinal.</param>
        /// <param name="sprite">Arte do sinal. Nulo cai num quadrado procedural.</param>
        /// <param name="cor">Tinta do sinal.</param>
        /// <param name="alturaDoSinal">Deslocamento vertical acima do alvo.</param>
        public static void Marcar(GameObject alvo, float duracao, Sprite sprite, Color cor, float alturaDoSinal)
        {
            if (alvo == null || duracao <= 0f) return;

            var existente = alvo.GetComponentInChildren<MarcadorDeRevelacao>();
            if (existente != null)
            {
                existente._restante = Mathf.Max(existente._restante, duracao);
                return;
            }

            var go = new GameObject("Marcador_Revelacao", typeof(SpriteRenderer), typeof(MarcadorDeRevelacao));
            go.transform.SetParent(alvo.transform, false);
            go.transform.localPosition = new Vector3(0f, alturaDoSinal, 0f);

            var sr = go.GetComponent<SpriteRenderer>();
            sr.sprite = sprite != null ? sprite : SpriteProcedural();
            sr.color = cor;
            sr.sortingOrder = OrdemAcimaDeTudo;

            go.GetComponent<MarcadorDeRevelacao>()._restante = duracao;
        }

        private void Update()
        {
            _restante -= Time.deltaTime;
            if (_restante <= 0f) Destroy(gameObject);
        }

        private static Sprite _procedural;

        /// <summary>Quadrado branco 1×1, criado uma vez e reaproveitado.</summary>
        private static Sprite SpriteProcedural()
        {
            if (_procedural != null) return _procedural;

            var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();

            _procedural = Sprite.Create(tex, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 32f);
            return _procedural;
        }
    }
}
