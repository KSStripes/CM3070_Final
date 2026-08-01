using UnityEngine;
using UnityEngine.Serialization;

// Player Resolve/health value shared by Dungeon1 and OfficeScene.
namespace CM3070.Dungeon1
{
    public sealed class HealthSystem : MonoBehaviour
    {
        public readonly struct HealthSnapshot
        {
            public HealthSnapshot(int currentHealth, int maxHealth)
            {
                CurrentHealth = currentHealth;
                MaxHealth = maxHealth;
            }

            public int CurrentHealth { get; }
            public int MaxHealth { get; }
        }

        [FormerlySerializedAs("StartingMaxHealth")]
        [SerializeField, Min(1)] private int startingMaxHealth = 100;
        [SerializeField] private int currentHealth = 100;

        private int maxHealth;

        public int MaxHealth => maxHealth;
        public int CurrentHealth => currentHealth;
        public bool IsDead => currentHealth <= 0;
        public bool IsFullHealth => currentHealth >= maxHealth;

        public HealthSnapshot CaptureSnapshot()
        {
            return new HealthSnapshot(currentHealth, maxHealth);
        }

        public void ApplySnapshot(HealthSnapshot snapshot)
        {
            maxHealth = Mathf.Max(1, snapshot.MaxHealth);
            currentHealth = Mathf.Clamp(snapshot.CurrentHealth, 0, maxHealth);
            GameManager.Instance?.NotifyHealthChanged(currentHealth, maxHealth);
        }

        private void Awake()
        {
            ResetHealth(false);
        }

        private void OnValidate()
        {
            startingMaxHealth = Mathf.Max(1, startingMaxHealth);
            currentHealth = Mathf.Clamp(currentHealth, 0, startingMaxHealth);
        }

        public bool Heal(int amount)
        {
            if (amount <= 0 || IsFullHealth)
            {
                return false;
            }

            currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
            GameManager.Instance?.NotifyHealthChanged(currentHealth, maxHealth);
            return true;
        }

        public void IncreaseMaxHealth(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            maxHealth += amount;
            currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
            GameManager.Instance?.NotifyHealthChanged(currentHealth, maxHealth);
        }

        public void TakeDamage(int amount)
        {
            if (amount <= 0 || IsDead)
            {
                return;
            }

            currentHealth = Mathf.Max(0, currentHealth - amount);
            GameManager.Instance?.NotifyHealthChanged(currentHealth, maxHealth);

            if (IsDead)
            {
                GameManager.Instance?.NotifyPlayerDied();
            }
        }

        public void ResetHealth()
        {
            ResetHealth(true);
        }

        private void ResetHealth(bool notifyGameManager)
        {
            maxHealth = startingMaxHealth;
            currentHealth = maxHealth;

            if (notifyGameManager)
            {
                GameManager.Instance?.NotifyHealthChanged(currentHealth, maxHealth);
            }
        }
    }
}
