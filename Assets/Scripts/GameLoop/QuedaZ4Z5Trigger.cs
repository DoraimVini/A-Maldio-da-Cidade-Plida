using System.Collections;
using UnityEngine;
using FavelaAmarela.Player;
using FavelaAmarela.CameraSystem;
using FavelaAmarela.Runtime.UI;

namespace FavelaAmarela.Runtime.GameLoop
{
    /// <summary>
    /// Camada Runtime (MonoBehaviour). Dispara a sequência roteirizada de queda:
    /// Damião é cercado por Cultistas e Espectros na Praça do Cerco (Zona 4,
    /// ver <see cref="CercoZ4Cutscene"/>), o chão cede e ele cai na Transição
    /// Dimensional (Zona 5). O entulho que bloqueia a volta já existe — é a
    /// própria barreira anômala construída pelo <see cref="FavelaAmarela.Level.Runtime.LevelBlockoutGenerator"/>
    /// entre as duas zonas, só atravessável com o Salto Dimensional.
    ///
    /// Mirror do padrão de <see cref="ColapsoTrigger"/> (trigger + CompareTag), só que
    /// com uma pequena sequência de efeitos em vez de uma ação imediata.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    [AddComponentMenu("Favela Amarela/GameLoop/Queda Z4-Z5 Trigger")]
    public class QuedaZ4Z5Trigger : MonoBehaviour
    {
        [Header("Destino")]
        [Tooltip("Marcador na Zona 5 onde Damião reaparece após a queda.")]
        [SerializeField] private Transform destino;

        [Header("Efeitos")]
        [SerializeField] private IsometricCameraController isoCameraController;
        [SerializeField] private ScreenFader fader;
        [Tooltip("Cutscene de cerco (Cultistas + Espectros) que toca antes do chão ceder. Opcional — se nulo, a queda acontece direto.")]
        [SerializeField] private CercoZ4Cutscene cerco;

        [Header("Timings")]
        [Tooltip("Pausa de tensão após o cerco terminar, antes do tremor de câmera começar.")]
        [SerializeField] private float pausaTensaoDuration = 1.5f;
        [SerializeField] private float shakeDuration = 0.3f;
        [SerializeField] private float shakeMagnitude = 0.15f;
        [SerializeField] private float fadeOutDuration = 0.25f;
        [SerializeField] private float blackHoldDuration = 0.15f;
        [SerializeField] private float fadeInDuration = 0.35f;

        private bool _disparado;

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (_disparado) return;
            if (!collision.CompareTag("Player")) return;

            var playerMovement = collision.GetComponent<PlayerMovement>();
            var rb = collision.attachedRigidbody;
            if (playerMovement == null || rb == null || destino == null) return;

            _disparado = true;
            StartCoroutine(SequenciaDeQueda(playerMovement, rb));
        }

        private CutsceneController _cutscene;
        private FavelaAmarela.Runtime.Environment.TempestadeAmbiente _tempestade;

        /// <summary>
        /// Liga a invulnerabilidade de cutscene e o driver da tempestade. Chamado pelo
        /// <c>GameLoopBootstrap</c>.
        ///
        /// <para><b>Fase 5, 2026-08-18:</b> substitui <c>GameManager.Instance</c> nos dois pontos
        /// desta sequência — a invulnerabilidade e o zeramento da faixa de tempestade.</para>
        /// </summary>
        public void Bind(CutsceneController cutscene,
                         FavelaAmarela.Runtime.Environment.TempestadeAmbiente tempestade)
        {
            _cutscene = cutscene;
            _tempestade = tempestade;

            if (_cutscene == null)
                Debug.LogError("[QuedaZ4Z5Trigger] Sem CutsceneController — Damião pode morrer " +
                               "durante a queda roteirizada, que deveria ser só tensão.", this);
        }

        private IEnumerator SequenciaDeQueda(PlayerMovement playerMovement, Rigidbody2D rb)
        {
            // Trava input — mesmo princípio de "lock" já usado em PlayerMovement
            // (isLeaping/isEsquivando), só que desabilitando o componente inteiro,
            // já que aqui não há necessidade de retomar o movimento normal no meio.
            playerMovement.enabled = false;
            rb.linearVelocity = Vector2.zero;

            // Preso na cutscene: imune a morte instantânea (Coisa etc.) — só tensão, não dano.
            if (_cutscene != null) _cutscene.DefinirInvulneravel(true);

            if (cerco != null)
            {
                yield return StartCoroutine(cerco.Tocar(rb.position));
                yield return new WaitForSeconds(pausaTensaoDuration);
            }

            if (isoCameraController != null) isoCameraController.Shake(shakeDuration, shakeMagnitude);
            yield return new WaitForSeconds(shakeDuration);

            if (fader != null) yield return fader.FadeTo(1f, fadeOutDuration);

            rb.position = destino.position;

            // Chegou na Zona 5 (subterrâneo fechado): sem tempestade. Zera a faixa
            // explicitamente — o teleporte adormece o Rigidbody e o TempestadeTrigger_Z5_Nula
            // não dispara OnTriggerEnter de forma confiável nesse caso.
            if (_tempestade != null) _tempestade.DefinirFaixa(0f, 0f);

            // Limpa os atores do cerco (Cultistas + Espectros) — eles eram set-piece da
            // Zona 4; sem isso o Espectro persegue e encalha na barreira de anomalia.
            if (cerco != null) cerco.LimparAtores();

            yield return new WaitForSeconds(blackHoldDuration);

            if (fader != null) yield return fader.FadeTo(0f, fadeInDuration);

            playerMovement.enabled = true;

            // Fim da cutscene: volta a ser vulnerável (já teleportado pra Z5, longe da Coisa).
            if (_cutscene != null) _cutscene.DefinirInvulneravel(false);
        }
    }
}
