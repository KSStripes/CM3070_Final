using CM3070.Office.Quest;
using System.Text;
using TMPro;
using UnityEngine;

namespace CM3070.Office
{
    // Player-facing office HUD for the workday loop.
    // GameUI owns shift/day text, health, panels, and minimap; OfficeHUD owns objectives,
    // carried quest items, exit availability, and the temporary cynical feedback line.
    public sealed class OfficeHUD : MonoBehaviour
    {
        private const int SlotCount = 3;

        [Header("Quest Rows")]
        [SerializeField] private TMP_Text[] quests = new TMP_Text[SlotCount];

        [Header("Inventory Slots")]
        [SerializeField] private TMP_Text[] items = new TMP_Text[SlotCount];

        [Header("Status Fields")]
        [SerializeField] private TMP_Text exit;
        [SerializeField] private GameObject feedbackRoot;
        [SerializeField] private TMP_Text feedback;

        [Header("Report Stats")]
        [SerializeField] private bool showReportStats = true;
        [SerializeField] private GameObject reportStatsRoot;
        [SerializeField] private TMP_Text reportStats;
        [SerializeField, Range(12f, 16f)] private float reportFontSize = 12f;
        [SerializeField] private Vector2 reportPanelSize = new(360f, 190f);
        [SerializeField] private Vector2 reportPanelOffset = new(-16f, 16f);
        [SerializeField] private string reportStatsPending = "Report stats pending";

        [Header("Display Text")]
        [SerializeField] private string emptyQuest = "[ ] Waiting for the day to decide what it wants.";
        [SerializeField] private string emptySlot = "Empty";
        [SerializeField] private string exitLocked = "Exit locked";
        [SerializeField] private string exitReady = "Exit ready";
        [SerializeField] private string shiftComplete = "Shift complete";
        [SerializeField, Min(0f)] private float feedbackSeconds = 6f;

        [Header("Colours")]
        [SerializeField] private Color incomplete = new(0.91f, 0.85f, 0.71f, 1f);
        [SerializeField] private Color complete = new(0.56f, 0.82f, 0.62f, 1f);
        [SerializeField] private Color empty = new(0.55f, 0.58f, 0.54f, 1f);
        [SerializeField] private Color filled = new(0.94f, 0.79f, 0.42f, 1f);
        [SerializeField] private Color locked = new(0.85f, 0.54f, 0.29f, 1f);
        [SerializeField] private Color ready = new(0.49f, 0.84f, 0.75f, 1f);
        [SerializeField] private Color message = new(0.62f, 0.85f, 0.91f, 1f);

        private QuestManager subscribedQuestManager;
        private QuestInventory subscribedInventory;
        private OfficeController subscribedOfficeController;
        private QuestStateSnapshot? lastQuestSnapshot;
        private QuestInventorySnapshot? lastInventorySnapshot;
        private OfficeRunStatsSnapshot? lastRunStatsSnapshot;
        private string latestFeedback = string.Empty;
        private float feedbackTimer;

        private void Awake()
        {
            EnsureReportStatsText();
        }

        private void OnEnable()
        {
            EnsureReportStatsText();
            SubscribeToSceneObjects();
            SubscribeToInventory();
            RefreshAll();
        }

        private void Start()
        {
            SubscribeToSceneObjects();
            SubscribeToInventory();
            RefreshAll();
        }

        private void Update()
        {
            // The player can be respawned, so only reconnect the inventory if it disappeared.
            if (subscribedInventory == null)
            {
                SubscribeToInventory();
            }

            if (feedbackSeconds > 0f && feedbackTimer > 0f)
            {
                feedbackTimer -= Time.deltaTime;
                if (feedbackTimer <= 0f)
                {
                    latestFeedback = string.Empty;
                    RefreshFeedback();
                }
            }
        }

        private void OnDisable()
        {
            UnsubscribeFromOfficeController();
            UnsubscribeFromQuestManager();
            UnsubscribeFromInventory();
        }

        private void OnValidate()
        {
            EnsureArraySize(ref quests);
            EnsureArraySize(ref items);
            reportFontSize = Mathf.Clamp(reportFontSize, 12f, 16f);
            reportPanelSize.x = Mathf.Max(220f, reportPanelSize.x);
            reportPanelSize.y = Mathf.Max(120f, reportPanelSize.y);
        }

        private void SubscribeToSceneObjects()
        {
            if (subscribedOfficeController == null)
            {
                SubscribeToOfficeController();
            }

            if (subscribedQuestManager == null)
            {
                SubscribeToQuestManager();
            }
        }

        private void SubscribeToOfficeController()
        {
            OfficeController officeController = FindFirstObjectByType<OfficeController>();
            if (officeController == null || subscribedOfficeController == officeController)
            {
                return;
            }

            UnsubscribeFromOfficeController();
            subscribedOfficeController = officeController;
            subscribedOfficeController.RunStatsChanged += OnRunStatsChanged;
            lastRunStatsSnapshot = subscribedOfficeController.CaptureRunStats();
            RefreshReportStats();
        }

