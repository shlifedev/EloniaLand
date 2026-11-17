namespace Elonia.DataManager
{
    public static class TableCodeGenerator
    {
        public static string Generate(TableSchema schema, string namespaceName)
        {
            string rowType = schema.RowTypeName;
            CodeWriter writer = new CodeWriter();
            writer.FileHeader(schema.Location);
            writer.Line("using System;");
            writer.Line("using System.Collections.Generic;");
            writer.Line("using System.Diagnostics.CodeAnalysis;");
            writer.Line();
            writer.Line("namespace " + namespaceName);
            writer.Open();
            writer.Summary(schema.Name + " 표.");
            writer.Line("public sealed partial class " + schema.TableTypeName);
            writer.Open();
            writer.Line("public const string TABLE_NAME = \"" + schema.Name + "\";");
            writer.Line();
            writer.Line("private readonly " + rowType + "[] _rows;");
            for (int index = 0; index < schema.Keys.Count; index++)
            {
                ColumnSchema key = schema.Keys[index];
                writer.Line("private readonly Dictionary<" + key.Type.CSharpTypeName + ", " + rowType + "> " + GetMapName(key) + ";");
            }

            writer.Line();
            writer.Summary("모든 행. 엑셀에 적힌 순서다.");
            writer.Line("public IReadOnlyList<" + rowType + "> Rows => _rows;");
            writer.Line("public int Count => _rows.Length;");
            writer.Line();
            WriteConstructor(writer, schema);
            for (int index = 0; index < schema.Keys.Count; index++)
            {
                WriteLookups(writer, schema, schema.Keys[index], index == 0 ? "" : "By" + schema.Keys[index].Field);
            }

            if (schema.FirstKeyOrNull != null)
            {
                WriteRefLookups(writer, schema);
            }

            writer.Close();
            if (schema.FirstKeyOrNull != null)
            {
                writer.Line();
                WriteRefStruct(writer, schema, schema.FirstKeyOrNull);
            }

            writer.Close();
            return writer.ToString();
        }

        private static string GetMapName(ColumnSchema key)
        {
            return "_rowsBy" + key.Field;
        }

        private static void WriteConstructor(CodeWriter writer, TableSchema schema)
        {
            writer.Line("public " + schema.TableTypeName + "(" + schema.RowTypeName + "[] rows)");
            writer.Open();
            writer.Line("_rows = rows;");
            for (int index = 0; index < schema.Keys.Count; index++)
            {
                ColumnSchema key = schema.Keys[index];
                writer.Line(GetMapName(key) + " = new Dictionary<" + key.Type.CSharpTypeName + ", " + schema.RowTypeName + ">(rows.Length);");
            }

            if (schema.Keys.Count > 0)
            {
                writer.Line("for (int index = 0; index < rows.Length; index++)");
                writer.Open();
                for (int index = 0; index < schema.Keys.Count; index++)
                {
                    writer.Line(GetMapName(schema.Keys[index]) + ".Add(rows[index]." + schema.Keys[index].Field + ", rows[index]);");
                }

                writer.Close();
            }

            writer.Close();
            writer.Line();
        }

        private static void WriteLookups(CodeWriter writer, TableSchema schema, ColumnSchema key, string suffix)
        {
            string rowType = schema.RowTypeName;
            string keyType = key.Type.CSharpTypeName;
            string parameter = Identifier.ToParameterName(key.Field);
            string map = GetMapName(key);

            writer.Summary(key.Field + "(으)로 행을 찾는다. 없으면 예외를 던진다.");
            writer.Line("public " + rowType + " Get" + suffix + "(" + keyType + " " + parameter + ")");
            writer.Open();
            writer.Line("if (" + map + ".TryGetValue(" + parameter + ", out " + rowType + "? row) && row != null)");
            writer.Open();
            writer.Line("return row;");
            writer.Close();
            writer.Line();
            writer.Line("throw new KeyNotFoundException(\"The table '" + schema.Name + "' has no row whose " + key.Field + " is '\" + " + parameter + " + \"'.\");");
            writer.Close();
            writer.Line();

            writer.Line("public bool TryGet" + suffix + "(" + keyType + " " + parameter + ", [NotNullWhen(true)] out " + rowType + "? row)");
            writer.Open();
            writer.Line("return " + map + ".TryGetValue(" + parameter + ", out row) && row != null;");
            writer.Close();
            writer.Line();

            writer.Line("public bool Contains" + suffix + "(" + keyType + " " + parameter + ")");
            writer.Open();
            writer.Line("return " + map + ".ContainsKey(" + parameter + ");");
            writer.Close();
            writer.Line();
        }

        private static void WriteRefLookups(CodeWriter writer, TableSchema schema)
        {
            string rowType = schema.RowTypeName;
            writer.Summary("참조가 가리키는 행을 찾는다. 아무것도 가리키지 않는 참조면 예외를 던진다.");
            writer.Line("public " + rowType + " Get(" + schema.RefTypeName + " reference)");
            writer.Open();
            writer.Line("if (!reference.HasValue)");
            writer.Open();
            writer.Line("throw new InvalidOperationException(\"The reference to the table '" + schema.Name + "' points to no row.\");");
            writer.Close();
            writer.Line();
            writer.Line("return Get(reference.Key);");
            writer.Close();
            writer.Line();

            writer.Line("public bool TryGet(" + schema.RefTypeName + " reference, [NotNullWhen(true)] out " + rowType + "? row)");
            writer.Open();
            writer.Line("if (!reference.HasValue)");
            writer.Open();
            writer.Line("row = null;");
            writer.Line("return false;");
            writer.Close();
            writer.Line();
            writer.Line("return TryGet(reference.Key, out row);");
            writer.Close();
        }

        private static void WriteRefStruct(CodeWriter writer, TableSchema schema, ColumnSchema firstKey)
        {
            string keyType = firstKey.Type.CSharpTypeName;
            writer.Summary(schema.Name + " 표의 행 하나를 Key로 가리킨다. 아무것도 가리키지 않을 수 있다.");
            writer.Line("public readonly struct " + schema.RefTypeName);
            writer.Open();
            writer.Line("public bool HasValue { get; }");
            writer.Line();
            writer.Summary("가리키는 행의 " + firstKey.Field + ". HasValue가 false면 의미 없는 값이다.");
            writer.Line("public " + keyType + " Key { get; }");
            writer.Line();
            writer.Line("public " + schema.RefTypeName + "(bool bHasValue, " + keyType + " key)");
            writer.Open();
            writer.Line("HasValue = bHasValue;");
            writer.Line("Key = key;");
            writer.Close();
            writer.Close();
        }
    }
}
