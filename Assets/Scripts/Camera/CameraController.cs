using UnityEngine;

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
            cam.orthographicSize = orthographicSize;

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
        /// Updates the orthographic size at runtime (e.g., for zoom effects during Salto Dimensional).
        /// </summary>
        public void SetZoom(float newSize)
        {
            orthographicSize = Mathf.Max(1f, newSize);
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
