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

    // Quest-item snapshot for HUD redraws.
    public readonly struct OfficeInventorySnapshot
    {
        public OfficeInventorySnapshot(QuestItemStack[] questItems)
        {
            QuestItems = questItems;
        }

        public QuestItemStack[] QuestItems { get; }
    }

    // Stores office quest items and optional coping pickups collected by the player.
    public sealed class OfficePlayerInventory : MonoBehaviour
    {
        private readonly Dictionary<QuestItemId, int> questItems = new();
        private readonly Dictionary<PickupId, int> pickups = new();

        public event Action<OfficeInventorySnapshot> InventoryChanged;

        public OfficeInventorySnapshot CaptureSnapshot()
        {
            QuestItemStack[] questItemSnapshot = new QuestItemStack[questItems.Count];
            int index = 0;
            foreach (KeyValuePair<QuestItemId, int> item in questItems)
            {
                questItemSnapshot[index] = new QuestItemStack(item.Key, item.Value);
                index++;
            }

            return new OfficeInventorySnapshot(questItemSnapshot);
        }

        public int GetQuestItemCount(QuestItemId itemId)
        {
            return questItems.TryGetValue(itemId, out int amount) ? amount : 0;
        }

        public void AddQuestItem(QuestItemId itemId, int amount = 1)
        {
            if (itemId == QuestItemId.None || amount <= 0) return;

            questItems.TryGetValue(itemId, out int currentAmount);
            questItems[itemId] = currentAmount + amount;
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

            NotifyInventoryChanged();
            return true;
        }

        public void ResetInventory()
        {
            // Reset only office inventory state; health is reset by HealthSystem.
            questItems.Clear();
            pickups.Clear();
            NotifyInventoryChanged();
        }

        private void NotifyQuestItemChanged(QuestItemId itemId)
        {
            NotifyInventoryChanged();
        }

        private void NotifyInventoryChanged()
        {
            InventoryChanged?.Invoke(CaptureSnapshot());
        }
    }
}
