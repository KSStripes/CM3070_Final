using UnityEngine;

namespace CM3070.PCG
{
    [CreateAssetMenu(fileName = "DungeonGenerationSettings", menuName = "CM3070/PCG/Dungeon Generation Settings")]
    public sealed class DungeonGenerationSettings : ScriptableObject
    {
        [Header("Core")]
        public DungeonGenerationMethod method = DungeonGenerationMethod.HybridBspCellular;
        public int width = 64;
        public int height = 46;
        public int seed = 3070;
        public bool randomizeSeedOnPlay = true;

        [Header("BSP Rooms")]
        public int minRoomSize = 8;
        public int maxRoomSize = 16;
        public int minPartitionSize = 18;
        public int corridorWidth = 2;
        public int maxSplitDepth = 5;

        [Header("Cellular Automata")]
        [Range(0f, 1f)] public float wallFillChance = 0.44f;
        public int smoothingSteps = 5;
        public int birthLimit = 4;
        public int deathLimit = 3;

        [Header("Hybrid")]
        [Range(0f, 1f)] public float cavePocketChance = 0.34f;
        public int hybridSmoothingSteps = 2;
        public int roomPreservationBorder = 1;

        [Header("Spawns And Balance")]
        [Range(0f, 0.2f)] public float enemyDensity = 0.035f;
        public int maxEnemies = 24;
        public int lootCount = 8;
        [Range(0f, 2f)] public float difficultyRamp = 1f;
        public int minimumStartExitDistance = 26;
        public int spawnExclusionRadius = 4;

        private void OnValidate()
        {
            width = Mathf.Max(20, width);
            height = Mathf.Max(16, height);
            minRoomSize = Mathf.Max(3, minRoomSize);
            maxRoomSize = Mathf.Max(minRoomSize, maxRoomSize);
            minPartitionSize = Mathf.Max(maxRoomSize + 2, minPartitionSize);
            corridorWidth = Mathf.Clamp(corridorWidth, 1, 4);
            maxSplitDepth = Mathf.Clamp(maxSplitDepth, 2, 10);
            smoothingSteps = Mathf.Max(0, smoothingSteps);
            hybridSmoothingSteps = Mathf.Max(0, hybridSmoothingSteps);
            roomPreservationBorder = Mathf.Max(0, roomPreservationBorder);
            maxEnemies = Mathf.Max(0, maxEnemies);
            lootCount = Mathf.Max(0, lootCount);
            minimumStartExitDistance = Mathf.Max(1, minimumStartExitDistance);
            spawnExclusionRadius = Mathf.Max(0, spawnExclusionRadius);
        }
    }
}
