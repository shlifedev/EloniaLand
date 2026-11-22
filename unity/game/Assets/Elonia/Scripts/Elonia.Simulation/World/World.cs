using System;
using Elonia.Events;

namespace Elonia.Simulation
{
    public sealed class World
    {
        public long Tick { get; private set; }
        public int Seed { get; }

        public WorldRandom Random { get; }

        public EventBus Events { get; } = new EventBus();

        public Map CurrentMap { get; }

        public object? GameDataOrNull { get; }

        public World(Map currentMap, object? gameDataOrNull = null, int seed = 0)
            : this(currentMap, gameDataOrNull, seed, new WorldRandom(seed))
        {
        }

        private World(Map currentMap, object? gameDataOrNull, int seed, WorldRandom random)
        {
            if (currentMap == null)
            {
                throw new ArgumentNullException(nameof(currentMap));
            }

            Seed = seed;
            Random = random;
            CurrentMap = currentMap;
            GameDataOrNull = gameDataOrNull;
        }

        public static World Restore(Map map, object? gameDataOrNull, long tick, ulong randomState, int seed = 0)
        {
            if (tick < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(tick));
            }

            return new World(map, gameDataOrNull, seed, WorldRandom.FromState(randomState)) { Tick = tick };
        }

        internal void AdvanceTick()
        {
            Tick++;
        }
    }
}
