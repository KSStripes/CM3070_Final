using System.Collections.Generic;
using CM3070.Dungeon1;
using CM3070.Office.Quest;
using CM3070.PCG;
using UnityEngine;
using UnityEngine.Serialization;

// Instantiates office-scene gameplay entities for a generated workplace layout.
namespace CM3070.Office
{
    public sealed class OfficeEntitySpawner : MonoBehaviour
    {
        private const int FirstNpcSlots = 6;

        [System.Serializable]
        private sealed class PickupSpawnOption
        {
            public GameObject prefab = null;
            [Min(0)] public int spawnWeight = 1;
        }

        [Header("Player")]
        [SerializeField] private GameObject playerPrefab;
        [SerializeField] private float playerMoveSpeed = 5.5f;

        [Header("NPCs")]
        [FormerlySerializedAs("enemyPrefabs")]
        [SerializeField] private GameObject[] npcPrefabs = new GameObject[FirstNpcSlots];

        [Header("Office Pickups")]
        [SerializeField] private PickupSpawnOption[] lootPrefabs;

        private Transform entityRoot;
        private DungeonLayout currentLayout;
        private DungeonVisualizer visualizer;
        private readonly HashSet<Vector2Int> blockedEntityPositions = new();
        private readonly HashSet<Vector2Int> occupiedNpcPositions = new();

        public OfficePlayerInventory PlayerInventory { get; private set; }
        public Transform PlayerTransform => PlayerInventory != null ? PlayerInventory.transform : null;

        private void OnValidate()
        {
            if (npcPrefabs == null)
            {
                npcPrefabs = new GameObject[FirstNpcSlots];
                return;
            }

            if (npcPrefabs.Length >= FirstNpcSlots)
            {
                return;
            }

            System.Array.Resize(ref npcPrefabs, FirstNpcSlots);
        }

        public void SpawnEntities(
            DungeonLayout layout,
            DungeonVisualizer dungeonVisualizer,
            bool runtimeObjects,
            bool resetPlayerStats,
            IReadOnlyCollection<Vector2Int> blockedPositions = null)
        {
            currentLayout = layout;
            visualizer = dungeonVisualizer;
            SetBlockedEntityPositions(blockedPositions);

            HealthSystem.HealthSnapshot? healthSnapshot = null;
            if (!resetPlayerStats && PlayerInventory != null)
            {
                if (PlayerInventory.TryGetComponent(out HealthSystem health))
                {
                    healthSnapshot = health.CaptureSnapshot();
                }
            }

            EnsureEntityRoot();
            ClearEntities();

            if (!runtimeObjects)
            {
                return;
            }

            SpawnPlayer(resetPlayerStats, healthSnapshot);
            SpawnNpcs();
            SpawnPickups();
        }

        private void SetBlockedEntityPositions(IReadOnlyCollection<Vector2Int> blockedPositions)
        {
            blockedEntityPositions.Clear();
            occupiedNpcPositions.Clear();

            if (blockedPositions == null)
            {
                return;
            }

            foreach (Vector2Int blockedPosition in blockedPositions)
            {
                blockedEntityPositions.Add(blockedPosition);
            }
        }

        private void EnsureEntityRoot()
        {
            if (entityRoot != null)
            {
                return;
            }

            Transform existing = transform.Find("Office Entities");
            if (existing != null)
            {
                entityRoot = existing;
                return;
            }

            GameObject root = new("Office Entities");
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
            HealthSystem.HealthSnapshot? healthSnapshot)
        {
            GameObject player = InstantiatePrefab(playerPrefab, "Player");
            if (player == null) return;

            player.transform.SetParent(entityRoot);
            player.transform.position = visualizer.GridToWorld(currentLayout.Start) + Vector3.up * 0.95f;

            if (!PlayerReady(player, resetPlayerStats, healthSnapshot))
            {
                Destroy(player);
            }
        }

        private void SpawnNpcs()
        {
            foreach (Vector2Int gridPosition in currentLayout.EnemyPositions)
            {
                if (!TryFindEntitySpawnPosition(gridPosition, out Vector2Int spawnPosition))
                {
                    continue;
                }

                GameObject npc = InstantiatePrefab(RandomNpcPrefab(), "NPC");
                if (npc == null) continue;

                npc.transform.SetParent(entityRoot);
                npc.transform.position = visualizer.GridToWorld(spawnPosition) + Vector3.up * 0.82f;

                if (!NpcReady(npc, spawnPosition))
                {
                    Destroy(npc);
                    continue;
                }

                occupiedNpcPositions.Add(spawnPosition);
            }
        }

        private void SpawnPickups()
        {
            foreach (Vector2Int gridPosition in currentLayout.LootPositions)
            {
                GameObject pickup = InstantiatePrefab(RandomPickupPrefab(), "Office Pickup");
                if (pickup == null) continue;

                pickup.transform.SetParent(entityRoot);
                pickup.transform.position = visualizer.GridToWorld(gridPosition) + Vector3.up * 0.36f;

                if (!PickupReady(pickup))
                {
                    Destroy(pickup);
                }
            }
        }

