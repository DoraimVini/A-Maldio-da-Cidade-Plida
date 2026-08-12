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
    /// <para>Vai <b>no Damião</b>. Funcionalmente rodaria em qualquer objeto — a posição do
    /// som vem da <c>Origem</c> do próprio evento, não do <c>transform</c> daqui —, mas é
    /// sobre ele, e ficar solto na hierarquia só esconderia essa relação de quem abrisse a
    /// cena depois.</para>
    ///
    /// <para><b>Premissa:</b> todo <c>SomEmitido</c> é ruído de Damião. Isso vale hoje porque
    /// só o <c>PlayerMovement</c> chama <c>Emitir</c> — os inimigos apenas escutam. Se algum
    /// dia um inimigo emitir pelo mesmo serviço, este componente tocaria "passo de Damião"
    /// para o passo dele; nesse dia, o <c>SomEmitido</c> precisa dizer <b>quem</b> emitiu.</para>
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
