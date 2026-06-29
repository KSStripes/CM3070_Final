using CM3070.PCG;
using UnityEngine;

// Patrol movement for an enemy.
// EntitySpawner configures the patrol from generated dungeon grid positions.
namespace CM3070.Dungeon1
{
    public sealed class EnemyPatrol : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 1.7f;
        [SerializeField] private float waitAtPointSeconds = 0.45f;
        [SerializeField] private int patrolTileDistance = 3;

        private Vector3 pointA;
        private Vector3 pointB;
        private Vector3 target;
        private float waitTimer;

        private void OnValidate()
        {
            // Prevent invalid patrol settings from entering play mode.
            moveSpeed = Mathf.Max(0f, moveSpeed);
            waitAtPointSeconds = Mathf.Max(0f, waitAtPointSeconds);
            patrolTileDistance = Mathf.Max(1, patrolTileDistance);
        }

        public void Configure(DungeonLayout layout, DungeonVisualizer visualizer, Vector2Int startGridPosition)
        {
            Vector2Int endGridPosition = ChoosePatrolEnd(layout, startGridPosition);
            // Convert logical grid tiles into world-space patrol endpoints.
            pointA = visualizer.GridToWorld(startGridPosition) + Vector3.up * 0.82f;
            pointB = visualizer.GridToWorld(endGridPosition) + Vector3.up * 0.82f;
            target = pointB;
        }

        private void Awake()
        {
            if (pointA == Vector3.zero && pointB == Vector3.zero)
            {
                // Fallback path supports testing the prefab outside generated dungeons.
                pointA = transform.position;
                pointB = transform.position + Vector3.forward * 2f;
                target = pointB;
            }
        }

        public void Tick()
        {
            if (waitTimer > 0f)
            {
                waitTimer -= Time.deltaTime;
                return;
            }

            transform.position = Vector3.MoveTowards(transform.position, target, moveSpeed * Time.deltaTime);
            Vector3 direction = target - transform.position;
            // Rotate only around Y so the capsule remains upright.
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            }

            if (Vector3.Distance(transform.position, target) <= 0.05f)
            {
                // Swap endpoints and pause briefly to make patrol motion readable.
                target = target == pointA ? pointB : pointA;
                waitTimer = waitAtPointSeconds;
            }
        }

        private Vector2Int ChoosePatrolEnd(DungeonLayout layout, Vector2Int start)
        {
            Vector2Int[] directions =
            {
                Vector2Int.right,
                Vector2Int.left,
                Vector2Int.up,
                Vector2Int.down
            };

            foreach (Vector2Int direction in directions)
            {
                Vector2Int candidate = start;
                // Walk in one direction until blocked or the requested patrol distance is reached.
                for (int i = 0; i < patrolTileDistance; i++)
                {
                    Vector2Int next = candidate + direction;
                    if (!layout.IsWalkable(next))
                    {
                        break;
                    }

                    candidate = next;
                }

                if (candidate != start)
                {
                    // First usable direction wins; simple and deterministic.
                    return candidate;
                }
            }

            return start;
        }
    }
}
