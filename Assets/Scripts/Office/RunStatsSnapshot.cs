using System.Collections.Generic;

namespace CM3070.Office
{
    // Small immutable snapshots used by the office report HUD.
    public readonly struct NpcRoleCount
    {
        public NpcRoleCount(string roleName, int count)
        {
            RoleName = roleName;
            Count = count;
        }

        public string RoleName { get; }
        public int Count { get; }
    }

    public readonly struct OfficeRoleCount
    {
        public OfficeRoleCount(string roleName, int count)
        {
            RoleName = roleName;
            Count = count;
        }

        public string RoleName { get; }
        public int Count { get; }
    }

    public readonly struct PropSpawnStatsSnapshot
    {
        public PropSpawnStatsSnapshot(int propCount, IReadOnlyList<OfficeRoleCount> propRoleCounts)
        {
            PropCount = propCount;
            PropRoleCounts = propRoleCounts;
        }

        public int PropCount { get; }
        public IReadOnlyList<OfficeRoleCount> PropRoleCounts { get; }
    }

    public readonly struct EntitySpawnStatsSnapshot
    {
        public EntitySpawnStatsSnapshot(
            int npcCount,
            IReadOnlyList<NpcRoleCount> npcRoleCounts)
        {
            NpcCount = npcCount;
            NpcRoleCounts = npcRoleCounts;
        }

        public int NpcCount { get; }
        public IReadOnlyList<NpcRoleCount> NpcRoleCounts { get; }
    }

    public readonly struct QuestSpawnStatsSnapshot
    {
        public QuestSpawnStatsSnapshot(int questCount, int questItemCount, int taskMarkerCount)
        {
            QuestCount = questCount;
            QuestItemCount = questItemCount;
            TaskMarkerCount = taskMarkerCount;
        }

        public int QuestCount { get; }
        public int QuestItemCount { get; }
        public int TaskMarkerCount { get; }
    }

    public readonly struct OfficeRunStatsSnapshot
    {
        public OfficeRunStatsSnapshot(
            int seed,
            int roomCount,
            IReadOnlyList<OfficeRoleCount> roomRoleCounts,
            int walkableArea,
            int reachableArea,
            int propCount,
            IReadOnlyList<OfficeRoleCount> propRoleCounts,
            int npcCount,
            int questCount,
            int questItemCount,
            int taskMarkerCount,
            IReadOnlyList<NpcRoleCount> npcRoleCounts)
        {
            Seed = seed;
            RoomCount = roomCount;
            RoomRoleCounts = roomRoleCounts;
            WalkableArea = walkableArea;
            ReachableArea = reachableArea;
            PropCount = propCount;
            PropRoleCounts = propRoleCounts;
            NpcCount = npcCount;
            QuestCount = questCount;
            QuestItemCount = questItemCount;
            TaskMarkerCount = taskMarkerCount;
            NpcRoleCounts = npcRoleCounts;
        }

        public int Seed { get; }
        public int RoomCount { get; }
        public IReadOnlyList<OfficeRoleCount> RoomRoleCounts { get; }
        public int WalkableArea { get; }
        public int ReachableArea { get; }
        public int PropCount { get; }
        public IReadOnlyList<OfficeRoleCount> PropRoleCounts { get; }
        public int NpcCount { get; }
        public int QuestCount { get; }
        public int QuestItemCount { get; }
        public int TaskMarkerCount { get; }
        public IReadOnlyList<NpcRoleCount> NpcRoleCounts { get; }
        public bool HasLayout => Seed != 0 || RoomCount > 0 || WalkableArea > 0 || ReachableArea > 0;
    }
}
