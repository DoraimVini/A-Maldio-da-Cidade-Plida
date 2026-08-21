using UnityEngine;
using UnityEngine.SceneManagement;
using FavelaAmarela.Core.Persistencia;
using FavelaAmarela.Runtime.Persistencia;
using FavelaAmarela.Runtime.UI;

namespace FavelaAmarela.Runtime.GameLoop
{
    /// <summary>
    /// Porta de transição entre cenas: ao contato do Player com este volume de
    /// trigger, carrega a cena <see cref="cenaDestino"/> (ex.: a entrada da Tumba
    /// de Alhazred no overworld do deserto carregando o S-Path).
    ///
    /// Carregamento mínimo e pontual via <see cref="SceneManager.LoadScene(string)"/>
    /// — NÃO é a infraestrutura completa de multi-cena (streaming/aditivo/persistência
    /// de estado ainda não existem). A cena destino precisa estar registrada em
    /// Build Settings para carregar por nome.
    ///
    /// <para><b>Tranca por chave de save (2026-08-20):</b> um portal pode exigir que uma
    /// <see cref="ChavesDeSave"/> já esteja gravada. Nasceu de um defeito de ordem: nenhum
    /// portal era travado, então dava para ir do Deserto direto aos Portões e encarar o
    /// Byakhee <b>sem arma e sem companheiro</b> — e seguir para o Castelo sem o Yug-Neth
    /// que vira o NPC de artesanato lá. Decisão do Vini: a Tumba passa a ser obrigatória,
    /// porque é onde Yug-Neth é libertado de Abdul.</para>
    ///
    /// <para>Vazio em <see cref="chaveExigida"/> mantém o portal livre, que é o caso de
    /// todos os outros.</para>
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class PortalDeCena : MonoBehaviour
    {
        [Tooltip("Nome da cena a carregar (sem extensão). Deve estar em Build Settings.")]
        [SerializeField] private string cenaDestino;

        [Tooltip("Onde aparecer na cena destino (identificador de um PontoDeChegada lá). " +
                 "Vazio = usa a posição padrão do jogador naquela cena.")]
        [SerializeField] private string chegarEm;

        [Tooltip("Segundos ignorando contato logo após a cena carregar. Impede que chegar " +
                 "em cima de um portal jogue o jogador de volta na hora.")]
        [SerializeField] private float carenciaAoCarregar = 0.5f;

        [Tooltip("Loga todo contato de trigger, mesmo os recusados (tag errada, carência " +
                 "ativa). Serve para distinguir 'o Player nunca chega até aqui' (problema de " +
                 "colisão/posição) de 'chega mas é recusado' (tag/carência) de 'nada acontece " +
                 "mesmo aceito' (cena destino/Build Settings). Desligue quando estabilizar.")]
        [SerializeField] private bool logarContatos = true;

        [Header("Tranca")]
        [Tooltip("Chave de save exigida para atravessar (ver ChavesDeSave). Vazio = livre.")]
        [SerializeField] private string chaveExigida;

        [Tooltip("Linha mostrada ao esbarrar sem a chave. PROVISÓRIA — reescrever com o Vini.")]
        [TextArea(2, 4)]
        [SerializeField] private string linhaSeTrancado =
            "Não sozinho. O que dorme além destes portões já bebeu de homens mais inteiros " +
            "que você.";

        [Tooltip("Caixa de texto da cena, para mostrar a linha. Injetada pelo montador.")]
        [SerializeField] private TutorialHintUI caixaDeTexto;

        private float _tempoDeAtivacao;
        private bool _jaAvisou;

        private void Reset()
        {
            // Ao adicionar o componente no Editor, já deixa o collider como trigger.
            var col = GetComponent<Collider2D>();
            if (col != null) col.isTrigger = true;
        }

        private void Awake()
        {
            _tempoDeAtivacao = Time.time + Mathf.Max(0f, carenciaAoCarregar);

            if (string.IsNullOrWhiteSpace(cenaDestino))
                Debug.LogError($"[PortalDeCena] '{name}' está sem cena destino; nenhuma transição vai ocorrer.", this);
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (logarContatos)
                Debug.Log($"[Portal:{name}] contato de '{collision.name}' (tag={collision.tag}) " +
                          $"destino={cenaDestino} carênciaRestante={Mathf.Max(0f, _tempoDeAtivacao - Time.time):0.00}s", this);

            if (!collision.CompareTag("Player")) return;
            if (string.IsNullOrWhiteSpace(cenaDestino)) return; // erro já logado em Awake

            // Carência: chegar numa cena em cima de um portal (o caso normal quando ida e
            // volta usam a mesma porta) dispararia o trigger no mesmo instante e devolveria
            // o jogador para a cena de origem — um pingue-pongue infinito. A janela deixa
            // ele sair de cima da porta antes de ela voltar a valer.
            if (Time.time < _tempoDeAtivacao) return;

            if (EstaTrancado())
            {
                // Uma vez por aproximação. Repetir a fala a cada quadro de contato viraria
                // ruído, e o jogador costuma encostar no volume várias vezes tentando passar.
                if (!_jaAvisou)
                {
                    _jaAvisou = true;

                    if (caixaDeTexto != null) caixaDeTexto.Mostrar(linhaSeTrancado);
                    else Debug.LogWarning($"[PortalDeCena] '{name}' está trancado por " +
                                          $"'{chaveExigida}' e não tem caixa de texto: o " +
                                          "jogador esbarraria numa parede invisível sem " +
                                          "explicação.", this);
                }

                return;
            }

            PontoDeChegada.Pendente = string.IsNullOrWhiteSpace(chegarEm) ? null : chegarEm;

            // A navegação fotografa o estado antes de a cena ser destruída — sem isso,
            // atravessar a porta faria Damião perder a arma do baú e a Vitalidade sofrida.
            NavegacaoDeCenas.IrPara(cenaDestino);
        }

        private void OnTriggerExit2D(Collider2D collision)
        {
            if (collision.CompareTag("Player")) _jaAvisou = false;
        }

        /// <summary>
        /// Se o portal recusa passagem agora. Sem <see cref="chaveExigida"/> nunca recusa.
        /// </summary>
        public bool EstaTrancado()
        {
            if (string.IsNullOrWhiteSpace(chaveExigida)) return false;
            return GerenciadorDeSave.ObterValor(chaveExigida) == null;
        }

        /// <summary>Chave de save exigida para atravessar (vazio = portal livre).</summary>
        public string ChaveExigida => chaveExigida;

        /// <summary>Entrega a caixa de texto da cena. Usado pelos montadores de cena.</summary>
        public void Configurar(TutorialHintUI caixa)
        {
            if (caixa != null) caixaDeTexto = caixa;
        }

        /// <summary>Define a cena destino por código (usado pelo gerador do deserto).</summary>
        public void DefinirCenaDestino(string cena) => cenaDestino = cena;

        /// <summary>
        /// Define em qual <see cref="PontoDeChegada"/> da cena destino o jogador aparece.
        /// Vazio/nulo mantém a posição padrão do jogador naquela cena.
        /// </summary>
        public void DefinirChegada(string identificador) => chegarEm = identificador;

        /// <summary>Identificador do ponto de chegada na cena destino (vazio = padrão).</summary>
        public string ChegarEm => chegarEm;

        /// <summary>Nome da cena destino atualmente configurada (para inspeção/testes de cena).</summary>
        public string CenaDestino => cenaDestino;
    }
}
