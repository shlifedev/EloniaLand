using Elonia.Commands;

namespace Elonia.Game
{
    public static class PlayerCommandText
    {
        public static CommandRecord<PlaySession> Parse(string text)
        {
            return new CommandTextFormat<PlaySession>()
                .Add<MoveCommand>()
                .Add<WaitCommand>()
                .Add<PickUpCommand>()
                .Add<ChooseCommand>()
                .Add<InteractCommand>()
                .Parse(text);
        }
    }
}
