namespace Elonia.Simulation
{
    public readonly struct ActorDied
    {
        public Actor Actor { get; }

        public ActorDied(Actor actor)
        {
            Actor = actor;
        }
    }
}
