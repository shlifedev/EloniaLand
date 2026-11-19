namespace Elonia.Simulation
{
    public abstract class Entity
    {
        public int ID { get; }

        public int X { get; private set; }
        public int Y { get; private set; }

        private protected Entity(int id, int x, int y)
        {
            ID = id;
            X = x;
            Y = y;
        }

        internal void MoveTo(int x, int y)
        {
            X = x;
            Y = y;
        }
    }
}
