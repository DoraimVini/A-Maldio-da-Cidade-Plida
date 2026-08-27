using UnityEngine;
using UnityEngine.U2D;

namespace FavelaAmarela.CameraSystem
{
    /// <summary>
    /// Orthographic 2D camera controller for isometric top-down view.
    /// Attaches to the Main Camera. Follows a target with smooth damping.
    /// 
    /// SETUP (Inspector):
    ///   1. Drag the player (Damiao) into the "Target" field.
    ///   2. Ensure Main Camera projection is set to "Orthographic".
    ///   3. Adjust "Orthographic Size" for zoom level (e.g., 8-12 for blockout).
    ///   4. Camera Z offset must be negative (default -10) so it renders the 2D scene.
    /// </summary>
    public class IsometricCameraController : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Transform target;

        [Header("Follow Settings")]
        [SerializeField] private float smoothTime = 0.15f;

        [Header("Camera Configuration")]
        [Tooltip("Z offset keeps camera behind the 2D plane. Must be negative.")]
        [SerializeField] private float zOffset = -10f;

        [Tooltip("Orthographic size controls the zoom level. Smaller = more zoomed in.")]
        [SerializeField] private float orthographicSize = 10f;

        private Vector3 velocity = Vector3.zero;
        private UnityEngine.Camera cam;
        private PixelPerfectCamera _pixelPerfect;

        // --- Shake state ---
        private float shakeTimeRemaining;
        private float shakeMagnitude;

        private void Awake()
        {
            cam = GetComponent<UnityEngine.Camera>();
            if (cam == null)
            {
                Debug.LogError("[IsometricCameraController] No Camera component found!", this);
                return;
            }

            // Force orthographic projection for 2D isometric
            cam.orthographic = true;

            // ── Quem manda no zoom (2026-08-27) ───────────────────────────────
            // Com um PixelPerfectCamera presente, o TAMANHO É DELE: ele recalcula
            // orthographicSize a cada OnPreCull a partir da resolução de referência e da tela
            // real. Escrever aqui não muda nada em jogo e mente no Inspector — o número que
            // este componente mostra deixaria de ser o número que se vê.
            //
            // A própria doc do pacote descreve esse conflito na seção do Cinemachine: dois
            // sistemas disputando orthographicSize "would cause them to fight for control over
            // the Camera and likely produce unwanted results".
            _pixelPerfect = GetComponent<PixelPerfectCamera>();

            if (_pixelPerfect == null) cam.orthographicSize = orthographicSize;

            if (target == null)
                Debug.LogWarning("[IsometricCameraController] No target assigned. Camera will not follow.", this);
        }

        private void LateUpdate()
        {
            if (target == null) return;

            // Follow target on X/Y plane, keep Z offset fixed
            Vector3 targetPosition = new Vector3(
                target.position.x,
                target.position.y,
                zOffset
            );

            transform.position = Vector3.SmoothDamp(
                transform.position,
                targetPosition,
                ref velocity,
                smoothTime
            );

            if (shakeTimeRemaining > 0f)
            {
                shakeTimeRemaining -= Time.deltaTime;
                Vector2 shakeOffset = UnityEngine.Random.insideUnitCircle * shakeMagnitude;
                transform.position += new Vector3(shakeOffset.x, shakeOffset.y, 0f);
            }
        }

        /// <summary>
        /// Sacode a câmera por <paramref name="duration"/> segundos, com deslocamento
        /// aleatório de até <paramref name="magnitude"/> unidades por frame. Reaproveitável
        /// por qualquer evento de impacto (ex.: chão desmoronando na queda Z4→Z5).
        /// </summary>
        public void Shake(float duration, float magnitude)
        {
            shakeTimeRemaining = duration;
            shakeMagnitude = magnitude;
        }

        /// <summary>
        /// Muda o zoom em tempo de execução (a ideia original era o efeito do Salto Dimensional).
        ///
        /// <para><b>Não faz efeito com o <c>PixelPerfectCamera</c> ligado</b>, e o aviso é
        /// deliberado: o componente reescreve <c>orthographicSize</c> a cada quadro, então um
        /// zoom por aqui seria desfeito no mesmo frame e o efeito simplesmente não aconteceria —
        /// sem erro nenhum. Quando o Salto Dimensional for ganhar zoom de verdade, o caminho é a
        /// resolução de referência do <c>PixelPerfectCamera</c>, não este método. Hoje ele não
        /// tem um único chamador em produção.</para>
        /// </summary>
        public void SetZoom(float newSize)
        {
            orthographicSize = Mathf.Max(1f, newSize);

            if (_pixelPerfect != null)
            {
                Debug.LogWarning($"[IsometricCameraController] SetZoom({newSize}) ignorado: o " +
                                 "PixelPerfectCamera reescreve o tamanho a cada quadro. Mude a " +
                                 "resolução de referência dele em vez disso.", this);
                return;
            }

            if (cam != null) cam.orthographicSize = orthographicSize;
        }

        /// <summary>
        /// Sets a new target for the camera to follow.
        /// </summary>
        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
        }
    }
}
