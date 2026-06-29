using CM3070.PCG;
using UnityEngine;

// Enemy state coordinator.
namespace CM3070.Dungeon1
{
    public enum EnemyState
    {
        // Only Patrol and Attack are active now;
        Patrol, 
        Attack,
        Chase,
        ReturnToPatrol
    }

    [RequireComponent(typeof(EnemyPatrol))]
    [RequireComponent(typeof(EnemyAttack))]
    public sealed class Enemy : MonoBehaviour
    {
        [SerializeField] private EnemyState currentState = EnemyState.Patrol;

        private EnemyPatrol patrol;
        private EnemyAttack attack;
        private HealthSystem playerHealth;

        public EnemyState CurrentState => currentState;

        private void Awake()
        {
            // Get refs to enemy states
            patrol = GetComponent<EnemyPatrol>();
            attack = GetComponent<EnemyAttack>();
        }

        public void Configure(DungeonLayout layout, DungeonVisualizer visualizer, Vector2Int startGridPosition)
        {
            // Pass PCG context to the movement component.
            patrol.Configure(layout, visualizer, startGridPosition);
        }

        private void Update()
        {
            EnsurePlayerHealth();

            // Minimal state rule for now: close enough to the player means attack, otherwise patrol.
            if (playerHealth != null && !playerHealth.IsDead && attack.IsInRange(playerHealth.transform))
            {
                currentState = EnemyState.Attack;
                // Attack owns damage timing and visual feedback.
                attack.Tick(playerHealth);
            }
            else
            {
                currentState = EnemyState.Patrol;
                // Patrol owns movement between generated points.
                patrol.Tick();
            }

            // Pulse cleanup after the attack frame ends.
            attack.UpdatePulse();
        }

        private void EnsurePlayerHealth()
        {
            if (playerHealth != null)
            {
                return;
            }

            // Find player health system. 
            PlayerInventory player = FindFirstObjectByType<PlayerInventory>();
            playerHealth = player != null ? player.GetComponent<HealthSystem>() : null;
        }
    }
}
