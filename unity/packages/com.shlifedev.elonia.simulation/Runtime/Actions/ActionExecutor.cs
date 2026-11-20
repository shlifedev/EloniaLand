using System;

namespace Elonia.Simulation
{
    internal static class ActionExecutor
    {
        public static ActionResult Execute(ActorAction action, ActionContext context)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            ActionResult result = action.Execute(context);
            if (result.IsExecuted)
            {
                context.Actor.RecordAction(result.Cost);
                context.World.Events.Broadcast(new ActionExecuted(context.Actor, action, result.Cost, context.World.Tick));
            }

            return result;
        }
    }
}
