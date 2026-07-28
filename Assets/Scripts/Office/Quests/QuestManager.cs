using System.Collections.Generic;
using CM3070.Dungeon1;
using UnityEngine;

namespace CM3070.Office.Quest
{
    // Coordinates the first playable office loop: complete tasks, then leave.
    public sealed class QuestManager : MonoBehaviour
    {
        private readonly Dictionary<OfficeTaskMarkerId, OfficeQuestDefinition> activeQuestsByMarker = new();
        private readonly HashSet<OfficeTaskMarkerId> completedQuestMarkers = new();

        public static QuestManager Instance { get; private set; }

        public bool ShiftComplete { get; private set; }

        private void Awake()
        {
            // Scene-level singleton mirrors GameManager for simple prototype access.
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        public void NotifyItemCollected(QuestItemId itemId, string displayName, int amount)
        {
            Debug.Log($"QuestManager noted pickup: {displayName} ({itemId}) x{amount}");
        }

        public void NotifyPickupCollected(PickupId pickupId, string displayName)
        {
            Debug.Log($"QuestManager noted coping pickup: {displayName} ({pickupId})");
        }

        public void NotifyMarkerReached(
            OfficeTaskMarkerId markerId,
            string displayName,
            QuestInventory inventory)
        {
            if (inventory == null || ShiftComplete) return;

            switch (markerId)
            {
                case OfficeTaskMarkerId.ExitMarker:
                    TryExit(displayName);
                    break;
                case OfficeTaskMarkerId.DeliveryPoint:
                case OfficeTaskMarkerId.Printer:
                case OfficeTaskMarkerId.BossDesk:
                case OfficeTaskMarkerId.MeetingArea:
                    TryCompleteActiveQuest(markerId, displayName, inventory);
                    break;
            }
        }

        public bool CanUseMarker(OfficeTaskMarkerId markerId, QuestInventory inventory)
        {
            if (ShiftComplete) return false;

            return markerId switch
            {
                OfficeTaskMarkerId.ExitMarker => RequiredTasksComplete(),
                _ => CanCompleteActiveQuest(markerId, inventory)
            };
        }

        public bool IsMarkerCompleted(OfficeTaskMarkerId markerId)
        {
            return markerId switch
            {
                OfficeTaskMarkerId.ExitMarker => ShiftComplete,
                _ => completedQuestMarkers.Contains(markerId)
            };
        }

        public void ConfigureActiveQuests(IEnumerable<OfficeQuestDefinition> activeQuests)
        {
            activeQuestsByMarker.Clear();
            completedQuestMarkers.Clear();
            ShiftComplete = false;

            if (activeQuests == null) return;

            foreach (OfficeQuestDefinition quest in activeQuests)
            {
                if (quest == null || !quest.IsRequiredQuest) continue;

                // One marker represents one active task in the current prototype loop.
                activeQuestsByMarker[quest.TaskMarkerId] = quest;
            }
        }

        public void ResetShift()
        {
            ShiftComplete = false;
            completedQuestMarkers.Clear();
        }

        private bool CanCompleteActiveQuest(OfficeTaskMarkerId markerId, QuestInventory inventory)
        {
            if (inventory == null || completedQuestMarkers.Contains(markerId))
            {
                return false;
            }

            return activeQuestsByMarker.TryGetValue(markerId, out OfficeQuestDefinition quest)
                && inventory.HasItem(quest.RequiredItemId);
        }

        private void TryCompleteActiveQuest(
            OfficeTaskMarkerId markerId,
            string displayName,
            QuestInventory inventory)
        {
            if (!activeQuestsByMarker.TryGetValue(markerId, out OfficeQuestDefinition quest))
            {
                Debug.Log($"{displayName} reached. No active quest is assigned here.");
                return;
            }

            if (completedQuestMarkers.Contains(markerId))
            {
                Debug.Log($"{quest.QuestName} is already complete.");
                return;
            }

            if (!inventory.RemoveItem(quest.RequiredItemId))
            {
                Debug.Log($"{quest.QuestName} needs {quest.RequiredItemId}.");
                return;
            }

            completedQuestMarkers.Add(markerId);
            Debug.Log($"{quest.QuestName} completed at {displayName}.");
        }

        private void TryExit(string displayName)
        {
            if (!RequiredTasksComplete())
            {
                Debug.Log("Exit is locked. Complete required tasks first.");
                return;
            }

            ShiftComplete = true;
            Debug.Log($"Exited through {displayName}. Shift complete.");
            GameManager.Instance?.NotifyExitReached();
        }

        private bool RequiredTasksComplete()
        {
            foreach (OfficeTaskMarkerId markerId in activeQuestsByMarker.Keys)
            {
                if (!completedQuestMarkers.Contains(markerId))
                {
                    return false;
                }
            }

            return activeQuestsByMarker.Count > 0;
        }
    }
}
