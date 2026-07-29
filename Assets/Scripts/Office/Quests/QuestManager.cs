using System.Collections.Generic;
using System;
using CM3070.Dungeon1;
using CM3070.Office;
using UnityEngine;

namespace CM3070.Office.Quest
{
    // UI/debug status for an interactable task marker.
    public enum QuestMarkerStatus
    {
        Inactive,
        Available,
        Unavailable,
        Completed
    }

    // Immutable UI/debug view of one active quest's completion state.
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

    // Captures whether a marker can currently progress a quest, plus the reason.
    public readonly struct QuestMarkerSnapshot
    {
        public QuestMarkerSnapshot(
            OfficeTaskMarkerId markerId,
            QuestMarkerStatus status,
            bool canUse,
            string reason)
        {
            MarkerId = markerId;
            Status = status;
            CanUse = canUse;
            Reason = reason;
        }

        public OfficeTaskMarkerId MarkerId { get; }
        public QuestMarkerStatus Status { get; }
        public bool CanUse { get; }
        public string Reason { get; }
    }

    // Full quest-state snapshot for HUDs that need to redraw all objectives.
    public readonly struct QuestStateSnapshot
    {
        public QuestStateSnapshot(
            QuestProgressSnapshot[] quests,
            OfficeTaskMarkerId[] completedMarkers,
            bool requiredTasksComplete,
            bool shiftComplete)
        {
            Quests = quests;
            CompletedMarkers = completedMarkers;
            RequiredTasksComplete = requiredTasksComplete;
            ShiftComplete = shiftComplete;
        }

        public QuestProgressSnapshot[] Quests { get; }
        public OfficeTaskMarkerId[] CompletedMarkers { get; }
        public bool RequiredTasksComplete { get; }
        public bool ShiftComplete { get; }
    }

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

