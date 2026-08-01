using UnityEngine;

namespace CM3070.Office
{
    public sealed class MovementAnimator : MonoBehaviour
    {
        [SerializeField] private string speedParameter = "Speed";
        [SerializeField] private float movingThreshold = 0.05f;
        [SerializeField] private float movingSpeedValue = 1f;
        [SerializeField] private float stoppedSpeedValue = 0f;
        [SerializeField] private float dampingTime = 0.08f;

        private Vector3 previousPosition;
        private int speedParameterId;

        private void Awake()
        {
            speedParameterId = Animator.StringToHash(speedParameter);
            previousPosition = transform.position;
        }

        private void Update()
        {
            Vector3 movement = transform.position - previousPosition;
            movement.y = 0f;
            previousPosition = transform.position;

            float targetSpeed = movement.magnitude / Mathf.Max(Time.deltaTime, 0.0001f) > movingThreshold
                ? movingSpeedValue
                : stoppedSpeedValue;

            Animator animator = ActiveAnimator();
            if (animator != null)
            {
                // Only changes the animation state; gameplay speed stays in PlayerController.
                animator.SetFloat(speedParameterId, targetSpeed, dampingTime, Time.deltaTime);
            }
        }

        private Animator ActiveAnimator()
        {
            Animator[] animators = GetComponentsInChildren<Animator>();
            foreach (Animator animator in animators)
            {
                if (animator.isActiveAndEnabled)
                {
                    return animator;
                }
            }

            return null;
        }
    }
}
