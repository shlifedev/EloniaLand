using System;
using Elonia.Commands;
using Elonia.Simulation;

namespace Elonia.Game
{
    public readonly struct MoveCommand : IPlayerCommand, ICommandReader<MoveCommand>
    {
        private const string SYMBOLS = "wsadyubn";

        public EDirection Direction { get; }

        public MoveCommand(EDirection direction)
        {
            Direction = direction;
        }

        public bool Execute(PlaySession play)
        {
            MoveAction move = GetMoveAction(Direction);
            Actor player = play.Session.Player;
            Interaction? bumpOrNull = play.Interactions.GetBumpOrNull(player.X + move.DeltaX, player.Y + move.DeltaY);
            if (bumpOrNull != null)
            {
                return play.SubmitTurn(bumpOrNull.Value.Action);
            }

            if (play.SubmitTurn(move))
            {
                return true;
            }

            play.Session.World.Events.Broadcast(new MoveBlocked(player, move.DeltaX, move.DeltaY));
            return false;
        }

        public void Write(CommandTextWriter writer)
        {
            writer.Write(SYMBOLS[(int)Direction]);
        }

        public bool TryRead(CommandTextReader reader, out MoveCommand command)
        {
            bool bRead = reader.TryReadOneOf(SYMBOLS, out int index);
            command = bRead ? new MoveCommand((EDirection)index) : default;
            return bRead;
        }

        private static MoveAction GetMoveAction(EDirection direction)
        {
            switch (direction)
            {
                case EDirection.Up:
                    return MoveAction.UP;
                case EDirection.Down:
                    return MoveAction.DOWN;
                case EDirection.Left:
                    return MoveAction.LEFT;
                case EDirection.Right:
                    return MoveAction.RIGHT;
                case EDirection.UpLeft:
                    return MoveAction.UP_LEFT;
                case EDirection.UpRight:
                    return MoveAction.UP_RIGHT;
                case EDirection.DownLeft:
                    return MoveAction.DOWN_LEFT;
                case EDirection.DownRight:
                    return MoveAction.DOWN_RIGHT;
                default:
                    throw new ArgumentOutOfRangeException(nameof(direction));
            }
        }
    }
}
