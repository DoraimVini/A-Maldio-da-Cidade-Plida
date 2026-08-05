using UnityEngine;
using FavelaAmarela.Core.Abilities;
using FavelaAmarela.Core.Combat;

namespace FavelaAmarela.Runtime.Enemies
{
    public class EnemyCombat : MonoBehaviour
    {
        [Header("Ataque")]
        [SerializeField] private float alcanceDeGolpe = 1.2f;
        [SerializeField] private float cadenciaDeAtaque = 1.2f;
        [SerializeField] private float danoDoGolpe = 20f;
        [SerializeField] private LayerMask camadaDoJogador;

        private float _attackCooldown;
        private readonly Collider2D[] _bufferAlvo = new Collider2D[4];
        private ContactFilter2D _filtroJogador;
        private IDanificavel _alvoCache;

        public event System.Action OnAtaqueDesferido;
        public bool EstaPronto => _attackCooldown <= 0f;

        private void Awake()
        {
            if (camadaDoJogador.value == 0) camadaDoJogador = LayerMask.GetMask("Player");
            _filtroJogador = new ContactFilter2D { useTriggers = true };
            _filtroJogador.SetLayerMask(camadaDoJogador);
        }

        private void Update()
        {
            if (_attackCooldown > 0f) _attackCooldown -= Time.deltaTime;
        }

        public bool AlvoEstaAoAlcance()
        {
            if (_alvoCache != null)
            {
                if (_alvoCache is MonoBehaviour mb && mb == null) _alvoCache = null;
                else if (_alvoCache is IDanificavel && !((MonoBehaviour)_alvoCache).enabled) _alvoCache = null;
            }

            int total = Physics2D.OverlapCircle(transform.position, alcanceDeGolpe, _filtroJogador, _bufferAlvo);
            if (total <= 0) { _alvoCache = null; return false; }

            if (_alvoCache == null)
            {
                for (int i = 0; i < total; i++)
                {
                    _alvoCache = _bufferAlvo[i].GetComponentInParent<IDanificavel>();
                    if (_alvoCache != null) break;
                }
            }
            return _alvoCache != null;
        }

        public bool TentarAtacar()
        {
            if (!EstaPronto || !AlvoEstaAoAlcance()) return false;
            _attackCooldown = cadenciaDeAtaque;
            _alvoCache?.ReceberGolpe(new ArmaResult(true, 0f, 0f, false, 0f, danoDoGolpe));
            OnAtaqueDesferido?.Invoke();
            return true;
        }
    }
}
