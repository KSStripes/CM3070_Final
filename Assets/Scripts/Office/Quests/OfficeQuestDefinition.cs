using CM3070.Office;
using UnityEngine;

namespace CM3070.Office.Quest
{
    [CreateAssetMenu(fileName = "Quest_OfficeTask", menuName = "CM3070/Office/Quest Definition")]
    public sealed class OfficeQuestDefinition : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string questName = "Office Quest";
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
        public int SpawnWeight => spawnWeight;
        public QuestItemId RequiredItemId => requiredItemId;
        public GameObject QuestItemPrefab => questItemPrefab;
        public OfficeRoomRole ItemRoomRole => itemRoomRole;
        public float ItemHeight => itemHeight;
        public OfficeTaskMarkerId TaskMarkerId => taskMarkerId;
        public GameObject TaskMarkerPrefab => taskMarkerPrefab;
        public OfficeRoomRole MarkerRoomRole => markerRoomRole;
        public float MarkerHeight => markerHeight;
        public bool IsRequiredQuest => requiredItemId != QuestItemId.None && taskMarkerId != OfficeTaskMarkerId.None;

        public bool IsSpawnable()
        {
            // A spawnable quest needs both halves of the item-to-marker loop.
            return IsRequiredQuest && questItemPrefab != null && taskMarkerPrefab != null && spawnWeight > 0;
        }
    }
}
