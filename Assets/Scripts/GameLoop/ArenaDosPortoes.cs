using UnityEngine;
using FavelaAmarela.Runtime.Enemies;

namespace FavelaAmarela.Runtime.GameLoop
{
    /// <summary>
    /// Camada Runtime. O <b>gatilho da arena dos Portões das Ruínas</b>: desperta o Byakhee
    /// quando Damião entra, e destranca os Portões quando ele cai.
    ///
    /// <para><b>Por que este componente existe:</b> o próprio <c>ByakheeAI.HandleDerrotado</c>
    /// diz de quem é a responsabilidade — "os Portões abrindo são responsabilidade do gatilho da
    /// arena". O adaptador do chefe cuida do corpo dele; quem move o mundo em volta é isto. Sem
    /// esta peça, <c>ByakheeAI.IniciarLuta()</c> só era chamado pelo Carcosa Debugger e a luta
    /// não existia em jogo.</para>
    ///
    /// <para><b>O abate também acende o Poste de Luz da arena.</b> Isso resolve a dependência
    /// que o GDD punha em Yug-Neth como "chave dimensional dos Portões" — em vez de um bloqueio
    /// a mais, o fim da luta entrega um Refúgio: ancora a Resiliência, cura, <b>grava a
    /// partida</b> e <b>reanima o companheiro</b> se ele estiver incapacitado
    /// (<c>RefugioDeLuz.ReanimarCompanheiro</c>). Chegar ao Castelo com o Yug-Neth de pé passa a
    /// ser consequência de vencer, não um pré-requisito escondido. Decisão do Vini, 2026-08-20.</para>
    ///
    /// <para><b>Abater destranca; quem abre é o jogador.</b> Este gatilho não escancara os
    /// Portões — ele chama <see cref="PortaoDosPortoes.Destrancar"/>, e a abertura vira uma
    /// interação deliberada no portão. Assim a transição de fase não cai por cima da morte do
    /// chefe, e o gesto final é do jogador.</para>
    ///
    /// <para><b>A luta começa por gatilho, não no <c>Start</c></b>: o dreno do grito
    /// infrassônico é passivo — 2 RM/s enquanto o Byakhee viver, sem precisar acertar ninguém
    /// (ver <c>systems/boss_byakhee.md</c>). Se a luta começasse ao carregar a cena, o relógio
    /// já estaria correndo enquanto o jogador lê a caixa de texto ou toca o Refúgio, e ele
    /// perderia Resiliência sem ter escolhido entrar.</para>
    /// </summary>
    [AddComponentMenu("Favela Amarela/Level/Arena dos Portões")]
    public sealed class ArenaDosPortoes : MonoBehaviour
    {
        [Header("O guardião")]
        [Tooltip("O Byakhee que tranca os Portões.")]
        [SerializeField] private ByakheeAI chefe;

        [Header("Os Portões")]
        [Tooltip("Os Portões das Ruínas. Destrancados — não abertos — ao abater o chefe.")]
        [SerializeField] private PortaoDosPortoes portao;

        [Header("Poste de Luz")]
        [Tooltip("O Refúgio que acende com o guardião abatido. Apagado até lá.")]
        [SerializeField] private RefugioDeLuz refugio;

        [Tooltip("A luz do poste. Escura enquanto o Byakhee vive.")]
        [SerializeField] private SpriteRenderer luzDoPoste;

        [Tooltip("Cor do poste apagado.")]
        [SerializeField] private Color corApagada = new Color(0.22f, 0.21f, 0.20f);

        [Tooltip("Cor do poste aceso.")]
        [SerializeField] private Color corAcesa = new Color(0.95f, 0.88f, 0.55f);

        [Header("Saida")]
        [Tooltip("Portal de volta ao Deserto. Desligado enquanto a luta corre.")]
        [SerializeField] private GameObject voltaAoDeserto;

        [Header("Leitura")]
        [Tooltip("Tag do que dispara a luta ao entrar.")]
        [SerializeField] private string tagDoJogador = "Player";

