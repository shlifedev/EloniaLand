#nullable enable
#pragma warning disable CS0169, CS0414, CS0649, CS1591, IDE0044, IDE0051

using System;
using System.Collections.Generic;
using MemoryPack;

namespace Elonia.Data
{
    public sealed partial class FloorTable : global::HolySheet.ITable
    {
        public const string TableName = "Floor";

        public const string SchemaHash = "b6cf4f5de791abdee4cfab9db80438d03cdc0eaade22ef6ca3bae1a65c534942";

        private readonly FloorRow[] _rows;
        private readonly Dictionary<string, FloorRow> _byId;

        public string Name => TableName;

        public int Count => _rows.Length;

        public global::HolySheet.TableManifestEntry Entry { get; }

        public global::HolySheet.ReadOnlyArray<FloorRow> Rows => new global::HolySheet.ReadOnlyArray<FloorRow>(_rows);

        private FloorTable(FloorRow[] rows, global::HolySheet.TableManifestEntry entry)
        {
            _rows = rows;
            Entry = entry;
            _byId = new Dictionary<string, FloorRow>(rows.Length, StringComparer.Ordinal);
            for (int index = 0; index < rows.Length; index++)
            {
                if (_byId.ContainsKey(rows[index].Id))
                {
                    throw new global::HolySheet.TableLoadException(global::HolySheet.TableLoadError.DecodeFailed, "The table 'Floor' has two rows with the same Id: '" + rows[index].Id + "'.", TableName);
                }

                _byId.Add(rows[index].Id, rows[index]);
            }

            OnLoaded();
        }

        public FloorRow this[string @id] => Get(@id);

        public FloorRow Get(string @id)
        {
            if (_byId.TryGetValue(@id, out FloorRow? row))
            {
                return row;
            }

            throw new KeyNotFoundException("The table 'Floor' has no row whose Id is '" + @id + "'.");
        }

        public FloorRow? Find(string @id)
        {
            return _byId.TryGetValue(@id, out FloorRow? row) ? row : null;
        }

        public bool TryGet(string @id, out FloorRow row)
        {
            if (_byId.TryGetValue(@id, out FloorRow? found))
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

        public FloorRow Get(FloorRef reference)
        {
            if (!reference.HasValue)
            {
                throw new InvalidOperationException("The reference to the table 'Floor' points at no row.");
            }

            return Get(reference.Key);
        }

        public FloorRow? Find(FloorRef reference)
        {
            return reference.HasValue ? Find(reference.Key) : null;
        }

        public bool TryGet(FloorRef reference, out FloorRow row)
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
            FloorRow[]? rows = MemoryPackSerializer.Deserialize<FloorRow[]>(data);
            return new FloorTable(rows ?? Array.Empty<FloorRow>(), entry);
        }
    }

    public readonly struct FloorRef : IEquatable<FloorRef>
    {
        public bool HasValue { get; }

        public string Key { get; }

        public FloorRef(bool hasValue, string key)
        {
            HasValue = hasValue;
            Key = key;
        }

        public bool Equals(FloorRef other) => HasValue == other.HasValue && (!HasValue || string.Equals(Key, other.Key, StringComparison.Ordinal));

        public override bool Equals(object? obj) => obj is FloorRef other && Equals(other);

        public override int GetHashCode() => HasValue ? (Key == null ? 0 : Key.GetHashCode()) : 0;

        public override string ToString() => HasValue ? "Floor(" + Key + ")" : "Floor(none)";
    }
}
