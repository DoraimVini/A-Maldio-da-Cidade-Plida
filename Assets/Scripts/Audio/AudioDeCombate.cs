using UnityEngine;
using FavelaAmarela.Runtime.Enemies;

namespace FavelaAmarela.Runtime.Audio
{
    /// <summary>
    /// Camada Runtime. Sonoriza o que acontece com <b>esta</b> entidade: levar dano e ser
    /// abatida. Vai no prefab do inimigo, ao lado do <see cref="EnemyBase"/>.
    ///
    /// <para>É por entidade, e não um observador global, porque o som precisa sair do lugar
    /// onde o golpe aconteceu — num jogo em que se caça por som, áudio sem posição mente
    /// para o jogador.</para>
    /// </summary>
    [RequireComponent(typeof(EnemyBase))]
    [AddComponentMenu("Favela Amarela/Audio/Áudio de Combate")]
    public sealed class AudioDeCombate : MonoBehaviour
    {
        private EnemyBase _corpo;

        private void Awake()
        {
            _corpo = GetComponent<EnemyBase>();
            if (_corpo == null)
            {
                Debug.LogError($"[AudioDeCombate] '{name}' não tem EnemyBase — nada será sonorizado.", this);
                return;
            }

            _corpo.OnDanoSofrido += HandleDano;
            _corpo.OnAbatido += HandleAbate;
        }

        private void OnDestroy()
        {
            if (_corpo == null) return;

            _corpo.OnDanoSofrido -= HandleDano;
            _corpo.OnAbatido -= HandleAbate;
        }

        private void HandleDano(float dano)
            => MixerDeAudio.Instancia?.Tocar(SomDoJogo.EntidadeFerida, transform.position);

        private void HandleAbate()
            => MixerDeAudio.Instancia?.Tocar(SomDoJogo.EntidadeAbatida, transform.position);
    }
}
