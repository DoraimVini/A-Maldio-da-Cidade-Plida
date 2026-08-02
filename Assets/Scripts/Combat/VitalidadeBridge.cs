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

        private FichaDeAtributos _atributos;
        private Vitalidade _vitalidade;
        private EquipamentosBridge _equipamentosBridge;

        /// <summary>Atributos finais desta unidade (podem vir de equipamentos ou diretos da ficha base).</summary>
        public FichaDeAtributos Atributos
        {
            get { GarantirInicializacao(); return _atributos; }
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
        ///
        /// <para><b>Por que não só no <c>Awake</c>:</b> a Unity não garante ordem de
        /// <c>Awake</c> entre GameObjects diferentes. O <c>GameManager</c> lê
        /// <see cref="Vitalidade"/> no <b>bootstrap</b> dele para injetar no HUD; se o
        /// <c>Awake</c> desta bridge ainda não tivesse rodado, ele recebia <c>null</c> e a
        /// barra de Vitalidade nunca era ligada — dano acontecendo, barra parada. Foi
        /// exatamente esse o bug de playtest de 2026-07-31. Inicializar sob demanda remove a
        /// dependência de ordem para <b>todos</b> os consumidores, não só o HUD.</para>
        /// </summary>
        private void GarantirInicializacao()
        {
            if (_vitalidade != null) return;

            // Busca equipamentos bridge primeiro (Damião). Se não tiver, usa a ficha direta (Inimigos).
            _equipamentosBridge = GetComponent<EquipamentosBridge>();

            if (_equipamentosBridge != null)
            {
                _atributos = _equipamentosBridge.FichaFinal;
                _equipamentosBridge.OnAtributosMudaram += AtualizarAtributosDeEquipamento;
            }
            else if (ficha != null)
            {
                _atributos = ficha.CriarFicha();
            }
            else
            {
                Debug.LogError($"[VitalidadeBridge] Nenhuma ficha ou equipamento encontrado em '{name}'. " +
                               "Usando ficha de emergência.", this);
                _atributos = new FichaDeAtributos(vitalidadeMax: 100f, ataque: 0f, defesa: 0f);
            }

            _vitalidade = new Vitalidade(_atributos.VitalidadeMax);
            _vitalidade.OnChanged += HandleVitalidadeChanged;
        }

        private void AtualizarAtributosDeEquipamento()
        {
            _atributos = _equipamentosBridge.FichaFinal;
            // Se o equipamento alterou a VitalidadeMax, o objeto Vitalidade precisa refletir.
            // Por enquanto, Vitalidade não tem suporte a alterar o Max depois de criada, mas fica a ponte feita.
        }

        private void OnDestroy()
        {
            if (_vitalidade != null)
                _vitalidade.OnChanged -= HandleVitalidadeChanged;

            if (_equipamentosBridge != null)
                _equipamentosBridge.OnAtributosMudaram -= AtualizarAtributosDeEquipamento;
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

            float danoFinal = MitigacaoDeDano.Aplicar(danoBruto, _atributos.Defesa);
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
