using Elonia.Simulation;

namespace Elonia.Game
{
    public readonly struct MoveBlocked
    {
        public Actor Actor { get; }
        public int DeltaX { get; }
        public int DeltaY { get; }

        public MoveBlocked(Actor actor, int deltaX, int deltaY)
        {
            Actor = actor;
            DeltaX = deltaX;
            DeltaY = deltaY;
        }
    }
}
