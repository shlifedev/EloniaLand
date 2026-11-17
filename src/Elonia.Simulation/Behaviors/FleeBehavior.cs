using System;

namespace Elonia.Simulation
{
    public sealed class FleeBehavior : IBehavior
    {
        private readonly Actor _threat;
        private readonly long _squaredSenseRange;

        public Actor Threat => _threat;
        public int SenseRange { get; }

        public string Name => "FLEE";

        public FleeBehavior(Actor threat, int senseRange)
        {
            if (threat == null)
            {
                throw new ArgumentNullException(nameof(threat));
            }

            if (senseRange <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(senseRange), "A sense range must be positive.");
            }

            _threat = threat;
            SenseRange = senseRange;
            _squaredSenseRange = (long)senseRange * senseRange;
        }

        public bool CanRun(ActionContext context)
        {
            Actor actor = context.Actor;
            return _threat.IsAlive
                && GridMath.GetSquaredDistance(actor.X, actor.Y, _threat.X, _threat.Y) <= _squaredSenseRange;
        }

        public ActorAction ChooseAction(ActionContext context)
        {
            Actor actor = context.Actor;

            ActorAction bestAction = WaitAction.DEFAULT;
            long bestDistance = GridMath.GetSquaredDistance(actor.X, actor.Y, _threat.X, _threat.Y);
            for (int index = 0; index < MoveAction.DIRECTIONS.Count; index++)
            {
                MoveAction move = MoveAction.DIRECTIONS[index];
                int nextX = actor.X + move.DeltaX;
                int nextY = actor.Y + move.DeltaY;
                if (!context.Map.CanEnter(nextX, nextY))
                {
                    continue;
                }

                long distance = GridMath.GetSquaredDistance(nextX, nextY, _threat.X, _threat.Y);
                if (distance > bestDistance)
                {
                    bestDistance = distance;
                    bestAction = move;
                }
            }

            return bestAction;
        }
    }
}
