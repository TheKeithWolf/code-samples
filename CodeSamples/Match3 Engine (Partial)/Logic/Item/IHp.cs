using Standard;

public interface IHp
{
    Bind<int> Hp { get; set; }
    DamageType DamageTypeAllowed { get; set; }
    bool FinalHitDamagesTwice => false;

    bool TryDamageReturnIsAlive(DamageType damageType, int damage, out int extraDamage);
}

public static class HpHelper
{
    public static bool TryDamageReturnIsAliveDefault(this IHp iHp, DamageType damageType, int damage, out int extraDamage)
    {
        if ((damageType & iHp.DamageTypeAllowed) == 0)
        {
            extraDamage = 0;
            return true;
        }
        
        var hp = iHp.Hp;
        hp.Value -= damage;
        if (hp <= 0)
        {
            extraDamage = -hp;
            if (iHp.FinalHitDamagesTwice)
            {
                extraDamage++;
            }
            return false;
        }

        extraDamage = 0;
        return true;
    }
}