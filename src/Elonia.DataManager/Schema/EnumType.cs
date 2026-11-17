using System;
using System.Buffers;
using System.Collections.Generic;
using MemoryPack;

namespace Elonia.DataManager
{
    public sealed class EnumType : ColumnType
    {
        private const char FLAG_SEPARATOR = ',';

        private readonly EnumDefinition _definition;

        public EnumDefinition Definition => _definition;
        public override string CanonicalName => "Enum<" + _definition.Name + ">";
        public override string CSharpTypeName => _definition.Name;
        public override bool CanBeKey => true;
        public override string Example => _definition.Members.Count > 0 ? _definition.Members[0].Name : "";

        public override object EmptyValue => 0L;

        public EnumType(EnumDefinition definition)
        {
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            _definition = definition;
        }

        public override bool TryParse(string text, out object value, out string reason)
        {
            value = 0L;
            reason = "";
            string trimmed = text.Trim();
            if (trimmed.Length == 0)
            {
                if (_definition.IsFlags || _definition.FindMemberByValueOrNull(0) != null)
                {
                    return true;
                }

                reason = "비어 있습니다. " + _definition.Name + "에는 값이 0인 멤버가 없어서 비워 둘 수 없습니다.";
                return false;
            }

            if (!_definition.IsFlags)
            {
                return TryParseMember(trimmed, out value, out reason);
            }

            long combined = 0;
            string[] parts = trimmed.Split(FLAG_SEPARATOR);
            for (int index = 0; index < parts.Length; index++)
            {
                if (!TryParseMember(parts[index].Trim(), out object member, out reason))
                {
                    return false;
                }

                combined |= (long)member;
            }

            value = combined;
            return true;
        }

        private bool TryParseMember(string name, out object value, out string reason)
        {
            EnumMember? memberOrNull = _definition.FindMemberOrNull(name);
            if (memberOrNull == null)
            {
                value = 0L;
                reason = _definition.Name + "에 '" + name + "'이라는 멤버가 없습니다. 쓸 수 있는 값: " + _definition.GetMemberList();
                return false;
            }

            value = memberOrNull.Value;
            reason = "";
            return true;
        }

        public override void Write(ref MemoryPackWriter<ArrayBufferWriter<byte>> writer, object value)
        {
            EnumUnderlying.Write(ref writer, _definition.Underlying, (long)value);
        }
    }

    public sealed class EnumArrayType : ColumnType
    {
        private readonly EnumType _elementType;

        public override string CanonicalName => "Enum<" + _elementType.Definition.Name + "[]>";
        public override string CSharpTypeName => _elementType.Definition.Name + "[]";
        public override object EmptyValue => Array.Empty<long>();
        public override ColumnType? ElementTypeOrNull => _elementType;
        public override string Example => _elementType.Example.Length == 0 ? "" : _elementType.Example + ELEMENT_SEPARATOR + _elementType.Example;

        public EnumArrayType(EnumType elementType)
        {
            if (elementType == null)
            {
                throw new ArgumentNullException(nameof(elementType));
            }

            _elementType = elementType;
        }

        public override bool TryParse(string text, out object value, out string reason)
        {
            List<object> elements = new List<object>();
            if (!CollectionColumnType.TryParseElements(_elementType, text, elements, out reason))
            {
                value = EmptyValue;
                return false;
            }

            value = CreateCollection(elements);
            return true;
        }

        public override object CreateCollection(List<object> elements)
        {
            long[] array = new long[elements.Count];
            for (int index = 0; index < array.Length; index++)
            {
                array[index] = (long)elements[index];
            }

            return array;
        }

        public override void Write(ref MemoryPackWriter<ArrayBufferWriter<byte>> writer, object value)
        {
            long[] array = (long[])value;
            writer.WriteCollectionHeader(array.Length);
            for (int index = 0; index < array.Length; index++)
            {
                EnumUnderlying.Write(ref writer, _elementType.Definition.Underlying, array[index]);
            }
        }
    }
}
