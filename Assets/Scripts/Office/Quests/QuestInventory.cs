using System.Collections.Generic;
using UnityEngine;

namespace CM3070.Office.Quest
{
    // Player-owned inventory for office quest items, separate from dungeon loot/credits.
    public sealed class QuestInventory : MonoBehaviour
    {
        private readonly Dictionary<QuestItemId, int> itemCounts = new();

        public void AddItem(QuestItemId itemId, int amount = 1)
        {
            if (itemId == QuestItemId.None || amount <= 0) return;

            itemCounts.TryGetValue(itemId, out int currentAmount);
            itemCounts[itemId] = currentAmount + amount;
            Debug.Log($"Quest item collected: {itemId} x{amount}. Total={itemCounts[itemId]}");
        }

        public bool HasItem(QuestItemId itemId, int amount = 1)
        {
            if (itemId == QuestItemId.None || amount <= 0) return false;

            return itemCounts.TryGetValue(itemId, out int currentAmount)
                && currentAmount >= amount;
        }

        public bool RemoveItem(QuestItemId itemId, int amount = 1)
        {
            if (!HasItem(itemId, amount)) return false;

            int remainingAmount = itemCounts[itemId] - amount;
            if (remainingAmount > 0)
            {
                itemCounts[itemId] = remainingAmount;
            }
            else
            {
                itemCounts.Remove(itemId);
            }

            Debug.Log($"Quest item used: {itemId} x{amount}. Remaining={remainingAmount}");
            return true;
        }

        public void ResetQuestInventory()
        {
            itemCounts.Clear();
        }
    }
}
