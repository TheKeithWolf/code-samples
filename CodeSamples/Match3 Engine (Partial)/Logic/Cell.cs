using System;
using System.Collections.Generic;
using Standard;
using UnityEngine;

public class Cell : IEquatable<Cell>
{
    public Vector2Int Position { get; }
    
    public ItemModifiers Modifiers { get; } = new();
    
    public Bind<CellType> Type { get; } = new();
    public Bind<int> IsBusy { get; } = new();
    public Bind<Item> Item { get; } = new();

    // Navigation:
    public readonly List<Cell> Feeders = new();
    public Action NavigationUpdated { get; set; }
    
    public Cell(int x, int y)
    {
        Position = new Vector2Int(x, y);
    }
    
    public Cell(Vector2Int position)
    {
        Position = position;
    }

    public void ResetNavigation()
    {
        Feeders.Clear();
    }

    public void Destroy()
    {
        Item.Value?.Destroy();
    }
    
    public bool TryGetItem(out Item item)
    {
        item = Item.Value;
        return item != null;
    }
    
    public bool TryGetItemAndMatchPattern(out Item item, out MatchPatternType matchPatternType)
    {
        if (TryGetItem(out item))
        {
            matchPatternType = item.Modifiers.MatchPatternType;
            return true;
        }

        matchPatternType = default;
        return false;
    }

    public override string ToString()
    {
        return $"[{Position.x}, {Position.y}]";
    }

#region Equality
    public static bool operator ==(Cell cell, Cell otherCell)
    {
        return cell?.Equals(otherCell) ?? otherCell is null;
    }

    public static bool operator !=(Cell cell, Cell otherCell)
    {
        return !(cell == otherCell);
    }

    public bool Equals(Cell other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return Position.Equals(other.Position);
    }

    public override bool Equals(object obj)
    {
        if (obj is null)
        {
            return false;
        }

        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        if (obj.GetType() != GetType())
        {
            return false;
        }

        return Equals((Cell)obj);
    }

    public override int GetHashCode()
    {
        return Position.GetHashCode();
    }
#endregion Equality
}

[Flags]
public enum CellType
{
    None   = 0,
    Spawner  = 1 << 0,
    End = 1 << 1,
}

public static class CellExtensions
{
    public static List<CellType> CellTypes = new()
    {
        CellType.Spawner, CellType.End
    };
    
    public static void Set(this Bind<CellType> state, CellType stateOther)
    {
        state.Value =  state | stateOther;
    }

    public static void Clear(this Bind<CellType> state, CellType stateOther)
    {
        state.Value = state & ~stateOther;
    }
}