namespace Elonia.Simulation
{
    public readonly struct PropHarvested
    {
        public Actor Actor { get; }

        public int X { get; }
        public int Y { get; }

        public PropType HarvestedProp { get; }
        public Thing DroppedThing { get; }

        public PropHarvested(Actor actor, int x, int y, PropType harvestedProp, Thing droppedThing)
        {
            Actor = actor;
            X = x;
            Y = y;
            HarvestedProp = harvestedProp;
            DroppedThing = droppedThing;
        }
    }
}
