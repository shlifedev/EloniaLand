using System.Collections.Generic;

namespace Elonia.DataManager
{
    public static class TablesCodeGenerator
    {
        public static string Generate(IReadOnlyList<TableSchema> schemas, string namespaceName, string className, string schemaHash)
        {
            CodeWriter writer = new CodeWriter();
            writer.FileHeader("모든 표");
            writer.Line("using System;");
            writer.Line("using Elonia.DataManager.Runtime;");
            writer.Line();
            writer.Line("namespace " + namespaceName);
            writer.Open();
            writer.Summary("엑셀에서 만든 모든 표. Load로 한 번 읽은 뒤에는 바뀌지 않는다.");
            writer.Line("public sealed partial class " + className);
            writer.Open();
            writer.Summary("이 코드를 만들 때의 표 모양. 데이터의 매니페스트와 다르면 Load가 실패한다.");
            writer.Line("public const string SCHEMA_HASH = \"" + schemaHash + "\";");
            writer.Line();
            for (int index = 0; index < schemas.Count; index++)
            {
                writer.Line("public " + schemas[index].TableTypeName + " " + schemas[index].Name + " { get; }");
            }

            writer.Line();
            WriteConstructor(writer, schemas, className);
            writer.Line();
            WriteLoad(writer, schemas, className);
            writer.Close();
            writer.Close();
            return writer.ToString();
        }

        private static void WriteConstructor(CodeWriter writer, IReadOnlyList<TableSchema> schemas, string className)
        {
            List<string> parameters = new List<string>(schemas.Count);
            for (int index = 0; index < schemas.Count; index++)
            {
                parameters.Add(schemas[index].TableTypeName + " " + Identifier.ToParameterName(schemas[index].Name));
            }

            writer.Line("public " + className + "(" + string.Join(", ", parameters) + ")");
            writer.Open();
            for (int index = 0; index < schemas.Count; index++)
            {
                writer.Line(schemas[index].Name + " = " + Identifier.ToParameterName(schemas[index].Name) + ";");
            }

            writer.Close();
        }

        private static void WriteLoad(CodeWriter writer, IReadOnlyList<TableSchema> schemas, string className)
        {
            writer.Summary("표를 전부 읽는다. manifestText는 tables.manifest의 내용이고, readFile은 파일 이름(예: Item.bytes)을 받아 내용을 돌려준다. 하나라도 읽지 못하면 TableLoadException을 던지고 아무것도 돌려주지 않는다.");
            writer.Line("public static " + className + " Load(string manifestText, Func<string, byte[]> readFile)");
            writer.Open();
            writer.Line("TableManifest manifest = TableManifest.Parse(manifestText);");
            writer.Line("manifest.ThrowIfSchemaDiffers(SCHEMA_HASH);");
            List<string> arguments = new List<string>(schemas.Count);
            for (int index = 0; index < schemas.Count; index++)
            {
                TableSchema schema = schemas[index];
                string local = Identifier.ToParameterName(schema.Name);
                writer.Line(schema.TableTypeName + " " + local + " = new " + schema.TableTypeName + "(TableFile.ReadRows<" + schema.RowTypeName + ">(manifest, " + schema.TableTypeName + ".TABLE_NAME, readFile));");
                arguments.Add(local);
            }

            writer.Line("return new " + className + "(" + string.Join(", ", arguments) + ");");
            writer.Close();
        }
    }
}
