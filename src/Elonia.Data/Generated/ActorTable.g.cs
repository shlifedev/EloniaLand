#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Elonia.Data
{
    public sealed partial class ActorTable
    {
        public const string TABLE_NAME = "Actor";

        private readonly ActorRow[] _rows;
        private readonly Dictionary<string, ActorRow> _rowsById;

        public IReadOnlyList<ActorRow> Rows => _rows;
        public int Count => _rows.Length;

        public ActorTable(ActorRow[] rows)
        {
            _rows = rows;
            _rowsById = new Dictionary<string, ActorRow>(rows.Length);
            for (int index = 0; index < rows.Length; index++)
            {
                _rowsById.Add(rows[index].Id, rows[index]);
            }
        }

        public ActorRow Get(string id)
        {
            if (_rowsById.TryGetValue(id, out ActorRow? row) && row != null)
            {
                return row;
            }

            throw new KeyNotFoundException("The table 'Actor' has no row whose Id is '" + id + "'.");
        }

        public bool TryGet(string id, [NotNullWhen(true)] out ActorRow? row)
        {
            return _rowsById.TryGetValue(id, out row) && row != null;
        }

        public bool Contains(string id)
        {
            return _rowsById.ContainsKey(id);
        }

        public ActorRow Get(ActorRef reference)
        {
            if (!reference.HasValue)
            {
                throw new InvalidOperationException("The reference to the table 'Actor' points to no row.");
            }

            return Get(reference.Key);
        }

        public bool TryGet(ActorRef reference, [NotNullWhen(true)] out ActorRow? row)
        {
            if (!reference.HasValue)
            {
                row = null;
                return false;
            }

            return TryGet(reference.Key, out row);
        }
    }

    public readonly struct ActorRef
    {
        public bool HasValue { get; }

        public string Key { get; }

        public ActorRef(bool bHasValue, string key)
        {
            HasValue = bHasValue;
            Key = key;
        }
    }
}
