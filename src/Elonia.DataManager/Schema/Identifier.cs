using System.Collections.Generic;

namespace Elonia.DataManager
{
    public static class Identifier
    {
        private static readonly HashSet<string> KEYWORDS = new HashSet<string>
        {
            "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked", "class", "const",
            "continue", "decimal", "default", "delegate", "do", "double", "else", "enum", "event", "explicit", "extern",
            "false", "finally", "fixed", "float", "for", "foreach", "goto", "if", "implicit", "in", "int", "interface",
            "internal", "is", "lock", "long", "namespace", "new", "null", "object", "operator", "out", "override",
            "params", "private", "protected", "public", "readonly", "ref", "return", "sbyte", "sealed", "short",
            "sizeof", "stackalloc", "static", "string", "struct", "switch", "this", "throw", "true", "try", "typeof",
            "uint", "ulong", "unchecked", "unsafe", "ushort", "using", "virtual", "void", "volatile", "while"
        };

        public static bool IsValid(string name)
        {
            if (string.IsNullOrEmpty(name) || KEYWORDS.Contains(name))
            {
                return false;
            }

            for (int index = 0; index < name.Length; index++)
            {
                char character = name[index];
                bool bLetter = (character >= 'a' && character <= 'z') || (character >= 'A' && character <= 'Z') || character == '_';
                bool bDigit = character >= '0' && character <= '9';
                if (!bLetter && !(bDigit && index > 0))
                {
                    return false;
                }
            }

            return true;
        }

        public const string RULE_TEXT = "영문자나 밑줄로 시작하고 영문자, 숫자, 밑줄만 쓴 이름이어야 합니다(C# 예약어 제외).";

        public static string ToParameterName(string fieldName)
        {
            string name = char.ToLowerInvariant(fieldName[0]) + fieldName.Substring(1);
            return KEYWORDS.Contains(name) ? "@" + name : name;
        }
    }
}
