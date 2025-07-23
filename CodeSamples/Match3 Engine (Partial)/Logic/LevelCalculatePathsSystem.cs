using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CustomLogDll;
using UnityEngine;

public class LevelCalculatePathsSystem
{
    private readonly List<Cell> _endCells = new();
    
    public void CalculateCellPaths(Level level)
    {
        if (!level.State.HasFlag(LevelState.IsPathRecalculationRequired))
        {
            return;
        }
        
        _endCells.Clear();
        level.SpawnersGroups.Clear();
        
        level.Cells = level.Cells.OrderBy(c => c.Position.y).ThenBy(c => c.Position.x).ToList();
        
        // TODO Optimize this:
        foreach (var cell in level.Cells)
        {
            cell.ResetNavigation();
            
            // Searching for bottom cells:
            if (!level.TryGetCell(cell.Position + Vector2Int.down, out _))
            {
                _endCells.Add(cell);
            }

            if (cell.Type.Value.HasFlag(CellType.Spawner))
            {
                if (!level.SpawnersGroups.TryGetValue(1, out var group))
                {
                    group = new List<SpawnerGroup>();
                    level.SpawnersGroups.Add(1, group);
                }
                group.Add(new SpawnerGroup(cell));
            }
        }

        // Building paths from bottom up:
        foreach (var cell in _endCells)
        {
            var cellBellow = cell;
            while (true)
            {
                if (!level.TryGetCell(cellBellow.Position + Vector2Int.up, out var cellAbove))
                {
                    cellBellow.NavigationUpdated.Invoke();
                    break;
                }
                
                cellBellow.Feeders.Add(cellAbove);
                cellBellow.NavigationUpdated.Invoke();
                cellBellow = cellAbove;
            }
        }
        
        // TODO Do it each time spawner added/removed:
        GroupSpawners(level);
        
        level.State = level.State.Clear(LevelState.IsPathRecalculationRequired);
        this.Log($"{MethodBase.GetCurrentMethod()!.Name}");
    }

    private void GroupSpawners(Level level)
    {
        if (!level.SpawnersGroups.TryGetValue(1, out var spawners))
        {
            this.LogError("Level has no spawners");
            return;
        }

        var count = 100000000;
        var cells = new List<Cell>();
        var direction = Vector2Int.left;
        foreach (var spawnerGroup in spawners.OrderBy(s => s.Spawners.First().Position.x))
        {
            cells.Clear();
            var spawner = spawnerGroup.Spawners.First();
            cells.Add(spawner);
            while (count -- > 0)
            {
                if (level.TryGetCell(spawner.Position + direction, out var cell) && cell.Type.Value.HasFlag(CellType.Spawner))
                {
                    cells.Add(cell);
                    spawner = cell;
                }
                else
                {
                    while (cells.Count > 1)
                    {
                        if (!level.SpawnersGroups.TryGetValue(cells.Count, out var spawners2))
                        {
                            spawners2 = new List<SpawnerGroup>();
                            level.SpawnersGroups.Add(cells.Count, spawners2);
                        }
                        spawners2.Add(new SpawnerGroup(cells.ToList()));
                        cells.Remove(cells.Last());
                    }
                    break;
                }
            }
        }
    }
}