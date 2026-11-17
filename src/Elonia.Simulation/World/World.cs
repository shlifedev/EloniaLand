using System;
using Elonia.Events;

namespace Elonia.Simulation
{
    public sealed class World
    {
        public long Tick { get; private set; }

        public EventBus Events { get; } = new EventBus();

        public Map CurrentMap { get; }

        public World(Map currentMap)
        {
            if (currentMap == null)
            {
                throw new ArgumentNullException(nameof(currentMap));
            }

            CurrentMap = currentMap;
        }

        internal void AdvanceTick()
        {
            Tick++;
            for (int index = 0; index < CurrentMap.Actors.Count; index++)
            {
                Actor actor = CurrentMap.Actors[index];
                if (actor.IsAlive)
                {
                    actor.AddEnergy(actor.Type.Speed);
                }
            }
        }
    }
}
