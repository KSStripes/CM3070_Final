using System.Collections.Generic;
using CM3070.Dungeon1;
using CM3070.PCG;
using UnityEngine;

// Lightweight generated-route patrol for office NPCs.
namespace CM3070.Office
{
    public sealed class NpcPatrol : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 1.7f;
        [SerializeField] private float waitSeconds = 0.45f;
        [SerializeField] private int tileDistance = 3;

        private Vector3 pointA;
        private Vector3 pointB;
        private Vector3 target;
        private float waitTimer;

        private void OnValidate()
        {
            moveSpeed = Mathf.Max(0f, moveSpeed);
            waitSeconds = Mathf.Max(0f, waitSeconds);
            tileDistance = Mathf.Max(1, tileDistance);
        }

        private void Awake()
        {
            if (pointA == Vector3.zero && pointB == Vector3.zero)
            {
                pointA = transform.position;
                pointB = transform.position + Vector3.forward * 2f;
                target = pointB;
            }
        }

        public void Configure(
            DungeonLayout layout,
            DungeonVisualizer visualizer,
            Vector2Int start,
            IReadOnlyCollection<Vector2Int> blockedPositions = null)
        {
            Vector2Int end = ChooseEnd(layout, start, blockedPositions);
            pointA = visualizer.GridToWorld(start) + Vector3.up * 0.82f;
            pointB = visualizer.GridToWorld(end) + Vector3.up * 0.82f;
            target = pointB;
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
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            }

            if (Vector3.Distance(transform.position, target) <= 0.05f)
            {
                target = target == pointA ? pointB : pointA;
                waitTimer = waitSeconds;
            }
        }

        private Vector2Int ChooseEnd(
            DungeonLayout layout,
            Vector2Int start,
            IReadOnlyCollection<Vector2Int> blockedPositions)
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
                for (int i = 0; i < tileDistance; i++)
                {
                    Vector2Int next = candidate + direction;
                    if (!layout.IsWalkable(next) || IsBlocked(next, blockedPositions))
                    {
                        break;
                    }

                    candidate = next;
                }

                if (candidate != start)
                {
                    return candidate;
                }
            }

            return start;
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
