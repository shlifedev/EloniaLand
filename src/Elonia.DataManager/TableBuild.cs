using System;
using System.Collections.Generic;
using Elonia.DataManager.Runtime;

namespace Elonia.DataManager
{
    public sealed class TableBuildOptions
    {
        public string Namespace { get; set; } = "Elonia.Data";

        public string TablesClassName { get; set; } = "GameTables";

        public TypeRegistry Registry { get; set; } = TypeRegistry.CreateDefault();
    }

    public sealed class TableBuildResult
    {
        public DiagnosticBag Diagnostics { get; }

        public IReadOnlyList<Workbook> Workbooks { get; }

        public GeneratedOutput? OutputOrNull { get; }

        public IReadOnlyList<TableData> Tables { get; }
        public IReadOnlyList<EnumDefinition> Enums { get; }
        public bool IsSuccess => OutputOrNull != null;

        public TableBuildResult(DiagnosticBag diagnostics, IReadOnlyList<Workbook> workbooks, GeneratedOutput? outputOrNull, IReadOnlyList<TableData> tables, IReadOnlyList<EnumDefinition> enums)
        {
            Diagnostics = diagnostics;
            Workbooks = workbooks;
            OutputOrNull = outputOrNull;
            Tables = tables;
            Enums = enums;
        }
    }

    public static class TableBuild
    {
        public static TableBuildResult Run(IWorkbookSource source, TableBuildOptions options)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            DiagnosticBag diagnostics = new DiagnosticBag();
            ValidateOptions(options, diagnostics);

            IReadOnlyList<Workbook> workbooks = source.Load(diagnostics);

            Dictionary<string, TableSource> sourcesByName = new Dictionary<string, TableSource>(StringComparer.Ordinal);
            List<TableSource> sources = new List<TableSource>();
            List<KeyValuePair<Sheet, string>> childSheets = new List<KeyValuePair<Sheet, string>>();
            List<KeyValuePair<Sheet, string>> extensionSheets = new List<KeyValuePair<Sheet, string>>();
            Dictionary<string, EnumDefinition> enums = new Dictionary<string, EnumDefinition>(StringComparer.Ordinal);
            for (int workbookIndex = 0; workbookIndex < workbooks.Count; workbookIndex++)
            {
                IReadOnlyList<Sheet> sheets = workbooks[workbookIndex].Sheets;
                for (int sheetIndex = 0; sheetIndex < sheets.Count; sheetIndex++)
                {
                    Sheet sheet = sheets[sheetIndex];
                    SheetRole role = SheetRole.FromName(sheet.Name);
                    switch (role.Kind)
                    {
                        case ESheetKind.Ignored:
                            break;
                        case ESheetKind.Invalid:
                            diagnostics.AddError(sheet.Location, "시트 이름을 이해하지 못했습니다.", SheetRole.NAME_RULE_TEXT);
                            break;
                        case ESheetKind.Enums:
                            AddEnums(EnumSheetReader.Read(sheet, diagnostics), enums, diagnostics);
                            break;
                        case ESheetKind.ChildRows:
                            childSheets.Add(new KeyValuePair<Sheet, string>(sheet, role.TableName));
                            break;
                        case ESheetKind.ColumnExtension:
                            extensionSheets.Add(new KeyValuePair<Sheet, string>(sheet, role.TableName));
                            break;
                        default:
                            AddTable(sheet, role, sourcesByName, sources, diagnostics);
                            break;
                    }
                }
            }

            for (int index = 0; index < childSheets.Count; index++)
            {
                TableSource? parentOrNull = FindParentOrNull(childSheets[index], sourcesByName, diagnostics);
                if (parentOrNull != null)
                {
                    ShapeMerger.AppendChildRows(parentOrNull, TableSourceReader.Read(childSheets[index].Key, parentOrNull.Name, diagnostics), diagnostics);
                }
            }

            for (int index = 0; index < sources.Count; index++)
            {
                AddEnums(MappingBuilder.Build(sources[index], diagnostics), enums, diagnostics);
            }

