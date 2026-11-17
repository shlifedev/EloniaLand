using System.Collections.Generic;
using System.Globalization;

namespace Elonia.DataManager
{
    public static class RowCodeGenerator
    {
        public static string Generate(TableSchema schema, string namespaceName)
        {
            CodeWriter writer = new CodeWriter();
            writer.FileHeader(schema.Location);
            writer.Line("using System;");
            writer.Line("using System.Collections.Generic;");
            writer.Line("using MemoryPack;");
            writer.Line();
            writer.Line("namespace " + namespaceName);
            writer.Open();
            writer.Summary(schema.Name + " 표의 행 하나.");
            writer.Line("[MemoryPackable(SerializeLayout.Explicit)]");
            writer.Line("public sealed partial class " + schema.RowTypeName);
            writer.Open();

            List<KeyValuePair<string, string>> members = new List<KeyValuePair<string, string>>();
            for (int index = 0; index < schema.Columns.Count; index++)
            {
                ColumnSchema column = schema.Columns[index];
                RefType? refTypeOrNull = column.Type as RefType;
                if (refTypeOrNull == null)
                {
                    writer.Summary(column.Comment);
                    WriteMember(writer, members, column.Type.CSharpTypeName, column.Field);
                    continue;
                }

                string hasName = RowMemberNames.GetRefHasName(column.Field);
                string keyName = RowMemberNames.GetRefKeyName(column.Field);
                writer.Summary(column.Field + "이(가) 가리키는 행이 있는가.");
                WriteMember(writer, members, "bool", hasName);
                writer.Summary(column.Field + "이(가) 가리키는 행의 Key. 가리키는 행이 없으면 의미 없는 값이다.");
                WriteMember(writer, members, refTypeOrNull.KeyType.CSharpTypeName, keyName);
                writer.Summary(column.Comment);
                writer.Line("[MemoryPackIgnore]");
                writer.Line("public " + refTypeOrNull.CSharpTypeName + " " + column.Field + " => new " + refTypeOrNull.CSharpTypeName + "(" + hasName + ", " + keyName + ");");
                writer.Line();
            }

            WriteConstructor(writer, schema.RowTypeName, members);
            writer.Close();
            writer.Close();
            return writer.ToString();
        }

        private static void WriteMember(CodeWriter writer, List<KeyValuePair<string, string>> members, string typeName, string name)
        {
            writer.Line("[MemoryPackOrder(" + members.Count.ToString(CultureInfo.InvariantCulture) + ")]");
            writer.Line("public " + typeName + " " + name + " { get; }");
            writer.Line();
            members.Add(new KeyValuePair<string, string>(typeName, name));
        }

        private static void WriteConstructor(CodeWriter writer, string typeName, List<KeyValuePair<string, string>> members)
        {
            List<string> parameters = new List<string>(members.Count);
            for (int index = 0; index < members.Count; index++)
            {
                parameters.Add(members[index].Key + " " + Identifier.ToParameterName(members[index].Value));
            }

            writer.Line("[MemoryPackConstructor]");
            writer.Line("public " + typeName + "(" + string.Join(", ", parameters) + ")");
            writer.Open();
            for (int index = 0; index < members.Count; index++)
            {
                writer.Line(members[index].Value + " = " + Identifier.ToParameterName(members[index].Value) + ";");
            }

            writer.Close();
        }
    }
}
