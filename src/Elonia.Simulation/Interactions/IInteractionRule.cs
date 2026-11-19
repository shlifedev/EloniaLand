namespace Elonia.Simulation
{
    internal interface IInteractionRule
    {
        void Collect(InteractionPlan plan, Map map, Actor actor, int x, int y);
    }
}
