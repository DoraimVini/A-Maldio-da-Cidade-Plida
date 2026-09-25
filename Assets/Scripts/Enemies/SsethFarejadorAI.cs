using UnityEngine;
using FavelaAmarela.Core.Combat;
using FavelaAmarela.Core.Abilities;
using FavelaAmarela.Player;

namespace FavelaAmarela.Runtime.Enemies
{
    [RequireComponent(typeof(EnemyBase), typeof(EnemyMovement), typeof(SpriteRenderer))]
    [RequireComponent(typeof(Rigidbody2D))]
    [AddComponentMenu("Favela Amarela/Enemies/Sseth Farejador")]
    public class SsethFarejadorAI : MonoBehaviour
    {
        [Header("Faro (Stealth Olfativo)")]
        [Tooltip("Raio de detecção pelo cheiro. Diferente da visão, o faro ignora paredes (Raycasts).")]
        [SerializeField] private float raioDeFaro = 8f;
        [SerializeField] private LayerMask camadaDoJogador;
        
        [Header("Patrulha")]
        [SerializeField] private Transform[] waypoints;
        private int _currentWaypoint = 0;
        
        [Header("Combate")]
        [SerializeField] private float cadenciaDeAtaque = 1.5f;
        [SerializeField] private float danoDoGolpe = 20f;
        
        private EnemyBase _enemyBase;
        private EnemyMovement _enemyMovement;
        private SpriteRenderer _spriteRenderer;
        private Transform _alvo;
        private float _attackCooldown;
        private float _alcanceDeGolpe;

        private void Awake()
        {
            _enemyBase = GetComponent<EnemyBase>();
            _enemyMovement = GetComponent<EnemyMovement>();
            _spriteRenderer = GetComponent<SpriteRenderer>();

            if (_enemyBase.Atributos != null)
            {
                _alcanceDeGolpe = _enemyBase.Atributos.AlcanceDeGolpe;
            }
            else
            {
                _alcanceDeGolpe = 1.2f;
            }
        }

        private void Start()
        {
            _enemyBase.OnDanoSofrido += (dano) => {
                if (_alvo == null)
                {
                    var player = GameObject.FindGameObjectWithTag("Player");
                    if (player != null) _alvo = player.transform;
                }
            };

            _enemyBase.OnAbatido += HandleAbatido;
        }

        private void Update()
        {
            _attackCooldown -= Time.deltaTime;
            
            ProcurarPeloFaro();
            
            if (_alvo != null)
            {
                CacarAlvo();
            }
            else
            {
                Patrulhar();
            }
        }

        private void ProcurarPeloFaro()
        {
            Collider2D hit = Physics2D.OverlapCircle(transform.position, raioDeFaro, camadaDoJogador);
            if (hit != null)
            {
                var playerMovement = hit.GetComponent<PlayerMovement>();
                if (playerMovement != null && playerMovement.StealthState != null)
                {
                    if (playerMovement.StealthState.IsOdorMasked) { PerderAlvo(); return; }
                }
                
                _alvo = hit.transform;
                _spriteRenderer.color = Color.red; 
            }
            else
            {
                PerderAlvo();
            }
        }

        private void PerderAlvo()
        {
            _alvo = null;
            _spriteRenderer.color = Color.white;
        }

        private void Patrulhar()
        {
            if (waypoints == null || waypoints.Length == 0)
            {
                _enemyMovement.Parar();
                return;
            }
            
            Transform destino = waypoints[_currentWaypoint];
            _enemyMovement.MoverPara(destino.position, _enemyMovement.VelocidadeErrante);
            
            if (Vector2.Distance(transform.position, destino.position) < 0.2f)
            {
                _currentWaypoint = (_currentWaypoint + 1) % waypoints.Length;
            }
        }

        private void CacarAlvo()
        {
            if (Vector2.Distance(transform.position, _alvo.position) <= _alcanceDeGolpe)
            {
                _enemyMovement.Parar();
                if (_attackCooldown <= 0f)
                {
                    Atacar();
                }
            }
            else
            {
                _enemyMovement.MoverPara(_alvo.position, _enemyMovement.VelocidadeCaca);
            }
        }

        private void Atacar()
        {
            _attackCooldown = cadenciaDeAtaque;
            
            IDanificavel alvoDanificavel = _alvo.GetComponent<IDanificavel>();
            if (alvoDanificavel != null)
            {
                alvoDanificavel.ReceberGolpe(new ArmaResult(true, 0f, 0f, false, 0f, danoDoGolpe));
            }
        }

        private void HandleAbatido()
        {
            _enemyBase.OnAbatido -= HandleAbatido;
            Destroy(gameObject); 
        }
    }
}
