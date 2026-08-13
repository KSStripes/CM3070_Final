using CM3070.Dungeon1;
using UnityEngine;

// Applies Resolve pressure when the player is close to an NPC.
namespace CM3070.Office
{
    public sealed class NpcPressure : MonoBehaviour
    {
        [SerializeField] private int damage = 10;
        [SerializeField] private float range = 1.25f;
        [SerializeField] private float interval = 1f;
        [SerializeField] private float pulseScale = 1.18f;
        [SerializeField] private float pulseSeconds = 0.12f;
        [SerializeField] private string[] pressureLines;

        private Vector3 baseScale;
        private float timer;
        private float pulseTimer;
        private OfficeHUD hud;

        private void Awake()
        {
            baseScale = transform.localScale;
        }

        private void OnValidate()
        {
            damage = Mathf.Max(0, damage);
            range = Mathf.Max(0f, range);
            interval = Mathf.Max(0.1f, interval);
            pulseScale = Mathf.Max(1f, pulseScale);
            pulseSeconds = Mathf.Max(0f, pulseSeconds);
        }

        public bool IsInRange(Transform target)
        {
            return target != null && Vector3.Distance(transform.position, target.position) <= range;
        }

        public void Tick(HealthSystem playerHealth)
        {
            if (playerHealth == null || playerHealth.IsDead)
            {
                return;
            }

            Face(playerHealth.transform);

            if (timer > 0f)
            {
                timer -= Time.deltaTime;
                return;
            }

            playerHealth.TakeDamage(damage);
            ShowPressureLine();
            timer = interval;
            pulseTimer = pulseSeconds;
        }

        public void UpdatePulse()
        {
            if (pulseTimer <= 0f)
            {
                transform.localScale = baseScale;
                return;
            }

            pulseTimer -= Time.deltaTime;
            transform.localScale = baseScale * pulseScale;
        }

        private void Face(Transform target)
        {
            Vector3 direction = target.position - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            }
        }

        private void ShowPressureLine()
        {
            if (pressureLines == null || pressureLines.Length == 0)
            {
                return;
            }

            hud ??= FindFirstObjectByType<OfficeHUD>();
            hud?.ShowFeedback(pressureLines[Random.Range(0, pressureLines.Length)]);
        }
    }
}
