using System;
using System.Buffers;
using System.Collections.Generic;
using MemoryPack;

namespace Elonia.DataManager
{
    public enum EEnumUnderlying
    {
        SByte,
        Byte,
        Short,
        UShort,
        Int,
        UInt,
        Long,
        ULong
    }

    public sealed class EnumMember
    {
        public string Name { get; }
        public long Value { get; }

        public Cell Source { get; }

        public EnumMember(string name, long value, Cell source)
        {
            Name = name;
            Value = value;
            Source = source;
        }
    }

    public sealed class EnumDefinition
    {
        private readonly List<EnumMember> _members;
        private readonly Dictionary<string, EnumMember> _membersByName = new Dictionary<string, EnumMember>(StringComparer.Ordinal);

        public string Name { get; }
        public EEnumUnderlying Underlying { get; }
        public bool IsFlags { get; }
        public string Comment { get; }

        public string Location { get; }

        public IReadOnlyList<EnumMember> Members => _members;

        public EnumDefinition(string name, EEnumUnderlying underlying, bool bFlags, string comment, string location, IEnumerable<EnumMember> members)
        {
            Name = name;
            Underlying = underlying;
            IsFlags = bFlags;
            Comment = comment;
            Location = location;
            _members = new List<EnumMember>(members);
            for (int index = 0; index < _members.Count; index++)
            {
                _membersByName[_members[index].Name] = _members[index];
            }
        }

        public EnumMember? FindMemberOrNull(string name)
        {
            return _membersByName.TryGetValue(name, out EnumMember? member) ? member : null;
        }

        public EnumMember? FindMemberByValueOrNull(long value)
        {
            for (int index = 0; index < _members.Count; index++)
            {
                if (_members[index].Value == value)
                {
                    return _members[index];
                }
            }

            return null;
        }

        public string GetMemberList()
        {
            List<string> names = new List<string>(_members.Count);
            for (int index = 0; index < _members.Count; index++)
            {
                names.Add(_members[index].Name);
            }

            return string.Join(", ", names);
        }
    }

    public static class EnumUnderlying
    {
        public static bool TryParse(string name, out EEnumUnderlying underlying)
        {
            switch (name)
            {
                case "sbyte":
                    underlying = EEnumUnderlying.SByte;
                    return true;
                case "byte":
                    underlying = EEnumUnderlying.Byte;
                    return true;
                case "short":
                    underlying = EEnumUnderlying.Short;
                    return true;
                case "ushort":
                    underlying = EEnumUnderlying.UShort;
                    return true;
                case "int":
                    underlying = EEnumUnderlying.Int;
                    return true;
                case "uint":
                    underlying = EEnumUnderlying.UInt;
                    return true;
                case "long":
                    underlying = EEnumUnderlying.Long;
                    return true;
                case "ulong":
                    underlying = EEnumUnderlying.ULong;
                    return true;
                default:
                    underlying = EEnumUnderlying.Int;
                    return false;
            }
        }

        public const string NAME_LIST = "sbyte, byte, short, ushort, int, uint, long, ulong";

        public static string GetCSharpName(EEnumUnderlying underlying)
        {
            switch (underlying)
            {
                case EEnumUnderlying.SByte:
                    return "sbyte";
                case EEnumUnderlying.Byte:
                    return "byte";
                case EEnumUnderlying.Short:
                    return "short";
                case EEnumUnderlying.UShort:
                    return "ushort";
                case EEnumUnderlying.Int:
                    return "int";
                case EEnumUnderlying.UInt:
                    return "uint";
                case EEnumUnderlying.Long:
                    return "long";
                default:
                    return "ulong";
            }
        }

        public static bool IsInRange(EEnumUnderlying underlying, long value)
        {
            switch (underlying)
            {
                case EEnumUnderlying.SByte:
                    return value >= sbyte.MinValue && value <= sbyte.MaxValue;
                case EEnumUnderlying.Byte:
                    return value >= byte.MinValue && value <= byte.MaxValue;
                case EEnumUnderlying.Short:
                    return value >= short.MinValue && value <= short.MaxValue;
                case EEnumUnderlying.UShort:
                    return value >= ushort.MinValue && value <= ushort.MaxValue;
                case EEnumUnderlying.Int:
                    return value >= int.MinValue && value <= int.MaxValue;
                case EEnumUnderlying.UInt:
                    return value >= uint.MinValue && value <= uint.MaxValue;
                case EEnumUnderlying.Long:
                    return true;
                default:
                    return value >= 0;
            }
        }

        public static void Write(ref MemoryPackWriter<ArrayBufferWriter<byte>> writer, EEnumUnderlying underlying, long value)
        {
            switch (underlying)
            {
                case EEnumUnderlying.SByte:
                    writer.WriteUnmanaged((sbyte)value);
                    break;
                case EEnumUnderlying.Byte:
                    writer.WriteUnmanaged((byte)value);
                    break;
                case EEnumUnderlying.Short:
                    writer.WriteUnmanaged((short)value);
                    break;
                case EEnumUnderlying.UShort:
                    writer.WriteUnmanaged((ushort)value);
                    break;
                case EEnumUnderlying.Int:
                    writer.WriteUnmanaged((int)value);
                    break;
                case EEnumUnderlying.UInt:
                    writer.WriteUnmanaged((uint)value);
                    break;
                case EEnumUnderlying.Long:
                    writer.WriteUnmanaged(value);
                    break;
                default:
                    writer.WriteUnmanaged((ulong)value);
                    break;
            }
        }
    }
}
