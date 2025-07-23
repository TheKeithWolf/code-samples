using System;
using System.Collections.Generic;
using System.Linq;
using CustomLogDll;

public class ItemModifiers
{
    private IMovable Movable { get; set; }
    public bool IsMovable => Movable?.IsMovable ?? true;
    
    private ISwappable Swappable { get; set; }
    public bool IsSwappable => Swappable?.IsSwappable ?? true;
    
    public IHp Hp { get; private set; }
    
    private IMatchPattern MatchPattern { get; set; }
    public MatchPatternType MatchPatternType => MatchPattern?.MatchPatternType ?? MatchPatternType.None;
    
    
    public readonly List<ItemModifier> Modifiers = new();
    
    public Action<ItemModifier> OnModifierAdded;
    public Action NoHpLeft;
    public Action<IDamagePattern> CreateActiveDamage;
    
    public void AddModifier<T>(T modifier, int index = -1) where T : ItemModifier
    {
        if (index == -1)
        {
            Modifiers.Add(modifier);
        }
        else
        {
            Modifiers.Insert(index, modifier);
        }
        CacheNewFlags(modifier);
        modifier.IsDestroyed -= RemoveModifier;
        modifier.IsDestroyed += RemoveModifier;
        OnModifierAdded?.Invoke(modifier);
    }

    private void RemoveModifier(ItemModifier modifier)
    {
        Modifiers.Remove(modifier);
        if (modifier is IDamagePattern iDamagePattern && iDamagePattern.DamagePattern != DamagePattern.None)
        {
            CreateActiveDamage.Invoke(iDamagePattern);
        }
        ReCacheFlags();
    }
    
    public void ReplaceModifier(ItemModifier modifierOld, ItemModifier modifierNew, bool noDamage = true)
    {
        var oldIndex = Modifiers.IndexOf(modifierOld);
        if (noDamage && modifierOld is IDamagePattern iDamagePattern)
        {
            iDamagePattern.DamagePattern = DamagePattern.None;
        }
        AddModifier(modifierNew, oldIndex);
        modifierOld.IsDestroyed.Invoke(modifierOld);
    }
    
    public void Damage(DamageType damageType, int damageAmount = 1)
    {
        if (Hp == null)
        {
            this.LogError("Hp is null!");
        }
        else
        {
            var damageLeft = damageAmount;
            while (damageLeft > 0)
            {
                if (!Hp.TryDamageReturnIsAlive(damageType, damageLeft, out damageLeft) && damageLeft > 0)
                {
                    if (Hp == null) break;
                }
            }
        }

        if (Hp == null)
        {
            NoHpLeft?.Invoke();
        }
    }

    private void ReCacheFlags()
    {
        SetDefaultValues();
        
        foreach (var itemModifier in Modifiers)
        {
            CacheNewFlags(itemModifier);
        }
    }

    private void CacheNewFlags(ItemModifier itemModifier)
    {
        if (itemModifier is IMovable movable)
        {
            Movable = movable;
        }
        if (itemModifier is ISwappable swappable)
        {
            Swappable = swappable;
        }
        if (itemModifier is IHp iHp)
        {
            Hp = iHp;
        }
        if (itemModifier is IMatchPattern matchPattern)
        {
            MatchPattern = matchPattern;
        }
    }

    private void SetDefaultValues()
    {
        Movable = null;
        Swappable = null;
        MatchPattern = null;
        Hp = null;
    }

    public override string ToString()
    {
        return $"NotMovable={Movable}, IsSwappable={Swappable}, Hp={Hp}, MatchPattern={MatchPattern}, Modifiers={Modifiers.Aggregate("", (current, modifier) => current + (modifier + ", "))}";
    }
}