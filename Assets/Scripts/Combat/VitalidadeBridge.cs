using System;
using UnityEngine;
using FavelaAmarela.Inventario;
using FavelaAmarela.Core.Abilities;
using FavelaAmarela.Core.Combat;

using FavelaAmarela.Player;

namespace FavelaAmarela.Runtime.Combat
{
    /// <summary>
    /// Bridge da vitalidade corpórea de um ator da cena (Damião hoje; reutilizável na
    /// Aparição Primordial depois). Instancia a <see cref="Vitalidade"/> a partir da
    /// <see cref="FichaAtributosConfig"/> e é o <b>único ponto</b> onde o dano físico
    /// recebido passa pela mitigação por Defesa (<see cref="MitigacaoDeDano"/>).
    ///
    /// Implementa <see cref="IDanificavel"/> para poder ser alvo de golpes de arma
    /// (relevante quando inimigos armados/boss existirem). O golpe corpo-a-corpo do
    /// Cultista chega por <see cref="ReceberDanoFisico"/>.
    /// </summary>
    [AddComponentMenu("Favela Amarela/Vitalidade Bridge")]
    public sealed class VitalidadeBridge : MonoBehaviour, IDanificavel
    {
        [Header("Ficha de Atributos")]
        [Tooltip("Ficha da unidade (Vitalidade, Ataque, Defesa, Conjuração, Resistência Anômala).")]
        [SerializeField] private FichaAtributosConfig ficha;

        [Header("Identidade de combate")]
        [Tooltip("Marque se esta unidade é uma Aparição Primordial (Vulto/boss) — imune a crítico de furtividade.")]
        [SerializeField] private bool ehAparicaoPrimordial = false;

        [Header("Feedback")]
        [Tooltip("Exibe números de dano flutuantes ao sofrer dano (diagnóstico visual enquanto não há animações).")]
        [SerializeField] private bool mostrarNumerosDeDano = true;

        [Tooltip("Cor dos números de dano sofridos por esta unidade.")]
        [SerializeField] private Color corDoDano = new Color(1f, 0.35f, 0.35f);

        private FichaDeAtributos _atributosBase;
        private FichaDeAtributos _atributosFinais;
        private Vitalidade _vitalidade;

        /// <summary>Atributos finais desta unidade (podem vir de equipamentos ou diretos da ficha base).</summary>
        public FichaDeAtributos Atributos
        {
            get { GarantirInicializacao(); return _atributosFinais; }
        }

        /// <summary>Vitalidade corpórea corrente desta unidade. Nunca null.</summary>
        public Vitalidade Vitalidade
        {
            get { GarantirInicializacao(); return _vitalidade; }
        }

        /// <summary>Dano final (já mitigado) sofrido neste golpe. Para HUD, câmera e áudio.</summary>
        public event Action<float> OnDanoSofrido;

        /// <summary>Disparado no instante em que esta unidade é abatida (Vitalidade zerada).</summary>
        public event Action OnAbatido;

        /// <inheritdoc />
        public bool EhAparicaoPrimordial => ehAparicaoPrimordial;

        /// <summary>
        /// Quando true, todo dano recebido é descartado. Usado pelo <c>GameManager</c>
        /// durante sequências roteirizadas (ver <c>GameManager.JogadorInvulneravel</c>) —
        /// Damião não pode morrer de porrada no meio de uma cutscene.
        /// </summary>
        public bool IgnorarDano { get; set; }

        private void Awake()
        {
            GarantirInicializacao();

            // O Damião também precisa de área atingível própria: o colisor da raiz é a
            // pegada no chão (0,60 × 0,30), estreita demais para representar o corpo.
            Hurtbox.GarantirPara(gameObject, "PlayerHurtbox");
        }

