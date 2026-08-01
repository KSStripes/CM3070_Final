using System.Collections.Generic;
using CM3070.Office;
using CM3070.PCG;
using UnityEngine;

namespace CM3070.Dungeon1
{
    [System.Serializable]
    public sealed class TilePrefabSet
    {
        public RoomRole role = RoomRole.None;
        public GameObject[] floorPrefabs;
        public GameObject[] wallPrefabs;
    }

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

        [Header("Base Tile Prefabs")]
        [SerializeField] private GameObject floorPrefab;
        [SerializeField] private GameObject wallPrefab;
        [SerializeField] private GameObject roomAccentFloorPrefab;

        [Header("Office Tile Sets")]
        [SerializeField] private GameObject[] corridorFloorPrefabs;
        [SerializeField] private GameObject[] corridorWallPrefabs;
        [SerializeField] private TilePrefabSet[] roomTileSets;

        [Header("Dungeon Markers")]
        [SerializeField] private bool spawnStartExitMarkers = true;
        [SerializeField] private GameObject startMarkerPrefab;
        [SerializeField] private GameObject exitMarkerPrefab;

        private DungeonLayout currentLayout;
        private RoomPlan officeRoomPlan;
        private PropPlacer propPlacer;

        public void SetRoomPlan(RoomPlan plan)
        {
            officeRoomPlan = plan;
        }

        public void Render(DungeonLayout layout)
        {
            currentLayout = layout;
            propPlacer ??= GetComponent<PropPlacer>();
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
                            CreateWall(layout, x, y);
                        }

                        continue;
                    }

                    CreateFloor(x, y, GetRoomRoleAt(x, y));

                    if (spawnStartExitMarkers)
                    {
                        // Dungeon1 uses these prefabs for its original start/exit gameplay loop.
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

            propPlacer?.PlaceProps(layout, dungeonRoot, tileSize);
        }

        public Vector3 GridToWorld(Vector2Int gridPosition)
        {
            return new Vector3(gridPosition.x * tileSize, 0f, gridPosition.y * tileSize);
        }

        public void SetRenderSpawnMarkers(bool shouldRender)
        {
            renderSpawnMarkers = shouldRender;
        }

        public void SetSpawnStartExitMarkers(bool shouldSpawn)
        {
            spawnStartExitMarkers = shouldSpawn;
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

        private void CreateFloor(int x, int y, RoomRole role)
        {
            GameObject prefab = FloorPrefabFor(role, x, y);

            CreateTile(prefab, $"Floor ({x}, {y})", new Vector3(x * tileSize, -0.05f, y * tileSize));
        }

        private void CreateWall(DungeonLayout layout, int x, int y)
        {
            RoomRole role = GetNearestWalkableRoomRole(layout, x, y);
            GameObject prefab = WallPrefabFor(role, x, y);

            CreateTile(prefab, $"Wall ({x}, {y})", new Vector3(x * tileSize, wallHeight * 0.5f, y * tileSize));
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

        private RoomRole GetRoomRoleAt(int x, int y)
        {
            if (officeRoomPlan != null && officeRoomPlan.TryGetRoleAt(new Vector2Int(x, y), out RoomRole role))
            {
                return role;
            }

            return RoomRole.None;
        }

        private RoomRole GetNearestWalkableRoomRole(DungeonLayout layout, int x, int y)
        {
            Vector2Int[] neighbors =
            {
                new(x + 1, y),
                new(x - 1, y),
                new(x, y + 1),
                new(x, y - 1)
            };

            foreach (Vector2Int neighbor in neighbors)
            {
                if (!layout.IsWalkable(neighbor))
                {
                    continue;
                }

                RoomRole role = GetRoomRoleAt(neighbor.x, neighbor.y);
                if (role != RoomRole.None)
                {
                    return role;
                }
            }

            return RoomRole.None;
        }

        private GameObject FloorPrefabFor(RoomRole role, int x, int y)
        {
            if (role == RoomRole.None)
            {
                return PickFrom(corridorFloorPrefabs, x, y, floorPrefab);
            }

            TilePrefabSet set = FindTileSet(role);
            if (set != null)
            {
                GameObject fallback = roomAccentFloorPrefab != null ? roomAccentFloorPrefab : floorPrefab;
                return PickFrom(set.floorPrefabs, x, y, fallback);
            }

            return roomAccentFloorPrefab != null ? roomAccentFloorPrefab : floorPrefab;
        }

        private GameObject WallPrefabFor(RoomRole role, int x, int y)
        {
            if (role == RoomRole.None)
            {
                return PickFrom(corridorWallPrefabs, x, y, wallPrefab);
            }

            TilePrefabSet set = FindTileSet(role);
            if (set != null)
            {
                return PickFrom(set.wallPrefabs, x, y, wallPrefab);
            }

            return wallPrefab;
        }

        private TilePrefabSet FindTileSet(RoomRole role)
        {
            if (roomTileSets == null)
            {
                return null;
            }

            foreach (TilePrefabSet set in roomTileSets)
            {
                if (set != null && set.role == role)
                {
                    return set;
                }
            }

            return null;
        }

        private static GameObject PickFrom(GameObject[] prefabs, int x, int y, GameObject fallback)
        {
            if (prefabs == null || prefabs.Length == 0)
            {
                return fallback;
            }

            int validCount = 0;
            foreach (GameObject prefab in prefabs)
            {
                if (prefab != null)
                {
                    validCount++;
                }
            }

            if (validCount == 0)
            {
                return fallback;
            }

            int index = Mathf.Abs((x * 73856093) ^ (y * 19349663)) % validCount;
            foreach (GameObject prefab in prefabs)
            {
                if (prefab == null)
                {
                    continue;
                }

                if (index == 0)
                {
                    return prefab;
                }

                index--;
            }

            return fallback;
        }
    }
}
