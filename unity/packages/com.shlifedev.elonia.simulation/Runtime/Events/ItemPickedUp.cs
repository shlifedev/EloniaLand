namespace Elonia.Simulation
{
    public readonly struct ItemPickedUp
    {
        public Actor Actor { get; }
        public Thing Thing { get; }

        public EItemEffect Effect { get; }
        public int Amount { get; }

        public ItemPickedUp(Actor actor, Thing thing, EItemEffect effect, int amount)
        {
            Actor = actor;
            Thing = thing;
            Effect = effect;
            Amount = amount;
        }
    }
}
