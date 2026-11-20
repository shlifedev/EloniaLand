using System;

namespace Elonia.Simulation
{
    public sealed class FloorType
    {
        public static readonly FloorType GROUND = new FloorType("Ground", true);

        public string Id { get; }
        public string Name { get; }

        public bool IsWalkable { get; }

        public FloorType(string name, bool bWalkable)
            : this(name, name, bWalkable)
        {
        }

        public FloorType(string id, string name, bool bWalkable)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentException("종류 ID가 필요합니다.", nameof(id));
            }

            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("A floor type needs a name.", nameof(name));
            }

            Id = id;
            Name = name;
            IsWalkable = bWalkable;
        }
    }
}
