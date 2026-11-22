namespace Elonia.Commands
{
    public interface ICommandReader<TCommand> where TCommand : struct
    {
        bool TryRead(CommandTextReader reader, out TCommand command);
    }
}
