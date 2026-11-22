using Elonia.Data;
using Elonia.Simulation;

namespace Elonia.Game
{
    public readonly struct PropHarvested
    {
        public Actor Actor { get; }

        public int X { get; }
        public int Y { get; }

        public PropRow HarvestedProp { get; }
        public Thing DroppedThing { get; }

        public PropHarvested(Actor actor, int x, int y, PropRow harvestedProp, Thing droppedThing)
        {
            Actor = actor;
            X = x;
            Y = y;
            HarvestedProp = harvestedProp;
            DroppedThing = droppedThing;
        }
    }
}
