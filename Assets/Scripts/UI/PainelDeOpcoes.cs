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
            Fechar();   // nasce fechada: quem abre é o menu
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
            Sincronizar();
            Raiz.SetActive(true);
        }

        /// <summary>Fecha a tela.</summary>
        public void Fechar() => Raiz.SetActive(false);

        /// <summary>Se está aberta agora — o menu de pausa consulta para não fechar os dois.</summary>
        public bool EstaAberta => Raiz.activeSelf;

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
