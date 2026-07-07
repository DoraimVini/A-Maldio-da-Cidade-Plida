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

        private IEnumerator SequenciaDeQueda(PlayerMovement playerMovement, Rigidbody2D rb)
        {
            // Trava input — mesmo princípio de "lock" já usado em PlayerMovement
            // (isLeaping/isEsquivando), só que desabilitando o componente inteiro,
            // já que aqui não há necessidade de retomar o movimento normal no meio.
            playerMovement.enabled = false;
            rb.linearVelocity = Vector2.zero;

            if (cerco != null)
            {
                yield return StartCoroutine(cerco.Tocar(rb.position));
                yield return new WaitForSeconds(pausaTensaoDuration);
            }

            if (isoCameraController != null) isoCameraController.Shake(shakeDuration, shakeMagnitude);
            yield return new WaitForSeconds(shakeDuration);

            if (fader != null) yield return fader.FadeTo(1f, fadeOutDuration);

            rb.position = destino.position;

            yield return new WaitForSeconds(blackHoldDuration);

            if (fader != null) yield return fader.FadeTo(0f, fadeInDuration);

            playerMovement.enabled = true;
        }
    }
}
