using System.Collections.Generic;

namespace Elonia.DataManager
{
    public sealed class SourceColumn
    {
        public Cell TypeCell { get; }
        public Cell FieldCell { get; }
        public Cell CommentCell { get; }

        public string TypeText => TypeCell.Trimmed;
        public string Field => FieldCell.Trimmed;

        public SourceColumn(Cell typeCell, Cell fieldCell, Cell commentCell)
        {
            TypeCell = typeCell;
            FieldCell = fieldCell;
            CommentCell = commentCell;
        }
    }

    public sealed class TableSource
    {
        public string Name { get; }

        public string Location { get; }

        public List<SourceColumn> Columns { get; } = new List<SourceColumn>();

        public List<Cell[]> Rows { get; } = new List<Cell[]>();

        public TableSource(string name, string location)
        {
            Name = name;
            Location = location;
        }
    }
}
