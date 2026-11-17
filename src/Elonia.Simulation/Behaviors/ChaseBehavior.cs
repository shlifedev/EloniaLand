using System;

namespace Elonia.Simulation
{
    public sealed class ChaseBehavior : IBehavior
    {
        private readonly Actor _target;
        private readonly long _squaredSenseRange;

        public Actor Target => _target;
        public int SenseRange { get; }

        public string Name => "CHASE";

        public ChaseBehavior(Actor target, int senseRange)
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            if (senseRange <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(senseRange), "A sense range must be positive.");
            }

            _target = target;
            SenseRange = senseRange;
            _squaredSenseRange = (long)senseRange * senseRange;
        }

        public bool CanRun(ActionContext context)
        {
            Actor actor = context.Actor;
            return _target.IsAlive
                && GridMath.GetSquaredDistance(actor.X, actor.Y, _target.X, _target.Y) <= _squaredSenseRange;
        }

        public ActorAction ChooseAction(ActionContext context)
        {
            Actor actor = context.Actor;

            ActorAction bestAction = WaitAction.DEFAULT;
            long bestDistance = GridMath.GetSquaredDistance(actor.X, actor.Y, _target.X, _target.Y);
            for (int index = 0; index < MoveAction.DIRECTIONS.Count; index++)
            {
                MoveAction move = MoveAction.DIRECTIONS[index];
                int nextX = actor.X + move.DeltaX;
                int nextY = actor.Y + move.DeltaY;
                if (!context.Map.CanEnter(nextX, nextY))
                {
                    continue;
                }

                long distance = GridMath.GetSquaredDistance(nextX, nextY, _target.X, _target.Y);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestAction = move;
                }
            }

            return bestAction;
        }
    }
}
