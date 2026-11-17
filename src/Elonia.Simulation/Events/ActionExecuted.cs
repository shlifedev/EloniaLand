namespace Elonia.Simulation
{
    public readonly struct ActionExecuted
    {
        public Actor Actor { get; }
        public ActorAction Action { get; }
        public int Cost { get; }

        public long Tick { get; }

        public ActionExecuted(Actor actor, ActorAction action, int cost, long tick)
        {
            Actor = actor;
            Action = action;
            Cost = cost;
            Tick = tick;
        }
    }
}
