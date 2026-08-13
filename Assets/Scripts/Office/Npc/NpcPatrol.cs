using System.Collections.Generic;
using CM3070.Dungeon1;
using CM3070.PCG;
using UnityEngine;

// Lightweight generated-route patrol for office NPCs.
namespace CM3070.Office
{
    public enum NpcPatrolType
    {
        LongLine,
        Wander
    }

    public sealed class NpcPatrol : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 1.7f;
        [SerializeField] private Vector2 waitSeconds = new(0.8f, 2.2f);
        [SerializeField] private NpcPatrolType patrolType = NpcPatrolType.LongLine;
        [SerializeField] private int tileDistance = 5;
        [SerializeField] private int wanderPoints = 6;
        [SerializeField, Range(0f, 180f)] private float idleTurnDegrees = 70f;

        private readonly List<Vector3> points = new();
        private int targetIndex;
        private float waitTimer;
        private float speedMultiplier = 1f;
        private Quaternion idleRotation;

        private void OnValidate()
        {
            moveSpeed = Mathf.Max(0f, moveSpeed);
            waitSeconds.x = Mathf.Max(0f, waitSeconds.x);
            waitSeconds.y = Mathf.Max(waitSeconds.x, waitSeconds.y);
            tileDistance = Mathf.Max(1, tileDistance);
            wanderPoints = Mathf.Clamp(wanderPoints, 2, 10);
        }

        private void Awake()
        {
            if (points.Count == 0)
            {
                points.Add(transform.position);
            }
        }

        public void Configure(
            DungeonLayout layout,
            DungeonVisualizer visualizer,
            Vector2Int start,
            IReadOnlyCollection<Vector2Int> blockedPositions = null,
            NpcPatrolType type = NpcPatrolType.LongLine)
        {
            patrolType = type;
            points.Clear();
            targetIndex = 0;
            speedMultiplier = SpeedMultiplier(start, type);

            // Routes are built from PCG grid tiles so NPCs can avoid props and reserved spaces.
            foreach (Vector2Int point in BuildGridPoints(layout, start, blockedPositions))
            {
                points.Add(visualizer.GridToWorld(point) + Vector3.up * 0.82f);
            }

            if (points.Count == 0)
            {
                points.Add(visualizer.GridToWorld(start) + Vector3.up * 0.82f);
            }

            transform.position = points[0];
            targetIndex = points.Count > 1 ? 1 : 0;
        }

        public void Tick()
        {
            if (points.Count <= 1)
            {
                return;
            }

            if (waitTimer > 0f)
            {
                waitTimer -= Time.deltaTime;
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation,
                    idleRotation,
                    idleTurnDegrees * Time.deltaTime);
                return;
            }

            Vector3 target = points[targetIndex];
            transform.position = Vector3.MoveTowards(transform.position, target, moveSpeed * speedMultiplier * Time.deltaTime);

            Vector3 direction = target - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            }

            if (Vector3.Distance(transform.position, target) <= 0.05f)
            {
                targetIndex = (targetIndex + 1) % points.Count;
                StartWait();
            }
        }

        private void StartWait()
        {
            // Small deterministic pauses make patrols feel less mechanical without random runtime state.
            float seed = Mathf.Abs(transform.position.x * 0.37f + transform.position.z * 0.61f + targetIndex);
            waitTimer = Mathf.Lerp(waitSeconds.x, waitSeconds.y, Mathf.Repeat(seed, 1f));
            float turn = Mathf.Lerp(-idleTurnDegrees, idleTurnDegrees, Mathf.Repeat(seed * 1.7f, 1f));
            idleRotation = Quaternion.Euler(0f, transform.eulerAngles.y + turn, 0f);
        }

        private List<Vector2Int> BuildGridPoints(
            DungeonLayout layout,
            Vector2Int start,
            IReadOnlyCollection<Vector2Int> blockedPositions)
        {
            return patrolType switch
            {
                NpcPatrolType.Wander => BuildWander(layout, start, blockedPositions),
                _ => BuildLine(layout, start, blockedPositions, tileDistance * 2)
            };
        }

        private List<Vector2Int> BuildLine(
            DungeonLayout layout,
            Vector2Int start,
            IReadOnlyCollection<Vector2Int> blockedPositions,
            int distance)
        {
            Vector2Int end = ChooseEnd(layout, start, blockedPositions, distance);
            return end == start
                ? new List<Vector2Int> { start }
                : new List<Vector2Int> { start, end };
        }

        private static float SpeedMultiplier(Vector2Int start, NpcPatrolType type)
        {
            float routeSpeed = type switch
            {
                NpcPatrolType.Wander => 0.82f,
                _ => 1f
            };

            float variation = 0.9f + 0.2f * Mathf.Repeat((start.x * 13 + start.y * 7) * 0.17f, 1f);
            return routeSpeed * variation;
        }

        private List<Vector2Int> BuildWander(
            DungeonLayout layout,
            Vector2Int start,
            IReadOnlyCollection<Vector2Int> blockedPositions)
        {
            // Wander picks several straight reachable ends around the start tile, then returns through the start.
            List<Vector2Int> result = new() { start };

            foreach (Vector2Int direction in OrderedDirections(start))
            {
                Vector2Int end = ChooseEndInDirection(layout, start, blockedPositions, direction, tileDistance * 2);
                if (end != start)
                {
                    result.Add(end);
                    result.Add(start);
                }

                if (result.Count >= wanderPoints)
                {
                    break;
                }
            }

            return result.Count > 1 ? result : BuildLine(layout, start, blockedPositions, tileDistance);
        }

        private Vector2Int ChooseEnd(
            DungeonLayout layout,
            Vector2Int start,
            IReadOnlyCollection<Vector2Int> blockedPositions,
            int distance)
        {
            foreach (Vector2Int direction in OrderedDirections(start))
            {
                Vector2Int candidate = ChooseEndInDirection(layout, start, blockedPositions, direction, distance);
                if (candidate != start)
                {
                    return candidate;
                }
            }

            return start;
        }

        private static Vector2Int ChooseEndInDirection(
            DungeonLayout layout,
            Vector2Int start,
            IReadOnlyCollection<Vector2Int> blockedPositions,
            Vector2Int direction,
            int distance)
        {
            Vector2Int candidate = start;
            for (int i = 0; i < distance; i++)
            {
                Vector2Int next = candidate + direction;
                if (!CanStandAt(layout, next, blockedPositions))
                {
                    break;
                }

                candidate = next;
            }

            return candidate;
        }

        private static Vector2Int[] OrderedDirections(Vector2Int start)
        {
            Vector2Int[] directions =
            {
                Vector2Int.right,
                Vector2Int.up,
                Vector2Int.left,
                Vector2Int.down
            };

            int offset = Mathf.Abs(start.x + start.y) % directions.Length;
            for (int i = 0; i < offset; i++)
            {
                (directions[0], directions[1], directions[2], directions[3]) =
                    (directions[1], directions[2], directions[3], directions[0]);
            }

            return directions;
        }

        private static bool CanStandAt(
            DungeonLayout layout,
            Vector2Int position,
            IReadOnlyCollection<Vector2Int> blockedPositions)
        {
            return layout.IsWalkable(position) && !IsBlocked(position, blockedPositions);
        }

        private static bool IsBlocked(Vector2Int position, IReadOnlyCollection<Vector2Int> blockedPositions)
        {
            if (blockedPositions == null)
            {
                return false;
            }

            foreach (Vector2Int blockedPosition in blockedPositions)
            {
                if (blockedPosition == position)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
