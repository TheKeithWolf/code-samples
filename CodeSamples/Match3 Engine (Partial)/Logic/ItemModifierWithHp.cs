using CustomLogDll;
using Standard;

public abstract class ItemModifierWithHp : ItemModifier, IHp
{
    public Bind<int> Hp { get; set; } = new();
    public abstract DamageType DamageTypeAllowed { get; set; }
    public virtual bool FinalHitDamagesTwice => false;
    
    public bool TryDamageReturnIsAlive(DamageType damageType, int damage, out int extraDamage)
    {
        var isAlive = this.TryDamageReturnIsAliveDefault(damageType, damage, out extraDamage);
        if (!isAlive)
        {
            IsDestroyed?.Invoke(this);
        }
        return isAlive;
    }

    protected ItemModifierWithHp(int hp = 1)
    {
        if (hp <= 0)
        {
            this.LogError("Hp is less or equal to zero");
        }
        
        Hp.Value = hp;
    }
    
    public override string ToString()
    {
        return $"{base.ToString()} Hp={Hp.Value}, FinalHitDamagesTwice={FinalHitDamagesTwice}";
    }
}