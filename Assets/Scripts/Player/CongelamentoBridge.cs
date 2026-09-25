using System;
using UnityEngine;
using FavelaAmarela.Core.Combat;
using FavelaAmarela.Core.Player;

namespace FavelaAmarela.Player
{
    /// <summary>
    /// Bridge do <b>congelamento</b> de Damião pelos Cones de Gelo de Abdul. Liga o POCO
    /// <see cref="AcumuloDeCongelamento"/> (a regra: 3 acúmulos congelam, acúmulos expiram)
    /// à <see cref="PlayerStateMachine"/> (o efeito: travar o jogador).
    ///
    /// <para>Usa <c>ForcarEstado</c> e não <c>TryEntrarAcao</c>: o congelamento é
    /// <b>imposto</b>, não escolhido — se dependesse de o jogador estar Livre, bastaria
    /// esquivar no momento certo para ignorá-lo por completo.</para>
    ///
    /// <para>Quem aplica os acúmulos é o <c>ConeDeGelo</c> ao acertar; este componente só
    /// guarda a regra e traduz o resultado para a FSM.</para>
    /// </summary>
    [AddComponentMenu("Favela Amarela/Congelamento (Damião)")]
    public sealed class CongelamentoBridge : MonoBehaviour
    {
        [Header("Regra de congelamento")]
        [Tooltip("Acúmulos de frio necessários para congelar.")]
        [SerializeField] private int limiteDeAcumulos = 3;

        [Tooltip("Segundos que um acúmulo dura sem tomar novo cone (depois expira).")]
        [SerializeField] private float duracaoDoAcumulo = 6f;

        [Tooltip("Segundos que Damião fica congelado ao atingir o limite.")]
        [SerializeField] private float duracaoDoCongelamento = 1.5f;

        [Header("Feedback")]
        [Tooltip("Sprite do Damião, tingido de azul enquanto congelado. Opcional.")]
        [SerializeField] private SpriteRenderer spriteDoDamiao;

        [Tooltip("Cor aplicada enquanto congelado.")]
        [SerializeField] private Color corCongelado = new Color(0.55f, 0.8f, 1f);

        private AcumuloDeCongelamento _acumulo;
        private PlayerStateMachine _fsm;
        private Color _corOriginal = Color.white;

        /// <summary>Acúmulos de frio correntes (para a UI de debuff).</summary>
        public int Acumulos => _acumulo?.Acumulos ?? 0;

        /// <summary>Se Damião está congelado neste instante.</summary>
        public bool EstaCongelado => _acumulo != null && _acumulo.EstaCongelado;

        /// <summary>Disparado quando a contagem de acúmulos muda (para a UI de debuff).</summary>
        public event Action<int> OnAcumulosMudaram;

        /// <summary>
        /// Injeta a FSM de ações do jogador (chamado por <c>PlayerMovement</c> no Awake),
        /// mesmo padrão dos demais bridges.
        /// </summary>
        public void BindStateMachine(PlayerStateMachine fsm) => _fsm = fsm;

        private void Awake()
        {
            _acumulo = new AcumuloDeCongelamento(
                limite: limiteDeAcumulos,
                duracaoDoAcumulo: duracaoDoAcumulo,
                duracaoDoCongelamento: duracaoDoCongelamento);

            _acumulo.OnCongelou += HandleCongelou;
            _acumulo.OnDescongelou += HandleDescongelou;
            _acumulo.OnAcumulosMudaram += HandleAcumulosMudaram;

            if (spriteDoDamiao == null) spriteDoDamiao = GetComponent<SpriteRenderer>();
            if (spriteDoDamiao != null) _corOriginal = spriteDoDamiao.color;
        }

        private void OnDestroy()
        {
            if (_acumulo == null) return;
            _acumulo.OnCongelou -= HandleCongelou;
            _acumulo.OnDescongelou -= HandleDescongelou;
            _acumulo.OnAcumulosMudaram -= HandleAcumulosMudaram;
        }

        private void Update() => _acumulo.Tick(Time.deltaTime);

        /// <summary>
        /// Damião foi atingido por um Cone de Gelo. Chamado pelo projétil — ao atingir o
        /// limite, congela.
        /// </summary>
        public void AplicarAcumulo() => _acumulo.AplicarAcumulo();

        /// <summary>Limpa acúmulos e congelamento (fim da luta, morte, Refúgio).</summary>
        public void Limpar() => _acumulo.Limpar();

        private void HandleCongelou()
        {
            if (_fsm == null)
            {
                Debug.LogError("[CongelamentoBridge] FSM não injetada — o congelamento não " +
                               "travou o jogador.", this);
                return;
            }

            _fsm.ForcarEstado(PlayerState.Congelado, duracaoDoCongelamento);
            AplicarCor(corCongelado);
        }

        private void HandleDescongelou() => AplicarCor(_corOriginal);

        private void HandleAcumulosMudaram(int acumulos) => OnAcumulosMudaram?.Invoke(acumulos);

        private void AplicarCor(Color cor)
        {
            if (spriteDoDamiao != null) spriteDoDamiao.color = cor;
        }
    }
}
