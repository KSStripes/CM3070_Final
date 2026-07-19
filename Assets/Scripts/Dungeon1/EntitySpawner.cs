using CM3070.PCG;
using UnityEngine;

// Instantiates runtime gameplay entities for a generated dungeon.
// Visual tiles stay in DungeonVisualizer; player, enemies, and loot prefabs are handled here.
namespace CM3070.Dungeon1
{
    public sealed class EntitySpawner : MonoBehaviour
    {
        [System.Serializable]
        private sealed class LootSpawnOption
        {
            public GameObject prefab = null;
            [Min(0)] public int spawnWeight = 1;
        }

        [Header("Player")]
        [SerializeField] private GameObject playerPrefab;
        [SerializeField] private float playerMoveSpeed = 5.5f;

        [Header("Enemies")]
        [SerializeField] private GameObject[] enemyPrefabs;

        [Header("Loot")]
        [SerializeField] private LootSpawnOption[] lootPrefabs;

        private Transform entityRoot;
        private DungeonLayout currentLayout;
        private DungeonVisualizer visualizer;

        public PlayerInventory PlayerInventory { get; private set; }
        public Transform PlayerTransform => PlayerInventory != null ? PlayerInventory.transform : null;

        public void SpawnEntities(DungeonLayout layout, DungeonVisualizer dungeonVisualizer, bool runtimeObjects, bool resetPlayerStats)
        {
            // Store layout context so spawned entities can be configured after instantiation.
            currentLayout = layout;
            visualizer = dungeonVisualizer;

            PlayerInventory.InventorySnapshot? inventorySnapshot = null;
            HealthSystem.HealthSnapshot? healthSnapshot = null;
            if (!resetPlayerStats && PlayerInventory != null)
            {
                inventorySnapshot = PlayerInventory.CaptureSnapshot();
                if (PlayerInventory.TryGetComponent(out HealthSystem health))
                {
                    healthSnapshot = health.CaptureSnapshot();
                }
            }

            EnsureEntityRoot();
            ClearEntities();

            if (!runtimeObjects)
            {
                // Edit-mode preview shows visual/gizmo output only, not live gameplay actors.
                return;
            }

            SpawnPlayer(resetPlayerStats, inventorySnapshot, healthSnapshot);
            SpawnEnemies();
            SpawnLoot();
        }

        private void EnsureEntityRoot()
        {
            if (entityRoot != null)
            {
                return;
            }

            Transform existing = transform.Find("Game Entities");
            if (existing != null)
            {
                // Reuse the container across regenerations.
                entityRoot = existing;
                return;
            }

            GameObject root = new("Game Entities");
            root.transform.SetParent(transform);
            entityRoot = root.transform;
        }

