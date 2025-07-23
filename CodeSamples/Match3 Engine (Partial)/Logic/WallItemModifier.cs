using System;

public class WallItemModifier : ItemModifierWithHp, IMovable, ISwappable
{
    public override Type Type => typeof(WallItemModifier);
    public override DamageType DamageTypeAllowed { get; set; } = DamageType.All;
    public override bool FinalHitDamagesTwice => true;
    public bool IsMovable => false;
    public bool IsSwappable => false;

    public WallItemModifier(int hp = 1) : base(hp) { }
}