        /// <summary>
        /// Nível de Exposição de Damião, ou 1 quando a progressão ainda não existe em cena.
        ///
        /// <para>É lido <b>uma vez</b>, na criação da ficha. Subir de nível no meio da partida
        /// não recalcula a ficha ainda — fazer isso exige decidir o que acontece com a
        /// Vitalidade corrente (cura junto? mantém a fração?), e essa é decisão de design, não
        /// detalhe de implementação. Fica registrado aqui em vez de virar surpresa depois.</para>
        /// </summary>
        private static int NivelDoAtor =>
            FavelaAmarela.Runtime.Progression.ProgressionBridge.Instancia?.NivelAtual ?? 1;

        /// <summary>
        /// Cria a ficha e a <see cref="Vitalidade"/> uma única vez, sob demanda.
        /// </summary>
        private void GarantirInicializacao()
        {
            if (_vitalidade != null) return;

            if (ficha != null)
                // O NÍVEL entra aqui: é o que faz "no nível 2 o Damião é mais forte e mais
                // defendido" (pedido do Vini, 2026-08-28). Sem ProgressionBridge em cena o
                // nível é 1, que devolve exatamente o valor autorado -- degrada para o
                // comportamento antigo em vez de zerar a ficha.
                _atributosBase = ficha.CriarFicha(NivelDoAtor);
            else
            {
                Debug.LogError($"[VitalidadeBridge] Nenhuma ficha encontrada em '{name}'. Usando base.", this);
                _atributosBase = new FichaDeAtributos(vitalidadeMax: 100f, ataque: 0f, defesa: 0f);
            }

            _atributosFinais = _atributosBase;

            _vitalidade = new Vitalidade(_atributosFinais.VitalidadeMax);
            _vitalidade.OnChanged += HandleVitalidadeChanged;
        }

        private void Start()
        {
            if (gameObject.CompareTag("Player"))
            {
                var efeitos = FavelaAmarela.Player.GerenciadorEfeitosPassivos.Instance;
                if (efeitos != null)
                {
                    efeitos.OnBonusChanged += AtualizarAtributosDeEquipamento;
                    AtualizarAtributosDeEquipamento(); // Força a primeira leitura
                }
                else
                {
                    Debug.LogWarning("[VitalidadeBridge] Jogador sem GerenciadorEfeitosPassivos na cena. Os status base serão mantidos.");
                }

                // Efeitos imediatos de curas
                var invManager = FavelaAmarela.Inventario.InventoryManager.Instance;
                if (invManager != null)
                {
                    invManager.OnItemConsumed += AplicarEfeitoConsumivel;
                }
            }
        }

        private void AplicarEfeitoConsumivel(FavelaAmarela.Inventario.ItemDef item, int indice)
        {
            if (item.Modificadores == null) return;

            foreach (var mod in item.Modificadores)
            {
                // Cura a vitalidade atual
                if (mod.Stat == FavelaAmarela.Inventario.StatType.VitMaxima && _vitalidade != null)
                {
                    _vitalidade.Curar(mod.Valor);
                }
                
                // Cura a Resiliencia Mental
                if (mod.Stat == FavelaAmarela.Inventario.StatType.RMMaxima)
                {
                    // Componente irmão: a ResilienciaBridge vive no mesmo Damião.
                    var mente = GetComponent<ResilienciaBridge>();
                    if (mente != null)
                    {
                        mente.Ancorar(mod.Valor);
                    }
                }
            }
        }

        private void AtualizarAtributosDeEquipamento()
        {
            float bonusVit = 0f;
            float bonusDefesa = 0f;

            var efeitos = FavelaAmarela.Player.GerenciadorEfeitosPassivos.Instance;
            if (efeitos != null)
            {
                bonusVit = efeitos.GetBonus(FavelaAmarela.Inventario.StatType.VitMaxima);
                bonusDefesa = efeitos.GetBonus(FavelaAmarela.Inventario.StatType.DefesaFisica);
            }

            // ComBonus preserva os campos que não recebem bônus. Chamar o construtor aqui
            // passava 3 dos 10 parâmetros e zerava ResistenciaAnomala e ResilienciaMax a cada
            // troca de equipamento — o defensor perdia a mitigação anômala em silêncio.
            _atributosFinais = _atributosBase.ComBonus(
                bonusVitalidade: bonusVit,
                bonusDefesa: bonusDefesa);

            // Ajusta o max da vitalidade em tempo real mantendo a % de vida
            if (_vitalidade != null)
                _vitalidade.SetValorMaximo(_atributosFinais.VitalidadeMax);
        }

