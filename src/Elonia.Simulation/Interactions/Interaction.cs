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
            Action = action;
            TargetActorOrNull = null;
            Reach = reach;
            _tileX = tileX;
            _tileY = tileY;
        }

        internal Interaction(ActorAction action, Actor targetActor, int reach)
        {
            Action = action;
            TargetActorOrNull = targetActor;
            Reach = reach;
            _tileX = targetActor.X;
            _tileY = targetActor.Y;
        }

        public bool IsInReach(Actor actor)
        {
            return GridMath.GetManhattanDistance(actor.X, actor.Y, TargetX, TargetY) == Reach;
        }
    }
}
