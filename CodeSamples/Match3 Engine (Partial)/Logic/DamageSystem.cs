using System;
using System.Collections.Generic;
using CustomLogDll;
using UnityEngine;

public class DamageSystem
{
    private readonly Dictionary<DamageVariety, Damage> _damages = DamageCacheSystem.CacheDamages();
    
    public static readonly Dictionary<DamagePattern, DamageVariety> ItemTypeToDamageType = new()
    {
        { DamagePattern.Bomb, DamageVariety.Bomb},
        { DamagePattern.Horizontal, DamageVariety.Horizontal},
        { DamagePattern.Vertical, DamageVariety.Vertical},
    };
    
    private readonly List<ActiveDamage> _activeDamages = new();

    public void ApplyDamage(List<Item> matches)
    {
        PopMatches(matches);
        TickActiveDamages();
    }
    
    public void PopMatches(List<Item> matches)
    {
        for (var i = matches.Count - 1; i >= 0; i--)
        {
            var item = matches[i];
            var cell = item.Navigation.GetPivotCell();
            DamageCell(cell, DamageType.Match);
        }
        matches.Clear();
    }

    public void DamageCell(Cell cell, DamageType damageType)
    {
        if (cell.TryGetItem(out var item))
        {
            item.Modifiers.Damage(damageType);
        }
        cell.IsBusy.Value = LevelConstants.BusyTime;
        
        // Debug:
        var color = Color.blue;

        if (damageType == DamageType.None)
        {
            this.LogError($"Damage type {damageType} is not supported");
        }
        else
        {
            if (damageType.HasFlag(DamageType.Adjacent))
            {
                color = Color.yellow;
            }
            if (damageType.HasFlag(DamageType.Match))
            {
                color = Color.green;
            }
            if (damageType.HasFlag(DamageType.Base))
            {
                color = Color.red;
            }
        }
        var debugSprite = LevelSystem.Instance.DebugSpawnSpriteSystem.SpawnDebugSprite(cell.Position, color.ChangeAlphaTo(0.7f), 1);
        debugSprite.IsFading = true;
    }

    private void TickActiveDamages()
    {
        for (var i = _activeDamages.Count - 1; i >= 0; i--)
        {
            var activeDamage = _activeDamages[i];
            if (activeDamage.TryGetCurrentTickHits(out var hits))
            {
                foreach (var hit in hits)
                {
                    if (LevelSystem.Instance.Level.TryGetCell(hit.Coords + activeDamage.Position, out var cell))
                    {
                        DamageCell(cell, activeDamage.DamageType);
                    }
                }
            }
            else
            {
                _activeDamages.Remove(activeDamage);
            }
        }
    }

    public void AddActiveDamage(Vector2Int position, DamageVariety damageVariety)
    {
        var activeDamage = new ActiveDamage(position, _damages[damageVariety], DamageType.Base);
        _activeDamages.Add(activeDamage);
    }
}

public class ActiveDamage
{
    public Vector2Int Position { get; }
    public Damage Damage { get; }
    private int _currentTick;
    public DamageType DamageType { get; set; }

    public ActiveDamage(Vector2Int position, Damage damage, DamageType damageType)
    {
        Position = position;
        Damage = damage;
        DamageType = damageType;
    }

    public bool TryGetCurrentTickHits(out List<DamageHit> hits)
    {
        if (_currentTick >= Damage.DamageTicks.Count)
        {
            hits = null;
            return false;
        }

        hits = Damage.DamageTicks[_currentTick];
        _currentTick++;
        return true;
    }
}

public class Damage
{
    public List<List<DamageHit>> DamageTicks = new();
    public bool IsDamageInfinite { get; set; }
}

public class DamageHit
{
    public Vector2Int Coords { get; }
    

    public DamageHit(Vector2Int coords)
    {
        Coords = coords;
    }
    
    public DamageHit(int x, int y)
    {
        Coords = new Vector2Int(x, y);
    }
}

public enum DamageVariety
{
    Match,
    Horizontal,
    Vertical,
    Bomb
}

[Flags]
public enum DamageType
{
    None   = 0,
    Adjacent  = 1 << 0,
    Match = 1 << 1,
    Base = 1 << 2,
    AllExceptAdjacent = Match | Base,
    All = Adjacent | Match | Base
}