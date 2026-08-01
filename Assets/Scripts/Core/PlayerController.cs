using UnityEngine;
using UnityEngine.InputSystem;

// Simple player movement using the new Input System keyboard state.
// Movement is world-axis based because this feels correct with the current isometric camera.
namespace CM3070.Dungeon1
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerController : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 5.5f;
        [SerializeField] private float gravity = -20f;

        [Header("Low Resolve Movement")]
        [SerializeField] private bool slowOnLowResolve = true;
        [SerializeField, Range(0f, 1f)] private float lowResolveAt = 0.3f;
        [SerializeField, Range(0.1f, 1f)] private float lowResolveSpeed = 0.75f;
        [SerializeField, Range(0f, 1f)] private float criticalResolveAt = 0.15f;
        [SerializeField, Range(0.1f, 1f)] private float criticalResolveSpeed = 0.6f;

        private CharacterController characterController;
        private HealthSystem health;
        private float verticalVelocity;

        public void Configure(float speed)
        {
            // EntitySpawner can override prefab speed at spawn time.
            moveSpeed = speed;
        }

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            health = GetComponent<HealthSystem>();
        }

        private void OnValidate()
        {
            moveSpeed = Mathf.Max(0f, moveSpeed);
            gravity = Mathf.Min(0f, gravity);
            criticalResolveAt = Mathf.Min(criticalResolveAt, lowResolveAt);
            criticalResolveSpeed = Mathf.Min(criticalResolveSpeed, lowResolveSpeed);
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            Vector2 input = Vector2.zero;
            // WASD and arrow keys map directly to world X/Z movement.
            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) input.y += 1f;
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) input.y -= 1f;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) input.x += 1f;
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) input.x -= 1f;

            Vector3 move = GetMove(input);
            if (move.sqrMagnitude > 0.001f)
            {
                // Face the direction of travel for simple visual feedback.
                transform.rotation = Quaternion.LookRotation(move, Vector3.up);
            }

            if (characterController.isGrounded && verticalVelocity < 0f)
            {
                // Small downward force keeps CharacterController grounded.
                verticalVelocity = -1f;
            }

            verticalVelocity += gravity * Time.deltaTime;
            Vector3 velocity = move * CurrentMoveSpeed() + Vector3.up * verticalVelocity;
            characterController.Move(velocity * Time.deltaTime);
        }

        private float CurrentMoveSpeed()
        {
            // Low Resolve makes movement heavier without changing input handling.
            if (!slowOnLowResolve || health == null || health.MaxHealth <= 0)
            {
                return moveSpeed;
            }

            float resolveRatio = health.CurrentHealth / (float)health.MaxHealth;
            if (resolveRatio <= criticalResolveAt)
            {
                return moveSpeed * criticalResolveSpeed;
            }

            if (resolveRatio <= lowResolveAt)
            {
                return moveSpeed * lowResolveSpeed;
            }

            return moveSpeed;
        }

        private static Vector3 GetMove(Vector2 input)
        {
            if (input.sqrMagnitude > 1f)
            {
                // Prevent diagonal movement from being faster than cardinal movement.
                input.Normalize();
            }

            return new Vector3(input.x, 0f, input.y);
        }
    }
}
