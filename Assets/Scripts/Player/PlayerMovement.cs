using UnityEngine;
using UnityEngine.InputSystem;

namespace FavelaAmarela.Player
{
    public enum MovementMode
    {
        Sneaking,
        Walking,
        Running
    }

    /// <summary>
    /// POCO: Pure C# logic for stealth state — no Unity dependencies except Mathf.
    /// Testable independently from MonoBehaviour.
    /// </summary>
    public class PlayerStealthState
    {
        public MovementMode CurrentMode { get; private set; } = MovementMode.Walking;
        public float Speed { get; private set; }
        public float NoiseRadius { get; private set; }

        private readonly float sneakSpeed;
        private readonly float sneakNoise;
        private readonly float walkSpeed;
        private readonly float walkNoise;
        private readonly float runSpeed;
        private readonly float runNoise;

        public PlayerStealthState(
            float sneakSpeed = 2.0f, float sneakNoise = 1.0f,
            float walkSpeed = 4.5f, float walkNoise = 4.0f,
            float runSpeed = 7.5f, float runNoise = 8.5f)
        {
            this.sneakSpeed = sneakSpeed;
            this.sneakNoise = sneakNoise;
            this.walkSpeed = walkSpeed;
            this.walkNoise = walkNoise;
            this.runSpeed = runSpeed;
            this.runNoise = runNoise;
            SetMode(MovementMode.Walking);
        }

        public void SetMode(MovementMode mode)
        {
            CurrentMode = mode;
            (Speed, NoiseRadius) = mode switch
            {
                MovementMode.Sneaking => (sneakSpeed, sneakNoise),
                MovementMode.Running  => (runSpeed, runNoise),
                _                     => (walkSpeed, walkNoise),
            };
        }

        /// <summary>
        /// Returns effective noise radius considering storm dampening.
        /// Storm acts as white noise, reducing how far player sounds propagate.
        /// </summary>
        public float GetCurrentNoiseEmission(bool isMoving, float stormIntensity)
        {
            if (!isMoving) return 0f;
            float dampening = 1.0f - Mathf.Clamp01(stormIntensity * 0.6f);
            return NoiseRadius * dampening;
        }
    }

    /// <summary>
    /// MonoBehaviour Bridge: Connects PlayerStealthState POCO to Unity's
    /// physics (Rigidbody2D) and input (Input System) APIs.
    /// Requires a BoxCollider2D on the same GameObject for wall collision.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(BoxCollider2D))]
    [AddComponentMenu("Favela Amarela/Damião Movement")]
    public class PlayerMovement : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private bool useIsometricGridAlignment = true;

        [Header("Debug")]
        [SerializeField] private bool showNoiseGizmo = true;
        [Range(0f, 1f)]
        [SerializeField] private float debugStormIntensity = 0f;

        // Cached references (set once in Awake, never in Update)
        private Rigidbody2D rb;
        private PlayerStealthState stealthState;
        private Vector2 inputDirection;
        private bool isMoving;

        // Input System actions (cached in Awake)
        private InputAction moveAction;
        private InputAction sneakAction;
        private InputAction runAction;

        // --- Leap State ---
        private AnomalyPowerBridge anomalyBridge;
        private bool isLeaping;
        private Vector2 leapVelocity;
        private InputAction leapAction;

        public PlayerStealthState StealthState => stealthState;
        public bool IsMoving => isMoving;

        private void Awake()
        {
            // --- Rigidbody2D setup for top-down 2D ---
            rb = GetComponent<Rigidbody2D>();
            if (rb == null)
            {
                Debug.LogError("[PlayerMovement] Rigidbody2D not found!", this);
                return;
            }
            rb.gravityScale = 0f;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;

            anomalyBridge = GetComponent<AnomalyPowerBridge>();

            // --- POCO init ---
            stealthState = new PlayerStealthState();

            // --- Input System: safe lookup via FindAction (returns null, never throws) ---
            var playerInput = GetComponent<PlayerInput>();
            if (playerInput != null)
            {
                moveAction  = playerInput.actions.FindAction("Move");
                sneakAction = playerInput.actions.FindAction("Crouch");
                runAction   = playerInput.actions.FindAction("Sprint");
                leapAction  = playerInput.actions.FindAction("Jump"); // Use Jump or Dash

                if (moveAction == null)
                    Debug.LogWarning("[PlayerMovement] 'Move' action not found in Input Actions asset.", this);
            }
            else
            {
                Debug.LogWarning("[PlayerMovement] No PlayerInput component found. Input disabled.", this);
            }
        }

