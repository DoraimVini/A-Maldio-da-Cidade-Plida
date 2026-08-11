using UnityEngine;
using FavelaAmarela.Core.Stealth;

namespace FavelaAmarela.Runtime.Audio
{
    /// <summary>
    /// Camada Runtime. Dá voz ao <b>ruído que Damião emite</b> — a mecânica central do jogo,
    /// que até aqui era completamente invisível para quem joga.
    ///
    /// <para>O Cultista caça por som, a tempestade abafa o ruído e a Esquiva faz barulho de
    /// propósito. Sem retorno sonoro, o jogador não tem como perceber nada disso: ele anda,
    /// é caçado, e não entende por quê. Este componente fecha esse laço — <b>o volume do
    /// passo é proporcional ao raio do som emitido</b>, então andar agachado quase não soa e
    /// correr na tempestade soa alto.</para>
    ///
    /// Observa <c>SoundBroadcastService.OnSomEmitido</c>; nenhum polling.
    /// </summary>
    [AddComponentMenu("Favela Amarela/Audio/Áudio de Stealth")]
    public sealed class AudioDeStealth : MonoBehaviour
    {
        [Tooltip("Raio de som considerado 'alto'. Um passo com este raio toca no volume cheio.")]
        [Min(0.1f)]
        [SerializeField] private float raioDeReferencia = 8f;

        [Range(0f, 1f)]
        [Tooltip("Volume mínimo audível, para um passo furtivo ainda dar algum retorno.")]
        [SerializeField] private float volumeMinimo = 0.12f;

        private SoundBroadcastService _fonte;

        /// <summary>
        /// Liga ao serviço de som do jogo. Chamado pelo <c>GameManager</c> no bootstrap.
        /// Idempotente: re-bind troca a fonte sem deixar handler pendurado.
        /// </summary>
        public void Bind(SoundBroadcastService fonte)
        {
            Unbind();

            _fonte = fonte;
            if (_fonte != null) _fonte.OnSomEmitido += HandleSomEmitido;
        }

        /// <summary>Desconecta do evento. Seguro chamar sem bind ativo.</summary>
        public void Unbind()
        {
            if (_fonte != null) _fonte.OnSomEmitido -= HandleSomEmitido;
            _fonte = null;
        }

        private void OnDestroy() => Unbind();

        private void HandleSomEmitido(SomEmitido som)
        {
            var mixer = MixerDeAudio.Instancia;
            if (mixer == null) return;

            // O quanto o som foi alto vira o quanto ele soa: é isso que ensina o jogador
            // que agachar reduz o rastro sonoro e correr o amplia.
            float proporcao = Mathf.Clamp01(som.RaioEfetivo / raioDeReferencia);
            float volume = Mathf.Lerp(volumeMinimo, 1f, proporcao);

            mixer.Tocar(SomDoJogo.PassoDeDamiao, som.Origem, volume);
        }
    }
}
