using System;
using System.Collections.Generic;

namespace Elonia.Simulation
{
    public sealed class MoveAction : ActorAction
    {
        public static readonly MoveAction UP = new MoveAction(0, -1);
        public static readonly MoveAction DOWN = new MoveAction(0, 1);
        public static readonly MoveAction LEFT = new MoveAction(-1, 0);
        public static readonly MoveAction RIGHT = new MoveAction(1, 0);

        public static readonly IReadOnlyList<MoveAction> DIRECTIONS = new MoveAction[] { UP, DOWN, LEFT, RIGHT };

        public override string Name => "MOVE";
        public int DeltaX { get; }
        public int DeltaY { get; }

        public MoveAction(int deltaX, int deltaY)
        {
            if (Math.Abs(deltaX) + Math.Abs(deltaY) != 1)
            {
                throw new ArgumentException("A move must be one orthogonal step.");
            }

            DeltaX = deltaX;
            DeltaY = deltaY;
        }

        public override ActionResult Execute(ActionContext context)
        {
            Actor actor = context.Actor;
            int nextX = actor.X + DeltaX;
            int nextY = actor.Y + DeltaY;
            if (!context.Map.CanEnter(nextX, nextY))
            {
                return ActionResult.REJECTED;
            }

            actor.MoveTo(nextX, nextY);
            return ActionResult.Executed(DEFAULT_COST);
        }
    }
}
