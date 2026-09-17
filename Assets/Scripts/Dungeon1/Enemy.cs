// File: Dungeon1/Enemy.cs
// Purpose: Dungeon1 enemy state coordinator.
// Inputs: Generated layout configuration, visualizer grid conversion, player HealthSystem, and EnemyAttack/EnemyPatrol components.
// Output/side effects: Switches between patrol and attack behaviour for the original dungeon reference scene.

using System.Collections.Generic;
using CM3070.PCG;
using UnityEngine;

// Enemy state coordinator.
namespace CM3070.Dungeon1
{
    public enum EnemyState
    {
        // Patrol and Attack are the active states in this prototype.
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
            // Cache state components.
            patrol = GetComponent<EnemyPatrol>();
            attack = GetComponent<EnemyAttack>();
        }

        public void Configure(
            DungeonLayout layout,
            DungeonVisualizer visualizer,
            Vector2Int startGridPosition,
            IReadOnlyCollection<Vector2Int> blockedPositions = null)
        {
            // Pass PCG context to the movement component.
            patrol.Configure(layout, visualizer, startGridPosition, blockedPositions);
        }

        private void Update()
        {
            EnsurePlayerHealth();

            // Minimal state rule: close enough to the player means attack, otherwise patrol.
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

            // Find health directly so Dungeon and Office players can both be attacked.
            playerHealth = FindFirstObjectByType<HealthSystem>();
        }
    }
}
