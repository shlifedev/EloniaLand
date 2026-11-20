using Elonia.Simulation;

namespace Elonia.Game
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
