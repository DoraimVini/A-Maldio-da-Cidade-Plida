using UnityEngine;
using UnityEngine.SceneManagement;
using FavelaAmarela.Core.Entrada;

namespace FavelaAmarela.Runtime.Entrada
{
    /// <summary>
    /// Camada Runtime. O ponto único onde qualquer leitor de tecla pergunta <b>"posso
    /// agir?"</b>.
    ///
    /// <para><b>Estático de propósito, e é a exceção que se justifica.</b> Os leitores de
    /// entrada estão espalhados por seis arquivos que não se conhecem — <c>PlayerMovement</c>,
    /// <c>BarraDeItens</c>, <c>DetectorDeInteracao</c>, <c>PainelDeInventario</c>,
    /// <c>ConsoleDeCarcosa</c>, <c>PausaInputHandler</c>. Injetar o árbitro em todos exigiria
    /// que o <c>GameLoopBootstrap</c> alcançasse cada um em cada cena, e o console <b>nasce
    /// sozinho</b>, fora de qualquer cena. Um acesso estático a um POCO testável é o menor mal
    /// aqui; a regra que importa — <b>a lógica mora no Core</b> — continua valendo, e
    /// <see cref="FocoDeEntrada"/> é testado sem Unity.</para>
    ///
    /// <para><b>Limpa a pilha a cada cena carregada.</b> Um painel destruído no meio de uma
    /// troca de cena deixaria uma camada pendurada, e o jogador ficaria sem controle sem nada
    /// explicando — o pior tipo de defeito deste projeto.</para>
    /// </summary>
    public static class ArbitroDeFoco
    {
        private static FocoDeEntrada _foco;

        /// <summary>O árbitro. Nasce na primeira leitura.</summary>
        public static FocoDeEntrada Foco => _foco ??= new FocoDeEntrada();

        /// <summary>
        /// Atalho para a pergunta que 90% dos chamadores fazem: <b>o jogo pode ler entrada
        /// agora?</b>
        /// </summary>
        public static bool JogoNoComando => Foco.JogoNoComando;

        /// <summary>Toma o comando para uma camada.</summary>
        public static void Tomar(CamadaDeEntrada camada) => Foco.Tomar(camada);

        /// <summary>Devolve o comando de uma camada.</summary>
        public static void Devolver(CamadaDeEntrada camada) => Foco.Devolver(camada);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Ligar()
        {
            // Domain reload desligado deixa o estático vivo entre execuções no Editor: sem este
            // reset, uma partida começaria com a pilha da anterior.
            _foco = new FocoDeEntrada();

            SceneManager.sceneLoaded -= HandleCenaCarregada;
            SceneManager.sceneLoaded += HandleCenaCarregada;
        }

        private static void HandleCenaCarregada(Scene cena, LoadSceneMode modo)
        {
            if (Foco.Profundidade == 0) return;

            Debug.LogWarning($"[ArbitroDeFoco] {Foco.Profundidade} camada(s) de entrada ainda " +
                             $"presas ao trocar para '{cena.name}' — devolvendo o comando ao " +
                             "jogo. Algum painel foi destruído sem devolver o foco.");

            Foco.Limpar();
        }
    }
}
