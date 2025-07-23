public static class LevelConstants
{
    public const int TicksInSecond = 10;
    private const int TimeToMoveFromCellToCell = 4;
    public const int BusyTime = 4;

#if UNITY_EDITOR
    public const string DebugCellInfo = "DebugCellInfo";
    public const string DebugCellInfoType = "DebugCellInfoType";
    public const string DebugItemInfo = "DebugItemInfo";
#endif

    public static int GetTimeToMoveFromCellToCell(int speed)
    {
        return speed switch
        {
            0 => TimeToMoveFromCellToCell,
            1 => TimeToMoveFromCellToCell - 1,
            2 => TimeToMoveFromCellToCell - 2,
            _ => TimeToMoveFromCellToCell - 2
        };
    }
}