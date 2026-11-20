namespace Elonia.Simulation
{
    public interface IInteractionRule
    {
        void Collect(InteractionPlan plan, Map map, Actor actor, int x, int y);
    }
}
