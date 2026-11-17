#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Elonia.Data
{
    public sealed partial class TileTable
    {
        public const string TABLE_NAME = "Tile";

        private readonly TileRow[] _rows;
        private readonly Dictionary<string, TileRow> _rowsById;

        public IReadOnlyList<TileRow> Rows => _rows;
        public int Count => _rows.Length;

        public TileTable(TileRow[] rows)
        {
            _rows = rows;
            _rowsById = new Dictionary<string, TileRow>(rows.Length);
            for (int index = 0; index < rows.Length; index++)
            {
                _rowsById.Add(rows[index].Id, rows[index]);
            }
        }

        public TileRow Get(string id)
        {
            if (_rowsById.TryGetValue(id, out TileRow? row) && row != null)
            {
                return row;
            }

            throw new KeyNotFoundException("The table 'Tile' has no row whose Id is '" + id + "'.");
        }

        public bool TryGet(string id, [NotNullWhen(true)] out TileRow? row)
        {
            return _rowsById.TryGetValue(id, out row) && row != null;
        }

        public bool Contains(string id)
        {
            return _rowsById.ContainsKey(id);
        }

        public TileRow Get(TileRef reference)
        {
            if (!reference.HasValue)
            {
                throw new InvalidOperationException("The reference to the table 'Tile' points to no row.");
            }

            return Get(reference.Key);
        }

        public bool TryGet(TileRef reference, [NotNullWhen(true)] out TileRow? row)
        {
            if (!reference.HasValue)
            {
                row = null;
                return false;
            }

            return TryGet(reference.Key, out row);
        }
    }

    public readonly struct TileRef
    {
        public bool HasValue { get; }

        public string Key { get; }

        public TileRef(bool bHasValue, string key)
        {
            HasValue = bHasValue;
            Key = key;
        }
    }
}
