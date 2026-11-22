using System;
using System.Collections.Generic;

namespace Elonia.Commands
{
    public sealed class CommandRecord<TContext>
    {
        private readonly Dictionary<Type, CommandList> _lists = new Dictionary<Type, CommandList>();
        private readonly List<Entry> _entries;
        private readonly int _capacity;

        public int Count => _entries.Count;

        public CommandRecord(int capacity = 0)
        {
            if (capacity < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity));
            }

            _capacity = capacity;
            _entries = new List<Entry>(capacity);
        }

        public void Add<TCommand>(in TCommand command) where TCommand : struct, ICommand<TContext>
        {
            CommandList<TCommand> list = GetOrCreateList<TCommand>();
            _entries.Add(new Entry(list, list.Count));
            list.Add(command);
        }

        public bool Execute(int index, TContext context)
        {
            Entry entry = GetEntry(index);
            return entry.List.Execute(entry.Index, context);
        }

        public void CopyTo(int index, CommandRecord<TContext> target)
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            Entry entry = GetEntry(index);
            entry.List.CopyTo(entry.Index, target);
        }

        public void WriteTo(CommandTextWriter writer)
        {
            if (writer == null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            for (int index = 0; index < _entries.Count; index++)
            {
                Entry entry = _entries[index];
                entry.List.Write(entry.Index, writer);
            }
        }

        public void Clear()
        {
            _entries.Clear();
            foreach (CommandList list in _lists.Values)
            {
                list.Clear();
            }
        }

        private CommandList<TCommand> GetOrCreateList<TCommand>() where TCommand : struct, ICommand<TContext>
        {
            if (_lists.TryGetValue(typeof(TCommand), out CommandList? listOrNull))
            {
                return (CommandList<TCommand>)listOrNull;
            }

            CommandList<TCommand> list = new CommandList<TCommand>(_capacity);
            _lists.Add(typeof(TCommand), list);
            return list;
        }

        private Entry GetEntry(int index)
        {
            if ((uint)index >= (uint)_entries.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            return _entries[index];
        }

        private readonly struct Entry
        {
            public CommandList List { get; }
            public int Index { get; }

            public Entry(CommandList list, int index)
            {
                List = list;
                Index = index;
            }
        }

        private abstract class CommandList
        {
            public abstract bool Execute(int index, TContext context);
            public abstract void CopyTo(int index, CommandRecord<TContext> target);
            public abstract void Write(int index, CommandTextWriter writer);
            public abstract void Clear();
        }

        private sealed class CommandList<TCommand> : CommandList where TCommand : struct, ICommand<TContext>
        {
            private readonly List<TCommand> _items;

            public int Count => _items.Count;

            public CommandList(int capacity)
            {
                _items = new List<TCommand>(capacity);
            }

            public void Add(in TCommand command)
            {
                _items.Add(command);
            }

            public override bool Execute(int index, TContext context)
            {
                return _items[index].Execute(context);
            }

            public override void CopyTo(int index, CommandRecord<TContext> target)
            {
                target.Add(_items[index]);
            }

            public override void Write(int index, CommandTextWriter writer)
            {
                _items[index].Write(writer);
            }

            public override void Clear()
            {
                _items.Clear();
            }
        }
    }
}
