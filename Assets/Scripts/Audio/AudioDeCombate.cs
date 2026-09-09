using UnityEngine;
using FavelaAmarela.Runtime.Enemies;

namespace FavelaAmarela.Runtime.Audio
{
    /// <summary>
    /// Camada Runtime. Sonoriza <b>o abate</b> desta entidade. Vai no prefab do inimigo, ao
    /// lado do <see cref="EnemyBase"/>.
    ///
    /// <para>É por entidade, e não um observador global, porque o som precisa sair do lugar
    /// onde o golpe aconteceu — num jogo em que se caça por som, áudio sem posição mente
    /// para o jogador.</para>
    ///
    /// <para><b>O som de LEVAR DANO saiu daqui em 2026-09-04</b> e foi para
    /// <c>Hurtbox.Receber</c>. Motivo: este componente exige <c>EnemyBase</c>, e só o Cultista
    /// e o Byakhee o têm — o Abdul, o Rei, os Esqueletos, os Cortesãos, as Pedras de Poder e o
    /// próprio Damião apanhavam em silêncio. A <c>Hurtbox</c> é o ponto de passagem obrigatório
    /// de todo golpe que acerta, nos dois sentidos, e por isso alcança o elenco inteiro sem
    /// lista nenhuma para manter.</para>
    ///
    /// <para><b>O abate continua aqui</b>, e não pode migrar junto: a <c>Hurtbox</c> só vê o
    /// golpe chegar, não sabe se ele derrubou alguém. Quem sabe é o <c>OnAbatido</c> do corpo.
    /// Consequência conhecida: quem não tem <c>EnemyBase</c> ainda <b>morre em silêncio</b>.</para>
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

            _corpo.OnAbatido += HandleAbate;
        }

        private void OnDestroy()
        {
            if (_corpo == null) return;

            _corpo.OnAbatido -= HandleAbate;
        }

        private void HandleAbate()
            => MixerDeAudio.Instancia?.Tocar(SomDoJogo.EntidadeAbatida, transform.position);
    }
}
