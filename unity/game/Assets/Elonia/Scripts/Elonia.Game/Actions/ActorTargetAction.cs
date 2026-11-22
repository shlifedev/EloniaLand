using System;
using Elonia.Simulation;

namespace Elonia.Game
{
    public abstract class ActorTargetAction : ActorAction
    {
        public Actor Target { get; }

        public int Reach { get; }

        protected ActorTargetAction(Actor target, int reach)
        {
            Target = target ?? throw new ArgumentNullException(nameof(target));
            Reach = reach;
        }

        public bool CanReachTarget(Actor actor)
        {
            return Target != actor
                && Target.IsAlive
                && GridMath.IsInReach(actor.X, actor.Y, Target.X, Target.Y, Reach);
        }
    }
}
