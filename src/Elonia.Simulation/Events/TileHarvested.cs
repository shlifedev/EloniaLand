namespace Elonia.Simulation
{
    public readonly struct TileHarvested
    {
        public Actor Actor { get; }

        public int X { get; }
        public int Y { get; }

        public TileType HarvestedTile { get; }
        public Item DroppedItem { get; }

        public TileHarvested(Actor actor, int x, int y, TileType harvestedTile, Item droppedItem)
        {
            Actor = actor;
            X = x;
            Y = y;
            HarvestedTile = harvestedTile;
            DroppedItem = droppedItem;
        }
    }
}
