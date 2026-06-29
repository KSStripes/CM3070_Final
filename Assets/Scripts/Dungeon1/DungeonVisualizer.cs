using System.Collections.Generic;
using CM3070.PCG;
using UnityEngine;

namespace CM3070.Dungeon1
{
    public sealed class DungeonVisualizer : MonoBehaviour
    {
        [SerializeField] private Transform dungeonRoot;
        [SerializeField] private float tileSize = 1f;
        [SerializeField] private float wallHeight = 1.25f;
        [SerializeField] private bool renderSpawnMarkers = true;

        [Header("Spawn Gizmos")]
        [SerializeField] private Color enemySpawnGizmoColor = new(0.86f, 0.42f, 0.16f, 0.85f);
        [SerializeField] private Color lootSpawnGizmoColor = new(0.94f, 0.72f, 0.20f, 0.85f);
        [SerializeField] private float spawnGizmoRadius = 0.28f;

        [Header("Dungeon Prefabs")]
        [SerializeField] private GameObject floorPrefab;
        [SerializeField] private GameObject wallPrefab;
        [SerializeField] private GameObject roomAccentFloorPrefab;
        [SerializeField] private GameObject startMarkerPrefab;
        [SerializeField] private GameObject exitMarkerPrefab;

        private DungeonLayout currentLayout;

        public void Render(DungeonLayout layout)
        {
            currentLayout = layout;
            EnsureRoot();
            Clear();

            for (int x = 0; x < layout.Width; x++)
            {
                for (int y = 0; y < layout.Height; y++)
                {
                    DungeonTile tile = layout.Tiles[x, y];

                    if (tile == DungeonTile.Wall)
                    {
                        if (HasWalkableNeighbor(layout, x, y))
                        {
                            CreateWall(x, y);
                        }

                        continue;
                    }

                    CreateFloor(x, y, IsInsideRoom(layout, x, y));

                    if (tile == DungeonTile.Start)
                    {
                        CreateMarker(startMarkerPrefab, $"Start Marker ({x}, {y})", x, y);
                    }
                    else if (tile == DungeonTile.Exit)
                    {
                        CreateMarker(exitMarkerPrefab, $"Exit Marker ({x}, {y})", x, y);
                    }
                }
            }
        }

        public Vector3 GridToWorld(Vector2Int gridPosition)
        {
            return new Vector3(gridPosition.x * tileSize, 0f, gridPosition.y * tileSize);
        }

        public void SetRenderSpawnMarkers(bool shouldRender)
        {
            renderSpawnMarkers = shouldRender;
        }

        public void Clear()
        {
            if (dungeonRoot == null)
            {
                return;
            }

            for (int i = dungeonRoot.childCount - 1; i >= 0; i--)
            {
                GameObject child = dungeonRoot.GetChild(i).gameObject;

                if (Application.isPlaying)
                {
                    Destroy(child);
                }
                else
                {
                    DestroyImmediate(child);
                }
            }
        }

        private void EnsureRoot()
        {
            if (dungeonRoot != null)
            {
                return;
            }

            GameObject root = new("Generated Dungeon");
            root.transform.SetParent(transform);
            dungeonRoot = root.transform;
        }

        private void CreateFloor(int x, int y, bool accent)
        {
            GameObject prefab = accent && roomAccentFloorPrefab != null
                ? roomAccentFloorPrefab
                : floorPrefab;

            CreateTile(prefab, $"Floor ({x}, {y})", new Vector3(x * tileSize, -0.05f, y * tileSize));
        }

        private void CreateWall(int x, int y)
        {
            CreateTile(wallPrefab, $"Wall ({x}, {y})", new Vector3(x * tileSize, wallHeight * 0.5f, y * tileSize));
        }

        private void CreateMarker(GameObject prefab, string objectName, int x, int y)
        {
            CreateTile(prefab, objectName, new Vector3(x * tileSize, 0.04f, y * tileSize));
        }

        private void CreateTile(GameObject prefab, string objectName, Vector3 position)
        {
            if (prefab == null)
            {
                Debug.LogError($"DungeonVisualizer is missing prefab for '{objectName}'.");
                return;
            }

            GameObject instance = Instantiate(prefab, dungeonRoot);
            instance.name = objectName;
            instance.transform.localPosition = position;
        }

        private void OnDrawGizmosSelected()
        {
            if (!renderSpawnMarkers || currentLayout == null)
            {
                return;
            }

            DrawSpawnGizmos(currentLayout.EnemyPositions, enemySpawnGizmoColor, spawnGizmoRadius);
            DrawSpawnGizmos(currentLayout.LootPositions, lootSpawnGizmoColor, spawnGizmoRadius * 0.75f);
        }

        private void DrawSpawnGizmos(IEnumerable<Vector2Int> positions, Color color, float radius)
        {
            Gizmos.color = color;
            foreach (Vector2Int position in positions)
            {
                Gizmos.DrawSphere(GridToWorld(position) + Vector3.up * 0.12f, radius);
            }
        }

        private static bool HasWalkableNeighbor(DungeonLayout layout, int x, int y)
        {
            return layout.IsWalkable(new Vector2Int(x + 1, y))
                || layout.IsWalkable(new Vector2Int(x - 1, y))
                || layout.IsWalkable(new Vector2Int(x, y + 1))
                || layout.IsWalkable(new Vector2Int(x, y - 1));
        }

        private static bool IsInsideRoom(DungeonLayout layout, int x, int y)
        {
            Vector2Int position = new(x, y);

            foreach (RectInt room in layout.Rooms)
            {
                if (room.Contains(position))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
