using UnityEngine;
using FavelaAmarela.Core.Persistencia;
using FavelaAmarela.Runtime.Enemies;
using FavelaAmarela.Runtime.Persistencia;

namespace FavelaAmarela.Runtime.GameLoop
{
    /// <summary>
    /// Camada Runtime (MonoBehaviour). Traz <b>Yug-Neth</b> para a cena atual quando ele já
    /// foi libertado na Tumba — sem isto, atravessar um <see cref="PortalDeCena"/> o deixava
    /// para trás. Bug relatado pelo Vini em playtest (2026-08-02): "o Mi-Go não sai da
    /// masmorra com o jogador".
    ///
    /// <para><b>A causa:</b> <see cref="PortalDeCena"/> usa
    /// <c>SceneManager.LoadScene</c> não-aditivo, que destrói a cena de origem inteira.
    /// Yug-Neth é um <c>GameObject</c> só daquela cena — sem <c>DontDestroyOnLoad</c>, ele
    /// não sobrevive à troca. <c>PontoDeChegada</c> já documenta essa limitação: só a
    /// <i>posição do jogador</i> atravessa, o resto do mundo se reconstrói do zero em cada
    /// cena, guiado pelas chaves de <see cref="GerenciadorDeSave"/>.</para>
    ///
    /// <para><b>Deriva de <see cref="ChavesDeSave.AbdulResolvido"/></b>, não de uma chave
    /// própria — os dois desfechos da conversa com Abdul (vencer ou poupar) libertam
    /// Yug-Neth sem exceção, e <c>ChavesDeSave.YugNethLibertado</c> já documenta a escolha
    /// de não duplicar essa fonte da verdade.</para>
    ///
    /// <para><b>Pendência conhecida:</b> a incapacitação de Yug-Neth não é persistida — se
    /// ele cair na Tumba e o jogador sair mesmo assim, ele reaparece aqui saudável e
    /// seguindo, em vez de incapacitado esperando um Refúgio. Corrigir isso exige persistir
    /// o estado de incapacitação (e a Vitalidade corrente), que hoje não existe em nenhuma
    /// chave — fora do escopo deste reparo.</para>
    ///
    /// <para>Roda depois de <see cref="PontoDeChegada"/> (ordem de execução +100) para
    /// nascer perto de onde o jogador <b>de fato</b> reapareceu, não da posição padrão da
    /// cena.</para>
    /// </summary>
    [DefaultExecutionOrder(100)]
    [AddComponentMenu("Favela Amarela/GameLoop/Travessia do Companheiro (Yug-Neth)")]
    public sealed class TravessiaDoCompanheiro : MonoBehaviour
    {
        [Tooltip("Prefab do Yug-Neth a instanciar quando ele precisa atravessar de cena. [ASSET]")]
        [SerializeField] private GameObject prefabYugNeth;

        [Tooltip("Deslocamento da posição do jogador onde ele reaparece (não em cima do próprio jogador).")]
        [SerializeField] private Vector2 deslocamento = new Vector2(1.2f, -0.6f);

        private FavelaAmarela.Player.CompanionManager _companheiro;

        /// <summary>
        /// Liga o registrador de companheiro. Chamado pelo <c>GameLoopBootstrap</c>.
        ///
        /// <para><b>Fase 5, 2026-08-18:</b> substitui
        /// <c>GameManager.Instance.RegistrarYugNeth</c>.</para>
        /// </summary>
        public void Bind(FavelaAmarela.Player.CompanionManager companheiro)
        {
            _companheiro = companheiro;
        }

        private void Start()
        {
            if (GerenciadorDeSave.ObterValor(ChavesDeSave.AbdulResolvido) == null) return; // nunca foi libertado
            if (FindAnyObjectByType<YugNethAI>(FindObjectsInactive.Include) != null) return; // já existe aqui

            var jogador = GameObject.FindGameObjectWithTag("Player");
            if (jogador == null) return;

            if (prefabYugNeth == null)
            {
                Debug.LogError("[TravessiaDoCompanheiro] Prefab do Yug-Neth não atribuído — " +
                               "ele continua ausente nesta cena.", this);
                return;
            }

            Vector3 posicao = (Vector2)jogador.transform.position + deslocamento;
            var instancia = Instantiate(prefabYugNeth, posicao, Quaternion.identity);

            var yugNeth = instancia.GetComponent<YugNethAI>();
            if (yugNeth == null)
            {
                Debug.LogError("[TravessiaDoCompanheiro] Prefab atribuído não tem YugNethAI.", this);
                return;
            }

            yugNeth.Bind(jogador.transform);

            var colisorDoJogador = jogador.GetComponent<Collider2D>();
            if (colisorDoJogador != null) yugNeth.IgnorarColisaoCom(colisorDoJogador);

            if (_companheiro != null) _companheiro.RegistrarYugNeth(yugNeth);
            else
                Debug.LogWarning("[TravessiaDoCompanheiro] Sem CompanionManager ligado — " +
                                 "Yug-Neth não será registrado como companheiro da run.", this);
        }
    }
}
