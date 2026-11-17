using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Elonia.DataManager.Runtime
{
    public sealed class TableManifest
    {
        public const string FILE_NAME = "tables.manifest";
        public const string DATA_EXTENSION = ".bytes";

        public const int FORMAT_VERSION = 1;

        private const string HEADER_KEYWORD = "elonia-tables";
        private const string SCHEMA_KEYWORD = "schema";
        private const string TABLE_KEYWORD = "table";

        private readonly List<TableManifestEntry> _tables;

        public string SchemaHash { get; }

        public IReadOnlyList<TableManifestEntry> Tables => _tables;

        public TableManifest(string schemaHash, IEnumerable<TableManifestEntry> tables)
        {
            if (string.IsNullOrEmpty(schemaHash))
            {
                throw new ArgumentException("A manifest needs a schema hash.", nameof(schemaHash));
            }

            if (tables == null)
            {
                throw new ArgumentNullException(nameof(tables));
            }

            SchemaHash = schemaHash;
            _tables = new List<TableManifestEntry>(tables);
            _tables.Sort(CompareByName);
        }

        public static string GetDataFileName(string tableName)
        {
            return tableName + DATA_EXTENSION;
        }

        public TableManifestEntry GetTable(string tableName)
        {
            for (int index = 0; index < _tables.Count; index++)
            {
                if (_tables[index].Name == tableName)
                {
                    return _tables[index];
                }
            }

            throw new TableLoadException(
                $"The manifest has no table '{tableName}'. The generated code and the data files are from different builds. Run the data manager again and ship both together.");
        }

        public void ThrowIfSchemaDiffers(string expectedSchemaHash)
        {
            if (SchemaHash != expectedSchemaHash)
            {
                throw new TableLoadException(
                    $"The table data does not match the generated code (schema {SchemaHash}, code expects {expectedSchemaHash}). Run the data manager again and ship the generated code and the data files together.");
            }
        }

        public string ToText()
        {
            StringBuilder builder = new StringBuilder();
            builder.Append(HEADER_KEYWORD).Append(' ').Append(FORMAT_VERSION.ToString(CultureInfo.InvariantCulture)).Append('\n');
            builder.Append(SCHEMA_KEYWORD).Append(' ').Append(SchemaHash).Append('\n');
            for (int index = 0; index < _tables.Count; index++)
            {
                TableManifestEntry table = _tables[index];
                builder.Append(TABLE_KEYWORD).Append(' ').Append(table.Name)
                    .Append(' ').Append(table.RowCount.ToString(CultureInfo.InvariantCulture))
                    .Append(' ').Append(table.FileHash).Append('\n');
            }

            return builder.ToString();
        }

        public static TableManifest Parse(string text)
        {
            if (text == null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            string[] lines = text.Split('\n');
            bool bHeaderRead = false;
            string? schemaHashOrNull = null;
            List<TableManifestEntry> tables = new List<TableManifestEntry>();
            for (int index = 0; index < lines.Length; index++)
            {
                string line = lines[index].Trim();
                if (line.Length == 0)
                {
                    continue;
                }

                string[] parts = line.Split(' ');
                if (!bHeaderRead)
                {
                    ReadHeader(parts, line);
                    bHeaderRead = true;
                }
                else if (parts[0] == SCHEMA_KEYWORD && parts.Length == 2)
                {
                    schemaHashOrNull = parts[1];
                }
                else if (parts[0] == TABLE_KEYWORD && parts.Length == 4
                    && int.TryParse(parts[2], NumberStyles.None, CultureInfo.InvariantCulture, out int rowCount))
                {
                    tables.Add(new TableManifestEntry(parts[1], rowCount, parts[3]));
                }
                else
                {
                    throw new TableLoadException($"The manifest line {index + 1} is not understood: '{line}'.");
                }
            }

            if (!bHeaderRead || schemaHashOrNull == null)
            {
                throw new TableLoadException("The manifest is empty or has no schema line.");
            }

            return new TableManifest(schemaHashOrNull, tables);
        }

        private static void ReadHeader(string[] parts, string line)
        {
            if (parts.Length != 2 || parts[0] != HEADER_KEYWORD
                || !int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out int version))
            {
                throw new TableLoadException($"This is not a table manifest. The first line is '{line}'.");
            }

            if (version != FORMAT_VERSION)
            {
                throw new TableLoadException(
                    $"The manifest format is version {version}, but this runtime reads version {FORMAT_VERSION}. Run the data manager that matches this runtime.");
            }
        }

        private static int CompareByName(TableManifestEntry left, TableManifestEntry right)
        {
            return string.CompareOrdinal(left.Name, right.Name);
        }
    }

    public readonly struct TableManifestEntry
    {
        public string Name { get; }
        public int RowCount { get; }

        public string FileHash { get; }

        public TableManifestEntry(string name, int rowCount, string fileHash)
        {
            Name = name;
            RowCount = rowCount;
            FileHash = fileHash;
        }
    }
}
