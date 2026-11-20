namespace Elonia.Simulation
{
    public readonly struct ActorAttacked
    {
        public Actor Attacker { get; }
        public Actor Target { get; }
        public int Damage { get; }

        public ActorAttacked(Actor attacker, Actor target, int damage)
        {
            Attacker = attacker;
            Target = target;
            Damage = damage;
        }
    }
}
