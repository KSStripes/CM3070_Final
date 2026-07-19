using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CM3070.PCG
{
    public sealed class DungeonGenerator
    {
        private static readonly Vector2Int[] CardinalDirections =
        {
            Vector2Int.up,
            Vector2Int.down,
            Vector2Int.left,
            Vector2Int.right
        };

        private readonly DungeonGenerationSettings settings;
        private System.Random random;

        public DungeonGenerator(DungeonGenerationSettings settings)
        {
            this.settings = settings;
        }

        public DungeonLayout Generate(int? seedOverride = null, DungeonGenerationMethod? methodOverride = null)
        {
            int seed = seedOverride ?? settings.seed;
            DungeonGenerationMethod method = methodOverride ?? settings.method;
            random = new System.Random(seed);

            DungeonLayout layout = new(settings.width, settings.height)
            {
                Seed = seed,
                Method = method
            };

            switch (method)
            {
                case DungeonGenerationMethod.BspRooms:
                    GenerateBsp(layout);
                    break;
                case DungeonGenerationMethod.CellularAutomata:
                    GenerateCellular(layout, settings.smoothingSteps);
                    break;
                case DungeonGenerationMethod.HybridBspCellular:
                    GenerateHybrid(layout);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            // After the selected architecture generator runs, the layout is converted into
            // measurable gameplay space: disconnected regions are removed, a reachable
            // start/exit route is chosen, and enemies/loot are placed using distance data.
            // The statistics displayed in ProtoScene are populated during these steps.
            KeepLargestConnectedRegion(layout);
            PruneDisconnectedRooms(layout);
            PlaceStartAndExit(layout);
            PlaceLootAndEnemies(layout);

            return layout;
        }

        private void GenerateBsp(DungeonLayout layout)
        {
            // BSP reference/inspiration:
            // https://chizaruu.github.io/roguebasin/basic_bsp_dungeon_generation
            // This follows the RogueBasin idea of recursively splitting a rectangular
            // dungeon into leaf partitions, then placing a room inside each usable leaf.
            // Gap from the reference: this implementation stores a flat list of leaves
            // rather than retaining the full BSP tree for sibling-to-sibling corridor
            // linking.
            List<RectInt> leaves = new();
            SplitPartition(new RectInt(1, 1, layout.Width - 2, layout.Height - 2), leaves, 0);

            foreach (RectInt leaf in leaves)
            {
                // Skip BSP leaves that cannot contain the requested minimum room plus margin.
                if (leaf.width <= settings.minRoomSize + 2 || leaf.height <= settings.minRoomSize + 2)
                {
                    continue;
                }

                // Room dimensions are sampled from the configured min/max room size,
                // clamped by the current BSP leaf. Larger maxRoomSize needs larger leaves.
                int maxWidth = Mathf.Min(settings.maxRoomSize, leaf.width - 2);
                int maxHeight = Mathf.Min(settings.maxRoomSize, leaf.height - 2);
                int roomWidth = random.Next(settings.minRoomSize, maxWidth + 1);
                int roomHeight = random.Next(settings.minRoomSize, maxHeight + 1);
                // Rooms are offset inside leaves to leave wall/corridor margin around them.
                int roomX = random.Next(leaf.xMin + 1, leaf.xMax - roomWidth);
                int roomY = random.Next(leaf.yMin + 1, leaf.yMax - roomHeight);

                RectInt room = new(roomX, roomY, roomWidth, roomHeight);
                layout.Rooms.Add(room);
                CarveRoom(layout, room);
            }

            ConnectRooms(layout);
        }

        private void GenerateCellular(DungeonLayout layout, int smoothingSteps)
        {
            // Cellular automata reference/inspiration:
            // https://chizaruu.github.io/roguebasin/cellular_automata_method_for_generating_random_cave-like_levels#c-code-1
            // This matches the RogueBasin CA pattern of starting with random wall/floor
            // noise, then repeatedly smoothing the map using neighbouring wall counts.
            // Gap from the reference: the RogueBasin article describes specific R1/R2
            // cave rules; this project uses configurable birth/death thresholds with a
            // radius-1 Moore neighbourhood instead.
            for (int x = 1; x < layout.Width - 1; x++)
            {
                for (int y = 1; y < layout.Height - 1; y++)
                {
                    layout.Tiles[x, y] = random.NextDouble() < settings.wallFillChance ? DungeonTile.Wall : DungeonTile.Floor;
                }
            }

            SmoothCellular(layout, smoothingSteps, null);
        }

        private void GenerateHybrid(DungeonLayout layout)
        {
            // Hybrid BSP + CA implementation:
            // BSP first creates readable room/corridor structure, then CA-style random
            // disturbance and smoothing add cave-like irregularity. This is inspired by
            // combining the two RogueBasin techniques above, but it is a project-specific
            // hybrid rather than an algorithm copied directly from either page.
            GenerateBsp(layout);

            HashSet<Vector2Int> preserved = new();
            foreach (RectInt room in layout.Rooms)
            {
                // Preserve room interiors so CA smoothing can roughen the surroundings
                // without destroying the main BSP rooms.
                // roomPreservationBorder controls how much room edge can be eroded.
                RectInt inner = Shrink(room, settings.roomPreservationBorder);
                for (int x = inner.xMin; x < inner.xMax; x++)
                {
                    for (int y = inner.yMin; y < inner.yMax; y++)
                    {
                        preserved.Add(new Vector2Int(x, y));
                    }
                }
            }

            for (int x = 1; x < layout.Width - 1; x++)
            {
                for (int y = 1; y < layout.Height - 1; y++)
                {
                    Vector2Int position = new(x, y);
                    if (preserved.Contains(position))
                    {
                        continue;
                    }

                    // Cave-pocket noise pass for the hybrid: floor can become wall and
                    // wall can become floor outside preserved room interiors. This gives
                    // the later CA smoothing something organic to work with.
                    if (layout.Tiles[x, y] == DungeonTile.Floor && random.NextDouble() < settings.cavePocketChance)
                    {
                        layout.Tiles[x, y] = DungeonTile.Wall;
                    }
                    else if (layout.Tiles[x, y] == DungeonTile.Wall && random.NextDouble() < settings.cavePocketChance * 0.32f)
                    {
                        layout.Tiles[x, y] = DungeonTile.Floor;
                    }
                }
            }

            // Smooth only the non-preserved tiles; preserved BSP room interiors remain floor.
            SmoothCellular(layout, settings.hybridSmoothingSteps, preserved);
            // Reconnect after smoothing because the CA-style pass can interrupt corridors.
            ConnectRooms(layout);
        }

        private void SplitPartition(RectInt partition, List<RectInt> leaves, int depth)
        {
            // Recursive rectangular partitioning, equivalent to the subdivision stage in
            // BSP dungeon generation. The stopping conditions are min partition size and
            // max depth, both exposed in DungeonGenerationSettings.
            // Larger minPartitionSize means fewer/larger leaves, which supports bigger rooms.
            bool canSplitHorizontally = partition.height >= settings.minPartitionSize * 2;
            bool canSplitVertically = partition.width >= settings.minPartitionSize * 2;

            if ((!canSplitHorizontally && !canSplitVertically) || depth >= settings.maxSplitDepth)
            {
                leaves.Add(partition);
                return;
            }

            bool splitHorizontally = canSplitHorizontally && (!canSplitVertically || random.NextDouble() > 0.5);
            if (splitHorizontally)
            {
                int split = random.Next(settings.minPartitionSize, partition.height - settings.minPartitionSize);
                SplitPartition(new RectInt(partition.x, partition.y, partition.width, split), leaves, depth + 1);
                SplitPartition(new RectInt(partition.x, partition.y + split, partition.width, partition.height - split), leaves, depth + 1);
            }
            else
            {
                int split = random.Next(settings.minPartitionSize, partition.width - settings.minPartitionSize);
                SplitPartition(new RectInt(partition.x, partition.y, split, partition.height), leaves, depth + 1);
                SplitPartition(new RectInt(partition.x + split, partition.y, partition.width - split, partition.height), leaves, depth + 1);
            }
        }

        private static void CarveRoom(DungeonLayout layout, RectInt room)
        {
            for (int x = room.xMin; x < room.xMax; x++)
            {
                for (int y = room.yMin; y < room.yMax; y++)
                {
                    layout.Tiles[x, y] = DungeonTile.Floor;
                }
            }
        }

        private void ConnectRooms(DungeonLayout layout)
        {
            // RogueBasin BSP connects sibling rooms by walking back up the BSP tree.
            // This implementation differs: it links the nearest unconnected room to the
            // growing connected set. That keeps corridors shorter than a simple sorted
            // room chain while preserving the BSP room/corridor plus CA hybrid pipeline.
            if (layout.Rooms.Count <= 1)
            {
                return;
            }

            List<RectInt> connected = new() { layout.Rooms[0] };
            List<RectInt> remaining = layout.Rooms.Skip(1).ToList();

            while (remaining.Count > 0)
            {
                RectInt fromRoom = connected[0];
                RectInt toRoom = remaining[0];
                float bestDistance = float.MaxValue;

                foreach (RectInt connectedRoom in connected)
                {
                    foreach (RectInt candidateRoom in remaining)
                    {
                        float distance = Vector2.SqrMagnitude(connectedRoom.center - candidateRoom.center);
                        if (distance < bestDistance)
                        {
                            bestDistance = distance;
                            fromRoom = connectedRoom;
                            toRoom = candidateRoom;
                        }
                    }
                }

                ConnectPoints(layout, Vector2Int.RoundToInt(fromRoom.center), Vector2Int.RoundToInt(toRoom.center));
                connected.Add(toRoom);
                remaining.Remove(toRoom);
            }
        }

        private void ConnectPoints(DungeonLayout layout, Vector2Int from, Vector2Int to)
        {
            Vector2Int cursor = from;
            while (cursor.x != to.x)
            {
                CarveCorridorTile(layout, cursor);
                cursor.x += Math.Sign(to.x - cursor.x);
            }

            while (cursor.y != to.y)
            {
                CarveCorridorTile(layout, cursor);
                cursor.y += Math.Sign(to.y - cursor.y);
            }

            CarveCorridorTile(layout, to);
        }

        private void CarveCorridorTile(DungeonLayout layout, Vector2Int center)
        {
            // corridorWidth controls corridor thickness. Width 1 gives a single tile;
            // width 2 gives radius 1, carving a 3x3 area at each corridor step.
            int radius = Mathf.Max(0, settings.corridorWidth - 1);
            for (int x = center.x - radius; x <= center.x + radius; x++)
            {
                for (int y = center.y - radius; y <= center.y + radius; y++)
                {
                    Vector2Int position = new(x, y);
                    if (layout.IsInBounds(position))
                    {
                        layout.Tiles[x, y] = DungeonTile.Floor;
                    }
                }
            }
        }

        private void SmoothCellular(DungeonLayout layout, int smoothingSteps, HashSet<Vector2Int> preserved)
        {
            // CA smoothing stage. Each pass clones the current tile grid so all cells are
            // updated from the same previous state, matching the parallel-update idea of
            // cellular automata. Preserved cells are forced to remain floor in the hybrid.
            for (int step = 0; step < smoothingSteps; step++)
            {
                DungeonTile[,] next = (DungeonTile[,])layout.Tiles.Clone();
                for (int x = 1; x < layout.Width - 1; x++)
                {
                    for (int y = 1; y < layout.Height - 1; y++)
                    {
                        Vector2Int position = new(x, y);
                        if (preserved != null && preserved.Contains(position))
                        {
                            next[x, y] = DungeonTile.Floor;
                            continue;
                        }

                        int wallCount = CountNeighborWalls(layout, x, y);
                        next[x, y] = wallCount > settings.birthLimit
                            ? DungeonTile.Wall
                            : wallCount < settings.deathLimit
                                ? DungeonTile.Floor
                                : layout.Tiles[x, y];
                    }
                }

                Array.Copy(next, layout.Tiles, next.Length);
            }
        }

        private static int CountNeighborWalls(DungeonLayout layout, int centerX, int centerY)
        {
            // Radius-1 Moore neighbourhood wall count: the eight surrounding cells are
            // inspected, and out-of-bounds cells count as walls to keep a closed border.
            int count = 0;
            for (int x = centerX - 1; x <= centerX + 1; x++)
            {
                for (int y = centerY - 1; y <= centerY + 1; y++)
                {
                    if (x == centerX && y == centerY)
                    {
                        continue;
                    }

                    if (!layout.IsInBounds(new Vector2Int(x, y)) || layout.Tiles[x, y] == DungeonTile.Wall)
                    {
                        count++;
                    }
                }
            }

            return count;
        }

        private static void KeepLargestConnectedRegion(DungeonLayout layout)
        {
            // Connectivity metric:
            // Treat each non-wall tile as a graph node connected by four-directional
            // adjacency. Flood fill finds each connected walkable component. The largest
            // component is kept as the playable dungeon, and its size becomes
            // layout.MainRegionSize.
            // RogueBasin notes that CA cave generation can create isolated cave regions.
            // This post-process addresses that gap by flood-filling all walkable regions
            // and keeping only the largest connected component.
            HashSet<Vector2Int> visited = new();
            List<Vector2Int> largestRegion = new();

            foreach (Vector2Int floor in layout.WalkablePositions())
            {
                if (visited.Contains(floor))
                {
                    continue;
                }

                List<Vector2Int> region = FloodFill(layout, floor, visited);
                if (region.Count > largestRegion.Count)
                {
                    largestRegion = region;
                }
            }

            HashSet<Vector2Int> keep = new(largestRegion);
            for (int x = 0; x < layout.Width; x++)
            {
                for (int y = 0; y < layout.Height; y++)
                {
                    Vector2Int position = new(x, y);
                    if (layout.Tiles[x, y] != DungeonTile.Wall && !keep.Contains(position))
                    {
                        layout.Tiles[x, y] = DungeonTile.Wall;
                    }
                }
            }

            layout.MainRegionSize = largestRegion.Count;
        }

        private static List<Vector2Int> FloodFill(DungeonLayout layout, Vector2Int start, HashSet<Vector2Int> visited)
        {
            // Breadth-first flood fill over the walkable grid. Each discovered tile is
            // marked visited once, so the returned list is one connected component.
            Queue<Vector2Int> queue = new();
            List<Vector2Int> region = new();
            queue.Enqueue(start);
            visited.Add(start);

            while (queue.Count > 0)
            {
                Vector2Int current = queue.Dequeue();
                region.Add(current);

                foreach (Vector2Int direction in CardinalDirections)
                {
                    Vector2Int next = current + direction;
                    if (layout.IsWalkable(next) && visited.Add(next))
                    {
                        queue.Enqueue(next);
                    }
                }
            }

            return region;
        }

        private static void PruneDisconnectedRooms(DungeonLayout layout)
        {
            // Connectivity cleanup can remove isolated room fragments, so room metadata is pruned before office roles are assigned.
            layout.Rooms.RemoveAll(room =>
            {
                Vector2Int center = Vector2Int.RoundToInt(room.center);
                if (!layout.IsWalkable(center))
                {
                    return true;
                }

                int walkableTiles = 0;
                int roomTiles = Mathf.Max(1, room.width * room.height);
                for (int x = room.xMin; x < room.xMax; x++)
                {
                    for (int y = room.yMin; y < room.yMax; y++)
                    {
                        if (layout.IsWalkable(new Vector2Int(x, y)))
                        {
                            walkableTiles++;
                        }
                    }
                }

                return walkableTiles < Mathf.Max(4, Mathf.RoundToInt(roomTiles * 0.5f));
            });
        }

        private void PlaceStartAndExit(DungeonLayout layout)
        {
            // Start/exit distance metric:
            // Run breadth-first search from the chosen start tile. The exit is selected
            // from reachable tiles at or beyond the configured minimum distance, preferring
            // the farthest reachable tile. The stored path length is the BFS distance, i.e.
            // the shortest number of four-directional grid steps from start to exit.
            List<Vector2Int> floors = layout.WalkablePositions().ToList();
            if (floors.Count == 0)
            {
                Vector2Int fallback = new(layout.Width / 2, layout.Height / 2);
                layout.Tiles[fallback.x, fallback.y] = DungeonTile.Floor;
                floors.Add(fallback);
            }

            List<Vector2Int> roomCenters = layout.Rooms
                .Select(room => Vector2Int.RoundToInt(room.center))
                .Where(layout.IsWalkable)
                .OrderBy(position => position.x + position.y)
                .ToList();

            Vector2Int start = roomCenters.Count > 0
                ? roomCenters[0]
                : floors[random.Next(floors.Count)];

            Dictionary<Vector2Int, int> distances = GetDistances(layout, start);
            List<Vector2Int> distantExitCandidates = distances
                .Where(pair => pair.Value >= settings.minimumStartExitDistance)
                .OrderByDescending(pair => pair.Value)
                .Select(pair => pair.Key)
                .ToList();

            Vector2Int exit = distantExitCandidates.Count > 0
                ? distantExitCandidates[0]
                : distances.OrderByDescending(pair => pair.Value).First().Key;

            layout.Start = start;
            layout.Exit = exit;
            layout.ShortestPathLength = distances.TryGetValue(exit, out int pathLength) ? pathLength : 0;
            layout.Tiles[start.x, start.y] = DungeonTile.Start;
            layout.Tiles[exit.x, exit.y] = DungeonTile.Exit;
        }

        private void PlaceLootAndEnemies(DungeonLayout layout)
        {
            Dictionary<Vector2Int, int> distances = GetDistances(layout, layout.Start);
            int maxDistance = Mathf.Max(1, distances.Values.DefaultIfEmpty(1).Max());

            // Furnishing candidates are reachable floor tiles, excluding start, exit, and
            // tiles too close to the start. This keeps early player space safer and ensures
            // enemies/loot are placed only in navigable parts of the generated dungeon.
            List<Vector2Int> candidates = distances.Keys
                .Where(position => position != layout.Start && position != layout.Exit)
                .Where(position => distances[position] > settings.spawnExclusionRadius)
                .OrderBy(_ => random.Next())
                .ToList();

            foreach (Vector2Int position in candidates.Take(settings.lootCount))
            {
                layout.Tiles[position.x, position.y] = DungeonTile.Loot;
                layout.LootPositions.Add(position);
            }

            int enemyBudget = Mathf.Min(settings.maxEnemies, Mathf.RoundToInt(layout.WalkableCount() * settings.enemyDensity));
            float difficultyScore = 0f;

            // Enemy probability increases with normalized distance from the start.
            // depth is 0 near the start and approaches 1 near the farthest reachable tile.
            // This creates a simple difficulty ramp: deeper tiles are more likely to contain
            // enemies, and each placed enemy contributes more to EstimatedDifficulty.
            foreach (Vector2Int position in candidates)
            {
                if (layout.EnemyPositions.Count >= enemyBudget)
                {
                    break;
                }

                if (layout.Tiles[position.x, position.y] != DungeonTile.Floor)
                {
                    continue;
                }

                float depth = distances[position] / (float)maxDistance;
                float placementChance = Mathf.Lerp(0.08f, 0.52f, depth * settings.difficultyRamp);
                if (random.NextDouble() <= placementChance)
                {
                    layout.Tiles[position.x, position.y] = DungeonTile.Enemy;
                    layout.EnemyPositions.Add(position);
                    difficultyScore += 1f + depth * settings.difficultyRamp;
                }
            }

            layout.EstimatedDifficulty = difficultyScore;
        }

        private static Dictionary<Vector2Int, int> GetDistances(DungeonLayout layout, Vector2Int start)
        {
            // Computes shortest-path distances from start to every reachable walkable tile.
            // Because all grid moves have equal cost, breadth-first search gives the
            // shortest path length without needing Dijkstra's algorithm.
            Dictionary<Vector2Int, int> distances = new()
            {
                [start] = 0
            };
            Queue<Vector2Int> queue = new();
            queue.Enqueue(start);

            while (queue.Count > 0)
            {
                Vector2Int current = queue.Dequeue();
                foreach (Vector2Int direction in CardinalDirections)
                {
                    Vector2Int next = current + direction;
                    if (layout.IsWalkable(next) && !distances.ContainsKey(next))
                    {
                        distances[next] = distances[current] + 1;
                        queue.Enqueue(next);
                    }
                }
            }

            return distances;
        }

        private static RectInt Shrink(RectInt rect, int amount)
        {
            if (amount <= 0 || rect.width <= amount * 2 || rect.height <= amount * 2)
            {
                return rect;
            }

            return new RectInt(rect.x + amount, rect.y + amount, rect.width - amount * 2, rect.height - amount * 2);
        }
    }
}
