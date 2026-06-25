using UnityEngine;

namespace FavelaAmarela.Camera
{
    public class CameraController : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Transform target;

        [Header("Follow Settings")]
        [SerializeField] private float smoothTime = 0.25f;
        [SerializeField] private Vector3 offset = new Vector3(0f, 0f, -10f);

        private Vector3 velocity = Vector3.zero;

        private void LateUpdate()
        {
            if (target == null) return;

            // Target position on the 2D plane combined with camera offset
            Vector3 targetPosition = target.position + offset;

            // Smoothly move the camera
            transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, smoothTime);
        }

        /// <summary>
        /// Manually sets a target to follow.
        /// </summary>
        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
        }
    }
}
