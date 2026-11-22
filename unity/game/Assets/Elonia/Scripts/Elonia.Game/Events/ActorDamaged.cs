using Elonia.Simulation;

namespace Elonia.Game
{
    public readonly struct ActorDamaged
    {
        public Actor Actor { get; }
        public int Damage { get; }
        public Actor? SourceOrNull { get; }

        public ActorDamaged(Actor actor, int damage, Actor? sourceOrNull)
        {
            Actor = actor;
            Damage = damage;
            SourceOrNull = sourceOrNull;
        }
    }
}