        // Future HUD scripts can subscribe here instead of depending on Debug.Log output.
        public event Action<QuestStateSnapshot> QuestStateChanged;
        public event Action<OfficeQuestDefinition> QuestCompleted;
        public event Action<string> FeedbackPublished;
        public event Action<QuestMarkerSnapshot> MarkerStatusReported;
        public event Action ShiftCompleted;

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
            Debug.Log($"[Quest] Item pickup noted: {displayName} ({itemId}) x{amount}");
            TryCompleteCollectQuest(itemId);
        }

        public void NotifyPickupCollected(PickupId pickupId, string displayName)
        {
            Debug.Log($"[Pickup] Coping pickup noted: {displayName} ({pickupId})");
        }

        public void NotifyMarkerReached(
            OfficeTaskMarkerId markerId,
            string displayName,
            OfficePlayerInventory inventory)
        {
            if (inventory == null || ShiftComplete) return;

            if (markerId == OfficeTaskMarkerId.ExitMarker)
            {
                TryExit(displayName);
                return;
            }

            TryCompleteActiveMarkerQuest(markerId, displayName, inventory);
        }

        public bool CanUseMarker(OfficeTaskMarkerId markerId, OfficePlayerInventory inventory)
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

        public bool IsMarkerActive(OfficeTaskMarkerId markerId)
        {
            if (ShiftComplete) return false;

            if (markerId == OfficeTaskMarkerId.ExitMarker)
            {
                return RequiredTasksComplete();
            }

            return activeQuestsByMarker.TryGetValue(markerId, out List<OfficeQuestDefinition> quests)
                && HasIncompleteQuest(quests);
        }

        public QuestStateSnapshot CaptureSnapshot()
        {
            // Copy private collections into arrays so UI cannot mutate quest progress.
            QuestProgressSnapshot[] questSnapshots = new QuestProgressSnapshot[activeQuests.Count];
            int index = 0;
            foreach (OfficeQuestDefinition quest in activeQuests)
            {
                questSnapshots[index] = new QuestProgressSnapshot(quest, completedQuests.Contains(quest));
                index++;
            }

            OfficeTaskMarkerId[] markerSnapshots = new OfficeTaskMarkerId[completedQuestMarkers.Count];
            index = 0;
            foreach (OfficeTaskMarkerId markerId in completedQuestMarkers)
            {
                markerSnapshots[index] = markerId;
                index++;
            }

            return new QuestStateSnapshot(
                questSnapshots,
                markerSnapshots,
                RequiredTasksComplete(),
                ShiftComplete);
        }

        public QuestMarkerSnapshot CaptureMarkerSnapshot(
            OfficeTaskMarkerId markerId,
            OfficePlayerInventory inventory)
        {
            if (IsMarkerCompleted(markerId))
            {
                return new QuestMarkerSnapshot(markerId, QuestMarkerStatus.Completed, false, "Already completed.");
            }

            if (ShiftComplete)
            {
                return new QuestMarkerSnapshot(markerId, QuestMarkerStatus.Inactive, false, "Shift is already complete.");
            }

            if (markerId == OfficeTaskMarkerId.ExitMarker)
            {
                bool exitReady = RequiredTasksComplete();
                return new QuestMarkerSnapshot(
                    markerId,
                    exitReady ? QuestMarkerStatus.Available : QuestMarkerStatus.Unavailable,
                    exitReady,
                    exitReady ? "All required tasks complete." : "Complete required tasks first.");
            }

            if (!activeQuestsByMarker.TryGetValue(markerId, out List<OfficeQuestDefinition> quests)
                || !HasIncompleteQuest(quests))
            {
                return new QuestMarkerSnapshot(markerId, QuestMarkerStatus.Inactive, false, "No active incomplete quest is assigned here.");
            }

            if (inventory == null)
            {
                return new QuestMarkerSnapshot(markerId, QuestMarkerStatus.Unavailable, false, "No office inventory was provided.");
            }

            if (TryFindCompletableMarkerQuest(markerId, inventory, out OfficeQuestDefinition quest))
            {
                return new QuestMarkerSnapshot(markerId, QuestMarkerStatus.Available, true, $"Ready: {quest.ObjectiveText}");
            }

            return new QuestMarkerSnapshot(markerId, QuestMarkerStatus.Unavailable, false, BuildMarkerUnavailableReason(markerId, inventory));
        }

        public QuestMarkerSnapshot ReportMarkerStatus(
            OfficeTaskMarkerId markerId,
            OfficePlayerInventory inventory)
        {
            // Emits a marker-status event for UI/debug overlays without changing gameplay state.
            QuestMarkerSnapshot snapshot = CaptureMarkerSnapshot(markerId, inventory);
            MarkerStatusReported?.Invoke(snapshot);
            return snapshot;
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

            Debug.Log($"[Quest] Active quest set configured. Count={this.activeQuests.Count}. ExitUnlocked={RequiredTasksComplete()}");
            foreach (OfficeQuestDefinition quest in this.activeQuests)
            {
                Debug.Log($"[Quest] Active: {quest.QuestName} | Type={quest.QuestType} | Objective={quest.ObjectiveText} | Item={quest.RequiredItemId} | Marker={quest.TaskMarkerId} | HealthImpact={quest.HealthImpact}");
            }

            NotifyQuestStateChanged();
        }

        public void ResetShift()
        {
            ShiftComplete = false;
            completedQuests.Clear();
            completedQuestMarkers.Clear();
            Debug.Log("[Shift] Quest progress reset.");
            NotifyQuestStateChanged();
        }

        private bool CanCompleteActiveQuest(OfficeTaskMarkerId markerId, OfficePlayerInventory inventory)
        {
            if (inventory == null)
            {
                return false;
            }

            return TryFindCompletableMarkerQuest(markerId, inventory, out _);
        }

        private bool TryFindCompletableMarkerQuest(
            OfficeTaskMarkerId markerId,
            OfficePlayerInventory inventory,
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
                    OfficeQuestType.DeliverItem => inventory.HasQuestItem(quest.RequiredItemId),
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
            OfficePlayerInventory inventory)
        {
            if (!activeQuestsByMarker.TryGetValue(markerId, out List<OfficeQuestDefinition> quests))
            {
                QuestMarkerSnapshot markerSnapshot = CaptureMarkerSnapshot(markerId, inventory);
                Debug.Log($"[Marker] {displayName} ({markerId}) reached. {markerSnapshot.Reason}");
                MarkerStatusReported?.Invoke(markerSnapshot);
                return;
            }

            if (!TryFindCompletableMarkerQuest(markerId, inventory, out OfficeQuestDefinition quest))
            {
                QuestMarkerSnapshot markerSnapshot = CaptureMarkerSnapshot(markerId, inventory);
                Debug.Log($"[Marker] {displayName} ({markerId}) reached. {markerSnapshot.Reason}");
                MarkerStatusReported?.Invoke(markerSnapshot);
                return;
            }

            if (quest.QuestType == OfficeQuestType.DeliverItem)
            {
                inventory.RemoveQuestItem(quest.RequiredItemId);
            }

            completedQuests.Add(quest);
            Debug.Log($"[Quest] Completed: {quest.QuestName} at {displayName} ({markerId}). ExitUnlocked={RequiredTasksComplete()}");
            QuestCompleted?.Invoke(quest);
            LogFeedbackComment(quest);

            if (IsMarkerFullyComplete(markerId))
            {
                completedQuestMarkers.Add(markerId);
                Debug.Log($"[Marker] Completed: {markerId}");
            }

            MarkerStatusReported?.Invoke(CaptureMarkerSnapshot(markerId, inventory));
            NotifyQuestStateChanged();
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
                Debug.Log($"[Quest] Completed: {quest.QuestName} by collecting {itemId}. ExitUnlocked={RequiredTasksComplete()}");
                QuestCompleted?.Invoke(quest);
                LogFeedbackComment(quest);
            }

            NotifyQuestStateChanged();
        }

        private void TryExit(string displayName)
        {
            if (!RequiredTasksComplete())
            {
                QuestMarkerSnapshot markerSnapshot = CaptureMarkerSnapshot(OfficeTaskMarkerId.ExitMarker, null);
                Debug.Log($"[Shift] Exit locked at {displayName}. {markerSnapshot.Reason}");
                MarkerStatusReported?.Invoke(markerSnapshot);
                return;
            }

            ShiftComplete = true;
            Debug.Log($"[Shift] Completed through {displayName}.");
            MarkerStatusReported?.Invoke(CaptureMarkerSnapshot(OfficeTaskMarkerId.ExitMarker, null));
            ShiftCompleted?.Invoke();
            NotifyQuestStateChanged();
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

        private static void LogFeedbackComment(OfficeQuestDefinition quest)
        {
            // Temporary UI stand-in: log the SO feedback text until UI exists.
            if (!string.IsNullOrWhiteSpace(quest.FeedbackComment))
            {
                Debug.Log($"[Feedback] {quest.FeedbackComment}");
                Instance?.FeedbackPublished?.Invoke(quest.FeedbackComment);
            }
        }

        private string BuildMarkerUnavailableReason(OfficeTaskMarkerId markerId, OfficePlayerInventory inventory)
        {
            if (!activeQuestsByMarker.TryGetValue(markerId, out List<OfficeQuestDefinition> quests))
            {
                return "No active quest is assigned here.";
            }

            foreach (OfficeQuestDefinition quest in quests)
            {
                if (completedQuests.Contains(quest)) continue;

                if (quest.QuestType == OfficeQuestType.DeliverItem)
                {
                    int itemCount = inventory != null ? inventory.GetQuestItemCount(quest.RequiredItemId) : 0;
                    return $"Missing {quest.RequiredItemId} for '{quest.QuestName}'. Current={itemCount}.";
                }
            }

            return "No matching quest can be completed yet.";
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
