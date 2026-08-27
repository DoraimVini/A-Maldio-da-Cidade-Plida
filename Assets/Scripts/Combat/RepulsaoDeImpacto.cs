using UnityEngine;

namespace FavelaAmarela.Runtime.Combat
{
    /// <summary>
    /// Empurra um corpo quando ele leva um golpe com <c>ForcaRepulsao</c>.
    ///
    /// <para><b>Por que este componente existe em vez de um <c>AddForce</c>.</b> A doc da Unity
    /// 6.4 diz sobre <c>Rigidbody2D.linearVelocity</c>: <i>"The value is not usually set
    /// directly but rather by using forces."</i> Este projeto faz exatamente o contrário, por
    /// convenção (<c>CLAUDE.md</c> §5): <c>PlayerMovement</c> e as IAs <b>atribuem</b>
    /// <c>linearVelocity</c> a cada <c>FixedUpdate</c> — são <b>20+ atribuições em 9
    /// arquivos</b>. Um impulso via <c>AddForce</c> seria sobrescrito no passo de física
    /// seguinte e o empurrão simplesmente <b>não apareceria, sem erro nenhum</b>.</para>
    ///
    /// <para><b>A solução é ordem de execução, não força.</b> A doc de
    /// <c>DefaultExecutionOrder</c>: <i>"Specifies the script execution order for a
    /// MonoBehaviour-derived class relative to other MonoBehaviour-derived types."</i> Com
    /// ordem 1000, este <c>FixedUpdate</c> roda <b>depois</b> do movimento e sobrescreve o que
    /// ele escreveu, enquanto a repulsão durar. <b>Nenhum dos 9 arquivos de IA precisa saber
    /// que isto existe</b> — que é o ponto: lista de N lugares para manter à mão é o modo de
    /// falha dominante deste repositório.</para>
    ///
    /// <para>Auto-construído por <see cref="GarantirPara"/>, no mesmo padrão de
    /// <c>Hurtbox.GarantirPara</c>: ator novo ganha repulsão de graça.</para>
    /// </summary>
    [DefaultExecutionOrder(1000)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class RepulsaoDeImpacto : MonoBehaviour
    {
        /// <summary>
        /// Quanto tempo o empurrão dura. Curto de propósito: repulsão em ARPG é um safanão que
        /// abre espaço, não um voo. Acima disso o jogador perde o controle e reclama do jogo.
        /// </summary>
        private const float DuracaoDoEmpurrao = 0.18f;

        /// <summary>
        /// Abaixo disto o empurrão é imperceptível e só atrapalharia o movimento. Um golpe sem
        /// <c>ForcaRepulsao</c> autorada não deve mexer no alvo.
        /// </summary>
        private const float ForcaMinima = 0.05f;

        private Rigidbody2D _rb;
        private Vector2 _velocidade;
        private float _tempoRestante;

        /// <summary>Verdadeiro enquanto este corpo está sendo empurrado.</summary>
        public bool Ativa => _tempoRestante > 0f;

        private void Awake() => _rb = GetComponent<Rigidbody2D>();

        /// <summary>
        /// Aplica um empurrão, já descontando o quanto este corpo deixou de ser matéria.
        /// </summary>
        /// <param name="direcao">Do agressor para o alvo. Normalizada aqui.</param>
        /// <param name="forca">O <c>ForcaRepulsao</c> do <c>ArmaResult</c>.</param>
        public void Empurrar(Vector2 direcao, float forca)
        {
            if (forca <= ForcaMinima) return;
            if (direcao.sqrMagnitude < 0.0001f) return;

            float sobra = 1f - CorpoImpregnado.De(this);
            if (sobra <= 0f) return;   // não é mais matéria: o golpe acerta, o corpo não cede

            _velocidade = direcao.normalized * (forca * sobra);
            _tempoRestante = DuracaoDoEmpurrao;
        }

        /// <summary>
        /// Sobrescreve o que o movimento escreveu, e desacelera até parar. A desaceleração é
        /// linear e termina exatamente em zero, para o ator não ficar deslizando.
        /// </summary>
        private void FixedUpdate()
        {
            if (_tempoRestante <= 0f) return;

            _tempoRestante -= Time.fixedDeltaTime;

            if (_tempoRestante <= 0f)
            {
                _tempoRestante = 0f;
                _rb.linearVelocity = Vector2.zero;
                return;
            }

            _rb.linearVelocity = _velocidade * (_tempoRestante / DuracaoDoEmpurrao);
        }

        /// <summary>
        /// Acha (ou cria) a repulsão de um alvo. Sobe até a raiz do ator porque o golpe acerta
        /// um colisor filho — a <c>Hurtbox</c> —, e é o corpo do ator que tem o
        /// <c>Rigidbody2D</c>.
        /// </summary>
        /// <returns><c>null</c> quando o alvo não tem <c>Rigidbody2D</c> (cenário, gatilho) —
        /// empurrar o que não tem corpo não faz sentido e não é erro.</returns>
        public static RepulsaoDeImpacto GarantirPara(Component alvo)
        {
            if (alvo == null) return null;

            var rb = alvo.GetComponentInParent<Rigidbody2D>();
            if (rb == null) return null;

            var existente = rb.GetComponent<RepulsaoDeImpacto>();
            if (existente != null) return existente;

            return rb.gameObject.AddComponent<RepulsaoDeImpacto>();
        }
    }
}
