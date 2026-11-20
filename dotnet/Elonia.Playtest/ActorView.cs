using Elonia.Simulation;

namespace Elonia.Playtest;

internal sealed class ActorView
{
    public float X { get; set; }
    public float Y { get; set; }
    public bool IsFacingLeft { get; set; }

    public float FlashTime { get; set; }

    public float LungeTime { get; set; }
    public float LungeX { get; set; }
    public float LungeY { get; set; }
    public float LungeDistance { get; set; }

    public float DeadTime { get; set; }

    public ActorView(Actor actor)
    {
        X = actor.X;
        Y = actor.Y;
    }
}
