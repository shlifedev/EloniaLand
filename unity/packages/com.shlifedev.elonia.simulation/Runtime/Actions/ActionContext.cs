namespace Elonia.Simulation
{
    public readonly struct ActionContext
    {
        public Actor Actor { get; }

        public Map Map { get; }

        public World World { get; }

        internal ActionContext(Actor actor, Map map, World world)
        {
            Actor = actor;
            Map = map;
            World = world;
        }
    }
}
