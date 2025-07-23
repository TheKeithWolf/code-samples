using System;

public class ContainerItemModifier : ItemModifierWithHp, IMovable, ISwappable
{
    public override Type Type => typeof(ContainerItemModifier);
    public override DamageType DamageTypeAllowed { get; set; } = DamageType.All;
    public override bool FinalHitDamagesTwice => true;
    public bool IsMovable => true;
    public bool IsSwappable => false;
    
    public ContainerItemModifier(int hp = 1) : base(hp) { }
}