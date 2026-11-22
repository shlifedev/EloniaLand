using Elonia.Simulation;

namespace Elonia.Game
{
    public sealed class WanderBehavior : IBehavior
    {
        public string Name => "WANDER";

        public bool CanRun(ActionContext context)
        {
            return true;
        }

        public ActorAction ChooseAction(ActionContext context)
        {
            Actor actor = context.Actor;
            MoveAction move = MoveAction.GetDirection(context.World.Random.NextInt(GridMath.DIRECTIONS.Count));
            if (!context.Map.CanStep(actor.X, actor.Y, move.DeltaX, move.DeltaY))
            {
                return WaitAction.DEFAULT;
            }

            return move;
        }
    }
}
