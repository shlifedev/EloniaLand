using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Elonia.DataManager.Runtime;

namespace Elonia.DataManager
{
    public static class SchemaHasher
    {
        public static string Compute(IReadOnlyList<TableSchema> schemas, IReadOnlyList<EnumDefinition> enums)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append("format ").Append(TableManifest.FORMAT_VERSION.ToString(CultureInfo.InvariantCulture)).Append('\n');
            for (int index = 0; index < schemas.Count; index++)
            {
                TableSchema schema = schemas[index];
                builder.Append("table ").Append(schema.Name).Append('\n');
                for (int columnIndex = 0; columnIndex < schema.Columns.Count; columnIndex++)
                {
                    ColumnSchema column = schema.Columns[columnIndex];
                    builder.Append("  column ").Append(column.Field).Append(' ').Append(column.Type.CanonicalName);
                    builder.Append(column.IsKey ? " key" : "").Append('\n');
                }
            }

            for (int index = 0; index < enums.Count; index++)
            {
                EnumDefinition definition = enums[index];
                builder.Append("enum ").Append(definition.Name).Append(' ').Append(EnumUnderlying.GetCSharpName(definition.Underlying));
                builder.Append(definition.IsFlags ? " flags" : "").Append('\n');
                for (int memberIndex = 0; memberIndex < definition.Members.Count; memberIndex++)
                {
                    EnumMember member = definition.Members[memberIndex];
                    builder.Append("  member ").Append(member.Name).Append(' ').Append(member.Value.ToString(CultureInfo.InvariantCulture)).Append('\n');
                }
            }

            return TableHash.Compute(builder.ToString());
        }
    }
}
