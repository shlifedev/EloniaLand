using Elonia.Simulation;

namespace Elonia.Game
{
    public readonly struct ActorAttacked
    {
        public Actor Attacker { get; }
        public Actor Target { get; }

        public ActorAttacked(Actor attacker, Actor target)
        {
            Attacker = attacker;
            Target = target;
        }
    }
}
