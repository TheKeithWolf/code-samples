using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CustomLogDll;
using Unity.VisualScripting;
using UnityEngine;

public class LevelSpawnItemsSystem
{
    private static readonly Dictionary<Vector2Int, List<Vector2Int>> SizeToShape = new();
    
    public void SpawnFromConfig(Level level) { }

    private int _debugIdStop;
    
    // TODO Do proper spawn config, probably as Dto so we can also use it for level reload etc.
    private List<SpawnConfig> _spawnConfigs = new()
    {
        new SpawnConfig(typeof(ChainItemModifier), 1, new Vector2Int(2, 2)),
        new SpawnConfig(typeof(SimpleMatchable), int.MaxValue),
    };
    
    public void SpawnNewItems(Level level)
    {
        level.State = level.State.Clear(LevelState.SpawnedNewItems);
        
        var tickId = LevelSystem.Instance.TickId;

        var loopBreak = 1000;
        var maxSize = int.MaxValue;
        while (loopBreak-- > 0)
        {         
            var spawnFromConfig = GetSpawnConfig(maxSize);
            if (spawnFromConfig != null)
            {
                if (TryFindSpawnerForConfig(level, spawnFromConfig, out var spawner))
                {
                    Item item = null;
                    // TODO Add chance to spawn:
                    
                    switch (spawnFromConfig.ItemSpawnConfig)
                    {
                        case var type when type == typeof(ChainItemModifier):
                            var shape = GetShapeFromSize(spawnFromConfig.Size);
                            item = new Item(level, shape);
                            item.Modifiers.AddModifier(new ContainerItemModifier());
                            break;
                        
                        case var type when type == typeof(SimpleMatchable):
                            item = new Item();
                            item.Modifiers.AddModifier(new SimpleMatchable((MatchPatternType)(1 << LevelSystem.XorShiftRandomController.Next(0, 4)), DamagePattern.None));
                            break;
                
                        default:
                            this.LogError($"Unknown ItemSpawnConfig type: {spawnFromConfig.ItemSpawnConfig}");
                            break;
                    }

                    if (item == null)
                    {
                        this.LogError($"Unknown ItemSpawnConfig: {spawnFromConfig}");
                        break;
                    }
                    
                    if (--spawnFromConfig.Count <= 0)
                    {
                        _spawnConfigs.Remove(spawnFromConfig);
                    }

                    level.AddItem(item);
                    item.Navigation.MoveItemFromPosToCell(spawner.Position + Vector2Int.up, spawner, tickId);
            
                    level.State = level.State.Set(LevelState.SpawnedNewItems);
                }
                else
                {
                    maxSize = spawnFromConfig.Size.x - 1;
                    if (maxSize < 1)
                    {
                        break;
                    }
                }
            }
            else
            {
                break;
            }
        }
        
        this.Log($"{MethodBase.GetCurrentMethod()!.Name}");
    }

    public static bool TryFindSpawnerForConfig(Level level, SpawnConfig config, out Cell spawner)
    {
        if (level.SpawnersGroups.TryGetValue(config.Size.x, out var spawnerGroups))
        {
            // TODO Add random:
            var spawnerGroup = spawnerGroups.FirstOrDefault(s => s.IsNotBusy);
            if (spawnerGroup != null)
            {
                spawner = spawnerGroup.Spawners.First();
                return true;
            }
        }

        spawner = null;
        return false;
    }

    public SpawnConfig GetSpawnConfig(int maxSize = int.MaxValue)
    {
        // TODO Add more logic
        return _spawnConfigs.FirstOrDefault(c => c.Size.x <= maxSize);
    }

    public static List<Vector2Int> GetShapeFromSize(Vector2Int size)
    {
        if (!SizeToShape.TryGetValue(size, out var shape))
        {
            shape = new List<Vector2Int>();
            for (var y = 0; y < size.y ; y++)
            {
                for (var x = size.x - 1; x >= 0 ; x--)
                {
                    shape.Add(new Vector2Int(x, y));
                }
            }
            SizeToShape.Add(size, shape);
        }
        
        return shape;
    }
}

public class SpawnConfig
{
    public Type ItemSpawnConfig { get; set; }
    public int Count { get; set; }
    public Vector2Int Size { get; set; }

    public SpawnConfig(Type itemSpawnConfig, int count)
    {
        ItemSpawnConfig = itemSpawnConfig;
        Count = count;
        Size = Vector2Int.one;
    }
    
    public SpawnConfig(Type itemSpawnConfig, int count, Vector2Int size)
    {
        ItemSpawnConfig = itemSpawnConfig;
        Count = count;
        Size = size;
    }

    public override string ToString()
    {
        return $"ItemSpawnConfig: {ItemSpawnConfig}, Count: {Count}, Size: {Size}";
    }
}