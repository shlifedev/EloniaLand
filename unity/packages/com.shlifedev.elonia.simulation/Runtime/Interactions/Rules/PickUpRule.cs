namespace Elonia.Simulation
{
    internal sealed class PickUpRule : IInteractionRule
    {
        public void Collect(InteractionPlan plan, Map map, Actor actor, int x, int y)
        {
            if (map.GetItemAtOrNull(x, y) != null)
            {
                plan.Add(PickUpAction.DEFAULT, x, y, PickUpAction.REACH);
            }
        }
    }
}
