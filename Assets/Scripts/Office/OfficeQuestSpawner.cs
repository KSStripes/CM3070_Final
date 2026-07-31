using System.Collections.Generic;
using CM3070.Office.Quest;
using CM3070.Dungeon1;
using CM3070.PCG;
using UnityEngine;

namespace CM3070.Office
{
    // Spawns the office exit marker and a selected subset of quest item/task-marker pairs.
    public sealed class OfficeQuestSpawner : MonoBehaviour
    {
        [Header("Exit Marker")]
        [SerializeField] private GameObject exitMarkerPrefab;
        [SerializeField] private float markerHeight = 0.08f;

        [Header("Quest Database")]
        [SerializeField] private OfficeQuestDatabase questDatabase;
        [SerializeField, Min(0)] private int questsPerRun = 3;
        [SerializeField] private int questSeedOffset = 9137;

        [Header("Placement")]
        [SerializeField] private int minSpacing = 3;
        [SerializeField] private int markerExclusionRadius = 2;

        private readonly HashSet<Vector2Int> occupiedPositions = new();
        private Transform questRoot;

        public void SpawnQuestObjects(
            DungeonLayout layout,
            OfficeRoomPlan roomPlan,
            DungeonVisualizer visualizer,
            Transform parent,
            IReadOnlyCollection<Vector2Int> blockedPositions = null)
        {
            if (layout == null || roomPlan == null || visualizer == null || parent == null)
            {
                Debug.LogWarning("OfficeQuestSpawner needs layout, room plan, visualizer, and parent before spawning.");
                return;
            }

            EnsureQuestRoot(parent);
            ClearQuestObjects();
            occupiedPositions.Clear();
            AddBlockedPositions(blockedPositions);

            System.Random random = new(layout.Seed ^ questSeedOffset);
            List<OfficeQuestDefinition> selectedQuests = SelectQuests(random);
            QuestManager.Instance?.ConfigureActiveQuests(selectedQuests);
            SpawnSelectedQuests(layout, roomPlan, visualizer, selectedQuests, random);
            SpawnExitMarker(layout, visualizer, random);
        }

        public void ClearQuestObjects()
        {
            if (questRoot == null)
            {
                Transform existing = transform.Find("Office Quest Objects");
                questRoot = existing;
            }

            if (questRoot == null) return;

            for (int i = questRoot.childCount - 1; i >= 0; i--)
            {
                GameObject child = questRoot.GetChild(i).gameObject;
                if (Application.isPlaying)
                {
                    Destroy(child);
                }
                else
                {
                    DestroyImmediate(child);
                }
            }
        }

        private void AddBlockedPositions(IReadOnlyCollection<Vector2Int> blockedPositions)
        {
            if (blockedPositions == null) return;

            foreach (Vector2Int blockedPosition in blockedPositions)
            {
                occupiedPositions.Add(blockedPosition);
            }
        }

        private void EnsureQuestRoot(Transform parent)
        {
            if (questRoot != null && questRoot.parent == parent)
            {
                return;
            }

            Transform existing = parent.Find("Office Quest Objects");
            if (existing != null)
            {
                questRoot = existing;
                return;
            }

            GameObject root = new("Office Quest Objects");
            root.transform.SetParent(parent);
            questRoot = root.transform;
        }

        private void SpawnExitMarker(
            DungeonLayout layout,
            DungeonVisualizer visualizer,
            System.Random random)
        {
            if (exitMarkerPrefab != null
                && TryFindNearExit(layout, random, out Vector2Int exitPosition))
            {
                SpawnPrefab(exitMarkerPrefab, "Exit", exitPosition, markerHeight, visualizer);
                return;
            }
        }

        private void SpawnSelectedQuests(
            DungeonLayout layout,
            OfficeRoomPlan roomPlan,
            DungeonVisualizer visualizer,
            List<OfficeQuestDefinition> selectedQuests,
            System.Random random)
        {
            foreach (OfficeQuestDefinition quest in selectedQuests)
            {
                if (quest.QuestItemPrefab != null
                    && TryFindInRoomRole(layout, roomPlan, quest.ItemRoomRole, random, out Vector2Int itemPosition))
                {
                    SpawnPrefab(quest.QuestItemPrefab, quest.QuestName, itemPosition, quest.ItemHeight, visualizer);
                }

                if (quest.TaskMarkerPrefab != null
                    && TryFindInRoomRole(layout, roomPlan, quest.MarkerRoomRole, random, out Vector2Int markerPosition))
                {
                    SpawnPrefab(quest.TaskMarkerPrefab, quest.QuestName, markerPosition, quest.MarkerHeight, visualizer);
                }
            }
        }

