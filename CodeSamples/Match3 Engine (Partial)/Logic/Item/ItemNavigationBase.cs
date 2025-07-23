using CustomLogDll;
using Standard;
using UnityEngine;

public abstract class ItemNavigationBase
{
    private int _speed;
    public Item Item { get; }
    
    public Bind<Cell> PivotalCell { get; } = new();
    public Bind<Vector2Int> PositionPrevious { get; } = new();
    public Bind<int> MoveProgress { get; } = new();

    public int Speed
    {
        get => _speed;
        set
        {
            _speed = value;
            this.LogError($"Speed: {_speed}, Tick={Time.frameCount}");
        }
    }

    public int LastMovementTickId { get; set; }
    public bool IsMoving => MoveProgress.Value > 0;
    public bool IsNotMoving => MoveProgress.Value == 0;

    public abstract void ClearCells();
    public abstract void MoveItemFromPosToCell(Vector2Int posPrevious, Cell cell, int tick);

    public ItemNavigationBase(Item item)
    {
        Item = item;
        
        MoveProgress.SubscribeAndSet(Kek);
    }
    
    private void Kek(Bind<int> moveProgress)
    {
        this.LogError($"OnPositionChanged: {moveProgress.Value}, Tick={Time.frameCount}");
    }
    
    public Cell GetPivotCell()
    {
        return PivotalCell;
    }
    
    public virtual bool TryMoveItemFromCellToCell(Cell cellFrom, Cell cellTo, int tick)
    {
        ClearCells();
        MoveItemFromPosToCell(cellFrom.Position, cellTo, tick);
        return true;
    }
    
    public override string ToString()
    {
        return $"Cell={GetPivotCell()}, PositionPrevious={PositionPrevious}, MoveProgress={MoveProgress}, Speed={Speed}, LastMovementTickId={LastMovementTickId}, IsMoving={IsMoving}";
    }
}