#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Elonia.Checks.Generated
{
    public sealed partial class AllTypesTable
    {
        public const string TABLE_NAME = "AllTypes";

        private readonly AllTypesRow[] _rows;
        private readonly Dictionary<int, AllTypesRow> _rowsById;
        private readonly Dictionary<EPlain, AllTypesRow> _rowsByPlain;

        public IReadOnlyList<AllTypesRow> Rows => _rows;
        public int Count => _rows.Length;

        public AllTypesTable(AllTypesRow[] rows)
        {
            _rows = rows;
            _rowsById = new Dictionary<int, AllTypesRow>(rows.Length);
            _rowsByPlain = new Dictionary<EPlain, AllTypesRow>(rows.Length);
            for (int index = 0; index < rows.Length; index++)
            {
                _rowsById.Add(rows[index].Id, rows[index]);
                _rowsByPlain.Add(rows[index].Plain, rows[index]);
            }
        }

        public AllTypesRow Get(int id)
        {
            if (_rowsById.TryGetValue(id, out AllTypesRow? row) && row != null)
            {
                return row;
            }

            throw new KeyNotFoundException("The table 'AllTypes' has no row whose Id is '" + id + "'.");
        }

        public bool TryGet(int id, [NotNullWhen(true)] out AllTypesRow? row)
        {
            return _rowsById.TryGetValue(id, out row) && row != null;
        }

        public bool Contains(int id)
        {
            return _rowsById.ContainsKey(id);
        }

        public AllTypesRow GetByPlain(EPlain plain)
        {
            if (_rowsByPlain.TryGetValue(plain, out AllTypesRow? row) && row != null)
            {
                return row;
            }

            throw new KeyNotFoundException("The table 'AllTypes' has no row whose Plain is '" + plain + "'.");
        }

        public bool TryGetByPlain(EPlain plain, [NotNullWhen(true)] out AllTypesRow? row)
        {
            return _rowsByPlain.TryGetValue(plain, out row) && row != null;
        }

        public bool ContainsByPlain(EPlain plain)
        {
            return _rowsByPlain.ContainsKey(plain);
        }

        public AllTypesRow Get(AllTypesRef reference)
        {
            if (!reference.HasValue)
            {
                throw new InvalidOperationException("The reference to the table 'AllTypes' points to no row.");
            }

            return Get(reference.Key);
        }

        public bool TryGet(AllTypesRef reference, [NotNullWhen(true)] out AllTypesRow? row)
        {
            if (!reference.HasValue)
            {
                row = null;
                return false;
            }

            return TryGet(reference.Key, out row);
        }
    }

    public readonly struct AllTypesRef
    {
        public bool HasValue { get; }

        public int Key { get; }

        public AllTypesRef(bool bHasValue, int key)
        {
            HasValue = bHasValue;
            Key = key;
        }
    }
}
