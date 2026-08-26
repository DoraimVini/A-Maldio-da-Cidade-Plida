using UnityEngine;
using FavelaAmarela.Core.Abilities;
using FavelaAmarela.Core.Combat;
using FavelaAmarela.Core.Persistencia;
using FavelaAmarela.Runtime.Interaction;
using FavelaAmarela.Runtime.Persistencia;
using FavelaAmarela.Runtime.UI;
using FavelaAmarela.Runtime.Combat;

namespace FavelaAmarela.Runtime.Enemies
{
    [RequireComponent(typeof(EnemyBase), typeof(SpriteRenderer), typeof(Rigidbody2D))]
    [AddComponentMenu("Favela Amarela/Enemies/Nagaraja (Sacerdote)")]
    public class NagarajaAI : MonoBehaviour, IInteragivel
    {
        [Header("Diálogo")]
        [SerializeField] private TutorialHintUI caixaDeTexto;
        
        [TextArea(2, 4)]
        [SerializeField] private string falaEmAklo = "[Aklo Serpentino incompreensível]";
        
        [TextArea(2, 4)]
        [SerializeField] private string falaTraduzida = "\"Ssseth-kaa... o tradutor. Alhazred nos mencionou em seu tomo podre. Você carrega o cheiro de Hali, viajante. Set-ur-haal... e você anda pelo nosso Templo como se a senha fosse o medo.\"";
        
        [TextArea(2, 4)]
        [SerializeField] private string pensamentoDamiao = "Não entendo as palavras. Mas entendo os dentes.";
        
        [Header("Combate")]
        [SerializeField] private float cadenciaDeAtaque = 1.0f;
        [SerializeField] private float danoDoGolpe = 35f;

        private enum EstadoDaLuta { Inativo, FalandoNagaraja, PensamentoDamiao, EmLuta, Morto }
        private EstadoDaLuta _estadoAtual = EstadoDaLuta.Inativo;
        
        private EnemyBase _enemyBase;
        private Rigidbody2D _rb;
        private Transform _jogador;
        private float _attackCooldown;
        private float _velocidadeCaca;
        private float _alcanceDeGolpe;

        private void Awake()
        {
            // A caixa de diálogo vive no prefab persistente do HUD desde 2026-08-22.
            // O campo do Inspector continua valendo para quem quiser uma própria;
            // vazio, cai para a global — senão esta referência viraria nula ao
            // migrar a caixa para fora da cena.
            if (caixaDeTexto == null) caixaDeTexto = FavelaAmarela.Runtime.UI.TutorialHintUI.Instancia;

            _enemyBase = GetComponent<EnemyBase>();
            _rb = GetComponent<Rigidbody2D>();

            if (_enemyBase.Atributos != null)
            {
                _velocidadeCaca = _enemyBase.Atributos.VelocidadeCaca;
                _alcanceDeGolpe = _enemyBase.Atributos.AlcanceDeGolpe;
            }
            else
            {
                _velocidadeCaca = 4f;
                _alcanceDeGolpe = 1.5f;
            }
        }

        private void Start()
        {
            _enemyBase.OnAbatido += HandleAbatido;
        }

        private void Update()
        {
            if (_estadoAtual == EstadoDaLuta.EmLuta && _jogador != null)
            {
                ExecutarRotinaDeLuta();
            }
        }

        // --- IInteragivel ---
        public string RotuloDeInteracao => "Aproximar-se do Sacerdote";
        public bool PodeInteragir => _estadoAtual == EstadoDaLuta.Inativo;
        public int PrioridadeDeInteracao => 10;
        public Vector2 PosicaoDeInteracao => transform.position;

        public void Interagir(GameObject quemInterage)
        {
            if (_estadoAtual != EstadoDaLuta.Inativo) return;
            
            _jogador = quemInterage.transform;
            _estadoAtual = EstadoDaLuta.FalandoNagaraja;
            
            bool possuiNecronomicon = GerenciadorDeSave.JaAconteceu(ChavesDeSave.NecronomiconColetado);
            string fala = possuiNecronomicon ? falaTraduzida : falaEmAklo;
            
            if (caixaDeTexto != null)
            {
                caixaDeTexto.Mostrar(fala, 5f);
                Invoke(nameof(MostrarPensamentoDamiao), 5.5f);
            }
            else
            {
                MostrarPensamentoDamiao();
            }
        }
        
        private void MostrarPensamentoDamiao()
        {
            _estadoAtual = EstadoDaLuta.PensamentoDamiao;
            bool possuiNecronomicon = GerenciadorDeSave.JaAconteceu(ChavesDeSave.NecronomiconColetado);
            
            if (!possuiNecronomicon && caixaDeTexto != null)
            {
                caixaDeTexto.Mostrar(pensamentoDamiao, 3f);
                Invoke(nameof(IniciarLuta), 3.5f);
            }
            else
            {
                IniciarLuta();
            }
        }

        private void IniciarLuta()
        {
            _estadoAtual = EstadoDaLuta.EmLuta;
            Debug.Log("[NagarajaAI] O Sacerdote parte para o ataque.");
        }

        private void ExecutarRotinaDeLuta()
        {
            _attackCooldown -= Time.deltaTime;
            float distancia = Vector2.Distance(transform.position, _jogador.position);
            
            if (distancia <= _alcanceDeGolpe)
            {
                _rb.linearVelocity = Vector2.zero;
                if (_attackCooldown <= 0)
                {
                    Atacar();
                }
            }
            else
            {
                Vector2 direcao = (_jogador.position - transform.position).normalized;
                _rb.linearVelocity = direcao * _velocidadeCaca;
                GetComponent<SpriteRenderer>().flipX = direcao.x < 0;
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
            _estadoAtual = EstadoDaLuta.Morto;
            _enemyBase.OnAbatido -= HandleAbatido;
            Destroy(gameObject);
        }
    }
}
