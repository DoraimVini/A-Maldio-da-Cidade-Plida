using UnityEngine;
using FavelaAmarela.Player;

namespace FavelaAmarela.Runtime.Audio
{
    /// <summary>
    /// Camada Runtime. Dá voz aos <b>golpes de Damião</b> — o gesto que o jogador executa mais
    /// vezes e o único do combate que não tinha retorno sonoro nenhum.
    ///
    /// <para><b>Por que existe (2026-08-20):</b> o Vini relatou que a luta contra o Byakhee
    /// <i>"não tem feel bom"</i>. Medindo os nove valores de <see cref="SomDoJogo"/>, quatro
    /// nunca eram disparados por ninguém — existiam só como forma de onda em
    /// <c>SinteseDeSom</c>. Dois deles são justamente o golpe e a habilidade de arma. Ou seja:
    /// o jogador atacava um chefe e <b>não ouvia o próprio golpe</b>, nem o acerto (o Byakhee
    /// também estava sem <c>AudioDeCombate</c>). Um combate mudo lê como um combate que não
    /// responde.</para>
    ///
    /// <para>Segue o molde dos irmãos <c>AudioDeStealth</c> e <c>AudioDeResiliencia</c>:
    /// observador com <c>Bind</c>/<c>Unbind</c>, ligado pelo <c>GameLoopBootstrap</c>. Nada de
    /// procurar componente por busca, e nada de o <c>MaoFisicaBridge</c> conhecer a camada de
    /// áudio — quem quiser o som assina o evento que já existe.</para>
    ///
    /// <para><b>O que este componente NÃO faz, de propósito:</b> golpear continua <b>sem emitir
    /// ruído de stealth</b>. Hoje só o <c>PlayerMovement</c> chama
    /// <c>SoundBroadcastService.Emitir</c>, e o <c>AudioDeStealth</c> assume que todo
    /// <c>SomEmitido</c> é passo de Damião — fazer o golpe emitir tocaria "passo" para uma
    /// espadada e mudaria o equilíbrio da furtividade sem ninguém ter pedido. Isso é decisão de
    /// design pendente com o Vini, não um esquecimento.</para>
    /// </summary>
    [AddComponentMenu("Favela Amarela/Audio/Áudio do Jogador")]
    public sealed class AudioDoJogador : MonoBehaviour
    {
        private MaoFisicaBridge _mao;

        /// <summary>
        /// Liga à Mão Física do jogador. Idempotente: re-bind troca a fonte sem deixar handler
        /// pendurado.
        /// </summary>
        public void Bind(MaoFisicaBridge mao)
        {
            Unbind();

            _mao = mao;
            if (_mao == null) return;

            _mao.OnAtaqueExecutado += HandleAtaque;
            _mao.OnHabilidadeExecutada += HandleHabilidade;
        }

        /// <summary>Desconecta dos eventos. Seguro chamar sem bind ativo.</summary>
        public void Unbind()
        {
            if (_mao != null)
            {
                _mao.OnAtaqueExecutado -= HandleAtaque;
                _mao.OnHabilidadeExecutada -= HandleHabilidade;
            }

            _mao = null;
        }

        private void OnDestroy() => Unbind();

        // A direção e a duração vêm no evento e são ignoradas aqui: o som do golpe é o mesmo
        // para qualquer lado, e a duração é da animação, não da onda.
        private void HandleAtaque(Vector2 _, float __)
            => MixerDeAudio.Instancia?.Tocar(SomDoJogo.GolpeDesferido, transform.position);

        private void HandleHabilidade(Vector2 _, float __)
            => MixerDeAudio.Instancia?.Tocar(SomDoJogo.HabilidadeDeArma, transform.position);
    }
}
