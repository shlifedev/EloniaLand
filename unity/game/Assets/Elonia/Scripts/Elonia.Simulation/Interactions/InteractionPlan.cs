using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using ZLinq;

namespace Elonia.Simulation
{
    public sealed class InteractionPlan
    {
        private readonly IInteractionRule[] _rules;

        private readonly List<Interaction> _items = new List<Interaction>();

        public IReadOnlyList<Interaction> Items { get; }

        public InteractionPlan(params IInteractionRule[] rules)
        {
            if (rules == null)
            {
                throw new ArgumentNullException(nameof(rules));
            }

            if (rules.AsValueEnumerable().Any(static rule => rule == null))
            {
                throw new ArgumentException("An interaction rule must not be null.", nameof(rules));
            }

            _rules = (IInteractionRule[])rules.Clone();
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

            for (int index = 0; index < _rules.Length; index++)
            {
                _rules[index].Collect(this, map, actor, x, y);
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

        public void Add(ActorAction action, int tileX, int tileY, int reach)
        {
            _items.Add(new Interaction(action, tileX, tileY, reach));
        }

        public void Add(ActorAction action, Actor targetActor, int reach)
        {
            _items.Add(new Interaction(action, targetActor, reach));
        }
    }
}
