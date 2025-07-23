using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class DamageCacheSystem
{
    private const int DirectionLimit = 20;  
    private static readonly Dictionary<DamageVariety, Damage> Damages = new();
    
    public static Dictionary<DamageVariety, Damage> CacheDamages()
    {
        Damages.Clear();
        
        CacheHorizontalDamage();
        CacheVerticalDamage();
        CacheBombDamage();

        return Damages;
    }
    
    private static void CacheHorizontalDamage()
    {
        var damage = CacheDirectionalDamage(new List<Vector2Int> { Vector2Int.right, Vector2Int.left}, DirectionLimit);
        damage.IsDamageInfinite = true;
        Damages.Add(DamageVariety.Horizontal, damage);
    }
    
    private static void CacheVerticalDamage()
    {
        var damage = CacheDirectionalDamage(new List<Vector2Int> { Vector2Int.up, Vector2Int.down}, DirectionLimit);
        damage.IsDamageInfinite = true;
        Damages.Add(DamageVariety.Vertical, damage);
    }
    
    private static Damage CacheDirectionalDamage(List<Vector2Int> directions, int directionLimit)
    {
        var directionalDamage = new Damage();
        for (var i = 1; i <= directionLimit; i++)
        {
            var directionalHits = directions.Select(direction => new DamageHit(direction * i)).ToList();
            directionalDamage.DamageTicks.Add(directionalHits);
        }
        return directionalDamage;
    }

    private static void CacheBombDamage()
    {
        var bombDamage = new Damage();
        var aroundHits = GetHitsAroundZone(0, 0, 0, 0);
        bombDamage.DamageTicks.Add(aroundHits);
        Damages.Add(DamageVariety.Bomb, bombDamage);
    }

    private static List<DamageHit> GetHitsAroundZone(int minX, int minY, int maxX, int maxY)
    {
        var hits = new List<DamageHit>();
        for (var i = minX - 1; i <= maxX + 1; i++)
        {
            hits.Add(new DamageHit(i, minY - 1));
            hits.Add(new DamageHit(i, maxY + 1));
        }
        for (var i = minY; i <= maxY; i++)
        {
            hits.Add(new DamageHit(minX - 1, i));
            hits.Add(new DamageHit(maxX + 1, i));
        }
        return hits;
    }
}