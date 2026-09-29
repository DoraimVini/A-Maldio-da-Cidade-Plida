using UnityEngine;

namespace FavelaAmarela.Runtime.Enemies
{
    /// <summary>
    /// Os dois traços que tornam o olhar do Rei <b>legível</b>, desenhados com
    /// <c>LineRenderer</c> — nenhuma arte nova:
    /// <list type="bullet">
    /// <item><b>O fio</b>: uma linha fina do Rei até o Damião, só enquanto ele está exposto. Some
    /// no instante em que ele entra na sombra. É o sinal que o plano diz ser obrigatório: num
    /// isométrico, "atrás da estátua" é uma linha no chão que o olho não traça sozinho.</item>
    /// <item><b>O farol</b>: o cone da Máscara (Fase 2), um triângulo translúcido no chão. Um
    /// <c>LineRenderer</c> de dois pontos com a largura indo de zero ao máximo <i>é</i> um
    /// triângulo — o cone sai de graça.</item>
    /// </list>
    ///
    /// <para>Não é <c>MonoBehaviour</c>: o Rei cria e comanda; não há ciclo de vida próprio.
    /// Os objetos nascem sob o pai do Rei, e não sob ele, porque o Rei tem escala 3,7 e a
    /// largura de linha é em unidades de mundo.</para>
    /// </summary>
    internal sealed class SinaisDoOlhar
    {
        private const float ComprimentoDoFarol = 16f;

        private static readonly Color CorDoFio = new Color(0.98f, 0.86f, 0.3f, 0.9f);
        private static readonly Color CorDoFarol = new Color(0.98f, 0.86f, 0.3f, 0.14f);
        private static readonly Color CorDoFarolAberto = new Color(1f, 0.55f, 0.2f, 0.3f);

        private readonly LineRenderer _fio;
        private readonly LineRenderer _farol;

        /// <summary>Cria os dois traços, escondidos.</summary>
        /// <param name="pai">Onde pendurar (sem escala).</param>
        /// <param name="material">Material de sprite do projeto (o do Rei serve).</param>
        /// <param name="camada">Sorting layer dos sprites da cena.</param>
        /// <param name="ordemDoFarol">Acima do chão e abaixo dos atores.</param>
        public SinaisDoOlhar(Transform pai, Material material, int camada, int ordemDoFarol)
        {
            _fio = Criar("Rito_FioDoOlhar", pai, material, camada, short.MaxValue - 10);
            _fio.widthMultiplier = 0.07f;
            _fio.startColor = _fio.endColor = CorDoFio;

            _farol = Criar("Rito_FarolDaMascara", pai, material, camada, ordemDoFarol);
            _farol.widthCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        }

        private static LineRenderer Criar(string nome, Transform pai, Material material, int camada, int ordem)
        {
            var go = new GameObject(nome);
            go.transform.SetParent(pai, false);
            var lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = true;
            lr.positionCount = 2;
            lr.numCapVertices = 0;
            lr.alignment = LineAlignment.View;
            lr.sharedMaterial = material;
            lr.sortingLayerID = camada;
            lr.sortingOrder = ordem;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows = false;
            lr.enabled = false;
            return lr;
        }

        /// <summary>Liga o fio entre dois pontos, ou o esconde.</summary>
        public void Fio(bool visivel, Vector2 de, Vector2 ate)
        {
            _fio.enabled = visivel;
            if (!visivel) return;
            _fio.SetPosition(0, de);
            _fio.SetPosition(1, ate);
        }

        /// <summary>Desenha o cone da Máscara, ou o esconde.</summary>
        /// <param name="centroGraus">Direção do cone, a partir de "para baixo", positiva à direita.</param>
        /// <param name="larguraGraus">Abertura total do cone.</param>
        public void Farol(bool visivel, Vector2 de, float centroGraus, float larguraGraus, bool aberta)
        {
            _farol.enabled = visivel;
            if (!visivel) return;

            float r = centroGraus * Mathf.Deg2Rad;
            var dir = new Vector2(Mathf.Sin(r), -Mathf.Cos(r));
            _farol.SetPosition(0, de);
            _farol.SetPosition(1, de + dir * ComprimentoDoFarol);

            float meia = Mathf.Clamp(larguraGraus * 0.5f, 1f, 80f) * Mathf.Deg2Rad;
            _farol.widthMultiplier = 2f * ComprimentoDoFarol * Mathf.Tan(meia);
            _farol.startColor = _farol.endColor = aberta ? CorDoFarolAberto : CorDoFarol;
        }

        /// <summary>Apaga os dois.</summary>
        public void Esconder()
        {
            _fio.enabled = false;
            _farol.enabled = false;
        }
    }
}
