using System;
using System.Collections.Generic;
using CM3070.Dungeon1;
using CM3070.Office;
using UnityEngine;

namespace CM3070.Office.Quest
{
    // Read-only progress for one active quest row in the HUD.
    public readonly struct QuestProgressSnapshot
    {
        public QuestProgressSnapshot(OfficeQuestDefinition quest, bool isCompleted)
        {
            Quest = quest;
            IsCompleted = isCompleted;
        }

        public OfficeQuestDefinition Quest { get; }
        public bool IsCompleted { get; }
    }

    // Current quest state needed by OfficeHUD.
    public readonly struct QuestStateSnapshot
    {
        public QuestStateSnapshot(
            QuestProgressSnapshot[] quests,
            bool requiredTasksComplete,
            bool shiftComplete)
        {
            Quests = quests;
            RequiredTasksComplete = requiredTasksComplete;
            ShiftComplete = shiftComplete;
        }

        public QuestProgressSnapshot[] Quests { get; }
        public bool RequiredTasksComplete { get; }
        public bool ShiftComplete { get; }
    }

    // Coordinates the playable office loop: complete tasks, then leave.
    public sealed class QuestManager : MonoBehaviour
    {
        private readonly Dictionary<OfficeTaskMarkerId, List<OfficeQuestDefinition>> activeQuestsByMarker = new();
        private readonly Dictionary<QuestItemId, List<OfficeQuestDefinition>> activeCollectQuestsByItem = new();
        private readonly List<OfficeQuestDefinition> activeQuests = new();
        private readonly HashSet<OfficeQuestDefinition> completedQuests = new();
        private readonly HashSet<OfficeTaskMarkerId> completedQuestMarkers = new();

        public static QuestManager Instance { get; private set; }

        public bool ShiftComplete { get; private set; }

        public event Action<QuestStateSnapshot> QuestStateChanged;
        public event Action<string> FeedbackPublished;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        public void ConfigureActiveQuests(IEnumerable<OfficeQuestDefinition> quests)
        {
            activeQuestsByMarker.Clear();
            activeCollectQuestsByItem.Clear();
            activeQuests.Clear();
            completedQuests.Clear();
            completedQuestMarkers.Clear();
            ShiftComplete = false;

            if (quests != null)
            {
                foreach (OfficeQuestDefinition quest in quests)
                {
                    AddActiveQuest(quest);
                }
            }

            NotifyQuestStateChanged();
        }

        public void ResetShift()
        {
            ShiftComplete = false;
            completedQuests.Clear();
            completedQuestMarkers.Clear();
            NotifyQuestStateChanged();
        }

        public QuestStateSnapshot CaptureSnapshot()
        {
            QuestProgressSnapshot[] questSnapshots = new QuestProgressSnapshot[activeQuests.Count];
            for (int i = 0; i < activeQuests.Count; i++)
            {
                OfficeQuestDefinition quest = activeQuests[i];
                questSnapshots[i] = new QuestProgressSnapshot(quest, completedQuests.Contains(quest));
            }

            return new QuestStateSnapshot(
                questSnapshots,
                RequiredTasksComplete(),
                ShiftComplete);
        }

        public void NotifyItemCollected(QuestItemId itemId, string displayName, int amount)
        {
            TryCompleteCollectQuest(itemId);
        }

        public void NotifyMarkerReached(
            OfficeTaskMarkerId markerId,
            string displayName,
            QuestInventory inventory)
        {
            if (inventory == null || ShiftComplete) return;

            if (markerId == OfficeTaskMarkerId.ExitMarker)
            {
                TryExit();
                return;
            }

            TryCompleteMarkerQuest(markerId, inventory);
        }

        public bool CanUseMarker(OfficeTaskMarkerId markerId, QuestInventory inventory)
        {
            if (ShiftComplete) return false;

            return markerId == OfficeTaskMarkerId.ExitMarker
                ? RequiredTasksComplete()
                : TryFindCompletableMarkerQuest(markerId, inventory, out _);
        }

        public bool IsMarkerCompleted(OfficeTaskMarkerId markerId)
        {
            return markerId == OfficeTaskMarkerId.ExitMarker
                ? ShiftComplete
                : completedQuestMarkers.Contains(markerId);
        }

