using CM3070.Dungeon1;
using CM3070.PCG;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

// Scene-level coordinator for OfficeScene.
namespace CM3070.Office
{
    [ExecuteAlways]
    [RequireComponent(typeof(DungeonVisualizer))]
    [RequireComponent(typeof(CameraController))]
    [RequireComponent(typeof(EntitySpawner))]
    [RequireComponent(typeof(OfficeQuestSpawner))]
    public sealed class OfficeController : MonoBehaviour
    {
        [Header("Edit Mode")]
        [SerializeField] private bool generatePreviewInEditMode = true;
        [SerializeField] private bool regenerateWhenInspectorChanges;

        [Header("Hybrid Office Layout")]
        [SerializeField] private int width = 64;
        [SerializeField] private int height = 46;
        [SerializeField] private int seed = 3070;
        [SerializeField] private bool randomizeSeedOnPlay = true;
        [SerializeField] private int minRoomSize = 8;
        [SerializeField] private int maxRoomSize = 16;
        [SerializeField] private int minPartitionSize = 18;
        [SerializeField] private int corridorWidth = 2;
        [SerializeField] private int maxSplitDepth = 5;
        [SerializeField, Range(0f, 1f)] private float wallFillChance = 0.44f;
        [SerializeField, Range(0f, 1f)] private float cavePocketChance = 0.34f;
        [SerializeField] private int hybridSmoothingSteps = 2;

        [Header("NPC And Pickup Proportions")]
        [SerializeField, Range(0f, 0.2f)] private float enemyDensity = 0.035f;
        [SerializeField] private int maxEnemies = 20;
        [SerializeField, Range(0f, 0.08f)] private float lootDensity = 0.005f;
        [SerializeField] private int maxLoot = 14;
        [SerializeField] private int spawnExclusionRadius = 4;

        [Header("Office Debug")]
        [SerializeField] private int layoutRetryAttempts = 20;

        private DungeonVisualizer visualizer;
        private CameraController cameraController;
        private EntitySpawner entitySpawner;
        private OfficeQuestSpawner questSpawner;
        private PropPlacer officePropPlacer;
        private DungeonLayout currentLayout;
        private RoomPlan currentRoomPlan;
        private int activeSeed;

        public event System.Action<OfficeRunStatsSnapshot> RunStatsChanged;

        [ContextMenu("Generate Office Preview")]
        public void GenerateHybridPreview()
        {
            GenerateDungeon(Application.isPlaying, true);
        }

        public void StartNewGame()
        {
            activeSeed = NextSeed();
            GenerateDungeon(true, true);
        }

        public void StartNextLevel()
        {
            activeSeed = NextSeed();
            GenerateDungeon(true, false);
        }

        private void OnEnable()
        {
            visualizer = GetComponent<DungeonVisualizer>();
            cameraController = GetComponent<CameraController>();
            entitySpawner = GetComponent<EntitySpawner>();
            questSpawner = GetComponent<OfficeQuestSpawner>();
            officePropPlacer = GetComponent<PropPlacer>();

            if (!Application.isPlaying && generatePreviewInEditMode)
            {
                GenerateDungeon(false, true);
            }
        }

        private void Start()
        {
            if (Application.isPlaying && GameManager.Instance == null)
            {
                StartNewGame();
            }
        }

