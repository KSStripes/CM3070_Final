using System.Collections.Generic;
using System;
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
    public readonly struct QuestInventorySnapshot
    {
        public QuestInventorySnapshot(QuestItemStack[] questItems)
        {
            QuestItems = questItems;
        }

        public QuestItemStack[] QuestItems { get; }
    }

    // Stores only task-critical quest items carried by the player.
    public sealed class QuestInventory : MonoBehaviour
    {
        private readonly Dictionary<QuestItemId, int> questItems = new();

        public event Action<QuestInventorySnapshot> InventoryChanged;

        public QuestInventorySnapshot CaptureSnapshot()
        {
            QuestItemStack[] questItemSnapshot = new QuestItemStack[questItems.Count];
            int index = 0;
            foreach (KeyValuePair<QuestItemId, int> item in questItems)
            {
                questItemSnapshot[index] = new QuestItemStack(item.Key, item.Value);
                index++;
            }

            return new QuestInventorySnapshot(questItemSnapshot);
        }

        public void AddQuestItem(QuestItemId itemId, int amount = 1)
        {
            if (itemId == QuestItemId.None || amount <= 0) return;

            questItems.TryGetValue(itemId, out int currentAmount);
            questItems[itemId] = currentAmount + amount;
            NotifyInventoryChanged();
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

            NotifyInventoryChanged();
            return true;
        }

        public void ResetInventory()
        {
            // Reset only office inventory state; health is reset by HealthSystem.
            questItems.Clear();
            NotifyInventoryChanged();
        }

        private void NotifyInventoryChanged()
        {
            InventoryChanged?.Invoke(CaptureSnapshot());
        }
    }
}
