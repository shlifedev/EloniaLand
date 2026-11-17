using System.Collections.Generic;

namespace Elonia.DataManager
{
    public static class TableSourceReader
    {
        public const int TYPE_ROW = 0;
        public const int FIELD_ROW = 1;
        public const int COMMENT_ROW = 2;
        public const int FIRST_DATA_ROW = 3;

        public static TableSource Read(Sheet sheet, string tableName, DiagnosticBag diagnostics)
        {
            TableSource source = new TableSource(tableName, sheet.Location);

            List<int> columnIndexes = new List<int>();
            List<int> headerlessIndexes = new List<int>();
            for (int columnIndex = 0; columnIndex < sheet.ColumnCount; columnIndex++)
            {
                Cell typeCell = sheet.GetCell(TYPE_ROW, columnIndex);
                Cell fieldCell = sheet.GetCell(FIELD_ROW, columnIndex);
                Cell commentCell = sheet.GetCell(COMMENT_ROW, columnIndex);
                if (SheetRole.IsIgnoreMark(typeCell.Text) || SheetRole.IsIgnoreMark(fieldCell.Text) || SheetRole.IsIgnoreMark(commentCell.Text))
                {
                    continue;
                }

                if (typeCell.IsBlank && fieldCell.IsBlank)
                {
                    headerlessIndexes.Add(columnIndex);
                    continue;
                }

                if (typeCell.IsBlank)
                {
                    diagnostics.AddError(typeCell, "필드 '" + fieldCell.Trimmed + "'의 타입이 비어 있습니다.", "1행에 타입을 적으세요. 예: int, string");
                    continue;
                }

                if (fieldCell.IsBlank)
                {
                    diagnostics.AddError(fieldCell, "타입 '" + typeCell.Trimmed + "'인 열의 필드명이 비어 있습니다.", "2행에 필드명을 적으세요.");
                    continue;
                }

                columnIndexes.Add(columnIndex);
                source.Columns.Add(new SourceColumn(typeCell, fieldCell, commentCell));
            }

            if (source.Columns.Count == 0)
            {
                diagnostics.AddError(sheet.Location, "읽을 열이 없습니다. 1행에 타입, 2행에 필드명이 있어야 합니다.",
                    "1행 타입, 2행 필드명, 3행 주석, 4행부터 데이터 순서로 적으세요. 읽지 않을 시트는 이름 앞에 #를 붙입니다.");
                return source;
            }

            bool bHeaderlessWarned = false;
            for (int rowIndex = FIRST_DATA_ROW; rowIndex < sheet.RowCount; rowIndex++)
            {
                if (SheetRole.IsIgnoreMark(sheet.GetCell(rowIndex, 0).Text))
                {
                    continue;
                }

                if (!bHeaderlessWarned)
                {
                    bHeaderlessWarned = TryWarnHeaderlessValue(sheet, rowIndex, headerlessIndexes, diagnostics);
                }

                Cell[] cells = new Cell[columnIndexes.Count];
                bool bAllBlank = true;
                for (int index = 0; index < cells.Length; index++)
                {
                    cells[index] = sheet.GetCell(rowIndex, columnIndexes[index]);
                    bAllBlank &= cells[index].IsBlank;
                }

                if (!bAllBlank)
                {
                    source.Rows.Add(cells);
                }
            }

            return source;
        }

        private static bool TryWarnHeaderlessValue(Sheet sheet, int rowIndex, List<int> headerlessIndexes, DiagnosticBag diagnostics)
        {
            for (int index = 0; index < headerlessIndexes.Count; index++)
            {
                Cell cell = sheet.GetCell(rowIndex, headerlessIndexes[index]);
                if (!cell.IsBlank)
                {
                    diagnostics.AddWarning(cell, "타입과 필드명이 없는 열에 값이 있습니다. 이 열은 읽지 않습니다.",
                        "읽어야 하는 열이면 1행과 2행을 채우고, 메모라면 헤더 칸을 #로 시작하게 하세요.");
                    return true;
                }
            }

            return false;
        }
    }
}
