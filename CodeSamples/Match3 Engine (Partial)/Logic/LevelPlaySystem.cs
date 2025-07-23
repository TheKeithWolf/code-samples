using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LevelPlaySystem
{
    private readonly FindMatchesSystem _findMatchesSystem = new();
    public readonly DamageSystem DamageSystem = new();
    
    public void Play(Level level)
    {
        level.State = level.State.Clear(LevelState.MovedItems);
        level.State = level.State.Clear(LevelState.PoppedMatches);

        var tickId = LevelSystem.Instance.TickId;
        
        _findMatchesSystem.Clear();
        
        foreach (var cell in level.Cells)
        {
            var itemNavigation = cell.Item.Value?.Navigation;
            
            if (itemNavigation == null)
            {
                TryToMoveSomethingIntoEmptyCell(level, cell);
            }
            else
            {
                if (itemNavigation.PivotalCell == cell)
                {
                    if (itemNavigation.MoveProgress > 0)
                    {
                        itemNavigation.LastMovementTickId = tickId;
                        itemNavigation.MoveProgress.Value--;
                        level.State = level.State.Set(LevelState.MovedItems);
                    }
                    else
                    {
                        if (itemNavigation.Speed > 0)
                        {
                        
                        }
                        else
                        {
                        
                        }
                        itemNavigation.LastMovementTickId = tickId;
                        itemNavigation.Speed = 0;
                        _findMatchesSystem.CheckMatches(level, cell);
                    }
                }
            }
            
            if (cell.IsBusy > 0)
            {
                cell.IsBusy.Value--;
            }
        }

        var matches = _findMatchesSystem.GetMatches();
        if (matches is { Count: > 0 })
        {
            DamageSystem.ApplyDamage(_findMatchesSystem.GetMatches());
            level.State = level.State.Set(LevelState.PoppedMatches);
        }
    }

    private void TryToMoveSomethingIntoEmptyCell(Level level, Cell cell)
    {
        if (cell.IsBusy != 0)
        {
            return;
        }

        var feeder = cell.Feeders.FirstOrDefault(); // TODO Allow multiple Feeders
        if (feeder == null)
        {
            return;
        }

        var feederItem = feeder.Item?.Value;
        if (feederItem == null)
        {
            return;
        }

        var feederItemNavigation = feederItem.Navigation;
        var feederItemModifiers = feederItem.Modifiers;
        if (!feederItemModifiers.IsMovable || feederItemNavigation.MoveProgress > 1)
        {
            return;
        }

        if (feederItemNavigation.PivotalCell == feeder) // We can move only PivotalCell in Item
        {
            if (feederItemNavigation.MoveProgress == 0)
            {
                feederItemNavigation.Speed = 0;
            }
            else if (feederItemNavigation.MoveProgress == 1)
            {
                feederItemNavigation.Speed++;
            }
        }

        if (feederItemNavigation.TryMoveItemFromCellToCell(feeder, cell, LevelSystem.Instance.TickId))
        {
            level.State = level.State.Set(LevelState.MovedItems);
        }
    }
}

public class FindMatchesSystem
{
    private readonly List<Item> _matches = new();
    private readonly List<Item> _testMatch = new();

    public void Clear()
    {
        _matches.Clear();
    }
    
    public void CheckMatches(Level level, Cell cell)
    {
        if (!cell.TryGetItemAndMatchPattern(out var item, out var matchPattern))
        {
            return;
        }
        if (matchPattern <= 0)
        {
            return;
        }

        _testMatch.Clear();
        _testMatch.Add(item);
        CheckDirection(level, cell.Position, Vector2Int.left, matchPattern, _testMatch);
        CheckDirection(level, cell.Position, Vector2Int.right, matchPattern, _testMatch);
        if (_testMatch.Count >= 3)
        {
            foreach (var item1 in _testMatch)
            {
                if (!_matches.Contains(item1))
                {
                    _matches.Add(item1);
                }
            }
        }

        _testMatch.Clear();
        _testMatch.Add(item);
        CheckDirection(level, cell.Position, Vector2Int.up, matchPattern, _testMatch);
        CheckDirection(level, cell.Position, Vector2Int.down, matchPattern, _testMatch);
        if (_testMatch.Count >= 3)
        {
            foreach (var item1 in _testMatch)
            {
                if (!_matches.Contains(item1))
                {
                    _matches.Add(item1);
                }
            }
        }
    }

    public List<Item> GetMatches()
    {
        return _matches;
    }

    private void CheckDirection(Level level, Vector2Int position, Vector2Int direction, MatchPatternType matchPatternType, List<Item> testMatch)
    {
        while (true)
        {
            if (level.TryGetCell(position + direction, out var nextCell))
            {
                if (nextCell.TryGetItem(out var nextItem))
                {
                    if (nextItem.IsMatchableWith(matchPatternType))
                    {
                        testMatch.Add(nextItem);
                    }
                }
            }

            break;
        }
    }
}