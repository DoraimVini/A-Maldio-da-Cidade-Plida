using System;
using UnityEngine;

namespace FavelaAmarela.Core.Stealth
{
    public readonly struct SomEmitido
    {
        public readonly Vector2 Origem;
        public readonly float RaioEfetivo;
        
        public SomEmitido(Vector2 origem, float raioEfetivo)
        {
            Origem = origem;
            RaioEfetivo = raioEfetivo;
        }
    }

    public sealed class SoundBroadcastService
    {
        public event Action<SomEmitido> OnSomEmitido;
        
        public void Emitir(SomEmitido som)
        {
            OnSomEmitido?.Invoke(som);
        }
    }
}
