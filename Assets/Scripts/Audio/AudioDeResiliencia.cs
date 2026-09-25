using UnityEngine;
using FavelaAmarela.Core.Combat;

namespace FavelaAmarela.Runtime.Audio
{
    /// <summary>
    /// Camada Runtime. Sonoriza as viradas de estado mental de Damião: entrar em Pânico e
    /// entrar em Colapso.
    ///
    /// <para>Toca <b>só nas transições</b>, não a cada variação de Resiliência — os campos
    /// <c>EntrouEmPanico</c>/<c>EntrouEmColapso</c> de <see cref="ResilienciaChangedArgs"/>
    /// existem exatamente para isso. Um som por ponto de dreno viraria ruído constante e
    /// perderia o susto.</para>
    /// </summary>
    [AddComponentMenu("Favela Amarela/Audio/Áudio de Resiliência")]
    public sealed class AudioDeResiliencia : MonoBehaviour
    {
        private ResilienciaMental _fonte;

        /// <summary>Liga à Resiliência de Damião. Chamado pelo <c>GameManager</c>.</summary>
        public void Bind(ResilienciaMental fonte)
        {
            Unbind();

            _fonte = fonte;
            if (_fonte != null) _fonte.OnChanged += HandleMudanca;
        }

        /// <summary>Desconecta do evento. Seguro chamar sem bind ativo.</summary>
        public void Unbind()
        {
            if (_fonte != null) _fonte.OnChanged -= HandleMudanca;
            _fonte = null;
        }

        private void OnDestroy() => Unbind();

        private void HandleMudanca(ResilienciaChangedArgs args)
        {
            var mixer = MixerDeAudio.Instancia;
            if (mixer == null) return;

            if (args.EntrouEmColapso)
                mixer.Tocar(SomDoJogo.Colapso, transform.position);
            else if (args.EntrouEmPanico)
                mixer.Tocar(SomDoJogo.EntrouEmPanico, transform.position);
        }
    }
}
