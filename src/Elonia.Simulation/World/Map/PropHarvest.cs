using System;

namespace Elonia.Simulation
{
    public sealed class PropHarvest
    {
        public ThingType DropType { get; }
        public int DropCount { get; }

        public PropHarvest(ThingType dropType, int dropCount)
        {
            if (dropType == null)
            {
                throw new ArgumentNullException(nameof(dropType));
            }

            if (dropCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(dropCount), "A harvest must drop a positive count.");
            }

            DropType = dropType;
            DropCount = dropCount;
        }
    }
}
