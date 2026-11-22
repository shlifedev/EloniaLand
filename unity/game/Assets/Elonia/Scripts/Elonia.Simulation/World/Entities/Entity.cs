using System;

namespace Elonia.Simulation
{
    public abstract class Entity
    {
        public int ID { get; }

        public int X { get; private set; }
        public int Y { get; private set; }

        internal Map? MapOrNull { get; set; }

        private protected Entity(int id, int x, int y)
        {
            ID = id;
            X = x;
            Y = y;
        }

        public void MoveTo(int x, int y)
        {
            if (MapOrNull == null)
            {
                SetPosition(x, y);
                return;
            }

            if (this is Actor actor)
            {
                MapOrNull.MoveActor(actor, x, y);
                return;
            }

            throw new InvalidOperationException("Remove a thing from its map before repositioning it.");
        }

        internal void SetPosition(int x, int y)
        {
            X = x;
            Y = y;
        }
    }
}
