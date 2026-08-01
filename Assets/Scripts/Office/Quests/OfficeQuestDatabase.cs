using UnityEngine;

namespace CM3070.Office.Quest
{
    // List of available office quest assets for the spawner.
    [CreateAssetMenu(fileName = "OfficeQuestDatabase", menuName = "CM3070/Office/Quest Database")]
    public sealed class OfficeQuestDatabase : ScriptableObject
    {
        [SerializeField] private OfficeQuestDefinition[] quests;

        public OfficeQuestDefinition[] Quests => quests;
    }
}