        private List<OfficeQuestDefinition> SelectQuests(System.Random random)
        {
            List<OfficeQuestDefinition> candidates = new();
            if (questDatabase != null && questDatabase.Quests != null)
            {
                foreach (OfficeQuestDefinition quest in questDatabase.Quests)
                {
                    if (quest != null && quest.IsSpawnable())
                    {
                        candidates.Add(quest);
                    }
                }
            }

            Shuffle(candidates, random);

            List<OfficeQuestDefinition> selected = new();
            int targetCount = Mathf.Min(questsPerRun, candidates.Count);
            for (int i = 0; i < targetCount; i++)
            {
                selected.Add(candidates[i]);
            }

            return selected;
        }

        private bool TryFindNearExit(
            DungeonLayout layout,
            System.Random random,
            out Vector2Int position)
        {
            List<Vector2Int> candidates = BuildNearPointCandidates(layout, layout.Exit);
            Shuffle(candidates, random);

            foreach (Vector2Int candidate in candidates)
            {
                if (CanPlaceAt(layout, candidate))
                {
                    position = candidate;
                    occupiedPositions.Add(candidate);
                    return true;
                }
            }

            foreach (Vector2Int candidate in candidates)
            {
                if (CanPlaceExitAt(layout, candidate))
                {
                    position = candidate;
                    occupiedPositions.Add(candidate);
                    return true;
                }
            }

            if (CanPlaceExitAt(layout, layout.Exit))
            {
                position = layout.Exit;
                occupiedPositions.Add(position);
                return true;
            }

            position = Vector2Int.zero;
            return false;
        }

        private bool TryFindInRoomRole(
            DungeonLayout layout,
            OfficeRoomPlan roomPlan,
            OfficeRoomRole roomRole,
            System.Random random,
            out Vector2Int position)
        {
            List<Vector2Int> candidates = BuildRoomCandidates(layout, roomPlan, roomRole);
            Shuffle(candidates, random);

            foreach (Vector2Int candidate in candidates)
            {
                if (CanPlaceAt(layout, candidate))
                {
                    position = candidate;
                    occupiedPositions.Add(candidate);
                    return true;
                }
            }

            position = Vector2Int.zero;
            return false;
        }

        private List<Vector2Int> BuildNearPointCandidates(DungeonLayout layout, Vector2Int point)
        {
            List<Vector2Int> candidates = new();
            int radius = Mathf.Max(1, markerExclusionRadius + 2);

            for (int x = point.x - radius; x <= point.x + radius; x++)
            {
                for (int y = point.y - radius; y <= point.y + radius; y++)
                {
                    Vector2Int candidate = new(x, y);
                    if (layout.IsWalkable(candidate) && candidate != point)
                    {
                        candidates.Add(candidate);
                    }
                }
            }

            return candidates;
        }

        private static List<Vector2Int> BuildRoomCandidates(
            DungeonLayout layout,
            OfficeRoomPlan roomPlan,
            OfficeRoomRole roomRole)
        {
            List<Vector2Int> candidates = new();
            foreach (RectInt room in roomPlan.RoomsFor(roomRole))
            {
                for (int x = room.xMin + 1; x < room.xMax - 1; x++)
                {
                    for (int y = room.yMin + 1; y < room.yMax - 1; y++)
                    {
                        Vector2Int candidate = new(x, y);
                        if (layout.IsWalkable(candidate))
                        {
                            candidates.Add(candidate);
                        }
                    }
                }
            }

            return candidates;
        }

        private bool CanPlaceAt(DungeonLayout layout, Vector2Int position)
        {
            if (!layout.IsWalkable(position)
                || layout.IsMarker(position)
                || position == layout.Start
                || position == layout.Exit
                || occupiedPositions.Contains(position))
            {
                return false;
            }

            int minDistanceSquared = minSpacing * minSpacing;
            foreach (Vector2Int occupiedPosition in occupiedPositions)
            {
                if ((position - occupiedPosition).sqrMagnitude < minDistanceSquared)
                {
                    return false;
                }
            }

            return true;
        }

        private bool CanPlaceExitAt(DungeonLayout layout, Vector2Int position)
        {
            return layout.IsWalkable(position)
                && position != layout.Start
                && (position == layout.Exit || !layout.IsMarker(position))
                && !occupiedPositions.Contains(position);
        }

        private void SpawnPrefab(
            GameObject prefab,
            string label,
            Vector2Int gridPosition,
            float height,
            DungeonVisualizer visualizer)
        {
            GameObject spawned = Instantiate(prefab, questRoot);
            spawned.name = $"{prefab.name} {label} ({gridPosition.x}, {gridPosition.y})";
            spawned.transform.position = visualizer.GridToWorld(gridPosition) + Vector3.up * height;
        }

        private void OnValidate()
        {
            questsPerRun = Mathf.Max(0, questsPerRun);
            minSpacing = Mathf.Max(0, minSpacing);
            markerExclusionRadius = Mathf.Max(0, markerExclusionRadius);
        }

        private static void Shuffle<T>(IList<T> items, System.Random random)
        {
            for (int i = items.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                (items[i], items[j]) = (items[j], items[i]);
            }
        }
    }
}
