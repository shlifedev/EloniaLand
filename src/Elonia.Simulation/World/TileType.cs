using System;

namespace Elonia.Simulation
{
    public sealed class TileType
    {
        public static readonly TileType GROUND = new TileType("Ground", true);

        public string Name { get; }
        public bool IsWalkable { get; }

        public TileHarvest? HarvestOrNull { get; }

        public TileType(string name, bool bWalkable, TileHarvest? harvestOrNull = null)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("A tile type needs a name.", nameof(name));
            }

            if (bWalkable && harvestOrNull != null)
            {
                throw new ArgumentException("A harvestable tile must not be walkable.", nameof(harvestOrNull));
            }

            Name = name;
            IsWalkable = bWalkable;
            HarvestOrNull = harvestOrNull;
        }

        internal void AddInteractions(InteractionPlan plan, int x, int y)
        {
            if (HarvestOrNull != null)
            {
                plan.Add(new HarvestAction(x, y), x, y, HarvestAction.REACH);
            }
        }
    }
}
