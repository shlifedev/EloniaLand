using System;

namespace Elonia.Simulation
{
    public sealed class WanderBehavior : IBehavior
    {
        private readonly Random _random;

        public string Name => "WANDER";

        public WanderBehavior(Random random)
        {
            if (random == null)
            {
                throw new ArgumentNullException(nameof(random));
            }

            _random = random;
        }

        public bool CanRun(ActionContext context)
        {
            return true;
        }

        public ActorAction ChooseAction(ActionContext context)
        {
            Actor actor = context.Actor;
            MoveAction move = MoveAction.DIRECTIONS[_random.Next(MoveAction.DIRECTIONS.Count)];
            if (!context.Map.CanEnter(actor.X + move.DeltaX, actor.Y + move.DeltaY))
            {
                return WaitAction.DEFAULT;
            }

            return move;
        }
    }
}
