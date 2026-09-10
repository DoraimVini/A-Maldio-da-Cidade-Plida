using UnityEngine;
using UnityEngine.UI;
using FavelaAmarela.Core.Preferencias;
using FavelaAmarela.Runtime.Preferencias;

namespace FavelaAmarela.Runtime.UI
{
    /// <summary>
    /// A tela de <b>Opções</b>: volume, janela e sincronização de quadros.
    ///
    /// <para><b>Por que é uma tela própria, e não parte do HUD.</b> O HUD se oculta em toda cena
    /// sem <c>GameLoopBootstrap</c> — ou seja, no <b>menu principal</b>, que é justamente onde
    /// um jogador procura as opções antes de começar. Pendurá-la ali exigiria furar aquela
    /// regra, e a regra é boa.</para>
    ///
    /// <para><b>Nasce sozinha e persiste</b>, como o <c>ProgressionBridge</c> e o
    /// <c>ItemDatabase</c>. Uma tela por cena seria mais uma lista para envelhecer — este
    /// repositório já catalogou oito.</para>
    ///
    /// <para><b>O que ela NÃO oferece, de propósito:</b> resolução. Uma resolução mal escolhida
    /// pode deixar a interface fora da tela, e então o jogador não consegue mais alcançar a
    /// opção para desfazê-la. Tela cheia cobre a necessidade comum sem esse risco; resolução
    /// entra quando houver uma confirmação com contagem regressiva para reverter sozinha.</para>
    /// </summary>
    [AddComponentMenu("Favela Amarela/UI/Painel de Opções")]
    public sealed class PainelDeOpcoes : MonoBehaviour
    {
        private static PainelDeOpcoes _instancia;

        /// <summary>Instância única. Nula fora de Play.</summary>
        public static PainelDeOpcoes Instancia => _instancia;

        [Header("Raiz")]
        [Tooltip("O objeto que é ligado e desligado. Vazio = este mesmo GameObject.")]
        [SerializeField] private GameObject conteudo;

        [Header("Controles")]
        [SerializeField] private Slider barraDeVolume;
        [SerializeField] private Text rotuloDoVolume;
        [SerializeField] private Toggle alternadorDeTelaCheia;
        [SerializeField] private Toggle alternadorDeVSync;
        [SerializeField] private Dropdown seletorDeQuadros;
        [SerializeField] private Button botaoDeFechar;
        [SerializeField] private Button botaoDeRestaurar;

        /// <summary>
        /// Os tetos oferecidos. <b>Só valem com a sincronização vertical desligada</b> — com ela
        /// ligada a Unity ignora o <c>targetFrameRate</c>, e é por isso que o seletor fica
        /// desabilitado nesse caso em vez de mostrar um número que não acontece.
        /// </summary>
        private static readonly int[] Tetos =
        {
            PreferenciasDoJogador.SemLimiteDeQuadros, 30, 60, 120, 144,
        };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void GarantirInstancia()
        {
            if (_instancia != null) return;

            var prefab = Resources.Load<GameObject>("Painel_Opcoes");
            if (prefab == null)
            {
                Debug.LogError("[PainelDeOpcoes] 'Resources/Painel_Opcoes' não encontrado — o " +
                               "jogo roda sem tela de opções. Conserto: " +
                               "'Tools/FavelaAmarela/UI: montar o painel de opções'.");
                return;
            }

            var obj = Instantiate(prefab);
            obj.name = prefab.name;   // sem o "(Clone)"
            DontDestroyOnLoad(obj);
        }

        private void Awake()
        {
            if (_instancia != null && _instancia != this) { Destroy(gameObject); return; }

            _instancia = this;
            DontDestroyOnLoad(gameObject);

            MontarSeletorDeQuadros();
            Ligar();

            // Nasce fechada: quem abre é o menu. Direto na raiz, e não por Fechar(): Fechar()
            // devolve um foco que ninguém tomou ainda.
            Raiz.SetActive(false);
        }

        private void OnDestroy()
        {
            if (_instancia == this) _instancia = null;
        }

        private GameObject Raiz => conteudo != null ? conteudo : gameObject;

        // ── Abrir e fechar ────────────────────────────────────────────────────

        /// <summary>Abre a tela, sincronizando os controles com o estado corrente.</summary>
        public void Abrir()
        {
            if (EstaAberta) return;

            Sincronizar();
            Raiz.SetActive(true);

            // Toma o comando do teclado enquanto está aberta — é o que a tela de inventário faz
            // e o que o guarda LeitorDeTeclaRespeitaOFocoTests exige de quem lê Keyboard.current:
            // com a pilha de foco sabendo dela, o jogo por baixo não anda nem ataca, e o Esc que
            // ela lê no Update é dela por direito, não por sorte de ordem de Update.
            FavelaAmarela.Runtime.Entrada.ArbitroDeFoco.Tomar(
                FavelaAmarela.Core.Entrada.CamadaDeEntrada.PainelModal);
        }

        /// <summary>Fecha a tela e devolve o comando a quem o tinha.</summary>
        public void Fechar()
        {
            if (!EstaAberta) return;

            Raiz.SetActive(false);
            FavelaAmarela.Runtime.Entrada.ArbitroDeFoco.Devolver(
                FavelaAmarela.Core.Entrada.CamadaDeEntrada.PainelModal);
        }

        /// <summary>Se está aberta agora — o <c>PausaInputHandler</c> consulta para não despausar por baixo.</summary>
        public bool EstaAberta => Raiz.activeSelf;

