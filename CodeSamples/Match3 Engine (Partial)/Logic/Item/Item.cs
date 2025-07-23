using System;
using System.Collections.Generic;
using CustomLogDll;
using UnityEngine;

public class Item
{
    public int Id { get; } = Ids.GetItemId(typeof(Item));
    
    public ItemNavigationBase Navigation { get; }
    public ItemModifiers Modifiers { get; } = new();
    
    public Action<Item> IsDestroyed;

    public bool IsMatchable => Navigation.IsNotMoving && Modifiers.MatchPatternType > 0;

    public bool IsMatchableWith(MatchPatternType matchPatternType)
    {
        return IsMatchable && (Modifiers.MatchPatternType & matchPatternType) > 0;
    }

    public Item()
    {
        Navigation = new ItemNavigation(this);
        
        Subscribe();
    }

    public Item(Level level, List<Vector2Int> shape)
    {
        Navigation = new ItemNavigationShape(this, level, shape);

        Subscribe();
    }

    private void Subscribe()
    {
        Modifiers.NoHpLeft -= Destroy;
        Modifiers.NoHpLeft += Destroy;
        Modifiers.CreateActiveDamage -= CreateActiveDamage;
        Modifiers.CreateActiveDamage += CreateActiveDamage;
    }
    
    public void Destroy()
    {
        Navigation.ClearCells();

        for (var i = Modifiers.Modifiers.Count - 1; i >= 0; i--)
        {
            var modifier = Modifiers.Modifiers[i];
            modifier.IsDestroyed.Invoke(modifier);
        }

        IsDestroyed?.Invoke(this);
        this.Log($"<color=red>Item {Id} destroyed</color>\n{this}");
    }
    
    private void CreateActiveDamage(IDamagePattern iDamagePattern)
    {
        var damagePattern = iDamagePattern.DamagePattern;
        var cell = Navigation.GetPivotCell();
        var damageSystem = LevelSystem.Instance.LevelPlaySystem.DamageSystem;
        if (damagePattern.HasFlag(DamagePattern.Horizontal))
        {
            damageSystem.AddActiveDamage(cell.Position, DamageSystem.ItemTypeToDamageType[DamagePattern.Horizontal]);
        }

        if (damagePattern.HasFlag(DamagePattern.Vertical))
        {
            damageSystem.AddActiveDamage(cell.Position, DamageSystem.ItemTypeToDamageType[DamagePattern.Vertical]);
        }

        if (damagePattern.HasFlag(DamagePattern.Bomb))
        {
            damageSystem.AddActiveDamage(cell.Position, DamageSystem.ItemTypeToDamageType[DamagePattern.Bomb]);
        }
    }
    
    public override string ToString()
    {
        return $"[{Id} | {Navigation} | {Modifiers}]";
    }
    
    public string ToEditorString()
    {
        return $"Id={Id}\nNavigation=[{Navigation}]\nModifiers=[{Modifiers}]";
    }
}