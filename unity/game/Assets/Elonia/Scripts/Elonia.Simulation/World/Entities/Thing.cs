using System;

namespace Elonia.Simulation
{
    public sealed class Thing : Entity
    {
        public IThingDef Def { get; }

        public int Count { get; }

        public Thing(int id, IThingDef def, int x, int y, int count = 1)
            : base(id, x, y)
        {
            if (def == null)
            {
                throw new ArgumentNullException(nameof(def));
            }

            if (count <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count), "A thing count must be positive.");
            }

            Def = def;
            Count = count;
        }
    }
}
