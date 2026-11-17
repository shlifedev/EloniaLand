using System;
using System.Buffers;
using MemoryPack;

namespace Elonia.DataManager
{
    public sealed class RefValue
    {
        public static readonly RefValue NONE = new RefValue(false, null);

        public bool HasValue { get; }

        public object? KeyOrNull { get; }

        private RefValue(bool bHasValue, object? keyOrNull)
        {
            HasValue = bHasValue;
            KeyOrNull = keyOrNull;
        }

        public static RefValue To(object key)
        {
            return new RefValue(true, key);
        }
    }

    public sealed class RefType : ColumnType
    {
        public const string NONE_TEXT = "~";

        public string TargetTable { get; }

        public ColumnType KeyType { get; }

        public override string CanonicalName => "Ref<" + TargetTable + ">";
        public override string CSharpTypeName => TargetTable + "Ref";
        public override object EmptyValue => RefValue.NONE;
        public override string Example => KeyType.Example;

        public RefType(string targetTable, ColumnType keyType)
        {
            if (string.IsNullOrEmpty(targetTable))
            {
                throw new ArgumentException("A reference needs a target table.", nameof(targetTable));
            }

            if (keyType == null)
            {
                throw new ArgumentNullException(nameof(keyType));
            }

            TargetTable = targetTable;
            KeyType = keyType;
        }

        public override bool TryParse(string text, out object value, out string reason)
        {
            value = RefValue.NONE;
            reason = "";
            string trimmed = text.Trim();
            if (trimmed == NONE_TEXT)
            {
                return true;
            }

            if (trimmed.Length == 0)
            {
                reason = "비어 있습니다. 아무것도 가리키지 않으려면 '" + NONE_TEXT + "'를 적습니다.";
                return false;
            }

            if (!KeyType.TryParse(trimmed, out object key, out reason))
            {
                return false;
            }

            value = RefValue.To(key);
            return true;
        }

        public override void Write(ref MemoryPackWriter<ArrayBufferWriter<byte>> writer, object value)
        {
            RefValue reference = (RefValue)value;
            writer.WriteUnmanaged(reference.HasValue);
            KeyType.Write(ref writer, reference.HasValue ? reference.KeyOrNull! : KeyType.EmptyValue);
        }
    }
}
