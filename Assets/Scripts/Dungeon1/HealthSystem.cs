using UnityEngine;
using UnityEngine.Serialization;

// Health component for the player.
// Clamps healing/damage, tracks death state, and notifies GameManager when health changes.
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
            // Start each fresh player from the Inspector-defined base health.
            ResetHealth(false);
        }

        private void OnValidate()
        {
            // Inspector safety; base health must be positive.
            startingMaxHealth = Mathf.Max(1, startingMaxHealth);
            currentHealth = Mathf.Clamp(currentHealth, 0, startingMaxHealth);
        }

        public bool Heal(int amount)
        {
            // Health pickups replenish current health only; they do not increase max health.
            if (amount <= 0 || IsFullHealth)
            {
                return false;
            }

            currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
            // GameManager is the future UI notification path.
            GameManager.Instance?.NotifyHealthChanged(currentHealth, maxHealth);
            return true;
        }

        public void IncreaseMaxHealth(int amount)
        {
            // Currently used by armour pickups; this may become armour-specific later.
            if (amount <= 0)
            {
                return;
            }

            maxHealth += amount;
            // Increasing max health should not overfill current health here.
            currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
            GameManager.Instance?.NotifyHealthChanged(currentHealth, maxHealth);
        }

        public void TakeDamage(int amount)
        {
            // Ignore further damage once dead so death notification only happens once.
            if (amount <= 0 || IsDead)
            {
                return;
            }

            currentHealth = Mathf.Max(0, currentHealth - amount);
            // Damage, enemy attacks, and later hazards all share this notification.
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
            // New Game removes armour bonuses and restores full starting health.
            maxHealth = startingMaxHealth;
            currentHealth = maxHealth;

            if (notifyGameManager)
            {
                GameManager.Instance?.NotifyHealthChanged(currentHealth, maxHealth);
            }
        }
    }
}