        private void UnsubscribeFromOfficeController()
        {
            if (subscribedOfficeController == null)
            {
                return;
            }

            subscribedOfficeController.RunStatsChanged -= OnRunStatsChanged;
            subscribedOfficeController = null;
        }

        private void SubscribeToQuestManager()
        {
            QuestManager questManager = QuestManager.Instance;
            if (questManager == null || subscribedQuestManager == questManager)
            {
                return;
            }

            UnsubscribeFromQuestManager();
            subscribedQuestManager = questManager;
            subscribedQuestManager.QuestStateChanged += OnQuestStateChanged;
            subscribedQuestManager.FeedbackPublished += OnFeedbackPublished;
            lastQuestSnapshot = subscribedQuestManager.CaptureSnapshot();
        }

        private void SubscribeToInventory()
        {
            QuestInventory inventory = FindFirstObjectByType<QuestInventory>();

            if (inventory == null || subscribedInventory == inventory)
            {
                return;
            }

            UnsubscribeFromInventory();
            subscribedInventory = inventory;
            subscribedInventory.InventoryChanged += OnInventoryChanged;
            lastInventorySnapshot = subscribedInventory.CaptureSnapshot();
            RefreshInventorySlots();
        }

        private void UnsubscribeFromQuestManager()
        {
            if (subscribedQuestManager == null)
            {
                return;
            }

            subscribedQuestManager.QuestStateChanged -= OnQuestStateChanged;
            subscribedQuestManager.FeedbackPublished -= OnFeedbackPublished;
            subscribedQuestManager = null;
        }

        private void UnsubscribeFromInventory()
        {
            if (subscribedInventory == null)
            {
                return;
            }

            subscribedInventory.InventoryChanged -= OnInventoryChanged;
            subscribedInventory = null;
        }

        private void OnQuestStateChanged(QuestStateSnapshot snapshot)
        {
            lastQuestSnapshot = snapshot;
            RefreshQuestRows();
            RefreshExitStatus();
        }

        private void OnFeedbackPublished(string feedback)
        {
            latestFeedback = feedback;
            feedbackTimer = feedbackSeconds;
            RefreshFeedback();
        }

        private void OnInventoryChanged(QuestInventorySnapshot snapshot)
        {
            lastInventorySnapshot = snapshot;
            RefreshInventorySlots();
        }

        private void OnRunStatsChanged(OfficeRunStatsSnapshot snapshot)
        {
            lastRunStatsSnapshot = snapshot;
            RefreshReportStats();
        }

        private void RefreshAll()
        {
            RefreshQuestRows();
            RefreshInventorySlots();
            RefreshExitStatus();
            RefreshFeedback();
            RefreshReportStats();
        }

        private void RefreshQuestRows()
        {
            for (int i = 0; i < SlotCount; i++)
            {
                TMP_Text row = TextAt(quests, i);
                if (row == null) continue;

                if (!lastQuestSnapshot.HasValue || i >= lastQuestSnapshot.Value.Quests.Length)
                {
                    SetText(row, emptyQuest, incomplete);
                    continue;
                }

                QuestProgressSnapshot questSnapshot = lastQuestSnapshot.Value.Quests[i];
                string marker = questSnapshot.IsCompleted ? "[x]" : "[ ]";
                string objective = questSnapshot.Quest != null
                    ? questSnapshot.Quest.ObjectiveText
                    : emptyQuest;

                SetText(
                    row,
                    $"{marker} {objective}",
                    questSnapshot.IsCompleted ? complete : incomplete);
            }
        }

        private void RefreshInventorySlots()
        {
            for (int i = 0; i < SlotCount; i++)
            {
                TMP_Text slot = TextAt(items, i);
                if (slot == null) continue;

                if (!lastInventorySnapshot.HasValue || i >= lastInventorySnapshot.Value.QuestItems.Length)
                {
                    SetText(slot, emptySlot, empty);
                    continue;
                }

                QuestItemStack stack = lastInventorySnapshot.Value.QuestItems[i];
                string amount = stack.Amount > 1 ? $" x{stack.Amount}" : string.Empty;
                SetText(slot, $"{stack.ItemId}{amount}", filled);
            }
        }

        private void RefreshExitStatus()
        {
            if (exit == null)
            {
                return;
            }

            if (!lastQuestSnapshot.HasValue)
            {
                SetText(exit, exitLocked, locked);
                return;
            }

            QuestStateSnapshot snapshot = lastQuestSnapshot.Value;
            if (snapshot.ShiftComplete)
            {
                SetText(exit, shiftComplete, ready);
            }
            else if (snapshot.RequiredTasksComplete)
            {
                SetText(exit, exitReady, ready);
            }
            else
            {
                SetText(exit, exitLocked, locked);
            }
        }

        private void RefreshFeedback()
        {
            SetText(feedback, latestFeedback, message);

            if (feedbackRoot != null)
            {
                feedbackRoot.SetActive(!string.IsNullOrWhiteSpace(latestFeedback));
            }
        }

