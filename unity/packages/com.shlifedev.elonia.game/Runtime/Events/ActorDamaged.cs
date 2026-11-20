using Elonia.Simulation;

namespace Elonia.Game
{
    public readonly struct ActorDamaged
    {
        public Actor Actor { get; }
        public int Damage { get; }

        public ActorDamaged(Actor actor, int damage)
        {
            Actor = actor;
            Damage = damage;
        }
    }
}
