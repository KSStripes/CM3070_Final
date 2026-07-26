using System.Collections.Generic;
using CM3070.PCG;
using UnityEngine;

namespace CM3070.Office
{
    public sealed class OfficePropPlacer : MonoBehaviour
    {
        [Header("Prop Prefabs")]
        [SerializeField] private GameObject[] propPrefabs;
        [SerializeField] private GameObject[] receptionPrefabs;
        [SerializeField] private GameObject[] bossRoomPrefabs;
        [SerializeField] private GameObject[] factoryPrefabs;
        [SerializeField] private GameObject[] officePrefabs;

        [Header("Placement")]
        [SerializeField, Range(0f, 0.2f)] private float floorDensity = 0.015f;
        [SerializeField] private int maxProps = 18;
        [SerializeField] private int minSpacing = 3;
        [SerializeField] private int markerExclusionRadius = 3;
        [SerializeField] private int propSeedOffset = 617;
        [SerializeField] private float propHeight = 0.08f;
        [SerializeField] private bool randomizeRotation = true;
        [SerializeField] private bool disablePropColliders = true;

        private readonly HashSet<Vector2Int> occupiedPositions = new();
        private OfficeRoomPlan roomPlan;

        public IReadOnlyCollection<Vector2Int> OccupiedPositions => occupiedPositions;

        public void SetRoomPlan(OfficeRoomPlan plan)
        {
            roomPlan = plan;
        }

        public void PlaceProps(DungeonLayout layout, Transform parent, float tileSize)
        {
            occupiedPositions.Clear();

            if (layout == null || parent == null || !HasAnyPrefab())
            {
                return;
            }

            OfficeRoomPlan plan = roomPlan != null && ReferenceEquals(roomPlan.Layout, layout)
                ? roomPlan
                : OfficeLayoutPlanner.CreatePlan(layout);

            List<PropCandidate> candidates = BuildCandidates(layout, plan);
            if (candidates.Count == 0)
            {
                return;
            }

            int propCount = Mathf.Min(maxProps, Mathf.RoundToInt(candidates.Count * floorDensity));
            if (propCount <= 0)
            {
                return;
            }

            System.Random random = new(layout.Seed ^ propSeedOffset);
            Shuffle(candidates, random);

            List<Vector2Int> occupied = new(propCount);
            foreach (PropCandidate candidate in candidates)
            {
                if (occupied.Count >= propCount)
                {
                    break;
                }

                if (IsTooClose(candidate.Position, occupied))
                {
                    continue;
                }

                GameObject prefab = PickPrefab(candidate.Role, random);
                if (prefab == null)
                {
                    continue;
                }

                GameObject prop = Instantiate(prefab, parent);
                prop.name = $"{prefab.name} {candidate.Role} ({candidate.Position.x}, {candidate.Position.y})";
                prop.transform.localPosition = new Vector3(candidate.Position.x * tileSize, propHeight, candidate.Position.y * tileSize);

                if (randomizeRotation)
                {
                    prop.transform.localRotation = Quaternion.Euler(0f, random.Next(0, 4) * 90f, 0f);
                }

                if (disablePropColliders)
                {
                    DisableColliders(prop);
                }

                occupied.Add(candidate.Position);
                occupiedPositions.Add(candidate.Position);
            }
        }

        private List<PropCandidate> BuildCandidates(DungeonLayout layout, OfficeRoomPlan plan)
        {
            List<PropCandidate> candidates = new();
            foreach (OfficeRoomAssignment assignment in plan.Assignments)
            {
                RectInt room = assignment.Room;
                for (int x = room.xMin + 1; x < room.xMax - 1; x++)
                {
                    for (int y = room.yMin + 1; y < room.yMax - 1; y++)
                    {
                        Vector2Int position = new(x, y);
                        if (CanPlaceAt(layout, position))
                        {
                            candidates.Add(new PropCandidate(position, assignment.Role));
                        }
                    }
                }
            }

            return candidates;
        }

        private bool CanPlaceAt(DungeonLayout layout, Vector2Int position)
        {
            return layout.IsWalkable(position)
                && !layout.IsMarker(position)
                && Vector2Int.Distance(position, layout.Start) > markerExclusionRadius
                && Vector2Int.Distance(position, layout.Exit) > markerExclusionRadius;
        }

        private GameObject PickPrefab(OfficeRoomRole role, System.Random random)
        {
            GameObject prefab = PickPrefab(RolePrefabs(role), random);
            return prefab != null ? prefab : PickPrefab(propPrefabs, random);
        }

        private GameObject[] RolePrefabs(OfficeRoomRole role)
        {
            return role switch
            {
                OfficeRoomRole.Reception => receptionPrefabs,
                OfficeRoomRole.BossRoom => bossRoomPrefabs,
                OfficeRoomRole.Factory => factoryPrefabs,
                OfficeRoomRole.Office => officePrefabs,
                _ => null
            };
        }

        private static GameObject PickPrefab(GameObject[] prefabs, System.Random random)
        {
            int validCount = 0;
            if (prefabs == null)
            {
                return null;
            }

            foreach (GameObject prefab in prefabs)
            {
                if (prefab != null)
                {
                    validCount++;
                }
            }

            if (validCount == 0)
            {
                return null;
            }

            int selected = random.Next(validCount);
            foreach (GameObject prefab in prefabs)
            {
                if (prefab == null)
                {
                    continue;
                }

                if (selected == 0)
                {
                    return prefab;
                }

                selected--;
            }

            return null;
        }

        private bool IsTooClose(Vector2Int position, List<Vector2Int> occupied)
        {
            int minDistance = minSpacing * minSpacing;
            foreach (Vector2Int other in occupied)
            {
                if ((position - other).sqrMagnitude < minDistance)
                {
                    return true;
                }
            }

            return false;
        }

        private void OnValidate()
        {
            floorDensity = Mathf.Max(0f, floorDensity);
            maxProps = Mathf.Max(0, maxProps);
            minSpacing = Mathf.Max(0, minSpacing);
            markerExclusionRadius = Mathf.Max(0, markerExclusionRadius);
        }

        private bool HasAnyPrefab()
        {
            return HasAnyPrefab(propPrefabs)
                || HasAnyPrefab(receptionPrefabs)
                || HasAnyPrefab(bossRoomPrefabs)
                || HasAnyPrefab(factoryPrefabs)
                || HasAnyPrefab(officePrefabs);
        }

        private static bool HasAnyPrefab(GameObject[] prefabs)
        {
            if (prefabs == null)
            {
                return false;
            }

            foreach (GameObject prefab in prefabs)
            {
                if (prefab != null)
                {
                    return true;
                }
            }

            return false;
        }

        private static void Shuffle<T>(IList<T> items, System.Random random)
        {
            for (int i = items.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                (items[i], items[j]) = (items[j], items[i]);
            }
        }

        private static void DisableColliders(GameObject prop)
        {
            foreach (Collider collider in prop.GetComponentsInChildren<Collider>())
            {
                collider.enabled = false;
            }
        }

        private readonly struct PropCandidate
        {
            public PropCandidate(Vector2Int position, OfficeRoomRole role)
            {
                Position = position;
                Role = role;
            }

            public Vector2Int Position { get; }
            public OfficeRoomRole Role { get; }
        }
    }
}