        private void OnEnable()
        {
            if (anomalyBridge != null)
            {
                anomalyBridge.OnDimensionalLeapActivated += HandleLeapActivated;
            }
        }

        private void OnDisable()
        {
            if (anomalyBridge != null)
            {
                anomalyBridge.OnDimensionalLeapActivated -= HandleLeapActivated;
            }
        }

        private void HandleLeapActivated(Vector2 direction, float duration, float speedMultiplier)
        {
            isLeaping = true;
            
            // Convert leap direction to isometric if needed
            Vector2 finalDirection = useIsometricGridAlignment ? ConvertToIsometric(direction) : direction.normalized;
            leapVelocity = finalDirection * (stealthState.Speed * speedMultiplier);

            // Make player invincible/intangible during leap
            gameObject.layer = LayerMask.NameToLayer("Ignore Raycast"); // Example intangible layer

            Invoke(nameof(EndLeap), duration);
        }

        private void EndLeap()
        {
            isLeaping = false;
            gameObject.layer = LayerMask.NameToLayer("Default"); // Restore layer
        }

        private void Update()
        {
            if (isLeaping) return; // Lock input while leaping

            // Read input from New Input System only
            inputDirection = moveAction?.ReadValue<Vector2>() ?? Vector2.zero;
            isMoving = inputDirection.sqrMagnitude > 0.01f;

            // Trigger Leap
            if (leapAction != null && leapAction.WasPressedThisFrame() && anomalyBridge != null)
            {
                anomalyBridge.TryActivateLeap(inputDirection);
                if (anomalyBridge.IsLeaping) return; // Successful leap
            }

            // Determine stealth mode from modifier keys
            if (sneakAction != null && sneakAction.IsPressed())
                stealthState.SetMode(MovementMode.Sneaking);
            else if (runAction != null && runAction.IsPressed())
                stealthState.SetMode(MovementMode.Running);
            else
                stealthState.SetMode(MovementMode.Walking);
        }

        private void FixedUpdate()
        {
            if (isLeaping)
            {
                rb.linearVelocity = leapVelocity;
                return;
            }

            if (!isMoving)
            {
                rb.linearVelocity = Vector2.zero;
                return;
            }

            Vector2 movement = inputDirection.normalized;

            if (useIsometricGridAlignment)
                movement = ConvertToIsometric(movement);

            rb.linearVelocity = movement * stealthState.Speed;
        }

        /// <summary>
        /// Converts screen-space WASD input to isometric world-space direction.
        /// </summary>
        private static Vector2 ConvertToIsometric(Vector2 input)
        {
            float isoX = input.x - input.y;
            float isoY = (input.x + input.y) * 0.5f;
            return new Vector2(isoX, isoY).normalized;
        }

        private void OnDrawGizmos()
        {
            if (!showNoiseGizmo || stealthState == null) return;

            float currentNoise = stealthState.GetCurrentNoiseEmission(isMoving, debugStormIntensity);
            if (currentNoise <= 0f) return;

            // Filled circle (projected sphere in 2D ortho looks like a disk)
            Gizmos.color = new Color(1f, 0.92f, 0.016f, 0.15f);
            Gizmos.DrawSphere(transform.position, currentNoise);

            // Outline
            Gizmos.color = new Color(1f, 0.92f, 0.016f, 0.7f);
            Gizmos.DrawWireSphere(transform.position, currentNoise);
        }
    }
}
