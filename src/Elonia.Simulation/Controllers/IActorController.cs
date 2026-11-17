namespace Elonia.Simulation
{
    public interface IActorController
    {
        ActorAction ChooseAction(ActionContext context);
    }
}