        private void RefreshReportStats()
        {
            EnsureReportStatsText();

            if (reportStatsRoot != null)
            {
                reportStatsRoot.SetActive(showReportStats);
            }

            if (!showReportStats || reportStats == null)
            {
                return;
            }

            if (!lastRunStatsSnapshot.HasValue || !lastRunStatsSnapshot.Value.HasLayout)
            {
                SetText(reportStats, reportStatsPending, message);
                return;
            }

            OfficeRunStatsSnapshot stats = lastRunStatsSnapshot.Value;
            SetText(reportStats, FormatReportStats(stats), message);
            reportStats.fontSize = reportFontSize;
        }

        private void EnsureReportStatsText()
        {
            if (reportStats != null)
            {
                reportStatsRoot ??= reportStats.gameObject;
                ConfigureReportRect(reportStats.rectTransform);
                reportStats.fontSize = reportFontSize;
                reportStats.alignment = TextAlignmentOptions.BottomRight;
                reportStats.textWrappingMode = TextWrappingModes.Normal;
                reportStats.raycastTarget = false;
                return;
            }

            Transform parent = transform.Find("HUDPanel") ?? transform;
            Transform existing = parent.Find("ReportStatsText");
            if (existing != null && existing.TryGetComponent(out TMP_Text existingText))
            {
                reportStats = existingText;
                reportStatsRoot = existing.gameObject;
                ConfigureReportRect(reportStats.rectTransform);
                reportStats.fontSize = reportFontSize;
                reportStats.alignment = TextAlignmentOptions.BottomRight;
                reportStats.textWrappingMode = TextWrappingModes.Normal;
                reportStats.raycastTarget = false;
            }
        }

        private void ConfigureReportRect(RectTransform rectTransform)
        {
            if (rectTransform == null)
            {
                return;
            }

            rectTransform.anchorMin = new Vector2(1f, 0f);
            rectTransform.anchorMax = new Vector2(1f, 0f);
            rectTransform.pivot = new Vector2(1f, 0f);
            rectTransform.anchoredPosition = reportPanelOffset;
            rectTransform.sizeDelta = reportPanelSize;
        }

        private static string FormatReportStats(OfficeRunStatsSnapshot stats)
        {
            StringBuilder builder = new();
            builder.AppendLine("PCG report");
            builder.AppendLine($"Seed: {stats.Seed}");
            builder.AppendLine($"Layout: {stats.RoomCount} rooms, {stats.ReachableArea}/{stats.WalkableArea} reachable floor tiles");
            builder.Append("Rooms: ");
            AppendOfficeRoleCounts(builder, stats.RoomRoleCounts);
            builder.AppendLine();
            builder.Append("Props: ");
            builder.Append(stats.PropCount);
            if (stats.PropRoleCounts != null && stats.PropRoleCounts.Count > 0)
            {
                builder.Append(" (");
                AppendOfficeRoleCounts(builder, stats.PropRoleCounts);
                builder.Append(')');
            }

            builder.AppendLine();
            builder.AppendLine($"Gameplay: {stats.QuestCount} quests, {stats.QuestItemCount} items, {stats.TaskMarkerCount} markers");
            builder.Append("NPCs: ");

            if (stats.NpcRoleCounts == null || stats.NpcRoleCounts.Count == 0)
            {
                builder.Append("none");
                return builder.ToString();
            }

            for (int i = 0; i < stats.NpcRoleCounts.Count; i++)
            {
                NpcRoleCount roleCount = stats.NpcRoleCounts[i];
                if (i > 0)
                {
                    builder.Append(", ");
                }

                builder.Append(roleCount.Count);
                builder.Append(' ');
                builder.Append(roleCount.RoleName);
            }

            return builder.ToString();
        }

        private static void AppendOfficeRoleCounts(StringBuilder builder, System.Collections.Generic.IReadOnlyList<OfficeRoleCount> roleCounts)
        {
            if (roleCounts == null || roleCounts.Count == 0)
            {
                builder.Append("none");
                return;
            }

            for (int i = 0; i < roleCounts.Count; i++)
            {
                OfficeRoleCount roleCount = roleCounts[i];
                if (i > 0)
                {
                    builder.Append(", ");
                }

                builder.Append(roleCount.Count);
                builder.Append(' ');
                builder.Append(roleCount.RoleName);
            }
        }

        private static TMP_Text TextAt(TMP_Text[] textFields, int index)
        {
            return textFields != null && index >= 0 && index < textFields.Length
                ? textFields[index]
                : null;
        }

        private static void SetText(TMP_Text textField, string value, Color color)
        {
            if (textField == null)
            {
                return;
            }

            textField.text = value;
            textField.color = color;
            textField.alpha = 1f;
        }

        private static void EnsureArraySize(ref TMP_Text[] textFields)
        {
            if (textFields != null && textFields.Length == SlotCount)
            {
                return;
            }

            TMP_Text[] resized = new TMP_Text[SlotCount];
            if (textFields != null)
            {
                for (int i = 0; i < Mathf.Min(textFields.Length, SlotCount); i++)
                {
                    resized[i] = textFields[i];
                }
            }

            textFields = resized;
        }
    }
}
