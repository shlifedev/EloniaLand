using Elonia.Simulation;

namespace Elonia.Game
{
    internal sealed class AttackRule : IInteractionRule
    {
        public void Collect(InteractionPlan plan, Map map, Actor actor, int x, int y)
        {
            if (InteractionTargets.TryGetOtherLivingActor(map, actor, x, y, out Actor? targetOrNull) && targetOrNull.Row().IsHostile)
            {
                plan.Add(new AttackAction(targetOrNull), targetOrNull, AttackAction.REACH);
            }
        }
    }
}
