#nullable enable
#pragma warning disable CS0169, CS0414, CS0649, CS1591, IDE0044, IDE0051

using System;
using System.Collections.Generic;
using MemoryPack;

namespace Elonia.Data
{
    public sealed partial class PropTable : global::HolySheet.ITable
    {
        public const string TableName = "Prop";

        public const string SchemaHash = "0a278d666125e5e7045a86a438c0a778f3abeca09f147c4af9f0d365fcec1bfe";

        private readonly PropRow[] _rows;
        private readonly Dictionary<string, PropRow> _byId;

        public string Name => TableName;

        public int Count => _rows.Length;

        public global::HolySheet.TableManifestEntry Entry { get; }

        public global::HolySheet.ReadOnlyArray<PropRow> Rows => new global::HolySheet.ReadOnlyArray<PropRow>(_rows);

        private PropTable(PropRow[] rows, global::HolySheet.TableManifestEntry entry)
        {
            _rows = rows;
            Entry = entry;
            _byId = new Dictionary<string, PropRow>(rows.Length, StringComparer.Ordinal);
            for (int index = 0; index < rows.Length; index++)
            {
                if (_byId.ContainsKey(rows[index].Id))
                {
                    throw new global::HolySheet.TableLoadException(global::HolySheet.TableLoadError.DecodeFailed, "The table 'Prop' has two rows with the same Id: '" + rows[index].Id + "'.", TableName);
                }

                _byId.Add(rows[index].Id, rows[index]);
            }

            OnLoaded();
        }

        public PropRow this[string @id] => Get(@id);

        public PropRow Get(string @id)
        {
            if (_byId.TryGetValue(@id, out PropRow? row))
            {
                return row;
            }

            throw new KeyNotFoundException("The table 'Prop' has no row whose Id is '" + @id + "'.");
        }

        public PropRow? Find(string @id)
        {
            return _byId.TryGetValue(@id, out PropRow? row) ? row : null;
        }

        public bool TryGet(string @id, out PropRow row)
        {
            if (_byId.TryGetValue(@id, out PropRow? found))
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

        public PropRow Get(PropRef reference)
        {
            if (!reference.HasValue)
            {
                throw new InvalidOperationException("The reference to the table 'Prop' points at no row.");
            }

            return Get(reference.Key);
        }

        public PropRow? Find(PropRef reference)
        {
            return reference.HasValue ? Find(reference.Key) : null;
        }

        public bool TryGet(PropRef reference, out PropRow row)
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
            PropRow[]? rows = MemoryPackSerializer.Deserialize<PropRow[]>(data);
            return new PropTable(rows ?? Array.Empty<PropRow>(), entry);
        }
    }

    public readonly struct PropRef : IEquatable<PropRef>
    {
        public bool HasValue { get; }

        public string Key { get; }

        public PropRef(bool hasValue, string key)
        {
            HasValue = hasValue;
            Key = key;
        }

        public bool Equals(PropRef other) => HasValue == other.HasValue && (!HasValue || string.Equals(Key, other.Key, StringComparison.Ordinal));

        public override bool Equals(object? obj) => obj is PropRef other && Equals(other);

        public override int GetHashCode() => HasValue ? (Key == null ? 0 : Key.GetHashCode()) : 0;

        public override string ToString() => HasValue ? "Prop(" + Key + ")" : "Prop(none)";
    }
}
