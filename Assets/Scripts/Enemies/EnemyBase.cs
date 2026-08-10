using UnityEngine;
using FavelaAmarela.Core.Combat;
using FavelaAmarela.Core.Abilities;
using FavelaAmarela.Core.Persistencia;
using FavelaAmarela.Runtime.Persistencia;
using FavelaAmarela.Runtime.Combat;

namespace FavelaAmarela.Runtime.Enemies
{
    [RequireComponent(typeof(Rigidbody2D), typeof(SpriteRenderer))]
    public class EnemyBase : MonoBehaviour, IDanificavel
    {
        [Header("Vitalidade")]
        [SerializeField] private FichaAtributosConfig ficha;
        [SerializeField] private bool ehAparicaoPrimordial = false;

        [Header("Feedback")]
        [SerializeField] private bool mostrarNumerosDeDano = true;
        [SerializeField] private Color corDoDano = new Color(1f, 0.35f, 0.35f);

        private Vitalidade _vitalidade;
        private FichaDeAtributos _atributos;
        private ObjetoPersistente _persistencia;

        public event System.Action<float> OnDanoSofrido;
        public event System.Action OnAbatido;
        public event System.Action<ArmaResult> OnGolpeRecebido;

        public Vitalidade Vitalidade => _vitalidade;
        public FichaDeAtributos Atributos => _atributos;
        public bool EstaAbatido => _vitalidade != null && _vitalidade.EstaAbatido;
        public bool EhAparicaoPrimordial => ehAparicaoPrimordial;
        public bool IgnorarDano { get; set; }

        public void ReceberGolpe(ArmaResult resultado)
        {
            if (IgnorarDano || EstaAbatido) return;

            OnGolpeRecebido?.Invoke(resultado);

            if (resultado.Dano > 0f)
            {
                float danoFinal = MitigacaoDeDano.Aplicar(resultado.Dano, _atributos.Defesa);
                if (danoFinal > 0f)
                {
                    _vitalidade.Ferir(danoFinal);
                    OnDanoSofrido?.Invoke(danoFinal);
                    if (mostrarNumerosDeDano)
                        DanoFlutuante.Mostrar(transform.position, danoFinal, corDoDano);
                }
            }
        }

        private void Awake()
        {
            _persistencia = GetComponent<ObjetoPersistente>();

            if (ficha == null)
            {
                Debug.LogError($"[EnemyBase] Ficha de atributos não atribuída em '{name}'.", this);
                _atributos = new FichaDeAtributos(vitalidadeMax: 100f, ataque: 10f, defesa: 5f);
            }
            else
            {
                _atributos = ficha.CriarFicha();
            }

            _vitalidade = new Vitalidade(_atributos.VitalidadeMax);
            _vitalidade.OnChanged += HandleVitalidadeChanged;
        }

        private void Start()
        {
            if (_persistencia == null) return;
            var chave = ChavesDeSave.ChaveDeAbatido(_persistencia.Chave);
            if (chave != null && GerenciadorDeSave.JaAconteceu(chave))
                Destroy(gameObject);
        }

        private void HandleVitalidadeChanged(VitalidadeChangedArgs args)
        {
            if (args.AcabouDeAbater) Abater();
        }

        private void Abater()
        {
            var chave = ChavesDeSave.ChaveDeAbatido(_persistencia?.Chave);
            if (chave != null) GerenciadorDeSave.MarcarAconteceu(chave);
            OnAbatido?.Invoke();
            Destroy(gameObject);
        }

        private void OnDestroy()
        {
            if (_vitalidade != null) _vitalidade.OnChanged -= HandleVitalidadeChanged;
        }
    }
}
