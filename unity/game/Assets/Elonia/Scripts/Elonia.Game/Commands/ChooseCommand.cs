using System;
using Elonia.Commands;

namespace Elonia.Game
{
    public readonly struct ChooseCommand : IPlayerCommand, ICommandReader<ChooseCommand>
    {
        public const int MAX_INDEX = 8;

        private const string SYMBOLS = "123456789";

        public int Index { get; }

        public ChooseCommand(int index)
        {
            if (index < 0 || index > MAX_INDEX)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            Index = index;
        }

        public bool Execute(PlaySession play)
        {
            play.Choose(Index);
            return false;
        }

        public void Write(CommandTextWriter writer)
        {
            writer.Write(SYMBOLS[Index]);
        }

        public bool TryRead(CommandTextReader reader, out ChooseCommand command)
        {
            bool bRead = reader.TryReadOneOf(SYMBOLS, out int index);
            command = bRead ? new ChooseCommand(index) : default;
            return bRead;
        }
    }
}
