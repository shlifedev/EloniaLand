using System;
using System.Collections.Generic;
using System.Globalization;

namespace Elonia.DataManager
{
    public static class MappingBuilder
    {
        public static List<EnumDefinition> Build(TableSource source, DiagnosticBag diagnostics)
        {
            List<EnumDefinition> definitions = new List<EnumDefinition>();
            int keyIndex = -1;
            EEnumUnderlying underlying = EEnumUnderlying.Int;
            for (int index = 0; index < source.Columns.Count && keyIndex < 0; index++)
            {
                if (TypeExpression.TryParse(source.Columns[index].TypeText, out TypeExpression? expressionOrNull, out string _) && expressionOrNull!.IsKey)
                {
                    keyIndex = index;

                    bool bInteger = expressionOrNull.Kind == ETypeKind.Value && EnumUnderlying.TryParse(expressionOrNull.Name, out underlying);
                    if (!bInteger)
                    {
                        keyIndex = -2;
                    }
                }
            }

            for (int index = 0; index < source.Columns.Count; index++)
            {
                SourceColumn column = source.Columns[index];
                bool bMapping = TypeExpression.TryParse(column.TypeText, out TypeExpression? expressionOrNull, out string _) && expressionOrNull!.Kind == ETypeKind.Mapping;
                if (!bMapping)
                {
                    continue;
                }

                if (keyIndex < 0)
                {
                    diagnostics.AddError(column.TypeCell, "Mapping<...>은 정수 Key가 있는 표에서만 쓸 수 있습니다.",
                        "표의 첫 번째 Key 열을 Key<int>처럼 정수 타입으로 두세요(" + EnumUnderlying.NAME_LIST + ").");
                    continue;
                }

                EnumDefinition? definitionOrNull = BuildEnum(source, index, keyIndex, underlying, expressionOrNull!.Name, diagnostics);
                if (definitionOrNull != null)
                {
                    definitions.Add(definitionOrNull);
                }
            }

            return definitions;
        }

        private static EnumDefinition? BuildEnum(TableSource source, int mappingIndex, int keyIndex, EEnumUnderlying underlying, string enumName, DiagnosticBag diagnostics)
        {
            SourceColumn column = source.Columns[mappingIndex];
            bool bValid = true;
            if (!Identifier.IsValid(enumName))
            {
                diagnostics.AddError(column.TypeCell, "'" + enumName + "'은(는) enum 이름으로 쓸 수 없습니다.", Identifier.RULE_TEXT);
                bValid = false;
            }

            List<EnumMember> members = new List<EnumMember>();
            HashSet<string> names = new HashSet<string>(StringComparer.Ordinal);
            for (int rowIndex = 0; rowIndex < source.Rows.Count; rowIndex++)
            {
                Cell nameCell = source.Rows[rowIndex][mappingIndex];
                Cell keyCell = source.Rows[rowIndex][keyIndex];
                if (nameCell.IsBlank || keyCell.IsBlank)
                {
                    continue;
                }

                string name = nameCell.Trimmed;
                if (!Identifier.IsValid(name))
                {
                    diagnostics.AddError(nameCell, "enum 멤버 이름으로 쓸 수 없습니다.", Identifier.RULE_TEXT);
                    bValid = false;
                    continue;
                }

                if (!names.Add(name))
                {
                    diagnostics.AddError(nameCell, enumName + "에 같은 이름의 멤버가 이미 있습니다.", "이름이 겹치지 않게 고치세요.");
                    bValid = false;
                    continue;
                }

                if (long.TryParse(keyCell.Trimmed, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out long value) && EnumUnderlying.IsInRange(underlying, value))
                {
                    members.Add(new EnumMember(name, value, nameCell));
                }
            }

            if (!bValid)
            {
                return null;
            }

            return new EnumDefinition(enumName, underlying, false, column.CommentCell.Trimmed, column.TypeCell.Origin.ToString(), members);
        }
    }
}
