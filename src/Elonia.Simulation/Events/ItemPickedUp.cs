namespace Elonia.Simulation
{
    public readonly struct ItemPickedUp
    {
        public Actor Actor { get; }
        public Item Item { get; }

        public ItemPickedUp(Actor actor, Item item)
        {
            Actor = actor;
            Item = item;
        }
    }
}
