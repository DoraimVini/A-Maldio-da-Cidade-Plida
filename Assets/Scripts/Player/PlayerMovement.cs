using System;
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
    /// Pure C# representation of the Player's Stealth and Movement state.
    /// Follows clean architecture by separating logic from Unity's MonoBehaviour.
    /// </summary>
    public class PlayerStealthState
    {
        public MovementMode CurrentMode { get; private set; } = MovementMode.Walking;
        
        public float Speed { get; private set; }
        public float NoiseRadius { get; private set; }

        // Configuration values (can be injected)
        private readonly float sneakSpeed = 2.0f;
        private readonly float sneakNoise = 1.0f;

        private readonly float walkSpeed = 4.5f;
        private readonly float walkNoise = 4.0f;

        private readonly float runSpeed = 7.5f;
        private readonly float runNoise = 8.5f;

        public PlayerStealthState()
        {
            UpdateStats();
        }

        public void SetMode(MovementMode mode)
        {
            CurrentMode = mode;
            UpdateStats();
        }

        private void UpdateStats()
        {
            switch (CurrentMode)
            {
                case MovementMode.Sneaking:
                    Speed = sneakSpeed;
                    NoiseRadius = sneakNoise;
                    break;
                case MovementMode.Walking:
                    Speed = walkSpeed;
                    NoiseRadius = walkNoise;
                    break;
                case MovementMode.Running:
                    Speed = runSpeed;
                    NoiseRadius = runNoise;
                    break;
            }
        }

        /// <summary>
        /// Calculates the dynamic noise radius, e.g. if the player is not moving, the noise is 0.
        /// </summary>
        public float GetCurrentNoiseEmission(bool isMoving, float stormIntensity)
        {
            if (!isMoving) return 0f;

            // Storm acts as "white noise" and dampens the propagation of the player's sound.
            // Under peak storm, the noise propagates less.
            float dampening = 1.0f - Mathf.Clamp01(stormIntensity * 0.6f);
            return NoiseRadius * dampening;
        }
    }

    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerMovement : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private bool useIsometricGridAlignment = true;

        [Header("Debug")]
        [SerializeField] private bool showNoiseGizmo = true;
        [Range(0f, 1f)] [SerializeField] private float debugStormIntensity = 0f;

        private Rigidbody2D rb;
        private PlayerStealthState stealthState;
        private Vector2 inputDirection;
        private bool isMoving;

        public PlayerStealthState StealthState => stealthState;
        public bool IsMoving => isMoving;

        // Action references (assuming Input System is configured)
        private PlayerInput playerInput;
        private InputAction moveAction;
        private InputAction sneakAction;
        private InputAction runAction;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.freezeRotation = true;

            stealthState = new PlayerStealthState();
            playerInput = GetComponent<PlayerInput>();

            if (playerInput != null)
            {
                moveAction = playerInput.actions["Move"];
                sneakAction = playerInput.actions["Sneak"]; // Map to Shift/Button
                runAction = playerInput.actions["Run"];     // Map to Space/Button
            }
        }

        private void Update()
        {
            // Read input direction
            if (moveAction != null)
            {
                inputDirection = moveAction.ReadValue<Vector2>();
            }
            else
            {
                // Fallback to legacy inputs for safety during blockout
                inputDirection = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            }

            isMoving = inputDirection.sqrMagnitude > 0.01f;

            // Determine movement mode based on inputs
            DetermineMovementMode();
        }

        private void FixedUpdate()
        {
            MovePlayer();
        }

        private void DetermineMovementMode()
        {
            if (sneakAction != null && sneakAction.IsPressed())
            {
                stealthState.SetMode(MovementMode.Sneaking);
            }
            else if (runAction != null && runAction.IsPressed())
            {
                stealthState.SetMode(MovementMode.Running);
            }
            else
            {
                // Keyboard legacy fallbacks
                if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.C))
                    stealthState.SetMode(MovementMode.Sneaking);
                else if (Input.GetKey(KeyCode.LeftShift))
                    stealthState.SetMode(MovementMode.Running);
                else
                    stealthState.SetMode(MovementMode.Walking);
            }
        }

        private void MovePlayer()
        {
            if (!isMoving)
            {
                rb.linearVelocity = Vector2.zero;
                return;
            }

            Vector2 movement = inputDirection.normalized;

            if (useIsometricGridAlignment)
            {
                // Convert orthographic input to isometric movement
                // In 2D isometric, X_iso = X - Y, Y_iso = (X + Y) / 2
                movement = ConvertToIsometric(movement);
            }

            rb.linearVelocity = movement * stealthState.Speed;
        }

        /// <summary>
        /// Converts a 2D orthographic direction vector to 2D isometric coordinates.
        /// </summary>
        private Vector2 ConvertToIsometric(Vector2 orthoVector)
        {
            // Standard Isometric projection matrix mapping
            float isoX = orthoVector.x - orthoVector.y;
            float isoY = (orthoVector.x + orthoVector.y) * 0.5f;
            return new Vector2(isoX, isoY).normalized;
        }

        private void OnDrawGizmos()
        {
            if (!showNoiseGizmo || stealthState == null) return;

            // Draw current sound propagation radius
            float currentNoise = stealthState.GetCurrentNoiseEmission(isMoving, debugStormIntensity);
            
            if (currentNoise > 0f)
            {
                Gizmos.color = new Color(1f, 0.92f, 0.016f, 0.15f); // Carcosa Yellow semi-transparent
                Gizmos.DrawSolidDisk(transform.position, Vector3.forward, currentNoise);
                
                Gizmos.color = new Color(1f, 0.92f, 0.016f, 0.7f);
                Gizmos.DrawWireDisk(transform.position, Vector3.forward, currentNoise);
            }
        }
    }
}
