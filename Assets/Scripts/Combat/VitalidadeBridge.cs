using System;
using UnityEngine;
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

        private void Awake() => GarantirInicializacao();

        /// <summary>
        /// Cria a ficha e a <see cref="Vitalidade"/> uma única vez, sob demanda.
        /// </summary>
        private void GarantirInicializacao()
        {
            if (_vitalidade != null) return;

            if (ficha != null)
                _atributosBase = ficha.CriarFicha();
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
                    var resiliencia = FavelaAmarela.Runtime.GameLoop.GameManager.Instance?.Resiliencia;
                    if (resiliencia != null)
                    {
                        resiliencia.Ancorar(mod.Valor);
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

            _atributosFinais = new FichaDeAtributos(
                vitalidadeMax: _atributosBase.VitalidadeMax + bonusVit,
                ataque: _atributosBase.Ataque, 
                defesa: _atributosBase.Defesa + bonusDefesa
            );

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
        public void ReceberDanoFisico(float danoBruto)
        {
            if (IgnorarDano) return;

            GarantirInicializacao(); // dano nunca some por causa de ordem de Awake
            if (_vitalidade.EstaAbatido) return;

            float danoFinal = MitigacaoDeDano.Aplicar(danoBruto, _atributosFinais.Defesa);
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
