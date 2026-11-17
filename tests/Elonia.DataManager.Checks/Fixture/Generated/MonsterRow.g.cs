#nullable enable

using System;
using System.Collections.Generic;
using MemoryPack;

namespace Elonia.Checks.Generated
{
    [MemoryPackable(SerializeLayout.Explicit)]
    public sealed partial class MonsterRow
    {
        [MemoryPackOrder(0)]
        public int Id { get; }

        [MemoryPackOrder(1)]
        public string Name { get; }

        [MemoryPackOrder(2)]
        public string Slug { get; }

        [MemoryPackOrder(3)]
        public bool HasDrop { get; }

        [MemoryPackOrder(4)]
        public string DropKey { get; }

        [MemoryPackIgnore]
        public ItemRef Drop => new ItemRef(HasDrop, DropKey);

        [MemoryPackOrder(5)]
        public int[] Level { get; }

        [MemoryPackOrder(6)]
        public int Hp { get; }

        [MemoryPackOrder(7)]
        public float Speed { get; }

        [MemoryPackConstructor]
        public MonsterRow(int id, string name, string slug, bool hasDrop, string dropKey, int[] level, int hp, float speed)
        {
            Id = id;
            Name = name;
            Slug = slug;
            HasDrop = hasDrop;
            DropKey = dropKey;
            Level = level;
            Hp = hp;
            Speed = speed;
        }
    }
}
