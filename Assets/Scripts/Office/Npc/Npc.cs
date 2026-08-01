using System.Collections.Generic;
using CM3070.Dungeon1;
using CM3070.PCG;
using UnityEngine;

// Office NPC state coordinator.
namespace CM3070.Office
{
    public enum NpcState
    {
        Patrol,
        Pressure,
        Idle,
        Interact,
        Helpful
    }

    [RequireComponent(typeof(NpcPatrol))]
    [RequireComponent(typeof(NpcPressure))]
    public sealed class Npc : MonoBehaviour
    {
        [SerializeField] private NpcState state = NpcState.Patrol;

        private NpcPatrol patrol;
        private NpcPressure pressure;
        private HealthSystem playerHealth;

        public NpcState State => state;

        private void Awake()
        {
            patrol = GetComponent<NpcPatrol>();
            pressure = GetComponent<NpcPressure>();
        }

        public void Configure(
            DungeonLayout layout,
            DungeonVisualizer visualizer,
            Vector2Int start,
            IReadOnlyCollection<Vector2Int> blockedPositions = null)
        {
            patrol.Configure(layout, visualizer, start, blockedPositions);
        }

        private void Update()
        {
            EnsurePlayerHealth();

            if (playerHealth != null && !playerHealth.IsDead && pressure.IsInRange(playerHealth.transform))
            {
                state = NpcState.Pressure;
                pressure.Tick(playerHealth);
            }
            else
            {
                state = NpcState.Patrol;
                patrol.Tick();
            }

            pressure.UpdatePulse();
        }

        private void EnsurePlayerHealth()
        {
            if (playerHealth != null)
            {
                return;
            }

            playerHealth = FindFirstObjectByType<HealthSystem>();
        }
    }
}
