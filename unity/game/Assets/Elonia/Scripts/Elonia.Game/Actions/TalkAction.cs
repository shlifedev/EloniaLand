using Elonia.Simulation;

namespace Elonia.Game
{
    public sealed class TalkAction : ActorTargetAction
    {
        public const int REACH = 1;

        public override string Id => "talk";

        public TalkAction(Actor target)
            : base(target, REACH)
        {
        }

        public override ActionResult Execute(ActionContext context)
        {
            if (!CanReachTarget(context.Actor))
            {
                return ActionResult.REJECTED;
            }

            return ActionResult.Executed(DEFAULT_COST);
        }
    }
}
