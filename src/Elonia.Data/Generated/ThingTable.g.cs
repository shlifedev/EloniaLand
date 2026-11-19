#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Elonia.Data
{
    public sealed partial class ThingTable
    {
        public const string TABLE_NAME = "Thing";

        private readonly ThingRow[] _rows;
        private readonly Dictionary<string, ThingRow> _rowsById;

        public IReadOnlyList<ThingRow> Rows => _rows;
        public int Count => _rows.Length;

        public ThingTable(ThingRow[] rows)
        {
            _rows = rows;
            _rowsById = new Dictionary<string, ThingRow>(rows.Length);
            for (int index = 0; index < rows.Length; index++)
            {
                _rowsById.Add(rows[index].Id, rows[index]);
            }
        }

        public ThingRow Get(string id)
        {
            if (_rowsById.TryGetValue(id, out ThingRow? row) && row != null)
            {
                return row;
            }

            throw new KeyNotFoundException("The table 'Thing' has no row whose Id is '" + id + "'.");
        }

        public bool TryGet(string id, [NotNullWhen(true)] out ThingRow? row)
        {
            return _rowsById.TryGetValue(id, out row) && row != null;
        }

        public bool Contains(string id)
        {
            return _rowsById.ContainsKey(id);
        }

        public ThingRow Get(ThingRef reference)
        {
            if (!reference.HasValue)
            {
                throw new InvalidOperationException("The reference to the table 'Thing' points to no row.");
            }

            return Get(reference.Key);
        }

        public bool TryGet(ThingRef reference, [NotNullWhen(true)] out ThingRow? row)
        {
            if (!reference.HasValue)
            {
                row = null;
                return false;
            }

            return TryGet(reference.Key, out row);
        }
    }

    public readonly struct ThingRef
    {
        public bool HasValue { get; }

        public string Key { get; }

        public ThingRef(bool bHasValue, string key)
        {
            HasValue = bHasValue;
            Key = key;
        }
    }
}
