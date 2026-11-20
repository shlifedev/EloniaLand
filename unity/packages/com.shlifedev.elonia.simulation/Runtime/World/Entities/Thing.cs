using System;

namespace Elonia.Simulation
{
    public sealed class Thing : Entity
    {
        public ThingType Type { get; }

        public int Count { get; }

        public Thing(int id, ThingType type, int x, int y, int count = 1)
            : base(id, x, y)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (count <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count), "A thing count must be positive.");
            }

            Type = type;
            Count = count;
        }
    }
}
