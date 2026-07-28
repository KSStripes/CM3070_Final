using CM3070.Dungeon1;
using CM3070.PCG;
using UnityEngine;
using UnityEngine.InputSystem;

// Scene-level coordinator for OfficeScene.
namespace CM3070.Office
{
    [ExecuteAlways]
    [RequireComponent(typeof(DungeonVisualizer))]
    [RequireComponent(typeof(CameraController))]
    [RequireComponent(typeof(OfficeEntitySpawner))]
    [RequireComponent(typeof(OfficeQuestSpawner))]
    public sealed class OfficeDungeonController : MonoBehaviour
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
        [SerializeField] private int maxEnemies = 24;
        [SerializeField, Range(0f, 0.08f)] private float lootDensity = 0.005f;
        [SerializeField] private int maxLoot = 14;
        [SerializeField] private int spawnExclusionRadius = 4;

        [Header("Office Debug")]
        [SerializeField] private bool logRoomTransitions = true;
        [SerializeField] private int layoutRetryAttempts = 20;

        private DungeonVisualizer visualizer;
        private CameraController cameraController;
        private OfficeEntitySpawner entitySpawner;
        private OfficeQuestSpawner questSpawner;
        private OfficePropPlacer officePropPlacer;
        private DungeonLayout currentLayout;
        private OfficeRoomPlan currentOfficeRoomPlan;
        private OfficeRoomRole currentPlayerRoomRole = OfficeRoomRole.None;
        private int activeSeed;

        public OfficeRoomPlan CurrentOfficeRoomPlan => currentOfficeRoomPlan;

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
            entitySpawner = GetComponent<OfficeEntitySpawner>();
            questSpawner = GetComponent<OfficeQuestSpawner>();
            officePropPlacer = GetComponent<OfficePropPlacer>();

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

            LogPlayerRoomTransition();
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
                entitySpawner = GetComponent<OfficeEntitySpawner>();
                questSpawner = GetComponent<OfficeQuestSpawner>();
                officePropPlacer = GetComponent<OfficePropPlacer>();
                GenerateDungeon(false, true);
            }
        }

        private void GenerateDungeon(bool runtimeObjects, bool resetPlayerStats)
        {
            visualizer ??= GetComponent<DungeonVisualizer>();
            cameraController ??= GetComponent<CameraController>();
            entitySpawner ??= GetComponent<OfficeEntitySpawner>();
            questSpawner ??= GetComponent<OfficeQuestSpawner>();
            officePropPlacer ??= GetComponent<OfficePropPlacer>();

            cameraController.EnsureCameras(transform);

            DungeonGenerationSettings settings = BuildSettings();
            GenerateValidOfficeLayout(settings, runtimeObjects);
            visualizer.SetOfficeRoomPlan(currentOfficeRoomPlan);
            officePropPlacer?.SetRoomPlan(currentOfficeRoomPlan);
            currentPlayerRoomRole = OfficeRoomRole.None;

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
                    currentOfficeRoomPlan,
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
                OfficeRoomPlan candidatePlan = OfficeLayoutPlanner.CreatePlan(candidateLayout);

                currentLayout = candidateLayout;
                currentOfficeRoomPlan = candidatePlan;

                if (IsValidOfficeLayout(candidateLayout, candidatePlan))
                {
                    activeSeed = candidateSeed;
                    return;
                }
            }

            Debug.LogWarning($"Generated office layout did not meet validation after {layoutRetryAttempts} attempts; using the last candidate.");
        }

        private static bool IsValidOfficeLayout(DungeonLayout layout, OfficeRoomPlan plan)
        {
            return layout != null
                && plan != null
                && plan.HasRequiredRooms
                && layout.Start != layout.Exit
                && layout.ShortestPathLength >= 20
                && layout.MainRegionSize >= 300;
        }

        private void LogPlayerRoomTransition()
        {
            if (!logRoomTransitions || currentOfficeRoomPlan == null || entitySpawner.PlayerTransform == null)
            {
                return;
            }

            Vector3 playerPosition = entitySpawner.PlayerTransform.position;
            Vector2Int gridPosition = new(Mathf.RoundToInt(playerPosition.x), Mathf.RoundToInt(playerPosition.z));
            currentOfficeRoomPlan.TryGetRoleAt(gridPosition, out OfficeRoomRole playerRoomRole);

            if (playerRoomRole == currentPlayerRoomRole)
            {
                return;
            }

            if (currentPlayerRoomRole != OfficeRoomRole.None)
            {
                Debug.Log($"Exited {currentPlayerRoomRole}");
            }

            if (playerRoomRole != OfficeRoomRole.None)
            {
                Debug.Log($"Entered {playerRoomRole}");
            }

            currentPlayerRoomRole = playerRoomRole;
        }
    }
}
