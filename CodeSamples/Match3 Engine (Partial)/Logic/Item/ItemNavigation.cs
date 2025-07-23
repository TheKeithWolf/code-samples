using CustomLogDll;
using UnityEngine;

public class ItemNavigation : ItemNavigationBase
{
    public ItemNavigation(Item item) : base(item) { }

    public override void ClearCells()
    {
        var cell = PivotalCell.Value;
        if (cell.Item.Value != Item)
        {
            this.LogError($"[{nameof(Item)}] {nameof(Cell)}'s Item = {cell.Item.Value} and not this = {this}]");
        }
        cell.Item.Value = null;
    }
    
    public override void MoveItemFromPosToCell(Vector2Int posPrevious, Cell cell, int tick)
    {
        cell.Item.Value = Item;
        Item.Navigation.PositionPrevious.Value = posPrevious;
        PivotalCell.Value = cell;
        if (cell.Position != posPrevious)
        {
            Item.Navigation.MoveProgress.Value = LevelConstants.GetTimeToMoveFromCellToCell(Item.Navigation.Speed);
        }
    }
}