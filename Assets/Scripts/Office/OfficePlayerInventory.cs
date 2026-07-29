using System.Collections.Generic;
using System;
using CM3070.Dungeon1;
using CM3070.Office.Quest;
using UnityEngine;

namespace CM3070.Office
{
    // Immutable UI/debug view of one quest item count.
    public readonly struct QuestItemStack
    {
        public QuestItemStack(QuestItemId itemId, int amount)
        {
            ItemId = itemId;
            Amount = amount;
        }

        public QuestItemId ItemId { get; }
        public int Amount { get; }
    }

    // Immutable UI/debug view of one optional coping pickup count.
    public readonly struct PickupStack
    {
        public PickupStack(PickupId pickupId, int amount)
        {
            PickupId = pickupId;
            Amount = amount;
        }

        public PickupId PickupId { get; }
        public int Amount { get; }
    }

    // Full office inventory snapshot for HUDs that need to redraw from current state.
    public readonly struct OfficeInventorySnapshot
    {
        public OfficeInventorySnapshot(QuestItemStack[] questItems, PickupStack[] pickups)
        {
            QuestItems = questItems;
            Pickups = pickups;
        }

        public QuestItemStack[] QuestItems { get; }
        public PickupStack[] Pickups { get; }
    }

    // Stores office quest items and optional coping pickups collected by the player.
    public sealed class OfficePlayerInventory : MonoBehaviour
    {
        private readonly Dictionary<QuestItemId, int> questItems = new();
        private readonly Dictionary<PickupId, int> pickups = new();

        // Future HUD scripts can subscribe here instead of polling private dictionaries.
        public event Action<OfficeInventorySnapshot> InventoryChanged;
        public event Action<QuestItemId, int> QuestItemCountChanged;
        public event Action<PickupId, string, int> PickupCountChanged;

        public OfficeInventorySnapshot CaptureSnapshot()
        {
            QuestItemStack[] questItemSnapshot = new QuestItemStack[questItems.Count];
            int index = 0;
            foreach (KeyValuePair<QuestItemId, int> item in questItems)
            {
                questItemSnapshot[index] = new QuestItemStack(item.Key, item.Value);
                index++;
            }

            PickupStack[] pickupSnapshot = new PickupStack[pickups.Count];
            index = 0;
            foreach (KeyValuePair<PickupId, int> pickup in pickups)
            {
                pickupSnapshot[index] = new PickupStack(pickup.Key, pickup.Value);
                index++;
            }

            return new OfficeInventorySnapshot(questItemSnapshot, pickupSnapshot);
        }

        public int GetQuestItemCount(QuestItemId itemId)
        {
            return questItems.TryGetValue(itemId, out int amount) ? amount : 0;
        }

        public int GetPickupCount(PickupId pickupId)
        {
            return pickups.TryGetValue(pickupId, out int amount) ? amount : 0;
        }

        public void AddQuestItem(QuestItemId itemId, int amount = 1)
        {
            if (itemId == QuestItemId.None || amount <= 0) return;

            questItems.TryGetValue(itemId, out int currentAmount);
            questItems[itemId] = currentAmount + amount;
            Debug.Log($"[Inventory] Quest item collected: {itemId} x{amount}. Total={questItems[itemId]}");
            NotifyQuestItemChanged(itemId);
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

            Debug.Log($"[Inventory] Quest item used: {itemId} x{amount}. Remaining={GetQuestItemCount(itemId)}");
            NotifyQuestItemChanged(itemId);
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

            Debug.Log($"[Inventory] Coping pickup collected: {displayName} ({pickupId}). Total={pickups[pickupId]}");
            PickupCountChanged?.Invoke(pickupId, displayName, pickups[pickupId]);
            NotifyInventoryChanged();
            return true;
        }

        public void ResetInventory()
        {
            // Reset only office inventory state; health is reset by HealthSystem.
            questItems.Clear();
            pickups.Clear();
            Debug.Log("[Inventory] Office inventory reset.");
            NotifyInventoryChanged();
        }

        private void NotifyQuestItemChanged(QuestItemId itemId)
        {
            QuestItemCountChanged?.Invoke(itemId, GetQuestItemCount(itemId));
            NotifyInventoryChanged();
        }

        private void NotifyInventoryChanged()
        {
            InventoryChanged?.Invoke(CaptureSnapshot());
        }
    }
}
