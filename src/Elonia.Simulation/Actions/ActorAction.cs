namespace Elonia.Simulation
{
    public abstract class ActorAction
    {
        public const int DEFAULT_COST = 100;

        public abstract string Name { get; }

        public abstract ActionResult Execute(ActionContext context);
    }
}
