using System;

namespace Elonia.Simulation
{
    public sealed class TileHarvest
    {
        public TileType ResultTile { get; }

        public EItemType DropType { get; }
        public int DropAmount { get; }

        public TileHarvest(TileType resultTile, EItemType dropType, int dropAmount)
        {
            if (resultTile == null)
            {
                throw new ArgumentNullException(nameof(resultTile));
            }

            if (!resultTile.IsWalkable)
            {
                throw new ArgumentException("A harvested tile must turn into a walkable tile.", nameof(resultTile));
            }

            if (dropAmount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(dropAmount), "A harvest must drop a positive amount.");
            }

            ResultTile = resultTile;
            DropType = dropType;
            DropAmount = dropAmount;
        }
    }
}
