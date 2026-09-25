using UnityEngine;
using UnityEngine.SceneManagement;
using FavelaAmarela.Core.Persistencia;
using FavelaAmarela.Runtime.Persistencia;

namespace FavelaAmarela.Runtime.GameLoop
{
    /// <summary>
    /// Camada Runtime. Único lugar por onde o jogo troca de cena.
    ///
    /// <para><b>Por que existe (2026-08-11):</b> <c>SceneManager.LoadScene</c> estava
    /// espalhado por quatro pontos — portal, travessia do companheiro, menu e retorno da
    /// morte — e cada um repetia o mesmo ritual de saída por conta própria: capturar o save,
    /// devolver o <c>timeScale</c>, então carregar. Repetir ritual é onde nascem os bugs:
    /// basta um caminho esquecer o <c>timeScale</c> para a partida seguinte nascer congelada,
    /// e um esquecer a captura para o jogador perder o trecho que acabou de jogar.</para>
    ///
    /// <para>É estático e sem estado, no mesmo espírito da API estática do
    /// <see cref="GerenciadorDeSave"/>: não há nada para configurar em cena, e um
    /// <c>MonoBehaviour</c> a mais só criaria outra dependência para injetar.</para>
    /// </summary>
    public static class NavegacaoDeCenas
    {
        /// <summary>Nome da cena do menu principal. É ela que abre o jogo (índice 0 do build).</summary>
        public const string CenaDoMenu = "Cena_Menu";

        /// <summary>Onde uma peregrinação nova começa.</summary>
        public const string CenaDeAbertura = "Deserto_Hali";

        /// <summary>
        /// Vai para uma cena de jogo, preservando o progresso da sessão.
        /// </summary>
        /// <param name="cena">Nome da cena destino.</param>
        public static void IrPara(string cena)
        {
            if (string.IsNullOrWhiteSpace(cena))
            {
                Debug.LogError("[Navegacao] Pedido de troca de cena sem destino — ignorado.");
                return;
            }

            Sair();
            SceneManager.LoadScene(cena);
        }

        /// <summary>
        /// Volta ao menu principal, descarregando a partida. Usado pela morte e por qualquer
        /// saída deliberada do jogo em andamento.
        /// </summary>
        public static void IrParaMenu() => IrPara(CenaDoMenu);

        /// <summary>
        /// Começa uma peregrinação nova: apaga o progresso e abre a cena inicial.
        /// </summary>
        public static void ComecarNovaPeregrinacao()
        {
            GerenciadorDeSave.Instancia?.ApagarSave();

            // Sem captura aqui, de propósito: capturar depois de apagar recriaria no registro
            // justamente o estado que o jogador pediu para jogar fora.
            RestaurarTempo();
            SceneManager.LoadScene(CenaDeAbertura);
        }

        /// <summary>
        /// Retoma a partida salva. Cai na cena de abertura se o save não souber onde parou —
        /// melhor recomeçar o trecho do que travar num botão que não faz nada.
        /// </summary>
        public static void Continuar()
        {
            string destino = GerenciadorDeSave.Instancia?.CenaSalva;

            if (string.IsNullOrEmpty(destino))
            {
                Debug.LogWarning("[Navegacao] Save sem cena registrada; abrindo a cena inicial.");
                destino = CenaDeAbertura;
            }

            IrPara(destino);
        }

        /// <summary>
        /// Devolve o jogador ao <b>último Refúgio de Luz</b> depois da morte. Se ele ainda não
        /// descansou em nenhum, volta à cena de abertura.
        ///
        /// <para><b>Não captura o estado, de propósito.</b> Morrer tem de <i>desfazer</i> o
        /// trecho, não gravá-lo: capturar aqui salvaria Damião morto e sem os itens gastos.
        /// Em vez disso relemos o disco, que guarda exatamente a foto do último Refúgio.</para>
        /// </summary>
        public static void RenascerNoUltimoRefugio()
        {
            var gerenciador = GerenciadorDeSave.Instancia;

            // Volta ao último save de verdade. Sem isto, o registro em memória ainda traria o
            // estado da morte — inclusive a Vitalidade zerada.
            gerenciador?.CarregarDoDisco();

            string cena = GerenciadorDeSave.ObterValor(ChavesDeSave.RefugioCena);
            string ponto = GerenciadorDeSave.ObterValor(ChavesDeSave.RefugioPonto);

            if (string.IsNullOrEmpty(cena))
            {
                Debug.Log("[Navegacao] Nenhum Refúgio visitado ainda; renascendo na entrada.");
                cena = CenaDeAbertura;
                ponto = null;
            }

            PontoDeChegada.Pendente = string.IsNullOrWhiteSpace(ponto) ? null : ponto;

            RestaurarTempo();
            SceneManager.LoadScene(cena);
        }

        /// <summary>Se existe um Refúgio registrado para onde renascer.</summary>
        public static bool TemRefugioRegistrado =>
            !string.IsNullOrEmpty(GerenciadorDeSave.ObterValor(ChavesDeSave.RefugioCena));

        /// <summary>
        /// Volta à entrada do Deserto sem passar pelo menu — a saída para quem morreu antes de
        /// alcançar qualquer Refúgio.
        /// </summary>
        public static void VoltarParaEntradaDoDeserto()
        {
            GerenciadorDeSave.Instancia?.CarregarDoDisco();
            PontoDeChegada.Pendente = null;

            RestaurarTempo();
            SceneManager.LoadScene(CenaDeAbertura);
        }

        /// <summary>
        /// O ritual de saída, num lugar só: grava o estado da sessão e devolve o tempo ao
        /// ritmo normal.
        /// </summary>
        private static void Sair()
        {
            GerenciadorDeSave.Instancia?.CapturarTudo();
            RestaurarTempo();
        }

        /// <summary>
        /// Devolve o <c>timeScale</c> a 1. Sair de um jogo pausado (ou da tela de morte) sem
        /// isto faz a cena seguinte nascer congelada — e o sintoma, "o jogo travou ao carregar",
        /// não aponta em nada para o pause.
        /// </summary>
        private static void RestaurarTempo() => Time.timeScale = 1f;
    }
}
