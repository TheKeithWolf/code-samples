using System.Collections.Generic;
using System.Linq;

public class SpawnerGroup
{
    public readonly List<Cell> Spawners;

    public bool IsNotBusy => Spawners.All(c => c.Item.Value == null && c.IsBusy == 0);
    
    public SpawnerGroup(Cell cell)
    {
        Spawners = new List<Cell> { cell };
    }

    public SpawnerGroup(List<Cell> cells)
    {
        Spawners = cells;
    }
}