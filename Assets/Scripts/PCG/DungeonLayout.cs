using System.Collections.Generic;
using UnityEngine;

namespace CM3070.PCG
{
    public sealed class DungeonLayout
    {
        public readonly int Width;
        public readonly int Height;
        public readonly DungeonTile[,] Tiles;
        public readonly List<RectInt> Rooms = new();
        public readonly List<Vector2Int> EnemyPositions = new();
        public readonly List<Vector2Int> LootPositions = new();

        public Vector2Int Start { get; set; }
        public Vector2Int Exit { get; set; }
        public int Seed { get; set; }
        public DungeonGenerationMethod Method { get; set; }
        /// <summary>
        /// Heuristic score based on enemy count and enemy distance from the start.
        /// </summary>
        public float EstimatedDifficulty { get; set; }

        /// <summary>
        /// Size of the largest connected walkable component after flood-fill cleanup.
        /// Used as a connectivity/playable-area metric.
        /// </summary>
        public int MainRegionSize { get; set; }

        /// <summary>
        /// Shortest four-directional grid path from start to exit, computed with BFS.
        /// </summary>
        public int ShortestPathLength { get; set; }

        public DungeonLayout(int width, int height)
        {
            Width = width;
            Height = height;
            Tiles = new DungeonTile[width, height];
            Fill(DungeonTile.Wall);
        }

        public bool IsInBounds(Vector2Int position)
        {
            return position.x >= 0 && position.x < Width && position.y >= 0 && position.y < Height;
        }

        public bool IsWalkable(Vector2Int position)
        {
            if (!IsInBounds(position))
            {
                return false;
            }

            return Tiles[position.x, position.y] != DungeonTile.Wall;
        }

        public bool IsMarker(Vector2Int position)
        {
            if (!IsInBounds(position))
            {
                return false;
            }

            DungeonTile tile = Tiles[position.x, position.y];
            return tile == DungeonTile.Start || tile == DungeonTile.Exit || tile == DungeonTile.Enemy || tile == DungeonTile.Loot;
        }

        public void Fill(DungeonTile tile)
        {
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    Tiles[x, y] = tile;
                }
            }
        }

        public IEnumerable<Vector2Int> WalkablePositions()
        {
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    Vector2Int position = new(x, y);
                    if (IsWalkable(position))
                    {
                        yield return position;
                    }
                }
            }
        }

        public int WalkableCount()
        {
            // Counts all non-wall tiles. Start, exit, enemy, and loot tiles are still
            // walkable because they occupy reachable floor positions with gameplay meaning.
            int count = 0;
            foreach (Vector2Int _ in WalkablePositions())
            {
                count++;
            }

            return count;
        }
    }
}
