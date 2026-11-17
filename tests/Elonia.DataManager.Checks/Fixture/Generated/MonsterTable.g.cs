#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Elonia.Checks.Generated
{
    public sealed partial class MonsterTable
    {
        public const string TABLE_NAME = "Monster";

        private readonly MonsterRow[] _rows;
        private readonly Dictionary<int, MonsterRow> _rowsById;
        private readonly Dictionary<string, MonsterRow> _rowsBySlug;

        public IReadOnlyList<MonsterRow> Rows => _rows;
        public int Count => _rows.Length;

        public MonsterTable(MonsterRow[] rows)
        {
            _rows = rows;
            _rowsById = new Dictionary<int, MonsterRow>(rows.Length);
            _rowsBySlug = new Dictionary<string, MonsterRow>(rows.Length);
            for (int index = 0; index < rows.Length; index++)
            {
                _rowsById.Add(rows[index].Id, rows[index]);
                _rowsBySlug.Add(rows[index].Slug, rows[index]);
            }
        }

        public MonsterRow Get(int id)
        {
            if (_rowsById.TryGetValue(id, out MonsterRow? row) && row != null)
            {
                return row;
            }

            throw new KeyNotFoundException("The table 'Monster' has no row whose Id is '" + id + "'.");
        }

        public bool TryGet(int id, [NotNullWhen(true)] out MonsterRow? row)
        {
            return _rowsById.TryGetValue(id, out row) && row != null;
        }

        public bool Contains(int id)
        {
            return _rowsById.ContainsKey(id);
        }

        public MonsterRow GetBySlug(string slug)
        {
            if (_rowsBySlug.TryGetValue(slug, out MonsterRow? row) && row != null)
            {
                return row;
            }

            throw new KeyNotFoundException("The table 'Monster' has no row whose Slug is '" + slug + "'.");
        }

        public bool TryGetBySlug(string slug, [NotNullWhen(true)] out MonsterRow? row)
        {
            return _rowsBySlug.TryGetValue(slug, out row) && row != null;
        }

        public bool ContainsBySlug(string slug)
        {
            return _rowsBySlug.ContainsKey(slug);
        }

        public MonsterRow Get(MonsterRef reference)
        {
            if (!reference.HasValue)
            {
                throw new InvalidOperationException("The reference to the table 'Monster' points to no row.");
            }

            return Get(reference.Key);
        }

        public bool TryGet(MonsterRef reference, [NotNullWhen(true)] out MonsterRow? row)
        {
            if (!reference.HasValue)
            {
                row = null;
                return false;
            }

            return TryGet(reference.Key, out row);
        }
    }

    public readonly struct MonsterRef
    {
        public bool HasValue { get; }

        public int Key { get; }

        public MonsterRef(bool bHasValue, int key)
        {
            HasValue = bHasValue;
            Key = key;
        }
    }
}
