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

        private CharacterController characterController;
        private float verticalVelocity;

        public void Configure(float speed)
        {
            // EntitySpawner can override prefab speed at spawn time.
            moveSpeed = speed;
        }

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
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
            Vector3 velocity = move * moveSpeed + Vector3.up * verticalVelocity;
            characterController.Move(velocity * Time.deltaTime);
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
