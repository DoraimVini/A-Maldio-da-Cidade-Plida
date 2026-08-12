using UnityEngine;
using FavelaAmarela.Core.Abilities;
using FavelaAmarela.Core.Combat;
using FavelaAmarela.Runtime.GameLoop;

namespace FavelaAmarela.Runtime.Enemies
{
    [RequireComponent(typeof(EnemyBase), typeof(EnemyMovement), typeof(SpriteRenderer))]
    [RequireComponent(typeof(Rigidbody2D))]
    [AddComponentMenu("Favela Amarela/Enemies/Avatar de Set (Boss)")]
    public class AvatarDeSetAI : MonoBehaviour
    {
        [Header("Combate (Boss)")]
        [SerializeField] private float alcanceDeGolpe = 2.5f;
        [SerializeField] private float cadenciaDeAtaque = 2.0f;
        [SerializeField] private float danoDoGolpe = 80f;

        [Header("Drops")]
        [Tooltip("Prefab do item coletável 'Elmo de Set' que dropa ao morrer")]
        [SerializeField] private GameObject prefabElmoDeSet;

        [Header("Arena")]
        [SerializeField] private TrancaDeArena trancaDaArena;

        private EnemyBase _enemyBase;
        private EnemyMovement _enemyMovement;
        private Transform _jogador;
        private float _attackCooldown;
        private bool _lutaAtiva;

        private void Awake()
        {
            _enemyBase = GetComponent<EnemyBase>();
            _enemyMovement = GetComponent<EnemyMovement>();
        }

        private void Start()
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) _jogador = p.transform;

            _enemyBase.OnAbatido += HandleAbatido;
        }

        private void Update()
        {
            if (_jogador == null) return;
            
            float distancia = Vector2.Distance(transform.position, _jogador.position);

            if (!_lutaAtiva && distancia < 10f)
            {
                IniciarLuta();
            }

            if (_lutaAtiva)
            {
                ExecutarComportamento(distancia);
            }
        }

        private void IniciarLuta()
        {
            _lutaAtiva = true;
            Debug.Log("[AvatarDeSetAI] O Avatar despertou.");
            if (trancaDaArena != null) trancaDaArena.Trancar();
        }

        private void ExecutarComportamento(float distancia)
        {
            _attackCooldown -= Time.deltaTime;
            
            if (distancia <= alcanceDeGolpe)
            {
                _enemyMovement.Parar();
                if (_attackCooldown <= 0)
                {
                    Atacar();
                }
            }
            else
            {
                _enemyMovement.MoverPara(_jogador.position, _enemyMovement.VelocidadeCaca);
            }
        }

        private void Atacar()
        {
            _attackCooldown = cadenciaDeAtaque;
            
            var alvo = _jogador.GetComponent<IDanificavel>();
            if (alvo != null)
            {
                alvo.ReceberGolpe(new ArmaResult(true, 0f, 0f, false, 0f, danoDoGolpe));
            }
        }

        private void HandleAbatido()
        {
            _lutaAtiva = false;
            if (trancaDaArena != null) trancaDaArena.Destrancar();
            DroparElmo();
        }

        private void DroparElmo()
        {
            if (prefabElmoDeSet != null)
            {
                Instantiate(prefabElmoDeSet, transform.position, Quaternion.identity);
                Debug.Log("[AvatarDeSetAI] Elmo de Set dropado!");
            }
            else
            {
                Debug.LogWarning("[AvatarDeSetAI] Boss derrotado, mas prefab do Elmo de Set não foi atribuído.");
            }
        }

        private void OnDestroy()
        {
            _enemyBase.OnAbatido -= HandleAbatido;
        }
    }
}
