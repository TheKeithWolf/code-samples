using System;

public class ChainItemModifier : ItemModifierWithHp, IMovable, ISwappable
{
    public override Type Type => typeof(ChainItemModifier);
    public sealed override DamageType DamageTypeAllowed { get; set; }
    public override bool FinalHitDamagesTwice => true;
    public bool IsMovable => false;
    public bool IsSwappable => false;

    public ChainItemModifier(DamageType damageTypeAllowed, int hp = 1) : base(hp)
    {
        DamageTypeAllowed = damageTypeAllowed;
    }
}