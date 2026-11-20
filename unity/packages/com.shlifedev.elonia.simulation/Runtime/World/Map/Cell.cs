using System;
using System.Collections.Generic;
using ZLinq;

namespace Elonia.Simulation
{
    public sealed class Cell
    {
        private Actor? _actorOrNull;

        private List<Thing>? _thingsOrNull;

        public int X { get; }
        public int Y { get; }

        public IFloorDef Floor { get; internal set; }

        public IPropDef? PropOrNull { get; internal set; }

        public Actor? LivingActorOrNull => _actorOrNull != null && _actorOrNull.IsAlive ? _actorOrNull : null;

        public IReadOnlyList<Thing> Things => (IReadOnlyList<Thing>?)_thingsOrNull ?? Array.Empty<Thing>();

        public bool IsWalkable => Floor.IsWalkable && (PropOrNull == null || PropOrNull.IsWalkable);

        public bool CanEnter => IsWalkable && LivingActorOrNull == null;

        internal Cell(int x, int y, IFloorDef floor)
        {
            X = x;
            Y = y;
            Floor = floor;
        }

        public Thing? GetItemOrNull()
        {
            if (_thingsOrNull == null)
            {
                return null;
            }

            return _thingsOrNull.AsValueEnumerable().FirstOrDefault(static thing => thing.Def.IsItem);
        }

        internal void EnterActor(Actor actor)
        {
            _actorOrNull = actor;
        }

        internal void LeaveActor(Actor actor)
        {
            if (_actorOrNull == actor)
            {
                _actorOrNull = null;
            }
        }

        internal void AddThing(Thing thing)
        {
            if (_thingsOrNull == null)
            {
                _thingsOrNull = new List<Thing>(1);
            }

            _thingsOrNull.Add(thing);
        }

        internal void RemoveThing(Thing thing)
        {
            _thingsOrNull?.Remove(thing);
        }
    }
}
