using System;
using Elonia.Commands;
using Elonia.Simulation;

namespace Elonia.Game
{
    public readonly struct InteractCommand : IPlayerCommand, ICommandReader<InteractCommand>
    {
        public int X { get; }
        public int Y { get; }
        public string ActionId { get; }

        public InteractCommand(int x, int y, string actionId)
        {
            if (!IsValidActionId(actionId))
            {
                throw new ArgumentException("An action ID needs one or more letters, digits or '_'.", nameof(actionId));
            }

            X = x;
            Y = y;
            ActionId = actionId;
        }

        public bool Execute(PlaySession play)
        {
            Interaction interaction;
            if (!play.Interactions.TryGetByActionId(X, Y, ActionId, out interaction) || !interaction.IsInReach(play.Session.Player))
            {
                return false;
            }

            return play.SubmitTurn(interaction.Action);
        }

        public void Write(CommandTextWriter writer)
        {
            writer.Write('[');
            writer.Write(X);
            writer.Write(',');
            writer.Write(Y);
            writer.Write(',');
            writer.Write(ActionId);
            writer.Write(']');
        }

        public bool TryRead(CommandTextReader reader, out InteractCommand command)
        {
            command = default;
            if (!reader.TryRead('[')
                || !reader.TryReadInt(out int x) || !reader.TryRead(',')
                || !reader.TryReadInt(out int y) || !reader.TryRead(',')
                || !reader.TryReadName(out string actionId) || !reader.TryRead(']'))
            {
                return false;
            }

            command = new InteractCommand(x, y, actionId);
            return true;
        }

        private static bool IsValidActionId(string? actionIdOrNull)
        {
            if (string.IsNullOrEmpty(actionIdOrNull))
            {
                return false;
            }

            for (int index = 0; index < actionIdOrNull.Length; index++)
            {
                if (!CommandTextReader.IsNameChar(actionIdOrNull[index]))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
