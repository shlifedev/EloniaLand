namespace Elonia.Simulation
{
    public readonly struct StepTriggered
    {
        public Actor Actor { get; }
        public Thing Thing { get; }

        public StepTriggered(Actor actor, Thing thing)
        {
            Actor = actor;
            Thing = thing;
        }
    }
}
