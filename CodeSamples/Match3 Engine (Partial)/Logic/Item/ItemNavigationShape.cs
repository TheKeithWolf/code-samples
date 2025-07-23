using System.Collections.Generic;
using System.Linq;
using CustomLogDll;
using UnityEngine;

public class ItemNavigationShape : ItemNavigationBase
{
    public List<Vector2Int> Shape { get; }
    
    // TODO Remove:
    public Level Level { get; }
    
    public readonly Dictionary<Vector2Int, int> PositionAndReadyToMoveTick = new();

    public Vector2Int LevelToShapeOffset => PivotalCell.Value.Position - Shape.First();
    
    public ItemNavigationShape(Item item, Level level, List<Vector2Int> shape) : base(item)
    {
        Level = level;
        Shape = shape;
        
        var minY = shape.Min(v => v.y);
        foreach (var position in shape.Where(v => v.y == minY))
        {
            PositionAndReadyToMoveTick.Add(position, -1);
        }
        PositionAndReadyToMoveTick.Remove(Shape.First());
    }
    
    public override void ClearCells()
    {
        var offset = LevelToShapeOffset;
        
        foreach (var shapePosition in Shape)
        {
            if (Level.TryGetCell(shapePosition + offset, out var cell))
            {
                if (cell.Item.Value != Item)
                {
                    this.LogError($"[{nameof(Item)}] {nameof(Cell)}'s Item = {cell.Item.Value} and not this = {this}]");
                }
                cell.Item.Value = null;
            }
            else
            {
                // Item can be out of bounds above spawner
            }
        }
    }
    
    public override void MoveItemFromPosToCell(Vector2Int posPrevious, Cell cell, int tick)
    {
        var offset = cell.Position - Shape.First();

        var isPivotalCell = true;
        foreach (var shapePosition in Shape)
        {
            if (Level.TryGetCell(shapePosition + offset, out var shapeCell))
            {
                shapeCell.Item.Value = Item;
                if (isPivotalCell)
                {
                    Item.Navigation.PositionPrevious.Value = posPrevious;
                    PivotalCell.Value = shapeCell;
                    if (cell.Position != posPrevious)
                    {
                        Item.Navigation.MoveProgress.Value = LevelConstants.GetTimeToMoveFromCellToCell(Item.Navigation.Speed);
                    }
                    isPivotalCell = false;
                }
            }
            else
            {
                // Item can be out of bounds above spawner
            }
        }
    }
    
    public override bool TryMoveItemFromCellToCell(Cell cellFrom, Cell cellTo, int tick)
    {
        if (cellFrom != PivotalCell.Value)
        {
            var positionInShape = cellFrom.Position - LevelToShapeOffset;
            if (PositionAndReadyToMoveTick.ContainsKey(positionInShape))
            {
                PositionAndReadyToMoveTick[positionInShape] = tick;
            }
            return false;
        }
        
        if (PositionAndReadyToMoveTick.Any(kvp => kvp.Value != tick)) // isReadyToMove
        {
            return false;
        }
        
        return base.TryMoveItemFromCellToCell(cellFrom, cellTo, tick);
    }
}