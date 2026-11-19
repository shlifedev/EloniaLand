#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Elonia.Data
{
    public sealed partial class PropTable
    {
        public const string TABLE_NAME = "Prop";

        private readonly PropRow[] _rows;
        private readonly Dictionary<string, PropRow> _rowsById;

        public IReadOnlyList<PropRow> Rows => _rows;
        public int Count => _rows.Length;

        public PropTable(PropRow[] rows)
        {
            _rows = rows;
            _rowsById = new Dictionary<string, PropRow>(rows.Length);
            for (int index = 0; index < rows.Length; index++)
            {
                _rowsById.Add(rows[index].Id, rows[index]);
            }
        }

        public PropRow Get(string id)
        {
            if (_rowsById.TryGetValue(id, out PropRow? row) && row != null)
            {
                return row;
            }

            throw new KeyNotFoundException("The table 'Prop' has no row whose Id is '" + id + "'.");
        }

        public bool TryGet(string id, [NotNullWhen(true)] out PropRow? row)
        {
            return _rowsById.TryGetValue(id, out row) && row != null;
        }

        public bool Contains(string id)
        {
            return _rowsById.ContainsKey(id);
        }

        public PropRow Get(PropRef reference)
        {
            if (!reference.HasValue)
            {
                throw new InvalidOperationException("The reference to the table 'Prop' points to no row.");
            }

            return Get(reference.Key);
        }

        public bool TryGet(PropRef reference, [NotNullWhen(true)] out PropRow? row)
        {
            if (!reference.HasValue)
            {
                row = null;
                return false;
            }

            return TryGet(reference.Key, out row);
        }
    }

    public readonly struct PropRef
    {
        public bool HasValue { get; }

        public string Key { get; }

        public PropRef(bool bHasValue, string key)
        {
            HasValue = bHasValue;
            Key = key;
        }
    }
}
