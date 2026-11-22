using Elonia.Simulation;

namespace Elonia.Game
{
    internal static class StepChooser
    {
        public static ActorAction ChooseStepToward(ActionContext context, Actor other)
        {
            return ChooseStep(context, other, true);
        }

        public static ActorAction ChooseStepAwayFrom(ActionContext context, Actor other)
        {
            return ChooseStep(context, other, false);
        }

        private static ActorAction ChooseStep(ActionContext context, Actor other, bool bApproach)
        {
            Actor actor = context.Actor;
            ActorAction bestAction = WaitAction.DEFAULT;
            long bestDistance = GridMath.GetSquaredDistance(actor.X, actor.Y, other.X, other.Y);
            for (int index = 0; index < GridMath.DIRECTIONS.Count; index++)
            {
                MoveAction move = MoveAction.GetDirection(index);
                if (!context.Map.CanStep(actor.X, actor.Y, move.DeltaX, move.DeltaY))
                {
                    continue;
                }

                long distance = GridMath.GetSquaredDistance(actor.X + move.DeltaX, actor.Y + move.DeltaY, other.X, other.Y);
                bool bBetter = bApproach ? distance < bestDistance : distance > bestDistance;
                if (bBetter)
                {
                    bestDistance = distance;
                    bestAction = move;
                }
            }

            return bestAction;
        }
    }
}
