using UnityEngine;

namespace FavelaAmarela.Runtime.GameLoop
{
    /// <summary>
    /// Camada Runtime (MonoBehaviour). Marca <b>onde Damião aparece</b> ao chegar numa cena
    /// vindo de um <see cref="PortalDeCena"/> específico. Sem isto, sair de uma dungeon
    /// devolveria o jogador ao ponto inicial da cena (onde o objeto do jogador está
    /// posicionado no arquivo), e não à porta por onde ele saiu.
    ///
    /// <para><b>Como funciona:</b> o portal grava seu identificador em
    /// <see cref="Pendente"/> (campo <c>static</c> comum — sobrevive à troca de cena porque
    /// não é um objeto de cena) e carrega o destino. Na cena nova, o ponto cujo
    /// <see cref="identificador"/> bate reposiciona o jogador e limpa a pendência.</para>
    ///
    /// <para><b>Escopo:</b> é o mínimo para dungeons terem porta de ida e volta. <b>Não</b>
    /// é persistência de estado entre cenas — inventário, vida e progresso continuam se
    /// perdendo na troca (ver <c>PortalDeCena</c>). Quando a arquitetura multi-cena com save
    /// existir, isto vira parte dela.</para>
    /// </summary>
    [AddComponentMenu("Favela Amarela/GameLoop/Ponto de Chegada")]
    public sealed class PontoDeChegada : MonoBehaviour
    {
        [Tooltip("Nome que o PortalDeCena de origem usa para pedir chegada aqui. Ex.: 'TumbaAlhazred'.")]
        [SerializeField] private string identificador;

        /// <summary>
        /// Identificador do ponto onde o jogador deve aparecer na próxima cena, ou vazio
        /// para "usar a posição padrão da cena". Escrito pelo <see cref="PortalDeCena"/>
        /// antes de carregar, lido pelo <see cref="PontoDeChegada"/> correspondente.
        /// </summary>
        public static string Pendente { get; set; }

        /// <summary>Identificador deste ponto de chegada.</summary>
        public string Identificador => identificador;

        private void Start()
        {
            if (string.IsNullOrWhiteSpace(Pendente)) return;
            if (Pendente != identificador) return;

            // Consome a pendência antes de mover: se dois pontos compartilharem o mesmo
            // identificador por engano, só o primeiro age em vez de brigarem pelo jogador.
            Pendente = null;

            var jogador = GameObject.FindGameObjectWithTag("Player");
            if (jogador == null)
            {
                Debug.LogError($"[PontoDeChegada] '{name}' não achou nenhum objeto com a tag " +
                               "Player para reposicionar.", this);
                return;
            }

            // Preserva o Z do jogador: em 2D ele carrega a profundidade de render, e
            // sobrescrever com o Z deste marcador poderia tirá-lo do plano de jogo.
            Vector3 destino = transform.position;
            destino.z = jogador.transform.position.z;
            jogador.transform.position = destino;
        }
    }
}
