using Elonia.Commands;

namespace Elonia.Game
{
    public readonly struct PickUpCommand : IPlayerCommand, ICommandReader<PickUpCommand>
    {
        private const char SYMBOL = 'e';

        public bool Execute(PlaySession play)
        {
            if (play.SubmitTurn(PickUpAction.DEFAULT))
            {
                return true;
            }

            play.Log("여기에는 주울 것이 없다.", ELogTone.Muted);
            return false;
        }

        public void Write(CommandTextWriter writer)
        {
            writer.Write(SYMBOL);
        }

        public bool TryRead(CommandTextReader reader, out PickUpCommand command)
        {
            command = default;
            return reader.TryRead(SYMBOL);
        }
    }
}
