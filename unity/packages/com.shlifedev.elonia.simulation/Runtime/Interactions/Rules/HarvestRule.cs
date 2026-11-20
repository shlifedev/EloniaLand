namespace Elonia.Simulation
{
    internal sealed class HarvestRule : IInteractionRule
    {
        public void Collect(InteractionPlan plan, Map map, Actor actor, int x, int y)
        {
            PropType? propOrNull = map.GetPropOrNull(x, y);
            if (propOrNull != null && propOrNull.HarvestOrNull != null)
            {
                plan.Add(new HarvestAction(x, y), x, y, HarvestAction.REACH);
            }
        }
    }
}
