using System;
using System.Collections.Generic;

namespace Elonia.DataManager
{
    public sealed class Sheet
    {
        private readonly Cell[][] _rows;

        private readonly bool _isTransposed;

        public string WorkbookName { get; }
        public string Name { get; }
        public int RowCount => _rows.Length;
        public int ColumnCount { get; }

        public Sheet(string workbookName, string name, IReadOnlyList<IReadOnlyList<string?>> rows)
        {
            if (workbookName == null)
            {
                throw new ArgumentNullException(nameof(workbookName));
            }

            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (rows == null)
            {
                throw new ArgumentNullException(nameof(rows));
            }

            WorkbookName = workbookName;
            Name = name;

            int columnCount = 0;
            for (int rowIndex = 0; rowIndex < rows.Count; rowIndex++)
            {
                columnCount = Math.Max(columnCount, rows[rowIndex].Count);
            }

            ColumnCount = columnCount;
            _rows = new Cell[rows.Count][];
            for (int rowIndex = 0; rowIndex < rows.Count; rowIndex++)
            {
                IReadOnlyList<string?> source = rows[rowIndex];
                Cell[] cells = new Cell[columnCount];
                for (int columnIndex = 0; columnIndex < columnCount; columnIndex++)
                {
                    string text = columnIndex < source.Count ? source[columnIndex] ?? "" : "";
                    cells[columnIndex] = new Cell(text, new CellOrigin(workbookName, name, rowIndex + 1, columnIndex + 1));
                }

                _rows[rowIndex] = cells;
            }
        }

        private Sheet(string workbookName, string name, Cell[][] rows, int columnCount, bool bTransposed)
        {
            WorkbookName = workbookName;
            Name = name;
            _rows = rows;
            ColumnCount = columnCount;
            _isTransposed = bTransposed;
        }

        public Cell GetCell(int rowIndex, int columnIndex)
        {
            if (rowIndex < 0 || columnIndex < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(rowIndex), "A cell index must not be negative.");
            }

            if (rowIndex < _rows.Length && columnIndex < ColumnCount)
            {
                return _rows[rowIndex][columnIndex];
            }

            int originRow = _isTransposed ? columnIndex + 1 : rowIndex + 1;
            int originColumn = _isTransposed ? rowIndex + 1 : columnIndex + 1;
            return new Cell("", new CellOrigin(WorkbookName, Name, originRow, originColumn));
        }

        public Sheet Transpose()
        {
            Cell[][] rows = new Cell[ColumnCount][];
            for (int columnIndex = 0; columnIndex < ColumnCount; columnIndex++)
            {
                Cell[] cells = new Cell[_rows.Length];
                for (int rowIndex = 0; rowIndex < _rows.Length; rowIndex++)
                {
                    cells[rowIndex] = _rows[rowIndex][columnIndex];
                }

                rows[columnIndex] = cells;
            }

            return new Sheet(WorkbookName, Name, rows, _rows.Length, !_isTransposed);
        }

        public string Location => WorkbookName + " / " + Name;
    }
}