        private EnemyBase _corpoDoChefe;
        private bool _lutaComecou;

        /// <summary>Se a luta já foi despertada.</summary>
        public bool LutaComecou => _lutaComecou;

        private void Awake()
        {
            if (chefe == null)
            {
                Debug.LogError("[ArenaDosPortoes] Sem Byakhee ligado — a luta nunca começaria " +
                               "e os Portões nunca abririam.", this);
            }
            else
            {
                // O abate vem do EnemyBase, não da FSM: a FSM decide o que "derrotado"
                // significa, mas quem avisa que a vida acabou é o EnemyBase — é nele que o
                // DropAoAbater também escuta, então o espólio e a abertura saem do mesmo evento.
                _corpoDoChefe = chefe.GetComponent<EnemyBase>();

                if (_corpoDoChefe == null)
                    Debug.LogError("[ArenaDosPortoes] O Byakhee não tem EnemyBase — sem ele não " +
                                   "há evento de abate para escutar.", this);
                else
                    _corpoDoChefe.OnAbatido += HandleChefeAbatido;
            }

            if (portao == null)
                Debug.LogError("[ArenaDosPortoes] Sem os Portões ligados — abater o Byakhee não " +
                               "destrancaria nada e a Fase 1 não teria saída.", this);

            ApagarOPoste();
        }

        /// <summary>
        /// Deixa o Refúgio inerte no começo. Desligar o <b>componente</b> e o <b>colisor</b>, e
        /// não o GameObject inteiro: um poste que surge do nada ao fim da luta lê como bug, um
        /// poste apagado que acende lê como recompensa. E o objeto vivo desde o início mantém o
        /// <c>PontoDeChegada</c> irmão registrável.
        /// </summary>
        private void ApagarOPoste()
        {
            if (refugio == null)
            {
                Debug.LogWarning("[ArenaDosPortoes] Sem Poste de Luz ligado — vencer o Byakhee " +
                                 "não daria onde descansar, gravar nem reanimar o companheiro.", this);
                return;
            }

            refugio.enabled = false;

            var col = refugio.GetComponent<Collider2D>();
            if (col != null) col.enabled = false;

            if (luzDoPoste != null) luzDoPoste.color = corApagada;
        }

        /// <summary>Acende o Poste. Público para o Carcosa Debugger poder testar sem a luta.</summary>
        public void AcenderOPoste()
        {
            if (refugio == null) return;

            refugio.enabled = true;

            var col = refugio.GetComponent<Collider2D>();
            if (col != null) col.enabled = true;

            if (luzDoPoste != null) luzDoPoste.color = corAcesa;

            Debug.Log("[ArenaDosPortoes] O Poste de Luz dos Portões acendeu.", this);
        }

        // Método nomeado, não lambda: '-=' com um lambda diferente do usado no '+=' nunca
        // desassina, e esse bug já existe em outro ponto do projeto.
        private void OnDestroy()
        {
            if (_corpoDoChefe != null) _corpoDoChefe.OnAbatido -= HandleChefeAbatido;
        }

        private void OnTriggerEnter2D(Collider2D outro)
        {
            if (_lutaComecou || chefe == null) return;
            if (!outro.CompareTag(tagDoJogador)) return;

            _lutaComecou = true;

            // Tranca a saída. Dava para atravessar o portal de volta no meio da luta, o que
            // deixava o Byakhee vivo numa cena descarregada — e ao voltar a arena remontava
            // com o chefe inteiro, mas o gatilho já disparado. Luta de chefe se termina ou se
            // perde; não se abandona pela porta.
            if (voltaAoDeserto != null) voltaAoDeserto.SetActive(false);

            chefe.IniciarLuta();
        }

        private void HandleChefeAbatido()
        {
            if (portao != null) portao.Destrancar();
            AcenderOPoste();

            // A saída volta com o chefe caído: o jogador pode querer refazer o caminho antes
            // de cruzar os Portões, e nada mais depende de ele ficar preso aqui.
            if (voltaAoDeserto != null) voltaAoDeserto.SetActive(true);
        }
    }
}
