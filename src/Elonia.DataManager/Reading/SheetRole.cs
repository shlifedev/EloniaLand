using System;

namespace Elonia.DataManager
{
    public enum ESheetKind
    {
        Ignored,

        Table,

        Config,

        ChildRows,

        ColumnExtension,

        Enums,

        Invalid
    }

    public readonly struct SheetRole
    {
        private const char SEPARATOR = '.';
        private const string ENUMS_NAME = "Enums";
        private const string CONFIG_NAME = "Config";
        private const string COLUMN_EXTENSION_NAME = "Col";

        public ESheetKind Kind { get; }

        public string TableName { get; }

        private SheetRole(ESheetKind kind, string tableName)
        {
            Kind = kind;
            TableName = tableName;
        }

        public static SheetRole FromName(string sheetName)
        {
            string name = sheetName.Trim();
            if (name.Length == 0)
            {
                return new SheetRole(ESheetKind.Invalid, "");
            }

            if (IsIgnoreMark(name))
            {
                return new SheetRole(ESheetKind.Ignored, "");
            }

            string[] parts = name.Split(SEPARATOR);
            string last = parts[parts.Length - 1];
            if (last == ENUMS_NAME)
            {
                return new SheetRole(ESheetKind.Enums, "");
            }

            if (parts.Length == 1)
            {
                return new SheetRole(ESheetKind.Table, name);
            }

            if (parts.Length == 2 && last == CONFIG_NAME)
            {
                return new SheetRole(ESheetKind.Config, parts[0]);
            }

            if (parts.Length == 2)
            {
                return new SheetRole(ESheetKind.ChildRows, parts[0]);
            }

            if (parts.Length == 3 && parts[1] == COLUMN_EXTENSION_NAME)
            {
                return new SheetRole(ESheetKind.ColumnExtension, parts[0]);
            }

            return new SheetRole(ESheetKind.Invalid, "");
        }

        public const string NAME_RULE_TEXT = "시트 이름은 '표', '표.Config', '표.자식이름', '표.Col.이름', 'Enums', '이름.Enums' 중 하나여야 합니다. 읽지 않을 시트는 이름 앞에 #나 ~를 붙입니다.";

        public static bool IsIgnoreMark(string text)
        {
            string trimmed = text.TrimStart();
            return trimmed.StartsWith("#", StringComparison.Ordinal) || trimmed.StartsWith("~", StringComparison.Ordinal);
        }
    }
}
