using System;
using System.Collections.Generic;

namespace Elonia.DataManager
{
    public static class DataValidator
    {
        public static void Validate(IReadOnlyList<TableData> tables, DiagnosticBag diagnostics)
        {
            Dictionary<string, HashSet<object>> firstKeysByTable = new Dictionary<string, HashSet<object>>(StringComparer.Ordinal);
            for (int index = 0; index < tables.Count; index++)
            {
                HashSet<object>? firstKeysOrNull = ValidateKeys(tables[index], diagnostics);
                if (firstKeysOrNull != null)
                {
                    firstKeysByTable.Add(tables[index].Schema.Name, firstKeysOrNull);
                }
            }

            for (int index = 0; index < tables.Count; index++)
            {
                ValidateReferences(tables[index], firstKeysByTable, diagnostics);
            }
        }

        private static HashSet<object>? ValidateKeys(TableData table, DiagnosticBag diagnostics)
        {
            HashSet<object>? firstKeysOrNull = null;
            TableSchema schema = table.Schema;
            for (int columnIndex = 0; columnIndex < schema.Columns.Count; columnIndex++)
            {
                ColumnSchema column = schema.Columns[columnIndex];
                if (!column.IsKey)
                {
                    continue;
                }

                Dictionary<object, Cell> firstCellByKey = new Dictionary<object, Cell>();
                for (int rowIndex = 0; rowIndex < table.Rows.Count; rowIndex++)
                {
                    RowData row = table.Rows[rowIndex];
                    object key = row.Values[columnIndex];
                    if (firstCellByKey.TryGetValue(key, out Cell firstCell))
                    {
                        diagnostics.AddError(row.Cells[columnIndex], "Key(" + column.Field + ")가 " + firstCell.Origin.Address + " 칸과 같습니다. Key는 표 안에서 겹칠 수 없습니다.",
                            "둘 중 하나의 값을 바꾸세요.");
                    }
                    else
                    {
                        firstCellByKey.Add(key, row.Cells[columnIndex]);
                    }
                }

                if (firstKeysOrNull == null)
                {
                    firstKeysOrNull = new HashSet<object>(firstCellByKey.Keys);
                }
            }

            return firstKeysOrNull;
        }

        private static void ValidateReferences(TableData table, Dictionary<string, HashSet<object>> firstKeysByTable, DiagnosticBag diagnostics)
        {
            TableSchema schema = table.Schema;
            for (int columnIndex = 0; columnIndex < schema.Columns.Count; columnIndex++)
            {
                RefType? refTypeOrNull = schema.Columns[columnIndex].Type as RefType;
                if (refTypeOrNull == null)
                {
                    continue;
                }

                if (!firstKeysByTable.TryGetValue(refTypeOrNull.TargetTable, out HashSet<object>? targetKeys))
                {
                    continue;
                }

                for (int rowIndex = 0; rowIndex < table.Rows.Count; rowIndex++)
                {
                    RowData row = table.Rows[rowIndex];
                    RefValue reference = (RefValue)row.Values[columnIndex];
                    if (reference.HasValue && !targetKeys.Contains(reference.KeyOrNull!))
                    {
                        diagnostics.AddError(row.Cells[columnIndex], "표 '" + refTypeOrNull.TargetTable + "'에 이 Key를 가진 행이 없습니다.",
                            "'" + refTypeOrNull.TargetTable + "' 표에 있는 Key 값을 적으세요. 아무것도 가리키지 않으려면 '" + RefType.NONE_TEXT + "'를 적습니다.");
                    }
                }
            }
        }
    }
}
