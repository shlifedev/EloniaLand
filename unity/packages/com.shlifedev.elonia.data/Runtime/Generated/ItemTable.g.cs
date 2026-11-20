#nullable enable
#pragma warning disable CS0169, CS0414, CS0649, CS1591, IDE0044, IDE0051

using System;
using System.Collections.Generic;
using MemoryPack;

namespace Elonia.Data
{
    public sealed partial class ItemTable : global::HolySheet.ITable
    {
        public const string TableName = "Item";

        public const string SchemaHash = "409ac9c2e3eb3d31c2da730f4d80fed1d33adb152e9439101d2849a08f1a49f1";

        private readonly ItemRow[] _rows;
        private readonly Dictionary<string, ItemRow> _byId;

        public string Name => TableName;

        public int Count => _rows.Length;

        public global::HolySheet.TableManifestEntry Entry { get; }

        public global::HolySheet.ReadOnlyArray<ItemRow> Rows => new global::HolySheet.ReadOnlyArray<ItemRow>(_rows);

        private ItemTable(ItemRow[] rows, global::HolySheet.TableManifestEntry entry)
        {
            _rows = rows;
            Entry = entry;
            _byId = new Dictionary<string, ItemRow>(rows.Length, StringComparer.Ordinal);
            for (int index = 0; index < rows.Length; index++)
            {
                if (_byId.ContainsKey(rows[index].Id))
                {
                    throw new global::HolySheet.TableLoadException(global::HolySheet.TableLoadError.DecodeFailed, "The table 'Item' has two rows with the same Id: '" + rows[index].Id + "'.", TableName);
                }

                _byId.Add(rows[index].Id, rows[index]);
            }

            OnLoaded();
        }

        public ItemRow this[string @id] => Get(@id);

        public ItemRow Get(string @id)
        {
            if (_byId.TryGetValue(@id, out ItemRow? row))
            {
                return row;
            }

            throw new KeyNotFoundException("The table 'Item' has no row whose Id is '" + @id + "'.");
        }

        public ItemRow? Find(string @id)
        {
            return _byId.TryGetValue(@id, out ItemRow? row) ? row : null;
        }

        public bool TryGet(string @id, out ItemRow row)
        {
            if (_byId.TryGetValue(@id, out ItemRow? found))
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

        public ItemRow Get(ItemRef reference)
        {
            if (!reference.HasValue)
            {
                throw new InvalidOperationException("The reference to the table 'Item' points at no row.");
            }

            return Get(reference.Key);
        }

        public ItemRow? Find(ItemRef reference)
        {
            return reference.HasValue ? Find(reference.Key) : null;
        }

        public bool TryGet(ItemRef reference, out ItemRow row)
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
            ItemRow[]? rows = MemoryPackSerializer.Deserialize<ItemRow[]>(data);
            return new ItemTable(rows ?? Array.Empty<ItemRow>(), entry);
        }
    }

    public readonly struct ItemRef : IEquatable<ItemRef>
    {
        public bool HasValue { get; }

        public string Key { get; }

        public ItemRef(bool hasValue, string key)
        {
            HasValue = hasValue;
            Key = key;
        }

        public bool Equals(ItemRef other) => HasValue == other.HasValue && (!HasValue || string.Equals(Key, other.Key, StringComparison.Ordinal));

        public override bool Equals(object? obj) => obj is ItemRef other && Equals(other);

        public override int GetHashCode() => HasValue ? (Key == null ? 0 : Key.GetHashCode()) : 0;

        public override string ToString() => HasValue ? "Item(" + Key + ")" : "Item(none)";
    }
}
