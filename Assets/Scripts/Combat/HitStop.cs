using UnityEngine;

namespace FavelaAmarela.Runtime.Combat
{
    /// <summary>
    /// Congela o mundo por alguns quadros no instante do acerto.
    ///
    /// <para><b>Por que isto vale mais que o resto junto.</b> É a ferramenta de <i>feel</i> mais
    /// barata que existe num jogo de ação: dois ou três quadros parados fazem o golpe "bater"
    /// em vez de atravessar. Este projeto não tinha nenhuma — o golpe do Damião era aritmética
    /// sobre uma consulta de sobreposição, sem uma única pista física de que algo aconteceu.</para>
    ///
    /// <para>Escala com a força do golpe: um espetada do Estilete quase não para o mundo, uma
    /// paulada do Alfanje para. Assim o hit-stop também <b>informa</b>, em vez de só enfeitar.</para>
    ///
    /// <para><b>A armadilha que o guarda abaixo evita.</b> <c>GameStatePresenter.Aplicar</c>
    /// também escreve <c>Time.timeScale</c> (0 ao pausar, 1 ao voltar). Se o jogador pausar
    /// <i>durante</i> um hit-stop, restaurar cegamente para 1 no fim <b>despausaria o jogo</b>.
    /// Por isso o tempo só é devolvido se ninguém mais tiver mexido nele nesse meio-tempo.</para>
    ///
    /// <para>Auto-instanciado no padrão canônico do projeto (<c>InventoryManager</c>,
    /// <c>GerenciadorDeSave</c>, <c>ProgressionBridge</c>, <c>HUDController</c>): sem wiring de
    /// cena para envelhecer.</para>
    /// </summary>
    [DefaultExecutionOrder(2000)]
    public sealed class HitStop : MonoBehaviour
    {
        /// <summary>Quase parado, não parado: em 0 alguns sistemas por <c>deltaTime</c> travam.</summary>
        private const float EscalaCongelada = 0.05f;

        /// <summary>Teto de duração. Acima disso o jogo parece engasgar, não impactar.</summary>
        private const float DuracaoMaxima = 0.12f;

        /// <summary>Abaixo disto o <c>timeScale</c> é considerado "já pausado por outro".</summary>
        private const float LimiarDePausa = 0.01f;

        private static HitStop _instancia;

        private float _restante;
        private bool _congelando;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void GarantirInstancia()
        {
            if (_instancia != null) return;

            var go = new GameObject("[Singleton] HitStop");
            _instancia = go.AddComponent<HitStop>();
            DontDestroyOnLoad(go);
        }

        /// <summary>
        /// Congela o mundo proporcionalmente ao peso do golpe.
        /// </summary>
        /// <param name="dano">Dano do golpe. Serve só de escala: 0 não congela nada.</param>
        public static void Bater(float dano)
        {
            if (_instancia == null) return;
            if (dano <= 0f) return;

            // 40 de dano (o Cravo) é a referência de "golpe cheio".
            float duracao = Mathf.Clamp(dano / 40f * 0.06f, 0.02f, DuracaoMaxima);
            _instancia.Congelar(duracao);
        }

        private void Congelar(float duracao)
        {
            // Já pausado (menu, Colapso, cinemática): não é hora de hit-stop, e mexer aqui
            // devolveria o tempo por cima da pausa.
            if (Time.timeScale <= LimiarDePausa && !_congelando) return;

            // Acerto múltiplo no mesmo quadro estende, não empilha.
            _restante = Mathf.Max(_restante, duracao);

            if (_congelando) return;

            _congelando = true;
            Time.timeScale = EscalaCongelada;
        }

        /// <summary>
        /// Conta em tempo <b>não-escalado</b> — em tempo escalado o congelamento se prolongaria
        /// na proporção do próprio congelamento e nunca terminaria direito.
        /// </summary>
        private void Update()
        {
            if (!_congelando) return;

            _restante -= Time.unscaledDeltaTime;
            if (_restante > 0f) return;

            _restante = 0f;
            _congelando = false;

            // Só devolve o tempo se ele ainda for o que ESTE componente deixou. Se o jogador
            // pausou no meio do hit-stop, o GameStatePresenter já zerou o timeScale — restaurar
            // para 1 aqui despausaria o jogo sozinho.
            if (Mathf.Approximately(Time.timeScale, EscalaCongelada))
                Time.timeScale = 1f;
        }
    }
}
