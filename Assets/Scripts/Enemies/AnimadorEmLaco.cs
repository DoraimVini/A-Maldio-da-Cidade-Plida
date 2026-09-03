using UnityEngine;

namespace FavelaAmarela.Runtime.Enemies
{
    /// <summary>
    /// Camada Runtime. Toca um ciclo de quadros <b>em laço, sem parar</b>, no
    /// <see cref="SpriteRenderer"/> do próprio objeto — mesmo padrão dos <c>AnimadorDo*</c> do
    /// elenco: escreve <c>sprite</c> direto, sem <c>AnimatorController</c>.
    ///
    /// <para><b>Quem usa, e por que é o mesmo componente.</b> Os <c>AnimadorDo*</c> do elenco
    /// diferem porque cada um lê um <b>driver</b> diferente (a FSM do Cultista, a do Espectro,
    /// o Rigidbody do Esqueleto). Estes dois não leem nada — eles giram enquanto o objeto
    /// estiver ativo, e quem decide se ele está ativo é outro:</para>
    /// <list type="bullet">
    ///   <item><description><b>Aura da <see cref="PedraDePoder"/>:</b> a Pedra sustenta o Escudo
    ///   Mágico do Abdul na Fase 1, e quebrá-la é a <b>única</b> forma de causar dano ali. A
    ///   aura diz quais pedras ainda seguram o escudo. <c>Estilhacar()</c> termina em
    ///   <c>SetActive(false)</c> — some a Pedra, some a aura.</description></item>
    ///   <item><description><b>Cúpula do Escudo Mágico:</b> filha do Abdul, ligada e desligada
    ///   por <c>AbdulAlhazredAI.AplicarVisualDeEscudo</c> conforme a FSM.</description></item>
    /// </list>
    ///
    /// <para><b>Histórico do nome (2026-09-03):</b> nasceu como <c>AnimadorDaPedraDePoder</c> e
    /// foi renomeado no mesmo dia, ao ganhar o segundo dono. Um componente chamado
    /// "AnimadorDaPedraDePoder" dentro do prefab do Abdul seria mentira no YAML — e o YAML é
    /// onde se vai procurar quando algo não liga.</para>
    ///
    /// <para>Não há POCO por trás porque não há regra por trás: isto é cadência de quadro, não
    /// decisão de jogo.</para>
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    [AddComponentMenu("Favela Amarela/Enemies/Animador em Laço")]
    public sealed class AnimadorEmLaco : MonoBehaviour
    {
        [Header("Quadros do ciclo")]
        [Tooltip("Os quadros, em ordem. [ASSET pixel art]")]
        [SerializeField] private Sprite[] quadros;

        [Header("Cadência")]
        [Min(1f)]
        [Tooltip("Uma volta completa no ciclo; 10 fecha a volta em pouco mais de um segundo, " +
                 "devagar o bastante para ler como pulso e não como piscada.")]
        [SerializeField] private float quadrosPorSegundo = 10f;

        private SpriteRenderer _sprite;
        private float _relogio;
        private int _quadro;

        private void Awake() => _sprite = GetComponent<SpriteRenderer>();

        private void Update()
        {
            if (quadros == null || quadros.Length == 0) return;

            _relogio += Time.deltaTime * quadrosPorSegundo;
            if (_relogio < 1f) return;

            _relogio -= 1f;
            _quadro = (_quadro + 1) % quadros.Length;
            _sprite.sprite = quadros[_quadro];
        }
    }
}