            TypeResolver resolver = new TypeResolver(options.Registry, enums);
            for (int index = 0; index < extensionSheets.Count; index++)
            {
                TableSource? parentOrNull = FindParentOrNull(extensionSheets[index], sourcesByName, diagnostics);
                if (parentOrNull != null)
                {
                    TableSource extension = TableSourceReader.Read(extensionSheets[index].Key, parentOrNull.Name, diagnostics);
                    ShapeMerger.JoinColumns(parentOrNull, extension, (SourceColumn keyColumn) => ResolveKeyTypeOrNull(keyColumn, resolver), diagnostics);
                }
            }

            List<TableSchema> schemas = SchemaBuilder.Build(sources, resolver, diagnostics);
            schemas.Sort((TableSchema left, TableSchema right) => string.CompareOrdinal(left.Name, right.Name));
            List<TableData> tables = new List<TableData>();
            for (int index = 0; index < schemas.Count; index++)
            {
                tables.Add(RowConverter.Convert(sourcesByName[schemas[index].Name], schemas[index], diagnostics));
            }

            DataValidator.Validate(tables, diagnostics);

            List<EnumDefinition> sortedEnums = new List<EnumDefinition>(enums.Values);
            sortedEnums.Sort((EnumDefinition left, EnumDefinition right) => string.CompareOrdinal(left.Name, right.Name));
            ValidateTypeNames(schemas, sortedEnums, options, diagnostics);
            if (!diagnostics.HasErrors && schemas.Count == 0)
            {
                diagnostics.AddError("입력", "표가 하나도 없습니다.", "1행 타입, 2행 필드명을 적은 시트를 하나 이상 두세요.");
            }

            GeneratedOutput? outputOrNull = diagnostics.HasErrors ? null : Generate(schemas, tables, sortedEnums, options);
            return new TableBuildResult(diagnostics, workbooks, outputOrNull, tables, sortedEnums);
        }

        private static GeneratedOutput Generate(List<TableSchema> schemas, List<TableData> tables, List<EnumDefinition> enums, TableBuildOptions options)
        {
            GeneratedOutput output = new GeneratedOutput();
            string schemaHash = SchemaHasher.Compute(schemas, enums);
            List<TableManifestEntry> entries = new List<TableManifestEntry>();
            for (int index = 0; index < tables.Count; index++)
            {
                TableSchema schema = tables[index].Schema;
                output.CodeFiles.Add(schema.RowTypeName + ".g.cs", RowCodeGenerator.Generate(schema, options.Namespace));
                output.CodeFiles.Add(schema.TableTypeName + ".g.cs", TableCodeGenerator.Generate(schema, options.Namespace));

                byte[] bytes = TableEncoder.Encode(tables[index]);
                output.DataFiles.Add(TableManifest.GetDataFileName(schema.Name), bytes);
                entries.Add(new TableManifestEntry(schema.Name, tables[index].Rows.Count, TableHash.Compute(bytes)));
            }

            if (enums.Count > 0)
            {
                output.CodeFiles.Add(EnumCodeGenerator.FILE_NAME, EnumCodeGenerator.Generate(enums, options.Namespace));
            }

            output.CodeFiles.Add(options.TablesClassName + ".g.cs", TablesCodeGenerator.Generate(schemas, options.Namespace, options.TablesClassName, schemaHash));
            string manifestText = new TableManifest(schemaHash, entries).ToText();
            output.DataFiles.Add(TableManifest.FILE_NAME, System.Text.Encoding.UTF8.GetBytes(manifestText));
            return output;
        }

        private static void AddTable(Sheet sheet, SheetRole role, Dictionary<string, TableSource> sourcesByName, List<TableSource> sources, DiagnosticBag diagnostics)
        {
            if (!Identifier.IsValid(role.TableName))
            {
                diagnostics.AddError(sheet.Location, "'" + role.TableName + "'은(는) 표 이름으로 쓸 수 없습니다.", Identifier.RULE_TEXT + " 읽지 않을 시트는 이름 앞에 #를 붙입니다.");
                return;
            }

            if (sourcesByName.TryGetValue(role.TableName, out TableSource? existing))
            {
                diagnostics.AddError(sheet.Location, "표 '" + role.TableName + "'이(가) 이미 '" + existing.Location + "'에 있습니다.",
                    "표 이름이 겹치지 않게 시트 이름을 바꾸세요. 같은 표의 행을 나눠 적으려면 '" + role.TableName + ".이름'으로 짓습니다.");
                return;
            }

            Sheet readable = role.Kind == ESheetKind.Config ? sheet.Transpose() : sheet;
            TableSource source = TableSourceReader.Read(readable, role.TableName, diagnostics);
            sourcesByName.Add(role.TableName, source);
            sources.Add(source);
        }

