namespace Elonia.Simulation
{
    public interface IBehavior
    {
        string Name { get; }

        bool CanRun(ActionContext context);

        ActorAction ChooseAction(ActionContext context);
    }
}
