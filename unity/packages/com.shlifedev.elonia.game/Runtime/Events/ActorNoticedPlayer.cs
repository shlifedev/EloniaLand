using Elonia.Simulation;

namespace Elonia.Game
{
    public readonly struct ActorNoticedPlayer
    {
        public Actor Actor { get; }
        public EAlertKind Kind { get; }

        public ActorNoticedPlayer(Actor actor, EAlertKind kind)
        {
            Actor = actor;
            Kind = kind;
        }
    }
}
