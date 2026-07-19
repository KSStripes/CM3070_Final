using CM3070.PCG;
using CM3070.Office;
using UnityEngine;
using UnityEngine.InputSystem;

// Scene-level coordinator for Dungeon1.
// Generates the hybrid dungeon, asks the visualizer to render it, spawns gameplay entities, and positions cameras.
namespace CM3070.Dungeon1
{
    [ExecuteAlways]
    [RequireComponent(typeof(DungeonVisualizer))]
    [RequireComponent(typeof(CameraController))]
    [RequireComponent(typeof(EntitySpawner))]
    public sealed class DungeonController : MonoBehaviour
    {
        [Header("Edit Mode")]
        [SerializeField] private bool generatePreviewInEditMode = true;
        [SerializeField] private bool regenerateWhenInspectorChanges;

        [Header("Hybrid Dungeon")]
        [SerializeField] private int width = 64;
        [SerializeField] private int height = 46;
        [SerializeField] private int seed = 3070;
        [SerializeField] private bool randomizeSeedOnPlay = true;
        // BSP room bounds: raising these makes each carved room-like workplace section larger.
        [SerializeField] private int minRoomSize = 8;
        [SerializeField] private int maxRoomSize = 16;
        // BSP leaf size: larger partitions give the generator space to fit larger rooms.
        [SerializeField] private int minPartitionSize = 18;
        // Corridor carving width: 2 carves a 3-tile-wide path, enough for NPC/item clearance.
        [SerializeField] private int corridorWidth = 2;
        [SerializeField] private int maxSplitDepth = 5;
        [SerializeField, Range(0f, 1f)] private float wallFillChance = 0.44f;
        // Hybrid CA roughness: higher values disrupt non-preserved BSP floors/walls more.
        [SerializeField, Range(0f, 1f)] private float cavePocketChance = 0.34f;
        // Hybrid CA smoothing passes: higher values make noisy pockets more cave-like.
        [SerializeField] private int hybridSmoothingSteps = 2;

        [Header("Enemy And Loot Proportions")]
        [SerializeField, Range(0f, 0.2f)] private float enemyDensity = 0.035f;
        [SerializeField] private int maxEnemies = 24;
        [SerializeField, Range(0f, 0.08f)] private float lootDensity = 0.005f;
        [SerializeField] private int maxLoot = 14;
        [SerializeField] private int spawnExclusionRadius = 4;

        [Header("Office Debug")]
        [SerializeField] private bool logRoomTransitions = true;

        private DungeonVisualizer visualizer;
        private CameraController cameraController;
        private EntitySpawner entitySpawner;
        private OfficePropPlacer officePropPlacer;
        private DungeonLayout currentLayout;
        private OfficeRoomPlan currentOfficeRoomPlan;
        private OfficeRoomRole currentPlayerRoomRole = OfficeRoomRole.None;
        private int activeSeed;

        public OfficeRoomPlan CurrentOfficeRoomPlan => currentOfficeRoomPlan;

        [ContextMenu("Generate Hybrid Preview")]
        public void GenerateHybridPreview()
        {
            // Lets the preview be rebuilt from the Inspector context menu.
            GenerateDungeon(Application.isPlaying, true);
        }

        public void StartNewGame()
        {
            // New Game resets player inventory/stats.
            activeSeed = NextSeed();
            GenerateDungeon(true, true);
        }

        public void StartNextLevel()
        {
            // Next Level keeps current player inventory/stats.
            activeSeed = NextSeed();
            GenerateDungeon(true, false);
        }

        private void OnEnable()
        {
            // Cache sibling systems required by the scene coordinator.
            visualizer = GetComponent<DungeonVisualizer>();
            cameraController = GetComponent<CameraController>();
            entitySpawner = GetComponent<EntitySpawner>();
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
                // Fallback for testing this scene without a GameManager.
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
                // R gives a quick playable regeneration loop for testing layouts.
                StartNewGame();
            }

            LogPlayerRoomTransition();
        }

        private void LateUpdate()
        {
            if (Application.isPlaying && entitySpawner.PlayerTransform != null)
            {
                // LateUpdate follows after player movement, reducing camera jitter.
                cameraController.PositionPlayerCamera(entitySpawner.PlayerTransform.position);
            }
        }

        private void OnValidate()
        {
            // Clamp Inspector values to generation-safe ranges.
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

            if (!Application.isPlaying && regenerateWhenInspectorChanges && isActiveAndEnabled)
            {
                visualizer = GetComponent<DungeonVisualizer>();
                cameraController = GetComponent<CameraController>();
                entitySpawner = GetComponent<EntitySpawner>();
                officePropPlacer = GetComponent<OfficePropPlacer>();
                GenerateDungeon(false, true);
            }
        }

        private void GenerateDungeon(bool runtimeObjects, bool resetPlayerStats)
        {
            visualizer ??= GetComponent<DungeonVisualizer>();
            cameraController ??= GetComponent<CameraController>();
            entitySpawner ??= GetComponent<EntitySpawner>();
            officePropPlacer ??= GetComponent<OfficePropPlacer>();

            cameraController.EnsureCameras(transform);

            DungeonGenerationSettings settings = BuildSettings();
            DungeonGenerator generator = new(settings);
            currentLayout = generator.Generate(runtimeObjects ? activeSeed : seed, DungeonGenerationMethod.HybridBspCellular);
            currentOfficeRoomPlan = OfficeLayoutPlanner.CreatePlan(currentLayout);
            officePropPlacer?.SetRoomPlan(currentOfficeRoomPlan);
            currentPlayerRoomRole = OfficeRoomRole.None;

            // Spawn markers are useful in edit-mode, but hidden during gameplay.
            visualizer.SetRenderSpawnMarkers(!runtimeObjects);
            visualizer.Render(currentLayout);

            entitySpawner.SpawnEntities(currentLayout, visualizer, runtimeObjects, resetPlayerStats);

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
            // Convert density into an absolute loot count for the generator.
            settings.lootCount = Mathf.Min(maxLoot, Mathf.RoundToInt(width * height * lootDensity));
            settings.spawnExclusionRadius = spawnExclusionRadius;
            return settings;
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
