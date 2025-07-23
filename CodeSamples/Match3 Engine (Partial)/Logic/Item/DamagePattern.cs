using System;

[Flags]
public enum DamagePattern
{
    None   = 0,
    Horizontal  = 1 << 0,
    Vertical = 1 << 1,
    Bomb = 1 << 2,
    ColorBomb = 1 << 3,
}