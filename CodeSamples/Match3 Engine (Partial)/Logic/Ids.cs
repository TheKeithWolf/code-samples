using System;
using System.Collections.Generic;

public static class Ids
{
    private static readonly Dictionary<Type, int> NameAndId = new();
    
    public static int GetItemId(Type type)
    {
        if (!NameAndId.TryGetValue(type, out var id))
        {
            NameAndId.Add(type, 0);
        }
        
        NameAndId[type] = id + 1;
        
        return id;
    }
    
    public static void ResetAllIds()
    {
        NameAndId.Clear();
    }
}