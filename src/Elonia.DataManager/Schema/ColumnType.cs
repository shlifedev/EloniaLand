using System;
using System.Buffers;
using System.Collections.Generic;
using MemoryPack;

namespace Elonia.DataManager
{
    public abstract class ColumnType
    {
        public const char ELEMENT_SEPARATOR = '|';

        public abstract string CanonicalName { get; }

        public abstract string CSharpTypeName { get; }

        public abstract object EmptyValue { get; }

        public virtual bool CanBeKey => false;

        public virtual ColumnType? ElementTypeOrNull => null;

        public virtual string Example => "";

        public abstract bool TryParse(string text, out object value, out string reason);

        public abstract void Write(ref MemoryPackWriter<ArrayBufferWriter<byte>> writer, object value);

        public virtual object CreateCollection(List<object> elements)
        {
            throw new InvalidOperationException($"'{CanonicalName}' is not a collection type.");
        }
    }

    public abstract class ScalarColumnType : ColumnType
    {
        public abstract ColumnType CreateArrayType();
        public abstract ColumnType CreateListType();
    }

    public delegate bool TryParseValue<T>(string text, out T value);

    public sealed class ScalarType<T> : ScalarColumnType
    {
        private readonly TryParseValue<T> _tryParse;
        private readonly T _emptyValue;
        private readonly string _invalidReason;
        private readonly bool _canBeKey;

        public override string CanonicalName { get; }
        public override string CSharpTypeName { get; }
        public override object EmptyValue => _emptyValue!;
        public override bool CanBeKey => _canBeKey;
        public override string Example { get; }

        public ScalarType(string name, T emptyValue, TryParseValue<T> tryParse, string invalidReason, string example, bool bCanBeKey)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("A type needs a name.", nameof(name));
            }

            if (tryParse == null)
            {
                throw new ArgumentNullException(nameof(tryParse));
            }

            CanonicalName = name;
            CSharpTypeName = name;
            _emptyValue = emptyValue;
            _tryParse = tryParse;
            _invalidReason = invalidReason;
            Example = example;
            _canBeKey = bCanBeKey;
        }

        public override bool TryParse(string text, out object value, out string reason)
        {
            reason = "";
            if (text.Length == 0)
            {
                value = _emptyValue!;
                return true;
            }

            if (_tryParse(text, out T parsed))
            {
                value = parsed!;
                return true;
            }

            value = _emptyValue!;
            reason = _invalidReason;
            return false;
        }

        public override void Write(ref MemoryPackWriter<ArrayBufferWriter<byte>> writer, object value)
        {
            T typed = (T)value;
            writer.WriteValue(typed);
        }

        public override ColumnType CreateArrayType()
        {
            return new ArrayType<T>(this);
        }

        public override ColumnType CreateListType()
        {
            return new ListType<T>(this);
        }
    }

    public abstract class CollectionColumnType : ColumnType
    {
        private readonly ColumnType _elementType;

        public override ColumnType? ElementTypeOrNull => _elementType;
        public override string Example => _elementType.Example.Length == 0 ? "" : _elementType.Example + ELEMENT_SEPARATOR + _elementType.Example;

        protected CollectionColumnType(ColumnType elementType)
        {
            _elementType = elementType;
        }

        public override bool TryParse(string text, out object value, out string reason)
        {
            List<object> elements = new List<object>();
            if (!TryParseElements(_elementType, text, elements, out reason))
            {
                value = EmptyValue;
                return false;
            }

            value = CreateCollection(elements);
            return true;
        }

        public static bool TryParseElements(ColumnType elementType, string text, List<object> elements, out string reason)
        {
            reason = "";
            if (text.Trim().Length == 0)
            {
                return true;
            }

            string[] parts = text.Split(ELEMENT_SEPARATOR);
            for (int index = 0; index < parts.Length; index++)
            {
                string part = parts[index].Trim();
                if (part.Length == 0)
                {
                    reason = (index + 1) + "번째 요소가 비어 있습니다. '" + ELEMENT_SEPARATOR + "'가 맨 앞이나 맨 뒤에 있거나 연달아 있지 않은지 확인하세요.";
                    return false;
                }

                if (!elementType.TryParse(part, out object element, out string elementReason))
                {
                    reason = (index + 1) + "번째 요소 \"" + part + "\": " + elementReason;
                    return false;
                }

                elements.Add(element);
            }

            return true;
        }
    }

    public sealed class ArrayType<T> : CollectionColumnType
    {
        private readonly ScalarType<T> _elementType;

        public override string CanonicalName => _elementType.CanonicalName + "[]";
        public override string CSharpTypeName => _elementType.CSharpTypeName + "[]";
        public override object EmptyValue => Array.Empty<T>();

        public ArrayType(ScalarType<T> elementType)
            : base(elementType)
        {
            _elementType = elementType;
        }

        public override object CreateCollection(List<object> elements)
        {
            T[] array = new T[elements.Count];
            for (int index = 0; index < array.Length; index++)
            {
                array[index] = (T)elements[index];
            }

            return array;
        }

        public override void Write(ref MemoryPackWriter<ArrayBufferWriter<byte>> writer, object value)
        {
            T[] typed = (T[])value;
            writer.WriteValue(typed);
        }
    }

    public sealed class ListType<T> : CollectionColumnType
    {
        private readonly ScalarType<T> _elementType;

        public override string CanonicalName => "List<" + _elementType.CanonicalName + ">";
        public override string CSharpTypeName => "List<" + _elementType.CSharpTypeName + ">";
        public override object EmptyValue => new List<T>();

        public ListType(ScalarType<T> elementType)
            : base(elementType)
        {
            _elementType = elementType;
        }

        public override object CreateCollection(List<object> elements)
        {
            List<T> list = new List<T>(elements.Count);
            for (int index = 0; index < elements.Count; index++)
            {
                list.Add((T)elements[index]);
            }

            return list;
        }

        public override void Write(ref MemoryPackWriter<ArrayBufferWriter<byte>> writer, object value)
        {
            List<T> typed = (List<T>)value;
            writer.WriteValue(typed);
        }
    }
}
