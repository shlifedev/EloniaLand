using System;

namespace Elonia.Simulation
{
    public sealed class PropType
    {
        public string Id { get; }
        public string Name { get; }

        public bool IsWalkable { get; }

        public PropHarvest? HarvestOrNull { get; }

        public PropType(string name, bool bWalkable, PropHarvest? harvestOrNull = null)
            : this(name, name, bWalkable, harvestOrNull)
        {
        }

        public PropType(string id, string name, bool bWalkable, PropHarvest? harvestOrNull = null)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentException("종류 ID가 필요합니다.", nameof(id));
            }

            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("A prop type needs a name.", nameof(name));
            }

            if (bWalkable && harvestOrNull != null)
            {
                throw new ArgumentException("A harvestable prop must not be walkable.", nameof(harvestOrNull));
            }

            Id = id;
            Name = name;
            IsWalkable = bWalkable;
            HarvestOrNull = harvestOrNull;
        }
    }
}
