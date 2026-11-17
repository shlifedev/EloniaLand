using System;

namespace Elonia.Simulation
{
    public sealed class TileType
    {
        public static readonly TileType GROUND = new TileType("Ground", true);

        public string Name { get; }
        public bool IsWalkable { get; }

        public TileType(string name, bool bWalkable)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("A tile type needs a name.", nameof(name));
            }

            Name = name;
            IsWalkable = bWalkable;
        }
    }
}
