using System.Collections.Generic;

namespace Elonia.DataManager
{
    public sealed class ColumnSchema
    {
        private readonly List<int> _sourceIndexes;

        public string Field { get; }
        public ColumnType Type { get; }
        public bool IsKey { get; }
        public string Comment { get; }

        public Cell FieldCell { get; }

        public IReadOnlyList<int> SourceIndexes => _sourceIndexes;

        public ColumnSchema(string field, ColumnType type, bool bKey, string comment, Cell fieldCell, IEnumerable<int> sourceIndexes)
        {
            Field = field;
            Type = type;
            IsKey = bKey;
            Comment = comment;
            FieldCell = fieldCell;
            _sourceIndexes = new List<int>(sourceIndexes);
        }
    }

    public sealed class TableSchema
    {
        private readonly List<ColumnSchema> _columns;
        private readonly List<ColumnSchema> _keys = new List<ColumnSchema>();

        public string Name { get; }

        public string Location { get; }

        public IReadOnlyList<ColumnSchema> Columns => _columns;

        public IReadOnlyList<ColumnSchema> Keys => _keys;

        public ColumnSchema? FirstKeyOrNull => _keys.Count > 0 ? _keys[0] : null;

        public TableSchema(string name, string location, IEnumerable<ColumnSchema> columns)
        {
            Name = name;
            Location = location;
            _columns = new List<ColumnSchema>(columns);
            for (int index = 0; index < _columns.Count; index++)
            {
                if (_columns[index].IsKey)
                {
                    _keys.Add(_columns[index]);
                }
            }
        }

        public string RowTypeName => Name + "Row";
        public string TableTypeName => Name + "Table";
        public string RefTypeName => Name + "Ref";
    }

    public sealed class RowData
    {
        public object[] Values { get; }

        public Cell[] Cells { get; }

        public RowData(object[] values, Cell[] cells)
        {
            Values = values;
            Cells = cells;
        }
    }

    public sealed class TableData
    {
        public TableSchema Schema { get; }
        public List<RowData> Rows { get; } = new List<RowData>();

        public TableData(TableSchema schema)
        {
            Schema = schema;
        }
    }
}
