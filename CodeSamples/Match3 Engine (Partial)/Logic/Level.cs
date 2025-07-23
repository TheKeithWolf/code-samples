using System;
using System.Collections.Generic;
using System.Linq;
using Standard;
using UnityEngine;

public class Level
{
    public LevelConfig LevelConfig { get; private set; }
    
    public List<Cell> Cells = new();
    
    public readonly Dictionary<int, List<SpawnerGroup>> SpawnersGroups = new();
    
    public readonly List<Item> NewItems = new();

    public LevelState State { get; set; } = LevelState.AreCellsChanged | LevelState.NeedRepositioning | LevelState.IsPathRecalculationRequired;

    public bool IsDirty => State > 0;
    
    public Level(LevelConfig levelConfig)
    {
        LevelConfig = levelConfig;

        var height = 5;
        for (var j = 0; j <= height; j++)
        {
            for (var i = 0; i <= 5; i++)
            {
                var cell = new Cell(i, j);
                Cells.Add(cell);
                cell.Type.SubscribeAndSet(OnCellTypeChanged);
                if (cell.Position.y == height)
                {
                    cell.Type.Set(CellType.Spawner);
                }
            }
        }
    }

    public void RemoveCell(Cell cell)
    {
        cell.Destroy();
        Cells.Remove(cell);
        
        State = State.Set(LevelState.AreCellsChanged | LevelState.NeedRepositioning | LevelState.IsPathRecalculationRequired);
    }

    public void AddCell(Vector2Int position)
    {
        var cell = new Cell(position);
        Cells.Add(cell);
        
        State = State.Set(LevelState.AreCellsChanged | LevelState.NeedRepositioning | LevelState.IsPathRecalculationRequired);
    }

    public Vector2Int GetMin()
    {
        return new Vector2Int(Cells.Min(c => c.Position.x), Cells.Min(c => c.Position.y));
    }

    public Vector2Int GetMax()
    {
        return new Vector2Int(Cells.Max(c => c.Position.x), Cells.Max(c => c.Position.y));
    }

    public bool TryGetCell(Vector2Int cellPosition, out Cell cell)
    {
        cell = Cells.FirstOrDefault(c => c.Position == cellPosition);
        return cell != null;
    }
    
    private void OnCellTypeChanged(Bind<CellType> type)
    {
        State = State.Set(LevelState.IsPathRecalculationRequired);
    }
    
    public void AddItem(Item item)
    {
        NewItems.Add(item);
    }

    public bool TrySwapItems(Vector2Int position, Vector2Int positionOther)
    {
        // TODO Fix swap:
        
        /*if (TryGetCell(position, out var cell))
        {
            var item = cell.Item.Value;
            if (item != null)
            {
                if (TryGetCell(positionOther, out var cellOther))
                {
                    var itemOther = cellOther.Item.Value;
                    if (itemOther != null && item.Modifiers.IsSwappable && itemOther.Modifiers.IsSwappable)
                    {
                        cell.Item.Value = itemOther;
                        cellOther.Item.Value = item;
                        item.SetCell(cellOther);
                        itemOther.SetCell(cell);
                        item.Navigation.MoveProgress.Value = 1;    // TODO Check if we need this?
                        itemOther.Navigation.MoveProgress.Value = 1;

                        return true;
                    }
                }
            }
        }*/

        return false;
    }
}

[Flags]
public enum LevelState
{
    None   = 0,
    AreCellsChanged  = 1 << 0,
    NeedRepositioning = 1 << 1,
    IsPathRecalculationRequired  = 1 << 2,
    SpawnedNewItems  = 1 << 3,
    MovedItems  = 1 << 5,
    PoppedMatches  = 1 << 6,
}

public static class LevelExtensions
{
    public static LevelState Set(this LevelState state, LevelState stateOther)
    {
        return state | stateOther;
    }

    public static LevelState Clear(this LevelState state, LevelState stateOther)
    {
        return state & ~stateOther;
    }
}