using System;

namespace Elonia.Simulation
{
    public sealed class WorldRandom
    {
        private const ulong MULTIPLIER = 6364136223846793005UL;
        private const ulong INCREMENT = (54UL << 1) | 1UL;

        private ulong _state;

        public ulong State => _state;

        public WorldRandom(int seed)
        {
            _state = INCREMENT + unchecked((uint)seed);
            NextUInt32();
        }

        private WorldRandom()
        {
        }

        internal static WorldRandom FromState(ulong state)
        {
            WorldRandom random = new WorldRandom();
            random._state = state;
            return random;
        }

        public int NextInt(int exclusiveMax)
        {
            if (exclusiveMax <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(exclusiveMax));
            }

            uint bound = (uint)exclusiveMax;
            uint threshold = unchecked(0U - bound) % bound;
            uint sample;
            do
            {
                sample = NextUInt32();
            }
            while (sample < threshold);

            return (int)(sample % bound);
        }

        private uint NextUInt32()
        {
            ulong previous = _state;
            _state = unchecked(previous * MULTIPLIER + INCREMENT);
            uint bits = unchecked((uint)(((previous >> 18) ^ previous) >> 27));
            int rotation = (int)(previous >> 59);
            return (bits >> rotation) | (bits << ((32 - rotation) & 31));
        }
    }
}
