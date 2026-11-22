namespace Elonia.Commands
{
    public interface ICommand<in TContext>
    {
        bool Execute(TContext context);

        void Write(CommandTextWriter writer);
    }
}
