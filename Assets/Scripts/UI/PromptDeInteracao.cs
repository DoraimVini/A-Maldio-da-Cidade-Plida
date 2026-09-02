using UnityEngine;
using UnityEngine.UI;
using FavelaAmarela.Runtime.Interaction;

namespace FavelaAmarela.Runtime.UI
{
    /// <summary>
    /// Mostra o convite de interação ("E — Abrir o baú") enquanto houver um alvo ao
    /// alcance, e some quando não houver.
    ///
    /// <para>Observa o evento <c>OnAlvoMudou</c> do <see cref="DetectorDeInteracao"/> —
    /// nada de polling por frame (Regra de Ouro 8). Sem alvo, o objeto de UI é
    /// desativado, então não custa nada quando não está em uso.</para>
    /// </summary>
    [AddComponentMenu("FavelaAmarela/UI/Prompt de Interação")]
    public sealed class PromptDeInteracao : MonoBehaviour
    {
        [Header("Referências")]
        [Tooltip("Detector no Damião que informa qual objeto está sob a mira.")]
        [SerializeField] private DetectorDeInteracao detector;

        [Tooltip("Painel do prompt — ligado/desligado conforme há alvo. DEVE ser um filho, " +
                 "nunca este mesmo GameObject. Se vazio, usa o objeto do Label.")]
        [SerializeField] private GameObject raiz;

        [Tooltip("Label onde o texto do prompt é escrito.")]
        [SerializeField] private Text label;

        [Header("Texto")]
        [Tooltip("Tecla mostrada antes do rótulo da ação.")]
        [SerializeField] private string tecla = "E";

        private void Awake()
        {
            // Nunca deixar a raiz ser este próprio objeto: desativá-lo derrubaria este
            // componente junto, o OnDisable desinscreveria do evento e o prompt nunca
            // mais voltaria a aparecer. Cai para o objeto do Label (um filho).
            if (raiz == gameObject)
            {
                Debug.LogError("[PromptDeInteracao] 'Raiz' não pode ser o próprio GameObject " +
                               "deste componente (ele se desativaria e pararia de ouvir o evento). " +
                               "Usando o objeto do Label.", this);
                raiz = null;
            }

            if (raiz == null && label != null) raiz = label.gameObject;

            // Aviso, e não erro: no HUD persistente este campo NASCE vazio por construção
            // (prefab-asset não referencia cena) e quem o preenche é o GameLoopBootstrap, no
            // Bind. Um erro no caso normal ensina a ignorar erro.
            if (detector == null)
                Debug.LogWarning("[PromptDeInteracao] Sem detector ainda — esperando o Bind do " +
                                 "GameLoopBootstrap. Se ele não vier, o prompt nunca aparece.",
                                 this);

            if (label == null)
                Debug.LogError("[PromptDeInteracao] Label de texto não atribuído — " +
                               "o prompt não terá o que escrever.", this);

            Esconder();
        }

        /// <summary>
        /// Liga o prompt ao detector de Damião. Chamado pelo <c>GameLoopBootstrap</c>.
        ///
        /// <para><b>Por que existe (2026-09-02).</b> Este prompt vivia numa <b>cena só das
        /// seis</b> do build — nas outras cinco o jogador <b>nunca via "E — ..."</b>, e portanto
        /// não tinha como saber que baú, item ou NPC eram interagíveis. Ele agora mora no
        /// <c>HUD_Gameplay.prefab</c>, que é um asset em <c>Resources</c>: um prefab-asset
        /// <b>não pode</b> referenciar objeto de cena, então o campo do Inspector é impossível
        /// de preencher e a injeção tem de vir de fora.</para>
        ///
        /// <para>É exatamente o caminho que o <c>PainelDeFicha</c> já percorreu — mesmo
        /// problema, mesmo conserto (ver <c>GameLoopBootstrap</c>).</para>
        ///
        /// <para>Idempotente: re-bind troca a fonte sem deixar handler pendurado na anterior.</para>
        /// </summary>
        public void Bind(DetectorDeInteracao novoDetector)
        {
            if (detector != null) detector.OnAlvoMudou -= HandleAlvoMudou;

            detector = novoDetector;

            if (detector != null && isActiveAndEnabled)
                detector.OnAlvoMudou += HandleAlvoMudou;

            Esconder();
        }

        private void OnEnable()
        {
            if (detector != null) detector.OnAlvoMudou += HandleAlvoMudou;
        }

        private void OnDisable()
        {
            if (detector != null) detector.OnAlvoMudou -= HandleAlvoMudou;
        }

        private void HandleAlvoMudou(IInteragivel alvo)
        {
            if (alvo == null)
            {
                Esconder();
                return;
            }

            if (label != null) label.text = $"{tecla} — {alvo.RotuloDeInteracao}";
            if (raiz != null) raiz.SetActive(true);
        }

        private void Esconder()
        {
            if (raiz != null) raiz.SetActive(false);
        }
    }
}
