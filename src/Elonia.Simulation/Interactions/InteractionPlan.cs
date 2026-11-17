using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Elonia.Simulation
{
    public sealed class InteractionPlan
    {
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

            Actor? targetOrNull = map.GetLivingActorAtOrNull(x, y);
            if (targetOrNull != null)
            {
                targetOrNull.AddInteractions(this, actor);
            }

            map.GetTile(x, y).AddInteractions(this, x, y);

            Item? itemOrNull = map.GetItemAtOrNull(x, y);
            if (itemOrNull != null)
            {
                itemOrNull.AddInteractions(this);
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