        private void Update()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.rKey.wasPressedThisFrame)
            {
                StartNewGame();
            }
        }

        private void LateUpdate()
        {
            if (Application.isPlaying && entitySpawner.PlayerTransform != null)
            {
                cameraController.PositionPlayerCamera(entitySpawner.PlayerTransform.position);
            }
        }

        private void OnValidate()
        {
            width = Mathf.Max(20, width);
            height = Mathf.Max(16, height);
            minRoomSize = Mathf.Max(3, minRoomSize);
            maxRoomSize = Mathf.Max(minRoomSize, maxRoomSize);
            minPartitionSize = Mathf.Max(maxRoomSize + 2, minPartitionSize);
            corridorWidth = Mathf.Clamp(corridorWidth, 1, 4);
            maxSplitDepth = Mathf.Clamp(maxSplitDepth, 2, 10);
            hybridSmoothingSteps = Mathf.Max(0, hybridSmoothingSteps);
            maxEnemies = Mathf.Max(0, maxEnemies);
            maxLoot = Mathf.Max(0, maxLoot);
            spawnExclusionRadius = Mathf.Max(0, spawnExclusionRadius);
            layoutRetryAttempts = Mathf.Max(1, layoutRetryAttempts);

            if (!Application.isPlaying && regenerateWhenInspectorChanges && isActiveAndEnabled)
            {
                visualizer = GetComponent<DungeonVisualizer>();
                cameraController = GetComponent<CameraController>();
                entitySpawner = GetComponent<EntitySpawner>();
                questSpawner = GetComponent<OfficeQuestSpawner>();
                officePropPlacer = GetComponent<PropPlacer>();
                GenerateDungeon(false, true);
            }
        }

        private void GenerateDungeon(bool runtimeObjects, bool resetPlayerStats)
        {
            visualizer ??= GetComponent<DungeonVisualizer>();
            cameraController ??= GetComponent<CameraController>();
            entitySpawner ??= GetComponent<EntitySpawner>();
            questSpawner ??= GetComponent<OfficeQuestSpawner>();
            officePropPlacer ??= GetComponent<PropPlacer>();

            cameraController.EnsureCameras(transform);

            DungeonGenerationSettings settings = BuildSettings();
            GenerateValidOfficeLayout(settings, runtimeObjects);
            visualizer.SetRoomPlan(currentRoomPlan);
            officePropPlacer?.SetRoomPlan(currentRoomPlan);

            visualizer.SetRenderSpawnMarkers(!runtimeObjects);
            // OfficeScene gets its exit interaction from OfficeQuestSpawner instead.
            visualizer.SetSpawnStartExitMarkers(false);
            visualizer.Render(currentLayout);

            entitySpawner.SpawnEntities(
                currentLayout,
                visualizer,
                runtimeObjects,
                resetPlayerStats,
                officePropPlacer != null ? officePropPlacer.OccupiedPositions : null);

            if (questSpawner != null && runtimeObjects)
            {
                questSpawner.SpawnQuestObjects(
                    currentLayout,
                    currentRoomPlan,
                    visualizer,
                    transform,
                    officePropPlacer != null ? officePropPlacer.OccupiedPositions : null);
            }
            else if (questSpawner != null)
            {
                questSpawner.ClearQuestObjects();
            }

            cameraController.PositionOverviewCamera(currentLayout);
            cameraController.PositionPlayerCamera(entitySpawner.PlayerTransform != null
                ? entitySpawner.PlayerTransform.position
                : visualizer.GridToWorld(currentLayout.Start));
            RunStatsChanged?.Invoke(CaptureRunStats());
        }

        public OfficeRunStatsSnapshot CaptureRunStats()
        {
            if (currentLayout == null)
            {
                return default;
            }

            EntitySpawnStatsSnapshot entityStats = entitySpawner != null
                ? entitySpawner.LastSpawnStats
                : default;
            QuestSpawnStatsSnapshot questStats = questSpawner != null
                ? questSpawner.LastSpawnStats
                : default;
            PropSpawnStatsSnapshot propStats = officePropPlacer != null
                ? officePropPlacer.LastSpawnStats
                : default;

            return new OfficeRunStatsSnapshot(
                currentLayout.Seed,
                currentLayout.Rooms.Count,
                CaptureRoomRoleCounts(),
                currentLayout.WalkableCount(),
                currentLayout.MainRegionSize,
                propStats.PropCount,
                propStats.PropRoleCounts,
                entityStats.NpcCount,
                questStats.QuestCount,
                questStats.QuestItemCount,
                questStats.TaskMarkerCount,
                entityStats.NpcRoleCounts);
        }

        private IReadOnlyList<OfficeRoleCount> CaptureRoomRoleCounts()
        {
            if (currentRoomPlan == null)
            {
                return new List<OfficeRoleCount>();
            }

            return currentRoomPlan.Assignments
                .GroupBy(assignment => assignment.Role)
                .OrderBy(group => PropPlacer.RoleSortIndex(group.Key))
                .Select(group => new OfficeRoleCount(PropPlacer.DisplayName(group.Key), group.Count()))
                .ToList();
        }

        private int NextSeed()
        {
            return randomizeSeedOnPlay ? Random.Range(1, int.MaxValue) : seed;
        }

        private DungeonGenerationSettings BuildSettings()
        {
            DungeonGenerationSettings settings = ScriptableObject.CreateInstance<DungeonGenerationSettings>();
            settings.method = DungeonGenerationMethod.HybridBspCellular;
            settings.width = width;
            settings.height = height;
            settings.seed = seed;
            settings.randomizeSeedOnPlay = false;
            settings.minRoomSize = minRoomSize;
            settings.maxRoomSize = maxRoomSize;
            settings.minPartitionSize = minPartitionSize;
            settings.corridorWidth = corridorWidth;
            settings.maxSplitDepth = maxSplitDepth;
            settings.wallFillChance = wallFillChance;
            settings.cavePocketChance = cavePocketChance;
            settings.hybridSmoothingSteps = hybridSmoothingSteps;
            settings.enemyDensity = enemyDensity;
            settings.maxEnemies = maxEnemies;
            settings.lootCount = Mathf.Min(maxLoot, Mathf.RoundToInt(width * height * lootDensity));
            settings.spawnExclusionRadius = spawnExclusionRadius;
            return settings;
        }

        private void GenerateValidOfficeLayout(DungeonGenerationSettings settings, bool runtimeObjects)
        {
            for (int attempt = 0; attempt < layoutRetryAttempts; attempt++)
            {
                int candidateSeed = runtimeObjects
                    ? (attempt == 0 ? activeSeed : Random.Range(1, int.MaxValue))
                    : seed + attempt;

                DungeonGenerator generator = new(settings);
                DungeonLayout candidateLayout = generator.Generate(candidateSeed, DungeonGenerationMethod.HybridBspCellular);
                RoomPlan candidatePlan = LayoutPlanner.CreatePlan(candidateLayout);

                currentLayout = candidateLayout;
                currentRoomPlan = candidatePlan;

                if (IsValidOfficeLayout(candidateLayout, candidatePlan))
                {
                    activeSeed = candidateSeed;
                    return;
                }
            }

            Debug.LogWarning($"Generated office layout did not meet validation after {layoutRetryAttempts} attempts; using the last candidate.");
        }

        private static bool IsValidOfficeLayout(DungeonLayout layout, RoomPlan plan)
        {
            return layout != null
                && plan != null
                && plan.HasRequiredRooms
                && layout.Start != layout.Exit
                && layout.ShortestPathLength >= 20
                && layout.MainRegionSize >= 300;
        }

    }
}
