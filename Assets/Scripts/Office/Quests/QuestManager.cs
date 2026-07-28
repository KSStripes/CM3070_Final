using System.Collections.Generic;
using CM3070.Dungeon1;
using UnityEngine;

namespace CM3070.Office.Quest
{
    // Coordinates the first playable office loop: complete tasks, then leave.
    public sealed class QuestManager : MonoBehaviour
    {
        private readonly Dictionary<OfficeTaskMarkerId, List<OfficeQuestDefinition>> activeQuestsByMarker = new();
        private readonly Dictionary<QuestItemId, List<OfficeQuestDefinition>> activeCollectQuestsByItem = new();
        private readonly HashSet<OfficeQuestDefinition> activeQuests = new();
        private readonly HashSet<OfficeQuestDefinition> completedQuests = new();
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
            TryCompleteCollectQuest(itemId);
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

            if (markerId == OfficeTaskMarkerId.ExitMarker)
            {
                TryExit(displayName);
                return;
            }

            TryCompleteActiveMarkerQuest(markerId, displayName, inventory);
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
            activeCollectQuestsByItem.Clear();
            this.activeQuests.Clear();
            completedQuests.Clear();
            completedQuestMarkers.Clear();
            ShiftComplete = false;

            if (activeQuests == null) return;

            foreach (OfficeQuestDefinition quest in activeQuests)
            {
                if (quest == null) continue;

                this.activeQuests.Add(quest);

                // Store the lookup needed by each simple quest type.
                switch (quest.QuestType)
                {
                    case OfficeQuestType.DeliverItem:
                    case OfficeQuestType.VisitMarker:
                        AddQuestByMarker(quest);
                        break;
                    case OfficeQuestType.CollectItemOnly:
                        AddQuestByItem(quest);
                        break;
                }
            }
        }

        public void ResetShift()
        {
            ShiftComplete = false;
            completedQuests.Clear();
            completedQuestMarkers.Clear();
        }

        private bool CanCompleteActiveQuest(OfficeTaskMarkerId markerId, QuestInventory inventory)
        {
            if (inventory == null)
            {
                return false;
            }

            return TryFindCompletableMarkerQuest(markerId, inventory, out _);
        }

        private bool TryFindCompletableMarkerQuest(
            OfficeTaskMarkerId markerId,
            QuestInventory inventory,
            out OfficeQuestDefinition completableQuest)
        {
            completableQuest = null;

            if (!activeQuestsByMarker.TryGetValue(markerId, out List<OfficeQuestDefinition> quests))
            {
                return false;
            }

            foreach (OfficeQuestDefinition quest in quests)
            {
                if (completedQuests.Contains(quest)) continue;

                bool canComplete = quest.QuestType switch
                {
                    OfficeQuestType.VisitMarker => true,
                    OfficeQuestType.DeliverItem => inventory.HasItem(quest.RequiredItemId),
                    _ => false
                };

                if (canComplete)
                {
                    completableQuest = quest;
                    return true;
                }
            }

            return false;
        }

        private void TryCompleteActiveMarkerQuest(
            OfficeTaskMarkerId markerId,
            string displayName,
            QuestInventory inventory)
        {
            if (!activeQuestsByMarker.TryGetValue(markerId, out List<OfficeQuestDefinition> quests))
            {
                Debug.Log($"{displayName} reached. No active quest is assigned here.");
                return;
            }

            if (!TryFindCompletableMarkerQuest(markerId, inventory, out OfficeQuestDefinition quest))
            {
                Debug.Log($"{displayName} reached. No matching quest can be completed yet.");
                return;
            }

            if (quest.QuestType == OfficeQuestType.DeliverItem)
            {
                inventory.RemoveItem(quest.RequiredItemId);
            }

            completedQuests.Add(quest);
            Debug.Log($"{quest.QuestName} completed at {displayName}.");
            LogFeedbackComment(quest);

            if (IsMarkerFullyComplete(markerId))
            {
                completedQuestMarkers.Add(markerId);
            }
        }

        private void TryCompleteCollectQuest(QuestItemId itemId)
        {
            if (!activeCollectQuestsByItem.TryGetValue(itemId, out List<OfficeQuestDefinition> quests))
            {
                return;
            }

            foreach (OfficeQuestDefinition quest in quests)
            {
                if (completedQuests.Contains(quest)) continue;

                completedQuests.Add(quest);
                Debug.Log($"{quest.QuestName} completed by collecting {itemId}.");
                LogFeedbackComment(quest);
            }
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
            foreach (List<OfficeQuestDefinition> quests in activeQuestsByMarker.Values)
            {
                foreach (OfficeQuestDefinition quest in quests)
                {
                    if (!completedQuests.Contains(quest))
                    {
                        return false;
                    }
                }
            }

            foreach (List<OfficeQuestDefinition> quests in activeCollectQuestsByItem.Values)
            {
                foreach (OfficeQuestDefinition quest in quests)
                {
                    if (!completedQuests.Contains(quest))
                    {
                        return false;
                    }
                }
            }

            return activeQuests.Count > 0;
        }

        private bool IsMarkerFullyComplete(OfficeTaskMarkerId markerId)
        {
            if (!activeQuestsByMarker.TryGetValue(markerId, out List<OfficeQuestDefinition> quests))
            {
                return false;
            }

            foreach (OfficeQuestDefinition quest in quests)
            {
                if (!completedQuests.Contains(quest))
                {
                    return false;
                }
            }

            return true;
        }

        private static void LogFeedbackComment(OfficeQuestDefinition quest)
        {
            // Temporary UI stand-in: log the SO feedback text until UI exists.
            if (!string.IsNullOrWhiteSpace(quest.FeedbackComment))
            {
                Debug.Log(quest.FeedbackComment);
            }
        }

        private void AddQuestByMarker(OfficeQuestDefinition quest)
        {
            if (!activeQuestsByMarker.TryGetValue(quest.TaskMarkerId, out List<OfficeQuestDefinition> quests))
            {
                quests = new List<OfficeQuestDefinition>();
                activeQuestsByMarker[quest.TaskMarkerId] = quests;
            }

            quests.Add(quest);
        }

        private void AddQuestByItem(OfficeQuestDefinition quest)
        {
            if (!activeCollectQuestsByItem.TryGetValue(quest.RequiredItemId, out List<OfficeQuestDefinition> quests))
            {
                quests = new List<OfficeQuestDefinition>();
                activeCollectQuestsByItem[quest.RequiredItemId] = quests;
            }

            quests.Add(quest);
        }
    }
}