        private bool TryFindEntitySpawnPosition(Vector2Int preferredPosition, out Vector2Int spawnPosition)
        {
            if (CanSpawnEntityAt(preferredPosition))
            {
                spawnPosition = preferredPosition;
                return true;
            }

            for (int radius = 1; radius <= 4; radius++)
            {
                for (int x = preferredPosition.x - radius; x <= preferredPosition.x + radius; x++)
                {
                    for (int y = preferredPosition.y - radius; y <= preferredPosition.y + radius; y++)
                    {
                        Vector2Int candidate = new(x, y);
                        if (CanSpawnEntityAt(candidate))
                        {
                            spawnPosition = candidate;
                            return true;
                        }
                    }
                }
            }

            spawnPosition = preferredPosition;
            return false;
        }

        private bool CanSpawnEntityAt(Vector2Int position)
        {
            return currentLayout.IsWalkable(position)
                && !currentLayout.IsMarker(position)
                && position != currentLayout.Start
                && position != currentLayout.Exit
                && !blockedEntityPositions.Contains(position)
                && !occupiedNpcPositions.Contains(position);
        }

        private GameObject RandomPickupPrefab()
        {
            if (lootPrefabs == null || lootPrefabs.Length == 0) return null;

            int totalWeight = 0;
            foreach (PickupSpawnOption option in lootPrefabs)
            {
                if (option != null && option.prefab != null)
                {
                    totalWeight += option.spawnWeight;
                }
            }

            if (totalWeight <= 0) return null;

            int roll = Random.Range(0, totalWeight);
            foreach (PickupSpawnOption option in lootPrefabs)
            {
                if (option == null || option.prefab == null) continue;

                roll -= option.spawnWeight;
                if (roll < 0) return option.prefab;
            }

            return null;
        }

        private GameObject RandomNpcPrefab()
        {
            if (npcPrefabs == null || npcPrefabs.Length == 0) return null;

            int validCount = 0;
            foreach (GameObject prefab in npcPrefabs)
            {
                if (prefab != null)
                {
                    validCount++;
                }
            }

            if (validCount == 0) return null;

            int roll = Random.Range(0, validCount);
            foreach (GameObject prefab in npcPrefabs)
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
            HealthSystem.HealthSnapshot? healthSnapshot)
        {
            if (!player.TryGetComponent(out CharacterController _)) return Fail("Player", "missing CharacterController.");
            if (!player.TryGetComponent(out OfficePlayerInventory inventory)) return Fail("Player", "missing OfficePlayerInventory.");
            if (!player.TryGetComponent(out HealthSystem health)) return Fail("Player", "missing HealthSystem.");
            if (!player.TryGetComponent(out PlayerController controller)) return Fail("Player", "missing PlayerController.");

            PlayerInventory = inventory;

            if (resetPlayerStats)
            {
                PlayerInventory.ResetInventory();
                health.ResetHealth();
                QuestManager.Instance?.ResetShift();
            }
            if (!resetPlayerStats && healthSnapshot.HasValue)
            {
                health.ApplySnapshot(healthSnapshot.Value);
            }

            controller.Configure(playerMoveSpeed);

            // The player root keeps gameplay scripts; this only swaps the child avatar visuals.
            if (player.TryGetComponent(out PlayerAvatarPresenter avatarPresenter))
            {
                PlayerAvatarChoice avatarChoice = GameManager.Instance != null
                    ? GameManager.Instance.SelectedPlayerAvatar
                    : PlayerAvatarChoice.Female;
                avatarPresenter.Apply(avatarChoice);
            }

            return true;
        }

        private bool NpcReady(GameObject npc, Vector2Int gridPosition)
        {
            // Office NPC prefabs use dedicated components so Dungeon1 enemies stay independent.
            if (!npc.TryGetComponent(out Npc npcController)) return Fail("NPC", "missing Npc.");
            if (!npc.TryGetComponent(out NpcPatrol _)) return Fail("NPC", "missing NpcPatrol.");
            if (!npc.TryGetComponent(out NpcPressure _)) return Fail("NPC", "missing NpcPressure.");

            npcController.Configure(currentLayout, visualizer, gridPosition, blockedEntityPositions);
            return true;
        }

        private static bool PickupReady(GameObject pickup)
        {
            if (!pickup.TryGetComponent(out Collider pickupCollider)) return Fail("Office Pickup", "missing Collider.");
            if (!pickupCollider.isTrigger) return Fail("Office Pickup", "Collider must be trigger.");
            if (!pickup.TryGetComponent(out Rigidbody body)) return Fail("Office Pickup", "missing Rigidbody.");
            if (!body.isKinematic || body.useGravity) return Fail("Office Pickup", "Rigidbody must be kinematic/no gravity.");

            bool hasCopingPickup = pickup.TryGetComponent(out Pickup _);
            bool hasQuestItem = pickup.TryGetComponent(out ItemPickup _);
            if (!hasCopingPickup && !hasQuestItem)
            {
                return Fail("Office Pickup", "missing Pickup or ItemPickup.");
            }

            return true;
        }

        private static bool Fail(string label, string message)
        {
            Debug.LogError($"{label} prefab: {message}");
            return false;
        }
    }
}