        /// <summary>
        /// Se o Esc deste quadro foi gasto fechando a tela. O <c>PausaInputHandler</c> lê isto
        /// porque a ordem dos <c>Update</c> não é garantida: se ele rodar depois deste, vê a
        /// tela já fechada e trataria o mesmo Esc como "despausar".
        /// </summary>
        public bool ConsumiuEscNesteQuadro => _quadroDoEsc == Time.frameCount;

        private int _quadroDoEsc = -1;

        private void Update()
        {
            // Esc fecha a tela de opções (2026-09-10). Antes não fechava nada aqui: com ela
            // aberta sobre a pausa, o Esc caía no PausaInputHandler e DESPAUSAVA o jogo por
            // baixo — a tela ficava aberta, o Damião andando atrás dela, e o único botão que a
            // fechava estava fora do monitor (ver MontarPainelDeOpcoes). "Não tem como voltar
            // do menu de opções", relatou o Vini.
            if (!EstaAberta) return;

            var teclado = UnityEngine.InputSystem.Keyboard.current;
            if (teclado == null || !teclado.escapeKey.wasPressedThisFrame) return;

            _quadroDoEsc = Time.frameCount;
            Fechar();
        }

        /// <summary>Abre a tela de opções de qualquer lugar, se ela existir.</summary>
        public static void AbrirSeExistir()
        {
            if (_instancia != null) _instancia.Abrir();
            else Debug.LogWarning("[PainelDeOpcoes] Nenhuma instância — o botão de Opções não " +
                                  "tem o que abrir.");
        }

        // ── Ligação com as preferências ───────────────────────────────────────

        private PreferenciasDoJogador Preferencias =>
            PreferenciasBridge.Instancia?.Preferencias;

        private void MontarSeletorDeQuadros()
        {
            if (seletorDeQuadros == null) return;

            seletorDeQuadros.ClearOptions();

            var opcoes = new System.Collections.Generic.List<Dropdown.OptionData>();
            foreach (int teto in Tetos)
                opcoes.Add(new Dropdown.OptionData(
                    teto == PreferenciasDoJogador.SemLimiteDeQuadros ? "Sem limite" : $"{teto}"));

            seletorDeQuadros.AddOptions(opcoes);
        }

        private void Ligar()
        {
            if (barraDeVolume != null)
            {
                barraDeVolume.minValue = 0f;
                barraDeVolume.maxValue = 1f;
                barraDeVolume.onValueChanged.AddListener(HandleVolume);
            }

            if (alternadorDeTelaCheia != null)
                alternadorDeTelaCheia.onValueChanged.AddListener(HandleTelaCheia);

            if (alternadorDeVSync != null)
                alternadorDeVSync.onValueChanged.AddListener(HandleVSync);

            if (seletorDeQuadros != null)
                seletorDeQuadros.onValueChanged.AddListener(HandleQuadros);

            if (botaoDeFechar != null) botaoDeFechar.onClick.AddListener(Fechar);
            if (botaoDeRestaurar != null) botaoDeRestaurar.onClick.AddListener(HandleRestaurar);
        }

        /// <summary>
        /// Põe os controles em acordo com as preferências, <b>sem disparar os handlers</b>: usar
        /// os setters normais faria cada sincronização reescrever a preferência que acabou de
        /// ser lida — um laço silencioso entre interface e estado.
        /// </summary>
        private void Sincronizar()
        {
            var p = Preferencias;
            if (p == null) return;

            if (barraDeVolume != null) barraDeVolume.SetValueWithoutNotify(p.VolumeGeral);
            if (alternadorDeTelaCheia != null)
                alternadorDeTelaCheia.SetIsOnWithoutNotify(p.TelaCheia);
            if (alternadorDeVSync != null)
                alternadorDeVSync.SetIsOnWithoutNotify(p.SincronizacaoVertical);

            if (seletorDeQuadros != null)
            {
                seletorDeQuadros.SetValueWithoutNotify(IndiceDoTeto(p.LimiteDeQuadros));

                // Desabilitado com VSync ligada: a Unity ignora o targetFrameRate nesse caso, e
                // um seletor ativo prometeria um efeito que não acontece.
                seletorDeQuadros.interactable = !p.SincronizacaoVertical;
            }

            AtualizarRotuloDoVolume(p.VolumeGeral);
        }

        private static int IndiceDoTeto(int valor)
        {
            for (int i = 0; i < Tetos.Length; i++)
                if (Tetos[i] == valor) return i;

            return 0;   // "Sem limite"
        }

        private void AtualizarRotuloDoVolume(float v)
        {
            if (rotuloDoVolume != null) rotuloDoVolume.text = $"Volume: {Mathf.RoundToInt(v * 100f)}%";
        }

        // ── Handlers ──────────────────────────────────────────────────────────

        private void HandleVolume(float v)
        {
            if (Preferencias != null) Preferencias.VolumeGeral = v;
            AtualizarRotuloDoVolume(v);
        }

        private void HandleTelaCheia(bool ligado)
        {
            if (Preferencias != null) Preferencias.TelaCheia = ligado;
        }

        private void HandleVSync(bool ligado)
        {
            if (Preferencias == null) return;

            Preferencias.SincronizacaoVertical = ligado;

            // O seletor de quadros muda de estado junto: é a única forma de a tela continuar
            // descrevendo o que o motor faz.
            if (seletorDeQuadros != null) seletorDeQuadros.interactable = !ligado;
        }

        private void HandleQuadros(int indice)
        {
            if (Preferencias == null || indice < 0 || indice >= Tetos.Length) return;

            Preferencias.LimiteDeQuadros = Tetos[indice];
        }

        private void HandleRestaurar()
        {
            Preferencias?.Restaurar();
            Sincronizar();
        }
    }
}
