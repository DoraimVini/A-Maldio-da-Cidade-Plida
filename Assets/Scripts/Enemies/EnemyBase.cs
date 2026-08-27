using UnityEngine;
using FavelaAmarela.Runtime.Progression;
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

        [Tooltip("Cor dos números de Trauma de Anomalia — a mente cedendo, não a carne. " +
                 "O amarelo pálido de Carcosa, para o jogador ler os dois canais de relance.")]
        [SerializeField] private Color corDoTraumaAnomalo = new Color(0.9f, 0.85f, 0.4f);

        private Vitalidade _vitalidade;
        private ResilienciaMental _resiliencia;
        private FichaDeAtributos _atributos;
        private ObjetoPersistente _persistencia;

        // Vitalidade e Resiliência podem zerar no mesmo golpe (uma arma que fere carne e
        // mente ao mesmo tempo). Sem esta trava, Abater() rodaria duas vezes: dois
        // OnAbatido para quem escuta e dois Destroy no mesmo objeto.
        private bool _jaAbatido;

        public event System.Action<float> OnDanoSofrido;

        /// <summary>Trauma de Anomalia já mitigado que esta unidade acabou de sofrer.</summary>
        public event System.Action<float> OnTraumaAnomaloSofrido;

        public event System.Action OnAbatido;
        public event System.Action<ArmaResult> OnGolpeRecebido;

        public Vitalidade Vitalidade => _vitalidade;

        /// <summary>
        /// Resiliência Mental desta unidade, ou <c>null</c> se a ficha tem
        /// <c>ResilienciaMax</c> = 0 — o caso comum. Só criaturas de Carcosa têm mente a ferir.
        /// </summary>
        public ResilienciaMental Resiliencia => _resiliencia;

        public FichaDeAtributos Atributos => _atributos;

        /// <summary>
        /// Abatida por qualquer um dos dois vetores: a carne cedeu (Vitalidade em zero) ou
        /// a mente se desfez (Resiliência Mental em Colapso).
        /// </summary>
        public bool EstaAbatido =>
            (_vitalidade != null && _vitalidade.EstaAbatido) ||
            (_resiliencia != null && _resiliencia.IsColapso);

        public bool EhAparicaoPrimordial => ehAparicaoPrimordial;
        public bool IgnorarDano { get; set; }

        /// <summary>
        /// Aplica um golpe de arma nos <b>dois canais</b> descritos em <see cref="FichaDeAtributos"/>:
        /// o dano físico é mitigado pela Defesa e fere a Vitalidade; o Trauma de Anomalia é
        /// mitigado pela Resistência Anômala e fere a Resiliência Mental. São vetores
        /// independentes — uma lâmina de Carcosa pode desfazer a mente de uma criatura muito
        /// antes de vencer a carne dela.
        /// </summary>
        public void ReceberGolpe(ArmaResult resultado)
        {
            if (IgnorarDano || EstaAbatido) return;

            OnGolpeRecebido?.Invoke(resultado);

            AplicarDanoFisico(resultado.Dano);

            // A carne cedeu neste mesmo golpe: não se fere a mente de um cadáver. Sem isto,
            // um golpe que mata pelos dois canais ainda emitiria OnTraumaAnomaloSofrido e um
            // número flutuante depois da morte (Destroy só efetiva no fim do frame).
            if (_jaAbatido) return;

            AplicarTraumaAnomalo(resultado.TraumaAnomalia);
        }

        private void AplicarDanoFisico(float bruto)
        {
            if (bruto <= 0f) return;

            float danoFinal = MitigacaoDeDano.Aplicar(bruto, _atributos.Defesa);
            if (danoFinal <= 0f) return;

            _vitalidade.Ferir(danoFinal);
            OnDanoSofrido?.Invoke(danoFinal);

            if (mostrarNumerosDeDano)
                DanoFlutuante.Mostrar(transform.position, danoFinal, corDoDano);
        }

        /// <summary>
        /// Fere a mente. Unidades sem Resiliência Mental na ficha (<c>ResilienciaMax</c> = 0)
        /// ignoram este canal de graça — é o que dispensa um <c>if</c> por tipo de inimigo
        /// espalhado pelo combate: quem não tem mente simplesmente não tem o objeto.
        /// </summary>
        private void AplicarTraumaAnomalo(float bruto)
        {
            if (bruto <= 0f || _resiliencia == null) return;

            float traumaFinal = MitigacaoDeDano.Aplicar(bruto, _atributos.ResistenciaAnomala);
            if (traumaFinal <= 0f) return;

            _resiliencia.SofrerTrauma(traumaFinal);
            OnTraumaAnomaloSofrido?.Invoke(traumaFinal);

            if (mostrarNumerosDeDano)
                DanoFlutuante.Mostrar(transform.position, traumaFinal, corDoTraumaAnomalo);
        }

        private void Awake()
        {
            // Área atingível derivada do sprite — todo inimigo que herda EnemyBase ganha hurtbox sem wiring nenhum.
            // A garantia vive aqui, no código, e não numa lista de prefabs: listas
            // escritas à mão são o modo de falha mais repetido deste projeto.
            FavelaAmarela.Runtime.Combat.Hurtbox.GarantirPara(gameObject, "EnemyHurtbox");

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

            // Só criaturas com mente autorada na ficha entram no canal anômalo. Threshold de
            // pânico em zero de propósito: pânico é estado do jogador (câmera, música, shader),
            // não de um inimigo — aqui a mente só interessa cheia ou desfeita.
            if (_atributos.ResilienciaMax > 0f)
            {
                _resiliencia = new ResilienciaMental(_atributos.ResilienciaMax, 0f);
                _resiliencia.OnChanged += HandleResilienciaChanged;
            }
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

        /// <summary>A mente se desfez — o segundo vetor de derrota, equivalente a abater.</summary>
        private void HandleResilienciaChanged(ResilienciaChangedArgs args)
        {
            if (args.EntrouEmColapso) Abater();
        }

        [Header("Progressão")]
        [Tooltip("Exposição concedida ao ser abatido. É o que faz o nível do jogador subir — " +
                 "e, por consequência, o que libera tiers de afixo no loot.")]
        [Min(0)]
        [SerializeField] private int exposicaoAoAbater = 1;

        private void Abater()
        {
            if (_jaAbatido) return;
            _jaAbatido = true;

            var chave = ChavesDeSave.ChaveDeAbatido(_persistencia?.Chave);
            if (chave != null) GerenciadorDeSave.MarcarAconteceu(chave);

            ConcederExposicao();

            OnAbatido?.Invoke();
            Destroy(gameObject);
        }

        /// <summary>
        /// Concede Exposição ao jogador.
        ///
        /// <para><b>Por que isto passou a existir (2026-08-27).</b>
        /// <c>ProgressionBridge.AdicionarExposicao</c> e <c>Progressao.AdicionarExposicao</c>
        /// existiam, estavam testados e <b>não eram chamados por nenhum código de gameplay</b>.
        /// O nível ficava travado em 1 para sempre — o que era aceitável enquanto o loot só
        /// entregava itens autorados (o <c>CLAUDE.md</c> registrava isso como esperado no
        /// Vertical Slice, não bug).</para>
        ///
        /// <para><b>Com afixos por nível do item, isso virou bloqueante:</b> o pool é filtrado
        /// por nível, então sem ninguém concedendo Exposição o gerador entregaria <i>sempre</i>
        /// o piso, e o sistema inteiro seria invisível em jogo.</para>
        ///
        /// <para>Mora aqui, e não num componente à parte, de propósito: <c>EnemyBase</c> é a
        /// raiz de todo inimigo comum, então inimigo novo concede Exposição <b>de graça</b>.
        /// Um componente separado seria mais uma lista de prefabs para manter à mão — o modo
        /// de falha que este repositório já catalogou nove vezes.</para>
        /// </summary>
        private void ConcederExposicao()
        {
            if (exposicaoAoAbater <= 0) return;

            // Ainda não há ProgressionBridge em cena nenhuma no arranque de algumas cenas de
            // teste; abater sem progressão não pode derrubar a partida.
            ProgressionBridge.Instancia?.AdicionarExposicao(exposicaoAoAbater);
        }

        private void OnDestroy()
        {
            if (_vitalidade != null) _vitalidade.OnChanged -= HandleVitalidadeChanged;
            if (_resiliencia != null) _resiliencia.OnChanged -= HandleResilienciaChanged;
        }
    }
}
