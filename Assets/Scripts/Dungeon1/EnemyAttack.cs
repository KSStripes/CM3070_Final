using UnityEngine;

// Handles enemy attack timing, damage, facing, and a simple visual pulse.
// Damage is routed through HealthSystem so GameManager/UI notifications stay centralized.
namespace CM3070.Dungeon1
{
    public sealed class EnemyAttack : MonoBehaviour
    {
        [SerializeField] private int attackDamage = 10;
        [SerializeField] private float attackRange = 1.25f;
        [SerializeField] private float attackInterval = 1f;
        [SerializeField] private float attackPulseScale = 1.18f;
        [SerializeField] private float attackPulseSeconds = 0.12f;

        private Vector3 baseScale;
        private float attackTimer;
        private float pulseTimer;

        private void Awake()
        {
            // Store prefab scale so the pulse can safely return to normal.
            baseScale = transform.localScale;
        }

        private void OnValidate()
        {
            // Keep attack tuning valid when edited in the Inspector.
            attackDamage = Mathf.Max(0, attackDamage);
            attackRange = Mathf.Max(0f, attackRange);
            attackInterval = Mathf.Max(0.1f, attackInterval);
            attackPulseScale = Mathf.Max(1f, attackPulseScale);
            attackPulseSeconds = Mathf.Max(0f, attackPulseSeconds);
        }

        public bool IsInRange(Transform target)
        {
            // Range check is intentionally simple until chase/pathfinding exists.
            return target != null && Vector3.Distance(transform.position, target.position) <= attackRange;
        }

        public void Tick(HealthSystem playerHealth)
        {
            if (playerHealth == null || playerHealth.IsDead)
            {
                return;
            }

            FaceTarget(playerHealth.transform);

            if (attackTimer > 0f)
            {
                // One enemy can only damage the player once per interval.
                attackTimer -= Time.deltaTime;
                return;
            }

            playerHealth.TakeDamage(attackDamage);
            attackTimer = attackInterval;
            // Trigger a visible placeholder attack cue.
            pulseTimer = attackPulseSeconds;
        }

        public void UpdatePulse()
        {
            if (pulseTimer <= 0f)
            {
                transform.localScale = baseScale;
                return;
            }

            // Lightweight placeholder feedback until real attack animation/FX are added.
            pulseTimer -= Time.deltaTime;
            transform.localScale = baseScale * attackPulseScale;
        }

        private void FaceTarget(Transform target)
        {
            Vector3 direction = target.position - transform.position;
            // Keep rotation horizontal; enemies should not tilt up/down.
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            }
        }
    }
}
