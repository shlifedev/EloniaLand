#nullable enable
#pragma warning disable CS0169, CS0414, CS0649, CS1591, IDE0044, IDE0051

using System;
using System.Collections.Generic;
using MemoryPack;

namespace Elonia.Data
{
    public sealed partial class TownLegendTable : global::HolySheet.ITable
    {
        public const string TableName = "TownLegend";

        public const string SchemaHash = "8053c5d25c2e2af3f50672dd32a8b9369b31fbd86ccc9dffd96b351e0a769da5";

        private readonly TownLegendRow[] _rows;
        private readonly Dictionary<string, TownLegendRow> _byId;
        private readonly Dictionary<string, TownLegendRow> _bySymbol;

        public string Name => TableName;

        public int Count => _rows.Length;

        public global::HolySheet.TableManifestEntry Entry { get; }

        public global::HolySheet.ReadOnlyArray<TownLegendRow> Rows => new global::HolySheet.ReadOnlyArray<TownLegendRow>(_rows);

        private TownLegendTable(TownLegendRow[] rows, global::HolySheet.TableManifestEntry entry)
        {
            _rows = rows;
            Entry = entry;
            _byId = new Dictionary<string, TownLegendRow>(rows.Length, StringComparer.Ordinal);
            _bySymbol = new Dictionary<string, TownLegendRow>(rows.Length, StringComparer.Ordinal);
            for (int index = 0; index < rows.Length; index++)
            {
                if (_byId.ContainsKey(rows[index].Id))
                {
                    throw new global::HolySheet.TableLoadException(global::HolySheet.TableLoadError.DecodeFailed, "The table 'TownLegend' has two rows with the same Id: '" + rows[index].Id + "'.", TableName);
                }

                _byId.Add(rows[index].Id, rows[index]);
                if (_bySymbol.ContainsKey(rows[index].Symbol))
                {
                    throw new global::HolySheet.TableLoadException(global::HolySheet.TableLoadError.DecodeFailed, "The table 'TownLegend' has two rows with the same Symbol: '" + rows[index].Symbol + "'.", TableName);
                }

                _bySymbol.Add(rows[index].Symbol, rows[index]);
            }

            OnLoaded();
        }

        public TownLegendRow this[string @id] => Get(@id);

        public TownLegendRow Get(string @id)
        {
            if (_byId.TryGetValue(@id, out TownLegendRow? row))
            {
                return row;
            }

            throw new KeyNotFoundException("The table 'TownLegend' has no row whose Id is '" + @id + "'.");
        }

        public TownLegendRow? Find(string @id)
        {
            return _byId.TryGetValue(@id, out TownLegendRow? row) ? row : null;
        }

        public bool TryGet(string @id, out TownLegendRow row)
        {
            if (_byId.TryGetValue(@id, out TownLegendRow? found))
            {
                row = found;
                return true;
            }

            row = null!;
            return false;
        }

        public bool Contains(string @id)
        {
            return _byId.ContainsKey(@id);
        }

        public TownLegendRow GetBySymbol(string @symbol)
        {
            if (_bySymbol.TryGetValue(@symbol, out TownLegendRow? row))
            {
                return row;
            }

            throw new KeyNotFoundException("The table 'TownLegend' has no row whose Symbol is '" + @symbol + "'.");
        }

        public TownLegendRow? FindBySymbol(string @symbol)
        {
            return _bySymbol.TryGetValue(@symbol, out TownLegendRow? row) ? row : null;
        }

        public bool TryGetBySymbol(string @symbol, out TownLegendRow row)
        {
            if (_bySymbol.TryGetValue(@symbol, out TownLegendRow? found))
            {
                row = found;
                return true;
            }

            row = null!;
            return false;
        }

        public bool ContainsBySymbol(string @symbol)
        {
            return _bySymbol.ContainsKey(@symbol);
        }

        public TownLegendRow Get(TownLegendRef reference)
        {
            if (!reference.HasValue)
            {
                throw new InvalidOperationException("The reference to the table 'TownLegend' points at no row.");
            }

            return Get(reference.Key);
        }

        public TownLegendRow? Find(TownLegendRef reference)
        {
            return reference.HasValue ? Find(reference.Key) : null;
        }

        public bool TryGet(TownLegendRef reference, out TownLegendRow row)
        {
            if (reference.HasValue)
            {
                return TryGet(reference.Key, out row);
            }

            row = null!;
            return false;
        }

        partial void OnLoaded();

        internal static global::HolySheet.ITable Decode(byte[] data, global::HolySheet.TableManifestEntry entry)
        {
            TownLegendRow[]? rows = MemoryPackSerializer.Deserialize<TownLegendRow[]>(data);
            return new TownLegendTable(rows ?? Array.Empty<TownLegendRow>(), entry);
        }
    }

    public readonly struct TownLegendRef : IEquatable<TownLegendRef>
    {
        public bool HasValue { get; }

        public string Key { get; }

        public TownLegendRef(bool hasValue, string key)
        {
            HasValue = hasValue;
            Key = key;
        }

        public bool Equals(TownLegendRef other) => HasValue == other.HasValue && (!HasValue || string.Equals(Key, other.Key, StringComparison.Ordinal));

        public override bool Equals(object? obj) => obj is TownLegendRef other && Equals(other);

        public override int GetHashCode() => HasValue ? (Key == null ? 0 : Key.GetHashCode()) : 0;

        public override string ToString() => HasValue ? "TownLegend(" + Key + ")" : "TownLegend(none)";
    }
}
