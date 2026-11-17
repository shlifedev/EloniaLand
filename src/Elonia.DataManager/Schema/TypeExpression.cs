using System;

namespace Elonia.DataManager
{
    public enum ETypeKind
    {
        Value,

        Enum,

        Reference,

        Mapping
    }

    public enum ECollectionKind
    {
        None,

        Array,

        List
    }

    public sealed class TypeExpression
    {
        private const string KEY_WRAPPER = "Key";
        private const string ENUM_WRAPPER = "Enum";
        private const string REF_WRAPPER = "Ref";
        private const string MAPPING_WRAPPER = "Mapping";
        private const string LIST_WRAPPER = "List";
        private const string ARRAY_SUFFIX = "[]";

        public ETypeKind Kind { get; }

        public string Name { get; }
        public bool IsKey { get; }
        public ECollectionKind Collection { get; }

        private TypeExpression(ETypeKind kind, string name, bool bKey, ECollectionKind collection)
        {
            Kind = kind;
            Name = name;
            IsKey = bKey;
            Collection = collection;
        }

        public static bool TryParse(string text, out TypeExpression? expressionOrNull, out string reason)
        {
            expressionOrNull = null;
            reason = "";
            string body = text.Trim();
            if (body.Length == 0)
            {
                reason = "타입이 비어 있습니다.";
                return false;
            }

            bool bKey = TryUnwrap(body, KEY_WRAPPER, out string keyInner);
            if (bKey)
            {
                body = keyInner;
            }

            if (TryUnwrap(body, ENUM_WRAPPER, out string enumInner))
            {
                bool bEnumArray = TryRemoveArraySuffix(enumInner, out string enumName);
                return TryCreate(ETypeKind.Enum, enumName, bKey, bEnumArray ? ECollectionKind.Array : ECollectionKind.None, text, out expressionOrNull, out reason);
            }

            if (TryUnwrap(body, REF_WRAPPER, out string targetTable))
            {
                return TryCreate(ETypeKind.Reference, targetTable, bKey, ECollectionKind.None, text, out expressionOrNull, out reason);
            }

            if (TryUnwrap(body, MAPPING_WRAPPER, out string mappingName))
            {
                return TryCreate(ETypeKind.Mapping, mappingName, bKey, ECollectionKind.None, text, out expressionOrNull, out reason);
            }

            if (TryUnwrap(body, LIST_WRAPPER, out string listElement))
            {
                return TryCreate(ETypeKind.Value, listElement, bKey, ECollectionKind.List, text, out expressionOrNull, out reason);
            }

            bool bArray = TryRemoveArraySuffix(body, out string elementName);
            return TryCreate(ETypeKind.Value, elementName, bKey, bArray ? ECollectionKind.Array : ECollectionKind.None, text, out expressionOrNull, out reason);
        }

        private static bool TryCreate(ETypeKind kind, string name, bool bKey, ECollectionKind collection, string text, out TypeExpression? expressionOrNull, out string reason)
        {
            expressionOrNull = null;
            reason = "";
            string trimmed = name.Trim();

            if (trimmed.Length == 0 || trimmed.IndexOfAny(new char[] { '<', '>', '[', ']', ' ' }) >= 0)
            {
                reason = "타입 '" + text.Trim() + "'을(를) 이해하지 못했습니다. 겹쳐 쓸 수 있는 것은 Key<...> 하나뿐이고, enum 배열은 Enum<이름[]>로 씁니다.";
                return false;
            }

            if (bKey && (collection != ECollectionKind.None || kind == ETypeKind.Reference || kind == ETypeKind.Mapping))
            {
                reason = "Key<...> 안에는 값 하나짜리 타입만 쓸 수 있습니다(배열, Ref, Mapping 불가).";
                return false;
            }

            expressionOrNull = new TypeExpression(kind, trimmed, bKey, collection);
            return true;
        }

        private static bool TryUnwrap(string text, string wrapper, out string inner)
        {
            inner = "";
            string prefix = wrapper + "<";
            if (!text.StartsWith(prefix, StringComparison.Ordinal) || !text.EndsWith(">", StringComparison.Ordinal))
            {
                return false;
            }

            inner = text.Substring(prefix.Length, text.Length - prefix.Length - 1).Trim();
            return true;
        }

        private static bool TryRemoveArraySuffix(string text, out string element)
        {
            if (text.EndsWith(ARRAY_SUFFIX, StringComparison.Ordinal))
            {
                element = text.Substring(0, text.Length - ARRAY_SUFFIX.Length).Trim();
                return true;
            }

            element = text;
            return false;
        }
    }
}
