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
