namespace Elonia.Simulation
{
    public sealed class WaitAction : ActorAction
    {
        public static readonly WaitAction DEFAULT = new WaitAction();

        public override string Id => "wait";

        public override ActionResult Execute(ActionContext context)
        {
            return ActionResult.Executed(DEFAULT_COST);
        }
    }
}
