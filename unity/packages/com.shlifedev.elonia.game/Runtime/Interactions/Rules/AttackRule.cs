using Elonia.Simulation;

namespace Elonia.Game
{
    internal sealed class AttackRule : IInteractionRule
    {
        public void Collect(InteractionPlan plan, Map map, Actor actor, int x, int y)
        {
            Actor? targetOrNull = map.GetLivingActorAtOrNull(x, y);
            if (targetOrNull != null && targetOrNull != actor && targetOrNull.Row().IsHostile)
            {
                plan.Add(new AttackAction(targetOrNull), targetOrNull, AttackAction.REACH);
            }
        }
    }
}