        public bool IsMarkerActive(OfficeTaskMarkerId markerId)
        {
            if (ShiftComplete) return false;
            if (markerId == OfficeTaskMarkerId.ExitMarker) return RequiredTasksComplete();

            return activeQuestsByMarker.TryGetValue(markerId, out List<OfficeQuestDefinition> quests)
                && HasIncompleteQuest(quests);
        }

        private void AddActiveQuest(OfficeQuestDefinition quest)
        {
            if (quest == null) return;

            activeQuests.Add(quest);

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

        private void TryCompleteMarkerQuest(
            OfficeTaskMarkerId markerId,
            QuestInventory inventory)
        {
            if (!TryFindCompletableMarkerQuest(markerId, inventory, out OfficeQuestDefinition quest))
            {
                return;
            }

            if (!completedQuests.Add(quest))
            {
                return;
            }

            if (quest.QuestType == OfficeQuestType.DeliverItem)
            {
                inventory.RemoveQuestItem(quest.RequiredItemId);
            }

            if (IsMarkerFullyComplete(markerId))
            {
                completedQuestMarkers.Add(markerId);
            }

            ApplyHealthImpact(quest, inventory.GetComponent<HealthSystem>());
            PublishFeedback(quest);
            NotifyQuestStateChanged();
        }

        private void TryCompleteCollectQuest(QuestItemId itemId)
        {
            if (!activeCollectQuestsByItem.TryGetValue(itemId, out List<OfficeQuestDefinition> quests))
            {
                return;
            }

            bool changed = false;
            foreach (OfficeQuestDefinition quest in quests)
            {
                if (completedQuests.Add(quest))
                {
                    changed = true;
                    ApplyHealthImpact(quest);
                    PublishFeedback(quest);
                }
            }

            if (changed)
            {
                NotifyQuestStateChanged();
            }
        }

        private void TryExit()
        {
            if (!RequiredTasksComplete())
            {
                return;
            }

            ShiftComplete = true;
            NotifyQuestStateChanged();
            GameManager.Instance?.NotifyExitReached();
        }

        private bool TryFindCompletableMarkerQuest(
            OfficeTaskMarkerId markerId,
            QuestInventory inventory,
            out OfficeQuestDefinition completableQuest)
        {
            completableQuest = null;
            if (inventory == null || !activeQuestsByMarker.TryGetValue(markerId, out List<OfficeQuestDefinition> quests))
            {
                return false;
            }

            foreach (OfficeQuestDefinition quest in quests)
            {
                if (completedQuests.Contains(quest)) continue;

                bool canComplete = quest.QuestType == OfficeQuestType.VisitMarker
                    || (quest.QuestType == OfficeQuestType.DeliverItem && inventory.HasQuestItem(quest.RequiredItemId));

                if (canComplete)
                {
                    completableQuest = quest;
                    return true;
                }
            }

            return false;
        }

        private bool RequiredTasksComplete()
        {
            return activeQuests.Count > 0 && completedQuests.Count >= activeQuests.Count;
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

        private bool HasIncompleteQuest(List<OfficeQuestDefinition> quests)
        {
            foreach (OfficeQuestDefinition quest in quests)
            {
                if (!completedQuests.Contains(quest))
                {
                    return true;
                }
            }

            return false;
        }

        private void PublishFeedback(OfficeQuestDefinition quest)
        {
            if (!string.IsNullOrWhiteSpace(quest.FeedbackComment))
            {
                FeedbackPublished?.Invoke(quest.FeedbackComment);
            }
        }

        private static void ApplyHealthImpact(
            OfficeQuestDefinition quest,
            HealthSystem preferredHealth = null)
        {
            if (quest == null || quest.HealthImpact == 0)
            {
                return;
            }

            HealthSystem health = preferredHealth != null
                ? preferredHealth
                : FindFirstObjectByType<HealthSystem>();

            if (health == null)
            {
                return;
            }

            if (quest.HealthImpact > 0)
            {
                health.Heal(quest.HealthImpact);
            }
            else
            {
                health.TakeDamage(-quest.HealthImpact);
            }
        }

        private void NotifyQuestStateChanged()
        {
            QuestStateChanged?.Invoke(CaptureSnapshot());
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
