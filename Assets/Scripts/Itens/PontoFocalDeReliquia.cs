using UnityEngine;
using FavelaAmarela.Core.Enemies;

namespace FavelaAmarela.Runtime.Itens
{
    /// <summary>
    /// Camada Runtime (MonoBehaviour). Um dos três altares de relíquia do Trono de Aldebaran — no
    /// Rito do Olhar, o lugar onde <b>arde um fragmento da Peça</b> (Fase 3).
    ///
    /// <para><b>O que mudou (2026-09-28).</b> Na luta antiga este era o ponto focal onde se
    /// ativava a relíquia (botão E), e ele erguia o escudo do ciclo. Com as relíquias viradas
    /// modificadores do rito — o Rei lê o que o Damião traz equipado —, não há mais nada para
    /// ativar: o altar só <b>mostra</b> o fragmento e responde se o Damião está em cima dele.
    /// Quem decide onde o fragmento arde é a <c>FragmentosDaPeca</c> do Core; aqui só se
    /// acende o feixe.</para>
    ///
    /// <para>O nome da classe ficou o da luta antiga de propósito: a cena guarda o componente
    /// pelo tipo, e renomear o arquivo trocaria o script de três objetos que já estão no lugar
    /// certo por uma referência quebrada.</para>
    /// </summary>
    [AddComponentMenu("Favela Amarela/Itens/Ponto Focal de Relíquia")]
    public sealed class PontoFocalDeReliquia : MonoBehaviour
    {
        [Header("Relíquia")]
        [Tooltip("Id do ItemDef de Artefato deste altar (ex.: 'necronomicon'). No Rito do Olhar é " +
                 "só identidade — a ordem dos fragmentos e os guardas usam.")]
        [SerializeField] private string artefatoId;

        [Header("Leitura do fragmento")]
        [Tooltip("Meia-largura da área onde o Damião 'está em cima' do fragmento. Menor que o " +
                 "Altar de Selamento: o fragmento é uma página, não uma sala.")]
        [Min(0.1f)]
        [SerializeField] private float semiEixoX = 1.2f;

        [Tooltip("Meia-altura da mesma área (metade da largura: o achatamento isométrico).")]
        [Min(0.1f)]
        [SerializeField] private float semiEixoY = 0.6f;

        [Header("Visual")]
        [Tooltip("Sprite do altar; o feixe o assume enquanto o fragmento arde. [ASSET pixel art]")]
        [SerializeField] private SpriteRenderer spriteDoPonto;

        [SerializeField] private Sprite spriteInativo;

        /// <summary>O id do artefato deste altar.</summary>
        public string ArtefatoId => artefatoId;

        /// <summary>Meia-largura da área de leitura, para guardas de geometria.</summary>
        public float SemiEixoX => semiEixoX;

        /// <summary>Meia-altura da área de leitura, para guardas de geometria.</summary>
        public float SemiEixoY => semiEixoY;

        /// <summary>Se o fragmento arde aqui agora.</summary>
        public bool ComFragmento { get; private set; }

        /// <summary>
        /// Feixe do altar. Resolvido por <c>GetComponent</c>, não por campo: ligar o feixe é
        /// acrescentar o componente, sem uma referência a mais para alguém esquecer de arrastar.
        /// </summary>
        private AnimadorDeAltarDeReliquia _feixe;

        private void Awake()
        {
            _feixe = GetComponent<AnimadorDeAltarDeReliquia>();
            if (spriteDoPonto == null) spriteDoPonto = GetComponent<SpriteRenderer>();

            if (string.IsNullOrWhiteSpace(artefatoId))
                Debug.LogError("[PontoFocalDeReliquia] Sem artefatoId configurado.", this);
        }

        private void Start() => MostrarFragmento(false);

        /// <summary>Se uma posição de mundo está sobre o fragmento deste altar.</summary>
        public bool Sob(Vector2 posicao)
            => AbrigoDeReliquia.EstaAbrigado(posicao, transform.position, semiEixoX, semiEixoY);

        /// <summary>Acende ou apaga o fragmento neste altar.</summary>
        public void MostrarFragmento(bool arde)
        {
            ComFragmento = arde;

            if (arde)
            {
                if (_feixe != null && spriteDoPonto != null) _feixe.Acender(spriteDoPonto);
                return;
            }

            if (_feixe != null) _feixe.Apagar();
            if (spriteDoPonto != null && spriteInativo != null) spriteDoPonto.sprite = spriteInativo;
        }
    }
}
