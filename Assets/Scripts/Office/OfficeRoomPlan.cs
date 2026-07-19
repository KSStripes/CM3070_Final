using System.Collections.Generic;
using System.Linq;
using CM3070.PCG;
using UnityEngine;

namespace CM3070.Office
{
    public readonly struct OfficeRoomAssignment
    {
        public OfficeRoomAssignment(RectInt room, OfficeRoomRole role)
        {
            Room = room;
            Role = role;
        }

        public RectInt Room { get; }
        public OfficeRoomRole Role { get; }
    }

    public sealed class OfficeRoomPlan
    {
        private readonly List<OfficeRoomAssignment> assignments;

        public OfficeRoomPlan(DungeonLayout layout, IEnumerable<OfficeRoomAssignment> assignments, bool hasRequiredRooms)
        {
            Layout = layout;
            this.assignments = assignments.ToList();
            HasRequiredRooms = hasRequiredRooms;
        }

        public DungeonLayout Layout { get; }
        public IReadOnlyList<OfficeRoomAssignment> Assignments => assignments;
        public bool HasRequiredRooms { get; }

        public IEnumerable<RectInt> RoomsFor(OfficeRoomRole role)
        {
            return assignments
                .Where(assignment => assignment.Role == role)
                .Select(assignment => assignment.Room);
        }

        public bool TryGetRole(RectInt room, out OfficeRoomRole role)
        {
            foreach (OfficeRoomAssignment assignment in assignments)
            {
                if (assignment.Room == room)
                {
                    role = assignment.Role;
                    return true;
                }
            }

            role = OfficeRoomRole.None;
            return false;
        }

        public bool TryGetRoleAt(Vector2Int position, out OfficeRoomRole role)
        {
            // Room roles are assigned to BSP room rectangles; corridors remain unassigned.
            foreach (OfficeRoomAssignment assignment in assignments)
            {
                if (assignment.Room.Contains(position))
                {
                    role = assignment.Role;
                    return true;
                }
            }

            role = OfficeRoomRole.None;
            return false;
        }
    }
}
