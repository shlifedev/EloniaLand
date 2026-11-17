using System.Collections.Generic;

namespace Elonia.DataManager
{
    public static class EnumSheetReader
    {
        private const string FLAGS_PREFIX = "Flags<";
        private const string FLAGS_SUFFIX = ">";

        public static List<EnumDefinition> Read(Sheet sheet, DiagnosticBag diagnostics)
        {
            List<EnumDefinition> definitions = new List<EnumDefinition>();
            int columnIndex = 0;
            while (columnIndex < sheet.ColumnCount)
            {
                Cell nameCell = sheet.GetCell(TableSourceReader.FIELD_ROW, columnIndex);
                Cell typeCell = sheet.GetCell(TableSourceReader.TYPE_ROW, columnIndex);
                Cell commentCell = sheet.GetCell(TableSourceReader.COMMENT_ROW, columnIndex);
                bool bIgnored = SheetRole.IsIgnoreMark(nameCell.Text) || SheetRole.IsIgnoreMark(typeCell.Text) || SheetRole.IsIgnoreMark(commentCell.Text);
                if (bIgnored || nameCell.IsBlank)
                {
                    columnIndex++;
                    continue;
                }

                bool bHasValueColumn = columnIndex + 1 < sheet.ColumnCount && sheet.GetCell(TableSourceReader.FIELD_ROW, columnIndex + 1).IsBlank;
                int valueColumnIndex = bHasValueColumn ? columnIndex + 1 : -1;
                EnumDefinition? definitionOrNull = ReadEnum(sheet, columnIndex, valueColumnIndex, diagnostics);
                if (definitionOrNull != null)
                {
                    definitions.Add(definitionOrNull);
                }

                columnIndex += bHasValueColumn ? 2 : 1;
            }

            return definitions;
        }

        private static EnumDefinition? ReadEnum(Sheet sheet, int nameColumnIndex, int valueColumnIndex, DiagnosticBag diagnostics)
        {
            Cell nameCell = sheet.GetCell(TableSourceReader.FIELD_ROW, nameColumnIndex);
            Cell typeCell = sheet.GetCell(TableSourceReader.TYPE_ROW, nameColumnIndex);
            string name = nameCell.Trimmed;
            bool bValid = true;
            if (!Identifier.IsValid(name))
            {
                diagnostics.AddError(nameCell, "enum 이름으로 쓸 수 없습니다.", Identifier.RULE_TEXT);
                bValid = false;
            }

            if (!TryReadUnderlying(typeCell, out EEnumUnderlying underlying, out bool bFlags))
            {
                diagnostics.AddError(typeCell, "enum의 정수 타입을 이해하지 못했습니다.",
                    "비워 두거나(int) " + EnumUnderlying.NAME_LIST + " 중 하나를 적으세요. 여러 값을 합쳐 쓰는 enum은 Flags<int>처럼 적습니다.");
                bValid = false;
            }

            List<EnumMember> members = new List<EnumMember>();
            Dictionary<string, long> valuesByName = new Dictionary<string, long>(System.StringComparer.Ordinal);
            long nextValue = 0;
            for (int rowIndex = TableSourceReader.FIRST_DATA_ROW; rowIndex < sheet.RowCount; rowIndex++)
            {
                Cell memberCell = sheet.GetCell(rowIndex, nameColumnIndex);
                if (memberCell.IsBlank || SheetRole.IsIgnoreMark(memberCell.Text))
                {
                    continue;
                }

                string memberName = memberCell.Trimmed;
                if (!Identifier.IsValid(memberName))
                {
                    diagnostics.AddError(memberCell, "enum 멤버 이름으로 쓸 수 없습니다.", Identifier.RULE_TEXT + " 값은 오른쪽 열에 따로 적습니다.");
                    bValid = false;
                    continue;
                }

                if (valuesByName.ContainsKey(memberName))
                {
                    diagnostics.AddError(memberCell, name + "에 같은 이름의 멤버가 이미 있습니다.", "멤버 이름이 겹치지 않게 고치세요.");
                    bValid = false;
                    continue;
                }

                long value = nextValue;
                Cell valueCell = valueColumnIndex >= 0 ? sheet.GetCell(rowIndex, valueColumnIndex) : memberCell;
                if (valueColumnIndex >= 0 && !valueCell.IsBlank)
                {
                    if (!EnumExpression.TryEvaluate(valueCell.Trimmed, valuesByName.TryGetValue, out value, out string reason))
                    {
                        diagnostics.AddError(valueCell, "enum 멤버의 값을 계산하지 못했습니다. " + reason, "정수나 식을 적으세요. 예: 3, 0x10, 1 << 2, Read | Write");
                        bValid = false;
                        continue;
                    }
                }

                if (!EnumUnderlying.IsInRange(underlying, value))
                {
                    diagnostics.AddError(valueCell, "값 " + value + "이(가) " + EnumUnderlying.GetCSharpName(underlying) + "에 들어가지 않습니다.",
                        "값을 줄이거나 1행의 정수 타입을 더 큰 것으로 바꾸세요.");
                    bValid = false;
                    continue;
                }

                valuesByName.Add(memberName, value);
                members.Add(new EnumMember(memberName, value, memberCell));
                nextValue = value + 1;
            }

            if (members.Count == 0 && bValid)
            {
                diagnostics.AddError(nameCell, "enum에 멤버가 하나도 없습니다.", "4행부터 멤버 이름을 적으세요.");
                bValid = false;
            }

            if (!bValid)
            {
                return null;
            }

            string comment = sheet.GetCell(TableSourceReader.COMMENT_ROW, nameColumnIndex).Trimmed;
            return new EnumDefinition(name, underlying, bFlags, comment, nameCell.Origin.ToString(), members);
        }

        private static bool TryReadUnderlying(Cell typeCell, out EEnumUnderlying underlying, out bool bFlags)
        {
            string text = typeCell.Trimmed;
            bFlags = text.StartsWith(FLAGS_PREFIX, System.StringComparison.Ordinal) && text.EndsWith(FLAGS_SUFFIX, System.StringComparison.Ordinal);
            if (bFlags)
            {
                text = text.Substring(FLAGS_PREFIX.Length, text.Length - FLAGS_PREFIX.Length - FLAGS_SUFFIX.Length).Trim();
            }

            if (text.Length == 0)
            {
                underlying = EEnumUnderlying.Int;
                return true;
            }

            return EnumUnderlying.TryParse(text, out underlying);
        }
    }
}
