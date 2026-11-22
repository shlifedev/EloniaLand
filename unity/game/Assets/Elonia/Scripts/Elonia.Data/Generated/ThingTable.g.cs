#nullable enable
#pragma warning disable CS0169, CS0414, CS0649, CS1591, IDE0044, IDE0051

using System;
using System.Collections.Generic;
using MemoryPack;

namespace Elonia.Data
{
    public sealed partial class ThingTable : global::HolySheet.ITable
    {
        public const string TableName = "Thing";

        public const string SchemaHash = "e8773a70153ee8af15c4f88ab5641f776aa0fbe07567e33e06034ff3f9f356c1";

        private readonly ThingRow[] _rows;
        private readonly Dictionary<string, ThingRow> _byId;

        public string Name => TableName;

        public int Count => _rows.Length;

        public global::HolySheet.TableManifestEntry Entry { get; }

        public global::HolySheet.ReadOnlyArray<ThingRow> Rows => new global::HolySheet.ReadOnlyArray<ThingRow>(_rows);

        private ThingTable(ThingRow[] rows, global::HolySheet.TableManifestEntry entry)
        {
            _rows = rows;
            Entry = entry;
            _byId = new Dictionary<string, ThingRow>(rows.Length, StringComparer.Ordinal);
            for (int index = 0; index < rows.Length; index++)
            {
                if (_byId.ContainsKey(rows[index].Id))
                {
                    throw new global::HolySheet.TableLoadException(global::HolySheet.TableLoadError.DecodeFailed, "The table 'Thing' has two rows with the same Id: '" + rows[index].Id + "'.", TableName);
                }

                _byId.Add(rows[index].Id, rows[index]);
            }

            OnLoaded();
        }

        public ThingRow this[string @id] => Get(@id);

        public ThingRow Get(string @id)
        {
            if (_byId.TryGetValue(@id, out ThingRow? row))
            {
                return row;
            }

            throw new KeyNotFoundException("The table 'Thing' has no row whose Id is '" + @id + "'.");
        }

        public ThingRow? Find(string @id)
        {
            return _byId.TryGetValue(@id, out ThingRow? row) ? row : null;
        }

        public bool TryGet(string @id, out ThingRow row)
        {
            if (_byId.TryGetValue(@id, out ThingRow? found))
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

        public ThingRow Get(ThingRef reference)
        {
            if (!reference.HasValue)
            {
                throw new InvalidOperationException("The reference to the table 'Thing' points at no row.");
            }

            return Get(reference.Key);
        }

        public ThingRow? Find(ThingRef reference)
        {
            return reference.HasValue ? Find(reference.Key) : null;
        }

        public bool TryGet(ThingRef reference, out ThingRow row)
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
            ThingRow[]? rows = MemoryPackSerializer.Deserialize<ThingRow[]>(data);
            return new ThingTable(rows ?? Array.Empty<ThingRow>(), entry);
        }
    }

    public readonly struct ThingRef : IEquatable<ThingRef>
    {
        public bool HasValue { get; }

        public string Key { get; }

        public ThingRef(bool hasValue, string key)
        {
            HasValue = hasValue;
            Key = key;
        }

        public bool Equals(ThingRef other) => HasValue == other.HasValue && (!HasValue || string.Equals(Key, other.Key, StringComparison.Ordinal));

        public override bool Equals(object? obj) => obj is ThingRef other && Equals(other);

        public override int GetHashCode() => HasValue ? (Key == null ? 0 : Key.GetHashCode()) : 0;

        public override string ToString() => HasValue ? "Thing(" + Key + ")" : "Thing(none)";
    }
}
