using System;
using System.Collections.Generic;
using Elonia.Simulation;

namespace Elonia.Game
{
    internal sealed class TileInteractions
    {
        private readonly GameSession _session;

        private readonly InteractionPlan _plan = new InteractionPlan(new AttackRule(), new TalkRule(), new HarvestRule(), new PickUpRule());

        public TileInteractions(GameSession session)
        {
            _session = session;
        }

        public void CollectMenuChoices(int x, int y, List<MenuChoice> choices)
        {
            choices.Clear();
            Map map = _session.Map;
            if (_session.Runner.IsGameOver || !map.IsInside(x, y))
            {
                return;
            }

            Actor player = _session.Player;
            _plan.Collect(map, player, x, y);
            for (int index = 0; index < _plan.Items.Count; index++)
            {
                Interaction interaction = _plan.Items[index];
                choices.Add(new MenuChoice(GameText.GetInteractionLabel(interaction, map), interaction));
            }

            if (x != player.X || y != player.Y)
            {
                choices.Add(new MenuChoice("여기로 걸어가기", null));
            }
        }

        public Interaction? GetBumpOrNull(int x, int y)
        {
            Interaction first;
            if (!_plan.TryCollectBump(_session.Map, _session.Player, x, y, out first) || first.Action is TalkAction)
            {
                return null;
            }

            return first;
        }

        public bool TryGetByActionId(int x, int y, string actionId, out Interaction interaction)
        {
            _plan.Collect(_session.Map, _session.Player, x, y);
            for (int index = 0; index < _plan.Items.Count; index++)
            {
                Interaction candidate = _plan.Items[index];
                if (string.Equals(candidate.Action.Id, actionId, StringComparison.Ordinal))
                {
                    interaction = candidate;
                    return true;
                }
            }

            interaction = default;
            return false;
        }
    }
}
