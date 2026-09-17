// File: Office/Quests/OfficeQuestDatabase.cs
// Purpose: ScriptableObject list of available OfficeScene quests.
// Inputs: Quest definitions assigned in the Inspector.
// Output/side effects: Provides the quest pool used by OfficeQuestSpawner when selecting daily tasks.

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
