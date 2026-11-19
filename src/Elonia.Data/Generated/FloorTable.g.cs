#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Elonia.Data
{
    public sealed partial class FloorTable
    {
        public const string TABLE_NAME = "Floor";

        private readonly FloorRow[] _rows;
        private readonly Dictionary<string, FloorRow> _rowsById;

        public IReadOnlyList<FloorRow> Rows => _rows;
        public int Count => _rows.Length;

        public FloorTable(FloorRow[] rows)
        {
            _rows = rows;
            _rowsById = new Dictionary<string, FloorRow>(rows.Length);
            for (int index = 0; index < rows.Length; index++)
            {
                _rowsById.Add(rows[index].Id, rows[index]);
            }
        }

        public FloorRow Get(string id)
        {
            if (_rowsById.TryGetValue(id, out FloorRow? row) && row != null)
            {
                return row;
            }

            throw new KeyNotFoundException("The table 'Floor' has no row whose Id is '" + id + "'.");
        }

        public bool TryGet(string id, [NotNullWhen(true)] out FloorRow? row)
        {
            return _rowsById.TryGetValue(id, out row) && row != null;
        }

        public bool Contains(string id)
        {
            return _rowsById.ContainsKey(id);
        }

        public FloorRow Get(FloorRef reference)
        {
            if (!reference.HasValue)
            {
                throw new InvalidOperationException("The reference to the table 'Floor' points to no row.");
            }

            return Get(reference.Key);
        }

        public bool TryGet(FloorRef reference, [NotNullWhen(true)] out FloorRow? row)
        {
            if (!reference.HasValue)
            {
                row = null;
                return false;
            }

            return TryGet(reference.Key, out row);
        }
    }

    public readonly struct FloorRef
    {
        public bool HasValue { get; }

        public string Key { get; }

        public FloorRef(bool bHasValue, string key)
        {
            HasValue = bHasValue;
            Key = key;
        }
    }
}
