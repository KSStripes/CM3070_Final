using System.Collections.Generic;
using System.Linq;
using CM3070.PCG;
using UnityEngine;

namespace CM3070.Office
{
    public enum RoomRole
    {
        None,
        Reception,
        BossRoom,
        Factory,
        Office,
        Overflow
    }

    public readonly struct RoomAssignment
    {
        public RoomAssignment(RectInt room, RoomRole role)
        {
            Room = room;
            Role = role;
        }

        public RectInt Room { get; }
        public RoomRole Role { get; }
    }

    public sealed class RoomPlan
    {
        private readonly List<RoomAssignment> rooms;

        public RoomPlan(DungeonLayout layout, IEnumerable<RoomAssignment> rooms, bool valid)
        {
            Layout = layout;
            this.rooms = rooms.ToList();
            HasRequiredRooms = valid;
        }

        public DungeonLayout Layout { get; }
        public IReadOnlyList<RoomAssignment> Assignments => rooms;
        public bool HasRequiredRooms { get; }

        public IEnumerable<RectInt> RoomsFor(RoomRole role)
        {
            return rooms.Where(room => room.Role == role).Select(room => room.Room);
        }

        public bool TryGetRole(RectInt room, out RoomRole role)
        {
            foreach (RoomAssignment item in rooms)
            {
                if (item.Room == room)
                {
                    role = item.Role;
                    return true;
                }
            }

            role = RoomRole.None;
            return false;
        }

        public bool TryGetRoleAt(Vector2Int position, out RoomRole role)
        {
            // Corridors are outside BSP room rectangles, so they return None.
            foreach (RoomAssignment item in rooms)
            {
                if (item.Room.Contains(position))
                {
                    role = item.Role;
                    return true;
                }
            }

            role = RoomRole.None;
            return false;
        }
    }
}
