using UnityEngine;

// Handles trigger pickup behaviour for loot prefabs.
// The actual pickup data lives on LootProperties so each prefab can configure its own effect.
namespace CM3070.Dungeon1
{
    public sealed class LootPickup : MonoBehaviour
    {
        [SerializeField] private float rotationSpeed = 85f;

        private void Update()
        {
            // Simple visibility cue so pickups are easier to see in the prototype.
            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
        }

        private void OnTriggerEnter(Collider other)
        {
            // The player prefab owns inventory; child colliders can still trigger pickup.
            PlayerInventory inventory = other.GetComponentInParent<PlayerInventory>();
            LootProperties lootProperties = GetComponent<LootProperties>();
            if (inventory == null || lootProperties == null)
            {
                return;
            }

            switch (lootProperties.LootType)
            {
                // Each loot type delegates to the system that owns that state.
                case LootType.Coin:
                    inventory.AddCoins(lootProperties.LootValue);
                    break;
                case LootType.Health:
                    if (!ApplyHealth(other, lootProperties)) return;
                    break;
                case LootType.Armour:
                    if (!ApplyArmour(other, inventory, lootProperties)) return;
                    break;
                case LootType.Weapon:
                    inventory.AddWeapon(lootProperties);
                    break;
            }
            // Only successful pickups are removed.
            Destroy(gameObject);
        }

        private static bool ApplyHealth(Component player, LootProperties lootProperties)
        {
            HealthSystem health = player.GetComponentInParent<HealthSystem>();
            if (health == null) return false;
            // Returning false leaves a full-health pickup in the scene for later.
            if (health.Heal(lootProperties.HealAmount)) return true;
            return false;
        }

        private static bool ApplyArmour(Component player, PlayerInventory inventory, LootProperties lootProperties)
        {
            HealthSystem health = player.GetComponentInParent<HealthSystem>();
            if (health == null) return false;

            inventory.AddArmour(lootProperties, health);
            return true;
        }
    }
}