        private void ClearEntities()
        {
            PlayerInventory = null;

            if (entityRoot == null)
            {
                return;
            }

            for (int i = entityRoot.childCount - 1; i >= 0; i--)
            {
                GameObject child = entityRoot.GetChild(i).gameObject;

                // Use the correct destroy path for play mode versus edit-mode preview.
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

        private void SpawnPlayer(
            bool resetPlayerStats,
            PlayerInventory.InventorySnapshot? inventorySnapshot,
            HealthSystem.HealthSnapshot? healthSnapshot)
        {
            GameObject player = InstantiatePrefab(playerPrefab, "Player");
            if (player == null) return;

            player.transform.SetParent(entityRoot);
            // Lift the prefab so its collider/mesh sits above the floor tile.
            player.transform.position = visualizer.GridToWorld(currentLayout.Start) + Vector3.up * 0.95f;

            if (!PlayerReady(player, resetPlayerStats, inventorySnapshot, healthSnapshot))
            {
                Destroy(player);
            }
        }

        private void SpawnEnemies()
        {
            foreach (Vector2Int gridPosition in currentLayout.EnemyPositions)
            {
                GameObject enemy = InstantiatePrefab(RandomEnemyPrefab(), "Enemy");
                if (enemy == null) continue;

                enemy.transform.SetParent(entityRoot);
                // Enemy positions come from the PCG furnishing pass.
                enemy.transform.position = visualizer.GridToWorld(gridPosition) + Vector3.up * 0.82f;

                if (!EnemyReady(enemy, gridPosition))
                {
                    Destroy(enemy);
                }
            }
        }

        private void SpawnLoot()
        {
            foreach (Vector2Int gridPosition in currentLayout.LootPositions)
            {
                GameObject loot = InstantiatePrefab(RandomLootPrefab(), "Loot");
                if (loot == null) continue;

                loot.transform.SetParent(entityRoot);
                // Loot is placed lower than actors because current prefabs are small pickups.
                loot.transform.position = visualizer.GridToWorld(gridPosition) + Vector3.up * 0.36f;

                if (!LootReady(loot))
                {
                    Destroy(loot);
                }
            }
        }

        private GameObject RandomLootPrefab()
        {
            if (lootPrefabs == null || lootPrefabs.Length == 0) return null;

            int totalWeight = 0;

            foreach (LootSpawnOption option in lootPrefabs)
            {
                if (option != null && option.prefab != null)
                {
                    // Inspector weights let common drops, such as coins, appear more often.
                    totalWeight += option.spawnWeight;
                }
            }

            if (totalWeight <= 0) return null;

            int roll = Random.Range(0, totalWeight);

            foreach (LootSpawnOption option in lootPrefabs)
            {
                if (option == null || option.prefab == null) continue;

                // Weighted random selection: subtract until the rolled bucket is reached.
                roll -= option.spawnWeight;
                if (roll < 0) return option.prefab;
            }

            return null;
        }

        private GameObject RandomEnemyPrefab()
        {
            if (enemyPrefabs == null || enemyPrefabs.Length == 0) return null;

            int validCount = 0;
            foreach (GameObject prefab in enemyPrefabs)
            {
                if (prefab != null)
                {
                    validCount++;
                }
            }

            if (validCount == 0) return null;

            int roll = Random.Range(0, validCount);
            foreach (GameObject prefab in enemyPrefabs)
            {
                if (prefab == null) continue;
                if (roll == 0) return prefab;
                roll--;
            }

            return null;
        }

        private static GameObject InstantiatePrefab(GameObject prefab, string label)
        {
            if (prefab != null) return Instantiate(prefab);
            Debug.LogError($"{label} prefab is missing.");
            return null;
        }

        private bool PlayerReady(
            GameObject player,
            bool resetPlayerStats,
            PlayerInventory.InventorySnapshot? inventorySnapshot,
            HealthSystem.HealthSnapshot? healthSnapshot)
        {
            // Validate prefab contracts early so setup mistakes are visible in the Console.
            if (!player.TryGetComponent(out CharacterController _)) return Fail("Player", "missing CharacterController.");
            if (!player.TryGetComponent(out PlayerInventory inventory)) return Fail("Player", "missing PlayerInventory.");
            if (!player.TryGetComponent(out HealthSystem health)) return Fail("Player", "missing HealthSystem.");
            if (!player.TryGetComponent(out PlayerController controller)) return Fail("Player", "missing PlayerController.");

            PlayerInventory = inventory;
            if (resetPlayerStats)
            {
                PlayerInventory.ResetInventory();
                health.ResetHealth();
            }
            else if (inventorySnapshot.HasValue)
            {
                PlayerInventory.ApplySnapshot(inventorySnapshot.Value);
            }

            if (!resetPlayerStats && healthSnapshot.HasValue)
            {
                health.ApplySnapshot(healthSnapshot.Value);
            }

            controller.Configure(playerMoveSpeed);
            return true;
        }

        private bool EnemyReady(GameObject enemy, Vector2Int gridPosition)
        {
            if (!enemy.TryGetComponent(out Enemy enemyController)) return Fail("Enemy", "missing Enemy.");
            if (!enemy.TryGetComponent(out EnemyPatrol _)) return Fail("Enemy", "missing EnemyPatrol.");
            if (!enemy.TryGetComponent(out EnemyAttack _)) return Fail("Enemy", "missing EnemyAttack.");

            enemyController.Configure(currentLayout, visualizer, gridPosition);
            return true;
        }

        private static bool LootReady(GameObject loot)
        {
            // Loot uses trigger physics; the pickup script owns the gameplay effect.
            if (!loot.TryGetComponent(out Collider collider)) return Fail("Loot", "missing Collider.");
            if (!collider.isTrigger) return Fail("Loot", "Collider must be trigger.");
            if (!loot.TryGetComponent(out Rigidbody body)) return Fail("Loot", "missing Rigidbody.");
            if (!body.isKinematic || body.useGravity) return Fail("Loot", "Rigidbody must be kinematic/no gravity.");
            if (!loot.TryGetComponent(out LootPickup _)) return Fail("Loot", "missing LootPickup.");
            if (!loot.TryGetComponent(out LootProperties _)) return Fail("Loot", "missing LootProperties.");
            return true;
        }

        private static bool Fail(string label, string message)
        {
            Debug.LogError($"{label} prefab: {message}");
            return false;
        }
    }
}
