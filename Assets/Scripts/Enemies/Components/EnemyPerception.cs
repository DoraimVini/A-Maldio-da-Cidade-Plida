using UnityEngine;
using FavelaAmarela.Core.Stealth;

namespace FavelaAmarela.Runtime.Enemies
{
    public class EnemyPerception : MonoBehaviour
    {
        [Header("Audição")]
        [SerializeField] private float raioAudicao = 10f;
        [SerializeField] private float taxaSuspeita = 0.4f;
        [Range(0f, 1f)] [SerializeField] private float limiarAlerta = 0.3f;
        [Range(0f, 1f)] [SerializeField] private float limiarCaca = 0.7f;

        private SoundBroadcastService _soundBroadcaster;
        private float _suspeita;
        private Vector2? _ultimaOrigemConhecida;
        private bool _estaOuvindo;
        private bool _jaEntrouAlerta;
        private bool _jaEntrouCaca;

        public event System.Action<float> OnSuspeitaChanged;
        public event System.Action OnEntrouAlerta;
        public event System.Action OnEntrouCaca;
        public event System.Action OnPerdeuAlvo;

        public float Suspeita => _suspeita;
        public Vector2? UltimaOrigemConhecida => _ultimaOrigemConhecida;

        public void Bind(SoundBroadcastService broadcaster)
        {
            _soundBroadcaster = broadcaster;
            _soundBroadcaster.OnSomEmitido += HandleSomEmitido;
        }

        private void Update()
        {
            float delta = taxaSuspeita * Time.deltaTime;
            _suspeita = Mathf.Clamp01(_suspeita + (_estaOuvindo ? delta : -delta));
            OnSuspeitaChanged?.Invoke(_suspeita);

            if (!_jaEntrouAlerta && _suspeita >= limiarAlerta)
            {
                _jaEntrouAlerta = true;
                OnEntrouAlerta?.Invoke();
            }
            if (!_jaEntrouCaca && _suspeita >= limiarCaca)
            {
                _jaEntrouCaca = true;
                OnEntrouCaca?.Invoke();
            }
            if (_jaEntrouCaca && _suspeita < limiarAlerta)
            {
                _jaEntrouAlerta = false;
                _jaEntrouCaca = false;
                OnPerdeuAlvo?.Invoke();
            }

            _estaOuvindo = false;
        }

        private void HandleSomEmitido(SomEmitido som)
        {
            if (Vector2.Distance(transform.position, som.Origem) > raioAudicao) return;
            _estaOuvindo = true;
            _ultimaOrigemConhecida = som.Origem;
        }

        public void PerderAlvo()
        {
            _suspeita = 0f;
            _ultimaOrigemConhecida = null;
            _jaEntrouAlerta = false;
            _jaEntrouCaca = false;
            OnSuspeitaChanged?.Invoke(0f);
            OnPerdeuAlvo?.Invoke();
        }

        private void OnDestroy()
        {
            if (_soundBroadcaster != null)
                _soundBroadcaster.OnSomEmitido -= HandleSomEmitido;
        }
    }
}
