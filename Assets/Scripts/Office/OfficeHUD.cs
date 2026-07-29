using CM3070.Office.Quest;
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
        [SerializeField] private TMP_Text feedback;

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
        private OfficePlayerInventory subscribedInventory;
        private QuestStateSnapshot? lastQuestSnapshot;
        private OfficeInventorySnapshot? lastInventorySnapshot;
        private string latestFeedback = string.Empty;
        private float feedbackTimer;

        private void OnEnable()
        {
            SubscribeToQuestManager();
            SubscribeToRuntimePlayer();
            RefreshAll();
        }

        private void Start()
        {
            SubscribeToQuestManager();
            SubscribeToRuntimePlayer();
            RefreshAll();
        }

        private void Update()
        {
            // OfficeScene can respawn the player on regeneration, so reconnect when needed.
            SubscribeToQuestManager();
            SubscribeToRuntimePlayer();

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
            UnsubscribeFromQuestManager();
            UnsubscribeFromInventory();
        }

        private void OnValidate()
        {
            EnsureArraySize(ref quests);
            EnsureArraySize(ref items);
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

        private void SubscribeToRuntimePlayer()
        {
            OfficePlayerInventory inventory = FindFirstObjectByType<OfficePlayerInventory>();

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

        private void OnInventoryChanged(OfficeInventorySnapshot snapshot)
        {
            lastInventorySnapshot = snapshot;
            RefreshInventorySlots();
        }

        private void RefreshAll()
        {
            RefreshQuestRows();
            RefreshInventorySlots();
            RefreshExitStatus();
            RefreshFeedback();
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