        private static TableSource? FindParentOrNull(KeyValuePair<Sheet, string> sheet, Dictionary<string, TableSource> sourcesByName, DiagnosticBag diagnostics)
        {
            if (sourcesByName.TryGetValue(sheet.Value, out TableSource? parent))
            {
                return parent;
            }

            diagnostics.AddError(sheet.Key.Location, "이 시트가 속할 표 '" + sheet.Value + "'이(가) 없습니다.",
                "'" + sheet.Value + "'이라는 시트를 만들거나, 이 시트의 이름을 고치세요.");
            return null;
        }

        private static void AddEnums(List<EnumDefinition> definitions, Dictionary<string, EnumDefinition> enums, DiagnosticBag diagnostics)
        {
            for (int index = 0; index < definitions.Count; index++)
            {
                EnumDefinition definition = definitions[index];
                if (enums.TryGetValue(definition.Name, out EnumDefinition? existing))
                {
                    diagnostics.AddError(definition.Location, "enum '" + definition.Name + "'이(가) 이미 '" + existing.Location + "'에 있습니다.", "enum 이름이 겹치지 않게 고치세요.");
                    continue;
                }

                enums.Add(definition.Name, definition);
            }
        }

        private static ColumnType? ResolveKeyTypeOrNull(SourceColumn keyColumn, TypeResolver resolver)
        {
            if (!TypeExpression.TryParse(keyColumn.TypeText, out TypeExpression? expressionOrNull, out string _))
            {
                return null;
            }

            bool bResolved = resolver.TryResolve(expressionOrNull!, false, (string tableName) => null, out ColumnType? typeOrNull, out string _, out string _);
            return bResolved && typeOrNull!.CanBeKey ? typeOrNull : null;
        }

        private static void ValidateTypeNames(List<TableSchema> schemas, List<EnumDefinition> enums, TableBuildOptions options, DiagnosticBag diagnostics)
        {
            Dictionary<string, string> ownersByName = new Dictionary<string, string>(StringComparer.Ordinal);
            ownersByName.Add(options.TablesClassName, "표 묶음 클래스");
            for (int index = 0; index < enums.Count; index++)
            {
                AddTypeName(ownersByName, enums[index].Name, enums[index].Location, diagnostics);
            }

            for (int index = 0; index < schemas.Count; index++)
            {
                TableSchema schema = schemas[index];
                AddTypeName(ownersByName, schema.RowTypeName, schema.Location, diagnostics);
                AddTypeName(ownersByName, schema.TableTypeName, schema.Location, diagnostics);
                AddTypeName(ownersByName, schema.RefTypeName, schema.Location, diagnostics);
            }
        }

        private static void AddTypeName(Dictionary<string, string> ownersByName, string name, string location, DiagnosticBag diagnostics)
        {
            if (ownersByName.TryGetValue(name, out string? owner))
            {
                diagnostics.AddError(location, "생성될 이름 '" + name + "'이(가) '" + owner + "'의 것과 겹칩니다.",
                    "표나 enum의 이름을 바꾸세요. 표 'X'는 XRow, XTable, XRef라는 이름을 씁니다.");
                return;
            }

            ownersByName.Add(name, location);
        }

        private static void ValidateOptions(TableBuildOptions options, DiagnosticBag diagnostics)
        {
            string[] parts = options.Namespace.Split('.');
            for (int index = 0; index < parts.Length; index++)
            {
                if (!Identifier.IsValid(parts[index]))
                {
                    diagnostics.AddError("설정", "네임스페이스 '" + options.Namespace + "'을(를) 쓸 수 없습니다.", "예: Elonia.Data");
                    break;
                }
            }

            if (!Identifier.IsValid(options.TablesClassName))
            {
                diagnostics.AddError("설정", "클래스 이름 '" + options.TablesClassName + "'을(를) 쓸 수 없습니다.", "예: GameTables");
            }
        }
    }
}
