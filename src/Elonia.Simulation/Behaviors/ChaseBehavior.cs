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
            return StepChooser.ChooseStepToward(context, _target);
        }
    }
}
