#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Elonia.Checks.Generated
{
    public sealed partial class ItemTable
    {
        public const string TABLE_NAME = "Item";

        private readonly ItemRow[] _rows;
        private readonly Dictionary<string, ItemRow> _rowsById;

        public IReadOnlyList<ItemRow> Rows => _rows;
        public int Count => _rows.Length;

        public ItemTable(ItemRow[] rows)
        {
            _rows = rows;
            _rowsById = new Dictionary<string, ItemRow>(rows.Length);
            for (int index = 0; index < rows.Length; index++)
            {
                _rowsById.Add(rows[index].Id, rows[index]);
            }
        }

        public ItemRow Get(string id)
        {
            if (_rowsById.TryGetValue(id, out ItemRow? row) && row != null)
            {
                return row;
            }

            throw new KeyNotFoundException("The table 'Item' has no row whose Id is '" + id + "'.");
        }

        public bool TryGet(string id, [NotNullWhen(true)] out ItemRow? row)
        {
            return _rowsById.TryGetValue(id, out row) && row != null;
        }

        public bool Contains(string id)
        {
            return _rowsById.ContainsKey(id);
        }

        public ItemRow Get(ItemRef reference)
        {
            if (!reference.HasValue)
            {
                throw new InvalidOperationException("The reference to the table 'Item' points to no row.");
            }

            return Get(reference.Key);
        }

        public bool TryGet(ItemRef reference, [NotNullWhen(true)] out ItemRow? row)
        {
            if (!reference.HasValue)
            {
                row = null;
                return false;
            }

            return TryGet(reference.Key, out row);
        }
    }

    public readonly struct ItemRef
    {
        public bool HasValue { get; }

        public string Key { get; }

        public ItemRef(bool bHasValue, string key)
        {
            HasValue = bHasValue;
            Key = key;
        }
    }
}
