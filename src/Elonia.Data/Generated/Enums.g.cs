#nullable enable

using System;

namespace Elonia.Data
{
    public enum EActorBehavior : byte
    {
        Player = 0,
        Wander = 1,
        Flee = 2,
        ChaseAttack = 3,
    }

    public enum EHouseStyle : byte
    {
        None = 0,
        Wood = 1,
        Stone = 2,
    }

    public enum EItemEffect : byte
    {
        None = 0,
        Heal = 1,
        Gold = 2,
    }
}
