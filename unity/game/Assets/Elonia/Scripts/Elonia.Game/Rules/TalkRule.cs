using Elonia.Simulation;

namespace Elonia.Game
{
    internal sealed class TalkRule : IInteractionRule
    {
        public void Collect(InteractionPlan plan, Map map, Actor actor, int x, int y)
        {
            if (InteractionTargets.TryGetOtherLivingActor(map, actor, x, y, out Actor? targetOrNull) && Dialogues.HasDialogue(targetOrNull.Row()))
            {
                plan.Add(new TalkAction(targetOrNull), targetOrNull, TalkAction.REACH);
            }
        }
    }
}
