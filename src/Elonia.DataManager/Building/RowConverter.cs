using System.Collections.Generic;

namespace Elonia.DataManager
{
    public static class RowConverter
    {
        public static TableData Convert(TableSource source, TableSchema schema, DiagnosticBag diagnostics)
        {
            TableData data = new TableData(schema);
            ColumnSchema? firstKeyOrNull = schema.FirstKeyOrNull;
            for (int rowIndex = 0; rowIndex < source.Rows.Count; rowIndex++)
            {
                Cell[] cells = source.Rows[rowIndex];

                if (firstKeyOrNull != null && cells[firstKeyOrNull.SourceIndexes[0]].IsBlank)
                {
                    diagnostics.AddWarning(cells[firstKeyOrNull.SourceIndexes[0]],
                        "Key(" + firstKeyOrNull.Field + ")가 비어 있어 이 행을 건너뜁니다. 같은 행의 다른 칸에는 값이 있습니다.",
                        "읽어야 하는 행이면 Key를 적고, 아니면 A열에 ~를 적거나 행을 지우세요.");
                    continue;
                }

                object[] values = new object[schema.Columns.Count];
                Cell[] valueCells = new Cell[schema.Columns.Count];
                for (int columnIndex = 0; columnIndex < schema.Columns.Count; columnIndex++)
                {
                    ColumnSchema column = schema.Columns[columnIndex];
                    valueCells[columnIndex] = cells[column.SourceIndexes[0]];
                    values[columnIndex] = column.SourceIndexes.Count == 1
                        ? ConvertCell(column, cells[column.SourceIndexes[0]], diagnostics)
                        : ConvertMergedCells(column, cells, diagnostics);
                }

                data.Rows.Add(new RowData(values, valueCells));
            }

            return data;
        }

        private static object ConvertCell(ColumnSchema column, Cell cell, DiagnosticBag diagnostics)
        {
            if (column.Type.TryParse(cell.Text, out object value, out string reason))
            {
                return value;
            }

            diagnostics.AddError(cell, column.Field + "(" + column.Type.CanonicalName + "): " + reason, GetExampleFix(column.Type));
            return column.Type.EmptyValue;
        }

        private static object ConvertMergedCells(ColumnSchema column, Cell[] cells, DiagnosticBag diagnostics)
        {
            ColumnType elementType = column.Type.ElementTypeOrNull!;
            List<object> elements = new List<object>();
            for (int index = 0; index < column.SourceIndexes.Count; index++)
            {
                Cell cell = cells[column.SourceIndexes[index]];
                if (!CollectionColumnType.TryParseElements(elementType, cell.Text, elements, out string reason))
                {
                    diagnostics.AddError(cell, column.Field + "(" + column.Type.CanonicalName + "): " + reason, GetExampleFix(elementType));
                }
            }

            return column.Type.CreateCollection(elements);
        }

        private static string? GetExampleFix(ColumnType type)
        {
            return type.Example.Length == 0 ? null : "예: " + type.Example;
        }
    }
}
