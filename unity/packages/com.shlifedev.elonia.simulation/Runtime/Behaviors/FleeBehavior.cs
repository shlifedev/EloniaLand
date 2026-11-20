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
            return StepChooser.ChooseStepAwayFrom(context, _threat);
        }
    }
}
