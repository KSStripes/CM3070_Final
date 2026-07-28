using System.Collections.Generic;
using CM3070.Dungeon1;
using CM3070.Office.Quest;
using UnityEngine;

namespace CM3070.Office
{
    // Stores office quest items and optional coping pickups collected by the player.
    public sealed class OfficePlayerInventory : MonoBehaviour
    {
        private readonly Dictionary<QuestItemId, int> questItems = new();
        private readonly Dictionary<PickupId, int> pickups = new();

        public void AddQuestItem(QuestItemId itemId, int amount = 1)
        {
            if (itemId == QuestItemId.None || amount <= 0) return;

            questItems.TryGetValue(itemId, out int currentAmount);
            questItems[itemId] = currentAmount + amount;
            Debug.Log($"Quest item collected: {itemId} x{amount}. Total={questItems[itemId]}");
        }

        public bool HasQuestItem(QuestItemId itemId, int amount = 1)
        {
            if (itemId == QuestItemId.None || amount <= 0) return false;

            return questItems.TryGetValue(itemId, out int currentAmount)
                && currentAmount >= amount;
        }

        public bool RemoveQuestItem(QuestItemId itemId, int amount = 1)
        {
            if (!HasQuestItem(itemId, amount)) return false;

            int remainingAmount = questItems[itemId] - amount;
            if (remainingAmount > 0)
            {
                questItems[itemId] = remainingAmount;
            }
            else
            {
                questItems.Remove(itemId);
            }

            Debug.Log($"Quest item used: {itemId} x{amount}. Remaining={remainingAmount}");
            return true;
        }

        public bool AddPickup(PickupId pickupId, string displayName, int healthRestore, HealthSystem health)
        {
            if (pickupId == PickupId.None) return false;

            if (healthRestore > 0 && (health == null || !health.Heal(healthRestore)))
            {
                return false;
            }

            pickups.TryGetValue(pickupId, out int currentAmount);
            pickups[pickupId] = currentAmount + 1;

            Debug.Log($"Pickup collected: {displayName} ({pickupId}). Total={pickups[pickupId]}");
            return true;
        }

        public void ResetInventory()
        {
            // Reset only office inventory state; health is reset by HealthSystem.
            questItems.Clear();
            pickups.Clear();
        }
    }
}