        private void OnDestroy()
        {
            if (_vitalidade != null)
                _vitalidade.OnChanged -= HandleVitalidadeChanged;

            if (gameObject.CompareTag("Player"))
            {
                var efeitos = FavelaAmarela.Player.GerenciadorEfeitosPassivos.Instance;
                if (efeitos != null) efeitos.OnBonusChanged -= AtualizarAtributosDeEquipamento;

                var invManager = FavelaAmarela.Inventario.InventoryManager.Instance;
                if (invManager != null) invManager.OnItemConsumed -= AplicarEfeitoConsumivel;
            }
        }

        /// <summary>
        /// Aplica um golpe físico bruto a esta unidade: mitiga pela Defesa da ficha e
        /// fere a Vitalidade. É por aqui que entra o golpe corpo-a-corpo do Cultista.
        /// </summary>
        /// <param name="danoBruto">Dano antes da defesa (ex.: <c>Ataque</c> da ficha do agressor).</param>
        /// <summary>
        /// Sorte do bloqueio. Injetável na prática — trocá-la é como se testa o escudo sem
        /// depender de aleatoriedade real.
        /// </summary>
        private readonly FavelaAmarela.Core.Loot.IFonteDeAleatoriedade _sorteDoBloqueio =
            new FavelaAmarela.Runtime.Itens.FonteDeAleatoriedadeUnity();

        /// <summary>
        /// Disparado quando a Mão Secundária apara um golpe. Existe para o áudio e a UI terem
        /// onde se pendurar — um bloqueio que não é visto nem ouvido é indistinguível de sorte.
        /// </summary>
        public event System.Action OnGolpeAparado;

        public void ReceberDanoFisico(float danoBruto)
        {
            if (IgnorarDano) return;

            GarantirInicializacao(); // dano nunca some por causa de ordem de Awake
            if (_vitalidade.EstaAbatido) return;

            // O escudo age ANTES da Defesa: ele apara o golpe, e o que passa é que precisa
            // ser mitigado. Na ordem inversa, um escudo forte contra um golpe fraco daria
            // números negativos que a mitigação teria de tratar.
            var bloqueio = Bloqueio.Tentar(danoBruto,
                                           MaoSecundaria.ChanceDeBloqueio(),
                                           MaoSecundaria.ReducaoAoBloquear(),
                                           _sorteDoBloqueio);

            float danoFinal = MitigacaoDeDano.Aplicar(bloqueio.DanoFinal, _atributosFinais.Defesa);

            if (bloqueio.Bloqueou) OnGolpeAparado?.Invoke();

            if (danoFinal <= 0f) return;

            _vitalidade.Ferir(danoFinal);
            OnDanoSofrido?.Invoke(danoFinal);

            if (mostrarNumerosDeDano)
                DanoFlutuante.Mostrar(transform.position, danoFinal, corDoDano);
        }

        /// <inheritdoc />
        public void ReceberGolpe(ArmaResult resultado)
        {
            // Golpe de arma contra esta unidade: só o canal de dano físico por ora.
            // (Sangramento/repulsão/interrupção são efeitos de alvo específico — o boss
            // os tratará ao ser construído.)
            if (resultado.Dano > 0f)
                ReceberDanoFisico(resultado.Dano);
        }

        private void HandleVitalidadeChanged(VitalidadeChangedArgs args)
        {
            if (args.AcabouDeAbater)
                OnAbatido?.Invoke();
        }
    }
}
