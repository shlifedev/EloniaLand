using Elonia.Simulation;

namespace Elonia.Game
{
    internal sealed class HarvestRule : IInteractionRule
    {
        public void Collect(InteractionPlan plan, Map map, Actor actor, int x, int y)
        {
            IPropDef? propOrNull = map.GetPropOrNull(x, y);
            if (propOrNull != null && propOrNull.Row().DropThing.HasValue)
            {
                plan.Add(new HarvestAction(x, y), x, y, HarvestAction.REACH);
            }
        }
    }
}
