using System;

public class AcornItemModifier : ItemModifierWithHp, ISwappable
{
    public override Type Type => typeof(AcornItemModifier);
    public override DamageType DamageTypeAllowed { get; set; } = DamageType.All;
    public override bool FinalHitDamagesTwice => true;
    public bool IsSwappable => false;
    
    public AcornItemModifier(int hp = 1) : base(hp) { }
}