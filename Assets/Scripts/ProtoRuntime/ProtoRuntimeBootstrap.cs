using CM3070.PCG;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace CM3070.ProtoRuntime
{
    [RequireComponent(typeof(ProtoDungeonVisualizer))]
    public sealed class ProtoRuntimeBootstrap : MonoBehaviour
    {
        [SerializeField] private DungeonGenerationSettings settings;
        [SerializeField] private Camera sceneCamera;
        [SerializeField] private TextMesh statusText;

        private ProtoDungeonVisualizer visualizer;
        private DungeonGenerationMethod activeMethod;
        private int activeSeed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void CreateDefaultBootstrap()
        {
            if (SceneManager.GetActiveScene().name != "ProtoScene")
            {
                return;
            }

            if (FindFirstObjectByType<ProtoRuntimeBootstrap>() != null)
            {
                return;
            }

            GameObject bootstrap = new("CM3070 Dungeon Prototype");
            bootstrap.AddComponent<ProtoDungeonVisualizer>();
            bootstrap.AddComponent<ProtoRuntimeBootstrap>();
        }

        private void Awake()
        {
            visualizer = GetComponent<ProtoDungeonVisualizer>();
            EnsureSettings();
            EnsureCameraAndLighting();
            EnsureStatusText();

            activeMethod = settings.method;
            activeSeed = settings.randomizeSeedOnPlay ? Random.Range(1, int.MaxValue) : settings.seed;
        }

        private void Start()
        {
            Generate();
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (keyboard.rKey.wasPressedThisFrame)
            {
                activeSeed = Random.Range(1, int.MaxValue);
                Generate();
            }

            if (keyboard.digit1Key.wasPressedThisFrame || keyboard.numpad1Key.wasPressedThisFrame)
            {
                activeMethod = DungeonGenerationMethod.BspRooms;
                Generate();
            }

            if (keyboard.digit2Key.wasPressedThisFrame || keyboard.numpad2Key.wasPressedThisFrame)
            {
                activeMethod = DungeonGenerationMethod.CellularAutomata;
                Generate();
            }

            if (keyboard.digit3Key.wasPressedThisFrame || keyboard.numpad3Key.wasPressedThisFrame)
            {
                activeMethod = DungeonGenerationMethod.HybridBspCellular;
                Generate();
            }
        }

        private void Generate()
        {
            DungeonGenerator generator = new(settings);
            DungeonLayout layout = generator.Generate(activeSeed, activeMethod);
            visualizer.Render(layout);
            PositionCamera(layout);
            UpdateStatus(layout);
        }

        private void EnsureSettings()
        {
            if (settings != null)
            {
                return;
            }

            settings = ScriptableObject.CreateInstance<DungeonGenerationSettings>();
            settings.name = "Runtime Default Dungeon Settings";
        }

        private void EnsureCameraAndLighting()
        {
            if (sceneCamera == null)
            {
                sceneCamera = Camera.main;
            }

            if (sceneCamera == null)
            {
                GameObject cameraObject = new("Isometric Camera");
                sceneCamera = cameraObject.AddComponent<Camera>();
                cameraObject.tag = "MainCamera";
            }

            sceneCamera.clearFlags = CameraClearFlags.SolidColor;
            sceneCamera.backgroundColor = new Color(0.10f, 0.12f, 0.13f);
            sceneCamera.orthographic = true;

            if (FindFirstObjectByType<Light>() == null)
            {
                GameObject lightObject = new("Factory Overhead Light");
                Light light = lightObject.AddComponent<Light>();
                light.type = LightType.Directional;
                light.color = new Color(0.94f, 0.96f, 0.92f);
                light.intensity = 1.35f;
                lightObject.transform.rotation = Quaternion.Euler(50f, -35f, 0f);
            }
        }

        private void EnsureStatusText()
        {
            if (statusText != null)
            {
                return;
            }

            GameObject textObject = new("Dungeon Generation Status");
            statusText = textObject.AddComponent<TextMesh>();
            statusText.anchor = TextAnchor.UpperLeft;
            statusText.alignment = TextAlignment.Left;
            statusText.characterSize = 0.42f;
            statusText.fontSize = 42;
            statusText.color = new Color(0.84f, 0.88f, 0.86f);
        }

// Method determining TopCamera position
        private void PositionCamera(DungeonLayout layout)
        {
            Vector3 center = new(layout.Width * 0.5f, 0f, layout.Height * 0.5f);
            float longestSide = Mathf.Max(layout.Width, layout.Height);
            sceneCamera.transform.position = center + Vector3.up * longestSide;
            sceneCamera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            sceneCamera.orthographic = true;
            sceneCamera.orthographicSize = longestSide * 0.8f;

            if (statusText != null)
            {
                statusText.transform.position = center + new Vector3(-layout.Width * 0.48f, 3.5f, layout.Height * 0.48f);
                statusText.transform.rotation = sceneCamera.transform.rotation;
            }
        }

        private void UpdateStatus(DungeonLayout layout)
        {
            if (statusText == null)
            {
                return;
            }

            statusText.text =
                $"CM3070 PCG Roguelite Prototype\n" +
                $"Method: {layout.Method}\n" +
                $"Seed: {layout.Seed}\n" +
                $"Walkable tiles: {layout.WalkableCount()} / {layout.Width * layout.Height}\n" +
                //$"Main region: {layout.MainRegionSize}\n" +
                //$"Start-exit path: {layout.ShortestPathLength}\n" +
                $"Enemies: {layout.EnemyPositions.Count}\n" +
                $"Loot: {layout.LootPositions.Count}\n" +
                // $"Difficulty estimate: {layout.EstimatedDifficulty:0.0}\n\n" +
                $"R new seed | 1 BSP | 2 cellular | 3 hybrid";
        }
    }
}
