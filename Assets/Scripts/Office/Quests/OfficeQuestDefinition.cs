using CM3070.Office;
using UnityEngine;

namespace CM3070.Office.Quest
{
    public enum OfficeQuestType
    {
        DeliverItem,
        VisitMarker,
        CollectItemOnly
    }

    [CreateAssetMenu(fileName = "Quest_OfficeTask", menuName = "CM3070/Office/Quest Definition")]
    public sealed class OfficeQuestDefinition : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string questName = "Office Quest";
        [SerializeField] private OfficeQuestType questType = OfficeQuestType.DeliverItem;
        [TextArea]
        [SerializeField] private string objectiveText = "Complete the task.";
        [TextArea]
        [SerializeField] private string feedbackComment = "Task complete.";
        [SerializeField] private int healthImpact;
        [SerializeField, Min(0)] private int spawnWeight = 1;

        [Header("Quest Item")]
        [SerializeField] private QuestItemId requiredItemId = QuestItemId.None;
        [SerializeField] private GameObject questItemPrefab;
        [SerializeField] private OfficeRoomRole itemRoomRole = OfficeRoomRole.Office;
        [SerializeField] private float itemHeight = 0.36f;

        [Header("Task Marker")]
        [SerializeField] private OfficeTaskMarkerId taskMarkerId = OfficeTaskMarkerId.None;
        [SerializeField] private GameObject taskMarkerPrefab;
        [SerializeField] private OfficeRoomRole markerRoomRole = OfficeRoomRole.Office;
        [SerializeField] private float markerHeight = 0.08f;

        public string QuestName => questName;
        public OfficeQuestType QuestType => questType;
        public string ObjectiveText => objectiveText;
        public string FeedbackComment => feedbackComment;
        public int HealthImpact => healthImpact;
        public int SpawnWeight => spawnWeight;
        public QuestItemId RequiredItemId => requiredItemId;
        public GameObject QuestItemPrefab => questItemPrefab;
        public OfficeRoomRole ItemRoomRole => itemRoomRole;
        public float ItemHeight => itemHeight;
        public OfficeTaskMarkerId TaskMarkerId => taskMarkerId;
        public GameObject TaskMarkerPrefab => taskMarkerPrefab;
        public OfficeRoomRole MarkerRoomRole => markerRoomRole;
        public float MarkerHeight => markerHeight;
        public bool HasQuestItem => requiredItemId != QuestItemId.None && questItemPrefab != null;
        public bool HasTaskMarker => taskMarkerId != OfficeTaskMarkerId.None && taskMarkerPrefab != null;
        public bool IsRequiredQuest => questType == OfficeQuestType.DeliverItem && requiredItemId != QuestItemId.None && taskMarkerId != OfficeTaskMarkerId.None;

        public bool IsSpawnable()
        {
            // Each quest type needs different spawn data.
            if (spawnWeight <= 0) return false;

            return questType switch
            {
                OfficeQuestType.DeliverItem => HasQuestItem && HasTaskMarker,
                OfficeQuestType.VisitMarker => HasTaskMarker,
                OfficeQuestType.CollectItemOnly => HasQuestItem,
                _ => false
            };
        }
    }
}
