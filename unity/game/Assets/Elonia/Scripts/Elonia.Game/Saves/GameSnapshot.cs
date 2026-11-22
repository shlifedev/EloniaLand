using System;
using System.Collections.Generic;
using MemoryPack;

namespace Elonia.Game
{
    [MemoryPackable(GenerateType.VersionTolerant)]
    public sealed partial class GameSnapshot
    {
        [MemoryPackOrder(0)] public int Seed { get; set; }
        [MemoryPackOrder(1)] public long Tick { get; set; }
        [MemoryPackOrder(2)] public ulong RandomState { get; set; }
        [MemoryPackOrder(3)] public ActorSnapshot Player { get; set; } = new ActorSnapshot();
        [MemoryPackOrder(4)] public MapSnapshot Map { get; set; } = new MapSnapshot();
        [MemoryPackOrder(5)] public Dictionary<string, int> Flags { get; set; } = new Dictionary<string, int>();
        [MemoryPackOrder(6)] public int DialogueSpeakerID { get; set; }
        [MemoryPackOrder(7)] public string? DialogueLineIdOrNull { get; set; }
        [MemoryPackOrder(8)] public bool HasUsedCheats { get; set; }

        [MemoryPackIgnore] public int PlayerHp => Player.Hp;
    }

    [MemoryPackable(GenerateType.VersionTolerant)]
    public sealed partial class MapSnapshot
    {
        [MemoryPackOrder(0)] public string Id { get; set; } = "";
        [MemoryPackOrder(1)] public int Width { get; set; }
        [MemoryPackOrder(2)] public int Height { get; set; }
        [MemoryPackOrder(3)] public string[] LegendIds { get; set; } = Array.Empty<string>();
        [MemoryPackOrder(4)] public string[] FloorIds { get; set; } = Array.Empty<string>();
        [MemoryPackOrder(5)] public string?[] PropIds { get; set; } = Array.Empty<string?>();
        [MemoryPackOrder(6)] public ActorSnapshot[] Npcs { get; set; } = Array.Empty<ActorSnapshot>();
        [MemoryPackOrder(7)] public int PlayerIndex { get; set; }
        [MemoryPackOrder(8)] public ThingSnapshot[] Things { get; set; } = Array.Empty<ThingSnapshot>();
        [MemoryPackOrder(9)] public int NextEntityID { get; set; }
        [MemoryPackOrder(10)] public int TurnIndex { get; set; }
    }

    [MemoryPackable(GenerateType.VersionTolerant)]
    public sealed partial class ActorSnapshot
    {
        [MemoryPackOrder(0)] public int ID { get; set; }
        [MemoryPackOrder(1)] public string RowId { get; set; } = "";
        [MemoryPackOrder(2)] public int X { get; set; }
        [MemoryPackOrder(3)] public int Y { get; set; }
        [MemoryPackOrder(4)] public int Hp { get; set; }
        [MemoryPackOrder(5)] public int Gold { get; set; }
        [MemoryPackOrder(6)] public long ActionCount { get; set; }
        [MemoryPackOrder(7)] public long Energy { get; set; }
        [MemoryPackOrder(8)] public bool IsInvincible { get; set; }
        [MemoryPackOrder(9)] public StackSnapshot[] Inventory { get; set; } = Array.Empty<StackSnapshot>();
        [MemoryPackOrder(10)] public ModifierSnapshot[] Modifiers { get; set; } = Array.Empty<ModifierSnapshot>();
        [MemoryPackOrder(11)] public string? BehaviorNameOrNull { get; set; }
        [MemoryPackOrder(12)] public bool WasAlert { get; set; }
    }

    [MemoryPackable(GenerateType.VersionTolerant)]
    public sealed partial class StackSnapshot
    {
        [MemoryPackOrder(0)] public string RowId { get; set; } = "";
        [MemoryPackOrder(1)] public int Count { get; set; }
    }

    [MemoryPackable(GenerateType.VersionTolerant)]
    public sealed partial class ModifierSnapshot
    {
        [MemoryPackOrder(0)] public int LegacySourceIndex { get; set; }
        [MemoryPackOrder(1)] public EStat Stat { get; set; }
        [MemoryPackOrder(2)] public int Amount { get; set; }
        [MemoryPackOrder(3)] public string? SourceOrNull { get; set; }
    }

    [MemoryPackable(GenerateType.VersionTolerant)]
    public sealed partial class ThingSnapshot
    {
        [MemoryPackOrder(0)] public int ID { get; set; }
        [MemoryPackOrder(1)] public string RowId { get; set; } = "";
        [MemoryPackOrder(2)] public int X { get; set; }
        [MemoryPackOrder(3)] public int Y { get; set; }
        [MemoryPackOrder(4)] public int Count { get; set; }
    }
}
