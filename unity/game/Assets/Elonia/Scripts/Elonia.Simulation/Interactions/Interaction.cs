using System;

namespace Elonia.Simulation
{
    public readonly struct Interaction
    {
        private readonly int _tileX;
        private readonly int _tileY;

        public ActorAction Action { get; }

        public Actor? TargetActorOrNull { get; }

        public int Reach { get; }

        public int TargetX => TargetActorOrNull != null ? TargetActorOrNull.X : _tileX;
        public int TargetY => TargetActorOrNull != null ? TargetActorOrNull.Y : _tileY;

        internal Interaction(ActorAction action, int tileX, int tileY, int reach)
        {
            if (reach < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(reach));
            }

            Action = action ?? throw new ArgumentNullException(nameof(action));
            TargetActorOrNull = null;
            Reach = reach;
            _tileX = tileX;
            _tileY = tileY;
        }

        internal Interaction(ActorAction action, Actor targetActor, int reach)
            : this(action, 0, 0, reach)
        {
            TargetActorOrNull = targetActor ?? throw new ArgumentNullException(nameof(targetActor));
        }

        public bool IsInReach(Actor actor)
        {
            return GridMath.IsInReach(actor.X, actor.Y, TargetX, TargetY, Reach);
        }
    }
}
