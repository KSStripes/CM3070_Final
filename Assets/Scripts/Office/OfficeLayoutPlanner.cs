using System.Collections.Generic;
using System.Linq;
using CM3070.PCG;
using UnityEngine;

namespace CM3070.Office
{
    public static class OfficeLayoutPlanner
    {
        private const int RequiredRoomCount = 6;

        public static OfficeRoomPlan CreatePlan(DungeonLayout layout)
        {
            if (layout == null || layout.Rooms.Count == 0)
            {
                return new OfficeRoomPlan(layout, Enumerable.Empty<OfficeRoomAssignment>(), false);
            }

            List<RectInt> remaining = layout.Rooms.ToList();
            List<OfficeRoomAssignment> assignments = new();

            // Reception anchors the run near the start/entrance.
            AssignBestRoom(remaining, assignments, OfficeRoomRole.Reception, room => DistanceTo(room, layout.Start), false);

            // Boss room anchors the far/deep end of the shift.
            AssignBestRoom(remaining, assignments, OfficeRoomRole.BossRoom, room => DistanceTo(room, layout.Start), true);

            // Factory rooms use the largest remaining spaces because they need machinery/lanes.
            AssignLargestRooms(remaining, assignments, OfficeRoomRole.Factory, 2);

            // Office rooms use the next largest remaining spaces for desk clusters.
            AssignLargestRooms(remaining, assignments, OfficeRoomRole.Office, 2);

            foreach (RectInt room in remaining)
            {
                assignments.Add(new OfficeRoomAssignment(room, OfficeRoomRole.Overflow));
            }

            bool hasRequiredRooms = layout.Rooms.Count >= RequiredRoomCount;
            return new OfficeRoomPlan(layout, assignments, hasRequiredRooms);
        }

        private static void AssignBestRoom(
            List<RectInt> remaining,
            List<OfficeRoomAssignment> assignments,
            OfficeRoomRole role,
            System.Func<RectInt, float> score,
            bool highestScore)
        {
            if (remaining.Count == 0)
            {
                return;
            }

            RectInt room = highestScore
                ? remaining.OrderByDescending(score).First()
                : remaining.OrderBy(score).First();

            remaining.Remove(room);
            assignments.Add(new OfficeRoomAssignment(room, role));
        }

        private static void AssignLargestRooms(
            List<RectInt> remaining,
            List<OfficeRoomAssignment> assignments,
            OfficeRoomRole role,
            int count)
        {
            foreach (RectInt room in remaining.OrderByDescending(Area).Take(count).ToList())
            {
                remaining.Remove(room);
                assignments.Add(new OfficeRoomAssignment(room, role));
            }
        }

        private static int Area(RectInt room)
        {
            return room.width * room.height;
        }

        private static float DistanceTo(RectInt room, Vector2Int position)
        {
            return Vector2.Distance(room.center, position);
        }
    }
}
