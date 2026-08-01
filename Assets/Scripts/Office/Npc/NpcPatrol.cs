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
        Square,
        Wander
    }

    public sealed class NpcPatrol : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 1.7f;
        [SerializeField] private float waitSeconds = 0.45f;
        [SerializeField] private NpcPatrolType patrolType = NpcPatrolType.LongLine;
        [SerializeField] private int tileDistance = 3;
        [SerializeField] private int wanderPoints = 4;

        private readonly List<Vector3> points = new();
        private int targetIndex;
        private float waitTimer;

        private void OnValidate()
        {
            moveSpeed = Mathf.Max(0f, moveSpeed);
            waitSeconds = Mathf.Max(0f, waitSeconds);
            tileDistance = Mathf.Max(1, tileDistance);
            wanderPoints = Mathf.Clamp(wanderPoints, 2, 8);
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
                return;
            }

            Vector3 target = points[targetIndex];
            transform.position = Vector3.MoveTowards(transform.position, target, moveSpeed * Time.deltaTime);

            Vector3 direction = target - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            }

            if (Vector3.Distance(transform.position, target) <= 0.05f)
            {
                targetIndex = (targetIndex + 1) % points.Count;
                waitTimer = waitSeconds;
            }
        }

        private List<Vector2Int> BuildGridPoints(
            DungeonLayout layout,
            Vector2Int start,
            IReadOnlyCollection<Vector2Int> blockedPositions)
        {
            return patrolType switch
            {
                NpcPatrolType.Square => BuildSquare(layout, start, blockedPositions),
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

        private List<Vector2Int> BuildSquare(
            DungeonLayout layout,
            Vector2Int start,
            IReadOnlyCollection<Vector2Int> blockedPositions)
        {
            Vector2Int[] directions = OrderedDirections(start);
            int distance = Mathf.Max(1, tileDistance);

            for (int i = 0; i < directions.Length; i++)
            {
                Vector2Int firstDirection = directions[i];
                Vector2Int secondDirection = directions[(i + 1) % directions.Length];
                Vector2Int cornerA = start + firstDirection * distance;
                Vector2Int cornerB = cornerA + secondDirection * distance;
                Vector2Int cornerC = start + secondDirection * distance;

                if (CanUsePath(layout, start, cornerA, firstDirection, blockedPositions)
                    && CanUsePath(layout, cornerA, cornerB, secondDirection, blockedPositions)
                    && CanUsePath(layout, cornerB, cornerC, -firstDirection, blockedPositions)
                    && CanUsePath(layout, cornerC, start, -secondDirection, blockedPositions))
                {
                    return new List<Vector2Int> { start, cornerA, cornerB, cornerC };
                }
            }

            return BuildLine(layout, start, blockedPositions, distance);
        }

        private List<Vector2Int> BuildWander(
            DungeonLayout layout,
            Vector2Int start,
            IReadOnlyCollection<Vector2Int> blockedPositions)
        {
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

        private static bool CanUsePath(
            DungeonLayout layout,
            Vector2Int from,
            Vector2Int to,
            Vector2Int direction,
            IReadOnlyCollection<Vector2Int> blockedPositions)
        {
            Vector2Int current = from;
            while (current != to)
            {
                current += direction;
                if (!CanStandAt(layout, current, blockedPositions))
                {
                    return false;
                }
            }

            return true;
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
