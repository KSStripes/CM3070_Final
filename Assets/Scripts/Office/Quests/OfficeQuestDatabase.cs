using UnityEngine;

namespace CM3070.Office.Quest
{
    [CreateAssetMenu(fileName = "OfficeQuestDatabase", menuName = "CM3070/Office/Quest Database")]
    public sealed class OfficeQuestDatabase : ScriptableObject
    {
        [SerializeField] private OfficeQuestDefinition[] quests;

        public OfficeQuestDefinition[] Quests => quests;
    }
}
