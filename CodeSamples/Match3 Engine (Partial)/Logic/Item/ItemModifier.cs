using System;

public abstract class ItemModifier
{
    public abstract Type Type { get; }
    public Action<ItemModifier> IsDestroyed;

    public override string ToString()
    {
        return $"{Type}: ";
    }
}

// Interfaces:
public interface IMovable
{
    bool IsMovable {get;}
}

public interface ISwappable
{
    bool IsSwappable {get;}
}

public interface IMatchPattern
{
    MatchPatternType MatchPatternType {get;}
}

public interface IDamagePattern
{
    DamagePattern DamagePattern { get; set; } // Has setter only for Debug ReplaceModifier
}

[Flags]
public enum MatchPatternType
{
    None   = 0,
    Match0  = 1 << 0,
    Match1  = 1 << 1,
    Match2  = 1 << 2,
    Match3  = 1 << 3,
    Match4  = 1 << 4,
    Match5  = 1 << 5,
    Match6  = 1 << 6,
    Match7  = 1 << 7,
    Match8  = 1 << 8
}