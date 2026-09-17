// File: PCG/DungeonTile.cs
// Purpose: Shared enums for procedural tile types and generator methods.
// Inputs: Enum selections from generators, visualizers, scenes, and ScriptableObject settings.
// Output/side effects: Provides common labels for wall/floor/marker tiles and BSP/CA/Hybrid method choice.

namespace CM3070.PCG
{
    public enum DungeonTile
    {
        Wall,
        Floor,
        Start,
        Exit,
        Enemy,
        Loot
    }

    public enum DungeonGenerationMethod
    {
        BspRooms,
        CellularAutomata,
        HybridBspCellular
    }
}
