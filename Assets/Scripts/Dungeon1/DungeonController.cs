using CM3070.PCG;
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
        [SerializeField] private int width = 58;
        [SerializeField] private int height = 42;
        [SerializeField] private int seed = 3070;
        [SerializeField] private bool randomizeSeedOnPlay = true;
        [SerializeField] private int minRoomSize = 5;
        [SerializeField] private int maxRoomSize = 12;
        [SerializeField] private int minPartitionSize = 13;
        [SerializeField] private int corridorWidth = 2;
        [SerializeField, Range(0f, 1f)] private float wallFillChance = 0.44f;
        [SerializeField, Range(0f, 1f)] private float cavePocketChance = 0.34f;
        [SerializeField] private int hybridSmoothingSteps = 2;

        [Header("Enemy And Loot Proportions")]
        [SerializeField, Range(0f, 0.2f)] private float enemyDensity = 0.035f;
        [SerializeField] private int maxEnemies = 24;
        [SerializeField, Range(0f, 0.08f)] private float lootDensity = 0.005f;
        [SerializeField] private int maxLoot = 14;
        [SerializeField] private int spawnExclusionRadius = 4;

        private DungeonVisualizer visualizer;
        private CameraController cameraController;
        private EntitySpawner entitySpawner;
        private DungeonLayout currentLayout;
        private int activeSeed;

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
            hybridSmoothingSteps = Mathf.Max(0, hybridSmoothingSteps);
            maxEnemies = Mathf.Max(0, maxEnemies);
            maxLoot = Mathf.Max(0, maxLoot);
            spawnExclusionRadius = Mathf.Max(0, spawnExclusionRadius);

            if (!Application.isPlaying && regenerateWhenInspectorChanges && isActiveAndEnabled)
            {
                visualizer = GetComponent<DungeonVisualizer>();
                cameraController = GetComponent<CameraController>();
                entitySpawner = GetComponent<EntitySpawner>();
                GenerateDungeon(false, true);
            }
        }

        private void GenerateDungeon(bool runtimeObjects, bool resetPlayerStats)
        {
            visualizer ??= GetComponent<DungeonVisualizer>();
            cameraController ??= GetComponent<CameraController>();
            entitySpawner ??= GetComponent<EntitySpawner>();

            cameraController.EnsureCameras(transform);

            DungeonGenerationSettings settings = BuildSettings();
            DungeonGenerator generator = new(settings);
            currentLayout = generator.Generate(runtimeObjects ? activeSeed : seed, DungeonGenerationMethod.HybridBspCellular);

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
    }
}
