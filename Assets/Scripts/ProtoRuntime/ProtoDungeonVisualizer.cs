using CM3070.PCG;
using UnityEngine;

namespace CM3070.ProtoRuntime
{
    public sealed class ProtoDungeonVisualizer : MonoBehaviour
    {
        [SerializeField] private Transform dungeonRoot;
        [SerializeField] private float tileSize = 1f;

        [Header("Colors")]
        [SerializeField] private Color floorColor = new(0.22f, 0.22f, 0.24f);
        [SerializeField] private Color wallColor = new(0.08f, 0.09f, 0.11f);
        [SerializeField] private Color startColor = new(0.24f, 0.75f, 0.44f);
        [SerializeField] private Color exitColor = new(0.8f, 0.3f, 0.25f);
        [SerializeField] private Color enemyColor = new(0.65f, 0.18f, 0.24f);
        [SerializeField] private Color lootColor = new(0.95f, 0.74f, 0.28f);

        private Material floorMaterial;
        private Material wallMaterial;
        private Material startMaterial;
        private Material exitMaterial;
        private Material enemyMaterial;
        private Material lootMaterial;

        public void Render(DungeonLayout layout)
        {
            EnsureRoot();
            EnsureMaterials();
            Clear();

            for (int x = 0; x < layout.Width; x++)
            {
                for (int y = 0; y < layout.Height; y++)
                {
                    CreateTile(layout.Tiles[x, y], x, y);
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

        private void EnsureMaterials()
        {
            floorMaterial ??= CreateMaterial("Generated Floor", floorColor);
            wallMaterial ??= CreateMaterial("Generated Wall", wallColor);
            startMaterial ??= CreateMaterial("Generated Start", startColor);
            exitMaterial ??= CreateMaterial("Generated Exit", exitColor);
            enemyMaterial ??= CreateMaterial("Generated Enemy", enemyColor);
            lootMaterial ??= CreateMaterial("Generated Loot", lootColor);
        }

        public Vector3 GridToWorld(Vector2Int gridPosition)
        {
            return new Vector3(gridPosition.x * tileSize, 0f, gridPosition.y * tileSize);
        }

        private static Material CreateMaterial(string name, Color color)
        {
            Material material = new(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"))
            {
                name = name,
                color = color
            };
            return material;
        }

        private void Clear()
        {
            for (int i = dungeonRoot.childCount - 1; i >= 0; i--)
            {
                Transform child = dungeonRoot.GetChild(i);
                if (Application.isPlaying)
                {
                    Destroy(child.gameObject);
                }
                else
                {
                    DestroyImmediate(child.gameObject);
                }
            }
        }

        private void CreateTile(DungeonTile tile, int x, int y)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = $"{tile} ({x}, {y})";
            cube.transform.SetParent(dungeonRoot);
            cube.transform.localPosition = new Vector3(x * tileSize, GetHeight(tile), y * tileSize);
            cube.transform.localScale = GetScale(tile);

            Renderer renderer = cube.GetComponent<Renderer>();
            renderer.sharedMaterial = GetMaterial(tile);
        }

        private float GetHeight(DungeonTile tile)
        {
            return tile == DungeonTile.Wall ? 0.45f : 0f;
        }

        private Vector3 GetScale(DungeonTile tile)
        {
            return tile switch
            {
                DungeonTile.Wall => new Vector3(tileSize, 0.9f, tileSize),
                DungeonTile.Enemy => new Vector3(tileSize * 0.58f, 0.55f, tileSize * 0.58f),
                DungeonTile.Loot => new Vector3(tileSize * 0.42f, 0.35f, tileSize * 0.42f),
                DungeonTile.Start or DungeonTile.Exit => new Vector3(tileSize * 0.82f, 0.18f, tileSize * 0.82f),
                _ => new Vector3(tileSize, 0.08f, tileSize)
            };
        }

        private Material GetMaterial(DungeonTile tile)
        {
            return tile switch
            {
                DungeonTile.Wall => wallMaterial,
                DungeonTile.Start => startMaterial,
                DungeonTile.Exit => exitMaterial,
                DungeonTile.Enemy => enemyMaterial,
                DungeonTile.Loot => lootMaterial,
                _ => floorMaterial
            };
        }
    }
}
