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
        

        /// <summary>
        /// O dano deste golpe: o <b>Ataque da ficha</b> quando existe, e só então o campo local.
        ///
        /// <para><b>Por que (2026-08-29).</b> Cada inimigo carregava <b>dois</b> números de dano
        /// — o da ficha e um campo serializado aqui — e só o segundo rodava. Rebalancear pela
        /// ficha não mudava nada em jogo. O Cultista e o Byakhee foram unificados na véspera;
        /// estes três ficaram para trás por não terem prefab nem cena.</para>
        ///
        /// <para>E é o que faz o <c>nivelDaUnidade</c> valer: o Ataque escala pela
        /// <c>EscalaDeNivel</c>, o campo local não escala com nada.</para>
        ///
        /// <para><b>O corpo ainda não existe.</b> Enquanto este ator não ganhar um
        /// <c>EnemyBase</c> no prefab, <c>_corpo</c> é nulo e o campo local responde — o que
        /// preserva exatamente o comportamento de hoje em vez de zerar o dano.</para>
        /// </summary>
        private float DanoDoGolpe =>
            _corpo != null && _corpo.Atributos != null && _corpo.Atributos.Ataque > 0f
                ? _corpo.Atributos.Ataque
                : danoDoGolpe;

        /// <summary>O corpo que carrega a ficha, quando este ator tiver um.</summary>
        private EnemyBase _corpo;

        private EnemyBase _enemyBase;
        private EnemyMovement _enemyMovement;
        private SpriteRenderer _spriteRenderer;
        private Transform _alvo;
        private float _attackCooldown;
        private float _alcanceDeGolpe;

        private void Awake()
        {
            // O corpo carrega a ficha, que e a fonte da verdade do dano (ver acima).
            _corpo = GetComponent<EnemyBase>();

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
            // Golpe por DISTANCIA pura: radial, instantaneo, sem janela.
            FavelaAmarela.Runtime.Diagnostico.VisualizadorDeGolpes.RegistrarCirculo(
                transform.position, _alcanceDeGolpe,
                FavelaAmarela.Runtime.Diagnostico.VisualizadorDeGolpes.CorDeGolpe);

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
                alvoDanificavel.ReceberGolpe(new ArmaResult(true, 0f, 0f, false, 0f, DanoDoGolpe));
            }
        }

        private void HandleAbatido()
        {
            _enemyBase.OnAbatido -= HandleAbatido;
            Destroy(gameObject); 
        }
    }
}
