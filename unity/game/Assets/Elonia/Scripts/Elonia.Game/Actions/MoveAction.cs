using System;
using System.Collections.Generic;
using Elonia.Simulation;

namespace Elonia.Game
{
    public sealed class MoveAction : ActorAction
    {
        public static readonly MoveAction UP = new MoveAction(0, -1);
        public static readonly MoveAction DOWN = new MoveAction(0, 1);
        public static readonly MoveAction LEFT = new MoveAction(-1, 0);
        public static readonly MoveAction RIGHT = new MoveAction(1, 0);
        public static readonly MoveAction UP_LEFT = new MoveAction(-1, -1);
        public static readonly MoveAction UP_RIGHT = new MoveAction(1, -1);
        public static readonly MoveAction DOWN_LEFT = new MoveAction(-1, 1);
        public static readonly MoveAction DOWN_RIGHT = new MoveAction(1, 1);

        public override string Id => "move";
        public int DeltaX { get; }
        public int DeltaY { get; }

        public MoveAction(int deltaX, int deltaY)
        {
            if (GridMath.GetChebyshevDistance(0, 0, deltaX, deltaY) != 1)
            {
                throw new ArgumentException("A move must be one step to a neighbouring cell.");
            }

            DeltaX = deltaX;
            DeltaY = deltaY;
        }

        internal static MoveAction GetDirection(int index)
        {
            return GridMath.DIRECTIONS[index] switch
            {
                (0, -1) => UP,
                (0, 1) => DOWN,
                (-1, 0) => LEFT,
                (1, 0) => RIGHT,
                (-1, -1) => UP_LEFT,
                (1, -1) => UP_RIGHT,
                (-1, 1) => DOWN_LEFT,
                (1, 1) => DOWN_RIGHT,
                _ => throw new InvalidOperationException("이동할 수 없는 방향입니다.")
            };
        }

        public override ActionResult Execute(ActionContext context)
        {
            Actor actor = context.Actor;
            if (!context.Map.CanStep(actor.X, actor.Y, DeltaX, DeltaY))
            {
                return ActionResult.REJECTED;
            }

            context.Map.MoveActor(actor, actor.X + DeltaX, actor.Y + DeltaY);
            TriggerSteppedThings(context, actor);
            return ActionResult.Executed(DEFAULT_COST);
        }

        private static void TriggerSteppedThings(ActionContext context, Actor actor)
        {
            IReadOnlyList<Thing> things = context.Map.GetCell(actor.X, actor.Y).Things;
            for (int index = 0; index < things.Count; index++)
            {
                Thing thing = things[index];
                int damage = thing.Row().StepDamage;
                if (damage == 0)
                {
                    continue;
                }

                context.World.Events.Broadcast(new StepTriggered(actor, thing));
                DamageEffect.Apply(context, actor, damage, null);
                if (!actor.IsAlive)
                {
                    break;
                }
            }
        }
    }
}
