using UnityEngine;
using FavelaAmarela.Core.Abilities;
using FavelaAmarela.Core.Combat;
using FavelaAmarela.Runtime.Combat;

namespace FavelaAmarela.Runtime.Enemies
{
    /// <summary>
    /// Camada Runtime (MonoBehaviour). <b>Pedra de Poder</b> da arena do Abdul: sustenta o
    /// Escudo Mágico dele na Fase 1. Quebrá-la derruba o escudo por um tempo — é a única
    /// forma de causar dano naquela fase, e o que transforma a Fase 1 numa luta de arena
    /// (procurar e quebrar) em vez de bater no escudo.
    ///
    /// <para>Implementa <see cref="IDanificavel"/> para ser alvo das armas normalmente.
    /// Não é uma Aparição Primordial — é cenário destrutível.</para>
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    [AddComponentMenu("Favela Amarela/Enemies/Pedra de Poder")]
    public sealed class PedraDePoder : MonoBehaviour, IDanificavel
    {
        [Header("Resistência")]
        [Tooltip("Vitalidade da pedra — quanto dano ela aguenta antes de estilhaçar.")]
        [SerializeField] private float vitalidadeMax = 60f;

        [Tooltip("Defesa da pedra (mitiga cada golpe). Deixe 0 para quebrar com qualquer arma.")]
        [SerializeField] private float defesa = 0f;

        [Header("Vínculo")]
        [Tooltip("O Abdul cujo escudo esta pedra sustenta.")]
        [SerializeField] private AbdulAlhazredAI abdul;

        [Header("Feedback")]
        [Tooltip("Exibe números de dano flutuantes ao ser golpeada.")]
        [SerializeField] private bool mostrarNumerosDeDano = true;

        [Tooltip("Cor dos números de dano da pedra.")]
        [SerializeField] private Color corDoDano = new Color(0.8f, 0.8f, 0.85f);

        [Tooltip("Objeto/efeito ligado quando a pedra estilhaça (opcional). [ASSET]")]
        [SerializeField] private GameObject efeitoDeQuebra;

        private Vitalidade _vitalidade;

        /// <summary>
        /// Injeta o Abdul cujo escudo esta pedra sustenta. Chamado por quem a instancia em
        /// runtime (<c>AbdulAlhazredAI</c>, ao entrar na Fase 1 — as Pedras não ficam
        /// pré-plantadas na dungeon, nascem só quando a luta começa) — mesmo padrão de
        /// injeção (<c>.Bind()</c>) usado no resto do Runtime, em vez de arrastar a
        /// referência no Inspector de um prefab compartilhado.
        /// </summary>
        public void Bind(AbdulAlhazredAI abdulASustentar) => abdul = abdulASustentar;

        /// <summary>Cenário destrutível, não é boss — leva crítico furtivo normalmente.</summary>
        public bool EhAparicaoPrimordial => false;

        /// <summary>Se a pedra já foi estilhaçada.</summary>
        public bool Quebrada => _vitalidade != null && _vitalidade.EstaAbatido;

        private void Awake()
        {
            _vitalidade = new Vitalidade(vitalidadeMax > 0f ? vitalidadeMax : 60f);
        }

        /// <remarks>
        /// A validação do vínculo mora em <c>Start</c>, não em <c>Awake</c>, de propósito:
        /// <c>Instantiate</c> roda o <c>Awake</c> <b>sincronamente</b>, antes de quem
        /// instanciou conseguir chamar <see cref="Bind"/>. Validar em <c>Awake</c> acusaria
        /// toda pedra nascida em runtime de estar órfã — falso, e o log assustava num erro
        /// que não existia. <c>Start</c> roda no frame seguinte, já com a injeção feita, e
        /// ainda pega o caso real (uma pedra colocada à mão na cena sem vínculo).
        /// </remarks>
        private void Start()
        {
            if (abdul == null)
            {
                Debug.LogError($"[PedraDePoder] '{name}' não está vinculada a um Abdul — " +
                               "quebrá-la não derrubará escudo nenhum.", this);
            }
        }

        /// <inheritdoc />
        public void ReceberGolpe(ArmaResult resultado)
        {
            if (Quebrada) return;
            if (resultado.Dano <= 0f) return; // golpe desarmado não quebra pedra

            float danoFinal = MitigacaoDeDano.Aplicar(resultado.Dano, defesa);
            if (danoFinal <= 0f) return;

            _vitalidade.Ferir(danoFinal);

            if (mostrarNumerosDeDano)
                DanoFlutuante.Mostrar(transform.position, danoFinal, corDoDano);

            if (_vitalidade.EstaAbatido)
                Estilhacar();
        }

        private void Estilhacar()
        {
            // Avisa o Abdul: é isto que abre a janela de dano da Fase 1.
            if (abdul != null) abdul.QuebrarPedraDePoder();

            if (efeitoDeQuebra != null)
                Instantiate(efeitoDeQuebra, transform.position, Quaternion.identity);

            gameObject.SetActive(false);
        }
    }
}
