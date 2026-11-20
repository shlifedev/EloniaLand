using System.Collections.Generic;
using Elonia.Events;
using Elonia.Simulation;

namespace Elonia.Game
{
    public sealed class TurnHistory : IEventListener<ActionExecuted>
    {
        private const int MAX_ENTRY_COUNT = 300;

        private readonly Actor _player;

        private readonly List<Entry> _entries = new List<Entry>(MAX_ENTRY_COUNT);

        public IReadOnlyList<Entry> Entries => _entries;

        public long LastPlayerActionTick { get; private set; }
        public int NpcActionCountSincePlayerAction { get; private set; }

        public TurnHistory(Actor player)
        {
            _player = player;
        }

        public void OnEvent(ActionExecuted message)
        {
            if (message.Actor == _player)
            {
                LastPlayerActionTick = message.Tick;
                NpcActionCountSincePlayerAction = 0;
            }
            else
            {
                NpcActionCountSincePlayerAction++;
            }

            if (_entries.Count == MAX_ENTRY_COUNT)
            {
                _entries.RemoveAt(0);
            }

            _entries.Add(new Entry(message.Tick, message.Actor, message.Action, message.Cost));
        }

        public void Clear()
        {
            _entries.Clear();
        }

        public readonly struct Entry
        {
            public long Tick { get; }
            public Actor Actor { get; }
            public ActorAction Action { get; }
            public int Cost { get; }

            public Entry(long tick, Actor actor, ActorAction action, int cost)
            {
                Tick = tick;
                Actor = actor;
                Action = action;
                Cost = cost;
            }
        }
    }
}
