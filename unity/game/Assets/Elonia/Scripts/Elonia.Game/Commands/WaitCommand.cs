using Elonia.Commands;
using Elonia.Simulation;

namespace Elonia.Game
{
    public readonly struct WaitCommand : IPlayerCommand, ICommandReader<WaitCommand>
    {
        private const char SYMBOL = '.';

        public bool Execute(PlaySession play)
        {
            return play.SubmitTurn(WaitAction.DEFAULT);
        }

        public void Write(CommandTextWriter writer)
        {
            writer.Write(SYMBOL);
        }

        public bool TryRead(CommandTextReader reader, out WaitCommand command)
        {
            command = default;
            return reader.TryRead(SYMBOL);
        }
    }
}
