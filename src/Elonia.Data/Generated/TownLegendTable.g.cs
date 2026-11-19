#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Elonia.Data
{
    public sealed partial class TownLegendTable
    {
        public const string TABLE_NAME = "TownLegend";

        private readonly TownLegendRow[] _rows;
        private readonly Dictionary<string, TownLegendRow> _rowsById;
        private readonly Dictionary<string, TownLegendRow> _rowsBySymbol;

        public IReadOnlyList<TownLegendRow> Rows => _rows;
        public int Count => _rows.Length;

        public TownLegendTable(TownLegendRow[] rows)
        {
            _rows = rows;
            _rowsById = new Dictionary<string, TownLegendRow>(rows.Length);
            _rowsBySymbol = new Dictionary<string, TownLegendRow>(rows.Length);
            for (int index = 0; index < rows.Length; index++)
            {
                _rowsById.Add(rows[index].Id, rows[index]);
                _rowsBySymbol.Add(rows[index].Symbol, rows[index]);
            }
        }

        public TownLegendRow Get(string id)
        {
            if (_rowsById.TryGetValue(id, out TownLegendRow? row) && row != null)
            {
                return row;
            }

            throw new KeyNotFoundException("The table 'TownLegend' has no row whose Id is '" + id + "'.");
        }

        public bool TryGet(string id, [NotNullWhen(true)] out TownLegendRow? row)
        {
            return _rowsById.TryGetValue(id, out row) && row != null;
        }

        public bool Contains(string id)
        {
            return _rowsById.ContainsKey(id);
        }

        public TownLegendRow GetBySymbol(string symbol)
        {
            if (_rowsBySymbol.TryGetValue(symbol, out TownLegendRow? row) && row != null)
            {
                return row;
            }

            throw new KeyNotFoundException("The table 'TownLegend' has no row whose Symbol is '" + symbol + "'.");
        }

        public bool TryGetBySymbol(string symbol, [NotNullWhen(true)] out TownLegendRow? row)
        {
            return _rowsBySymbol.TryGetValue(symbol, out row) && row != null;
        }

        public bool ContainsBySymbol(string symbol)
        {
            return _rowsBySymbol.ContainsKey(symbol);
        }

        public TownLegendRow Get(TownLegendRef reference)
        {
            if (!reference.HasValue)
            {
                throw new InvalidOperationException("The reference to the table 'TownLegend' points to no row.");
            }

            return Get(reference.Key);
        }

        public bool TryGet(TownLegendRef reference, [NotNullWhen(true)] out TownLegendRow? row)
        {
            if (!reference.HasValue)
            {
                row = null;
                return false;
            }

            return TryGet(reference.Key, out row);
        }
    }

    public readonly struct TownLegendRef
    {
        public bool HasValue { get; }

        public string Key { get; }

        public TownLegendRef(bool bHasValue, string key)
        {
            HasValue = bHasValue;
            Key = key;
        }
    }
}
