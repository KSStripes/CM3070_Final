using System.Collections.Generic;
using CM3070.PCG;
using UnityEngine;

namespace CM3070.Office
{
    public sealed class OfficePropPlacer : MonoBehaviour
    {
        [Header("Prop Prefabs")]
        [SerializeField] private GameObject[] propPrefabs;

        [Header("Placement")]
        [SerializeField, Range(0f, 0.2f)] private float floorDensity = 0.015f;
        [SerializeField] private int maxProps = 18;
        [SerializeField] private int minSpacing = 3;
        [SerializeField] private int markerExclusionRadius = 3;
        [SerializeField] private int propSeedOffset = 617;
        [SerializeField] private float propHeight = 0.08f;
        [SerializeField] private bool randomizeRotation = true;
        [SerializeField] private bool disablePropColliders = true;

        public void PlaceProps(DungeonLayout layout, Transform parent, float tileSize)
        {
            if (layout == null || parent == null || propPrefabs == null || propPrefabs.Length == 0)
            {
                return;
            }

            List<Vector2Int> candidates = BuildCandidates(layout);
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
            foreach (Vector2Int position in candidates)
            {
                if (occupied.Count >= propCount)
                {
                    break;
                }

                if (IsTooClose(position, occupied))
                {
                    continue;
                }

                GameObject prefab = PickPrefab(random);
                if (prefab == null)
                {
                    continue;
                }

                GameObject prop = Instantiate(prefab, parent);
                prop.name = $"{prefab.name} ({position.x}, {position.y})";
                prop.transform.localPosition = new Vector3(position.x * tileSize, propHeight, position.y * tileSize);

                if (randomizeRotation)
                {
                    prop.transform.localRotation = Quaternion.Euler(0f, random.Next(0, 4) * 90f, 0f);
                }

                if (disablePropColliders)
                {
                    DisableColliders(prop);
                }

                occupied.Add(position);
            }
        }

        private List<Vector2Int> BuildCandidates(DungeonLayout layout)
        {
            List<Vector2Int> candidates = new();
            foreach (RectInt room in layout.Rooms)
            {
                for (int x = room.xMin + 1; x < room.xMax - 1; x++)
                {
                    for (int y = room.yMin + 1; y < room.yMax - 1; y++)
                    {
                        Vector2Int position = new(x, y);
                        if (CanPlaceAt(layout, position))
                        {
                            candidates.Add(position);
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

        private GameObject PickPrefab(System.Random random)
        {
            int validCount = 0;
            foreach (GameObject prefab in propPrefabs)
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
            foreach (GameObject prefab in propPrefabs)
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
    }
}
