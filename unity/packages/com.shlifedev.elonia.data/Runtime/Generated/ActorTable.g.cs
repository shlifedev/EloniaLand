#nullable enable
#pragma warning disable CS0169, CS0414, CS0649, CS1591, IDE0044, IDE0051

using System;
using System.Collections.Generic;
using MemoryPack;

namespace Elonia.Data
{
    public sealed partial class ActorTable : global::HolySheet.ITable
    {
        public const string TableName = "Actor";

        public const string SchemaHash = "61d87b82522d57941a25dbe76eccefc7064efabc232c177a5fd3cf69b3ac9000";

        private readonly ActorRow[] _rows;
        private readonly Dictionary<string, ActorRow> _byId;

        public string Name => TableName;

        public int Count => _rows.Length;

        public global::HolySheet.TableManifestEntry Entry { get; }

        public global::HolySheet.ReadOnlyArray<ActorRow> Rows => new global::HolySheet.ReadOnlyArray<ActorRow>(_rows);

        private ActorTable(ActorRow[] rows, global::HolySheet.TableManifestEntry entry)
        {
            _rows = rows;
            Entry = entry;
            _byId = new Dictionary<string, ActorRow>(rows.Length, StringComparer.Ordinal);
            for (int index = 0; index < rows.Length; index++)
            {
                if (_byId.ContainsKey(rows[index].Id))
                {
                    throw new global::HolySheet.TableLoadException(global::HolySheet.TableLoadError.DecodeFailed, "The table 'Actor' has two rows with the same Id: '" + rows[index].Id + "'.", TableName);
                }

                _byId.Add(rows[index].Id, rows[index]);
            }

            OnLoaded();
        }

        public ActorRow this[string @id] => Get(@id);

        public ActorRow Get(string @id)
        {
            if (_byId.TryGetValue(@id, out ActorRow? row))
            {
                return row;
            }

            throw new KeyNotFoundException("The table 'Actor' has no row whose Id is '" + @id + "'.");
        }

        public ActorRow? Find(string @id)
        {
            return _byId.TryGetValue(@id, out ActorRow? row) ? row : null;
        }

        public bool TryGet(string @id, out ActorRow row)
        {
            if (_byId.TryGetValue(@id, out ActorRow? found))
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

        public ActorRow Get(ActorRef reference)
        {
            if (!reference.HasValue)
            {
                throw new InvalidOperationException("The reference to the table 'Actor' points at no row.");
            }

            return Get(reference.Key);
        }

        public ActorRow? Find(ActorRef reference)
        {
            return reference.HasValue ? Find(reference.Key) : null;
        }

        public bool TryGet(ActorRef reference, out ActorRow row)
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
            ActorRow[]? rows = MemoryPackSerializer.Deserialize<ActorRow[]>(data);
            return new ActorTable(rows ?? Array.Empty<ActorRow>(), entry);
        }
    }

    public readonly struct ActorRef : IEquatable<ActorRef>
    {
        public bool HasValue { get; }

        public string Key { get; }

        public ActorRef(bool hasValue, string key)
        {
            HasValue = hasValue;
            Key = key;
        }

        public bool Equals(ActorRef other) => HasValue == other.HasValue && (!HasValue || string.Equals(Key, other.Key, StringComparison.Ordinal));

        public override bool Equals(object? obj) => obj is ActorRef other && Equals(other);

        public override int GetHashCode() => HasValue ? (Key == null ? 0 : Key.GetHashCode()) : 0;

        public override string ToString() => HasValue ? "Actor(" + Key + ")" : "Actor(none)";
    }
}
