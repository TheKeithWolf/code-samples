using System;

public class SimpleMatchable : ItemModifierWithHp, IMatchPattern, IDamagePattern
{
    public override Type Type => typeof(SimpleMatchable);
    public override DamageType DamageTypeAllowed { get; set; } = DamageType.AllExceptAdjacent;
    public MatchPatternType MatchPatternType { get; }
    public DamagePattern DamagePattern { get; set; }
    

    public SimpleMatchable(MatchPatternType patternType, DamagePattern simpleDamagePattern)
    {
        MatchPatternType = patternType;
        DamagePattern = simpleDamagePattern;
    }
    
    public override string ToString()
    {
        return $"{base.ToString()}, Pattern={MatchPatternType}, SimpleSimpleSpecialType={DamagePattern}";
    }
}