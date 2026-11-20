using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Elonia.Simulation
{
    public sealed class InteractionPlan
    {
        private static readonly IInteractionRule[] RULES = new IInteractionRule[]
        {
            new AttackRule(),
            new HarvestRule(),
            new PickUpRule()
        };

        private readonly List<Interaction> _items = new List<Interaction>();

        public IReadOnlyList<Interaction> Items { get; }

        public InteractionPlan()
        {
            Items = new ReadOnlyCollection<Interaction>(_items);
        }

        public void Collect(Map map, Actor actor, int x, int y)
        {
            if (map == null)
            {
                throw new ArgumentNullException(nameof(map));
            }

            if (actor == null)
            {
                throw new ArgumentNullException(nameof(actor));
            }

            _items.Clear();
            if (!map.IsInside(x, y))
            {
                return;
            }

            for (int index = 0; index < RULES.Length; index++)
            {
                RULES[index].Collect(this, map, actor, x, y);
            }
        }

        public bool TryGetFirst(int reach, out Interaction interaction)
        {
            for (int index = 0; index < _items.Count; index++)
            {
                if (_items[index].Reach == reach)
                {
                    interaction = _items[index];
                    return true;
                }
            }

            interaction = default;
            return false;
        }

        public bool TryCollectBump(Map map, Actor actor, int x, int y, out Interaction interaction)
        {
            Collect(map, actor, x, y);
            return TryGetFirst(1, out interaction);
        }

        internal void Add(ActorAction action, int tileX, int tileY, int reach)
        {
            _items.Add(new Interaction(action, tileX, tileY, reach));
        }

        internal void Add(ActorAction action, Actor targetActor, int reach)
        {
            _items.Add(new Interaction(action, targetActor, reach));
        }
    }
}
