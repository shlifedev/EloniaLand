using System;
using System.Collections.Generic;

namespace Elonia.DataManager
{
    public static class SchemaBuilder
    {
        private const int MAX_MEMBER_COUNT = 249;

        public static List<TableSchema> Build(IReadOnlyList<TableSource> sources, TypeResolver resolver, DiagnosticBag diagnostics)
        {
            List<TableDraft> drafts = new List<TableDraft>();
            Dictionary<string, TableDraft> draftsByName = new Dictionary<string, TableDraft>(StringComparer.Ordinal);
            for (int index = 0; index < sources.Count; index++)
            {
                TableDraft draft = CreateDraft(sources[index], diagnostics);
                ResolveFirstKey(draft, resolver, diagnostics);
                drafts.Add(draft);
                draftsByName[draft.Source.Name] = draft;
            }

            Func<string, ColumnType?> findKeyTypeOrNull = (string tableName) =>
                draftsByName.TryGetValue(tableName, out TableDraft? target) ? target.FirstKeyTypeOrNull : null;

            List<TableSchema> schemas = new List<TableSchema>();
            for (int index = 0; index < drafts.Count; index++)
            {
                TableSchema? schemaOrNull = CreateSchema(drafts[index], resolver, findKeyTypeOrNull, diagnostics);
                if (schemaOrNull != null)
                {
                    schemas.Add(schemaOrNull);
                }
            }

            return schemas;
        }

        private static TableDraft CreateDraft(TableSource source, DiagnosticBag diagnostics)
        {
            TableDraft draft = new TableDraft(source);
            Dictionary<string, ColumnDraft> columnsByField = new Dictionary<string, ColumnDraft>(StringComparer.Ordinal);
            for (int index = 0; index < source.Columns.Count; index++)
            {
                SourceColumn column = source.Columns[index];
                if (!TypeExpression.TryParse(column.TypeText, out TypeExpression? expressionOrNull, out string reason))
                {
                    diagnostics.AddError(column.TypeCell, reason, "예: int, string, float[], List<int>, Key<int>, Enum<EKind>, Ref<Item>");
                    draft.IsValid = false;
                    continue;
                }

                TypeExpression expression = expressionOrNull!;
                if (!Identifier.IsValid(column.Field))
                {
                    diagnostics.AddError(column.FieldCell, "필드명으로 쓸 수 없습니다.", Identifier.RULE_TEXT);
                    draft.IsValid = false;
                    continue;
                }

                if (expression.Kind == ETypeKind.Mapping)
                {
                    continue;
                }

                if (!columnsByField.TryGetValue(column.Field, out ColumnDraft? existing))
                {
                    ColumnDraft created = new ColumnDraft(column, expression, index);
                    columnsByField.Add(column.Field, created);
                    draft.Columns.Add(created);
                    continue;
                }

                bool bMergeable = existing.First.TypeText == column.TypeText && !expression.IsKey
                    && (expression.Kind == ETypeKind.Value || expression.Kind == ETypeKind.Enum)
                    && expression.Collection != ECollectionKind.List;
                if (!bMergeable)
                {
                    diagnostics.AddError(column.FieldCell, "필드명 '" + column.Field + "'이(가) 이미 있습니다. 같은 필드명의 열은 타입이 같을 때만 배열 하나로 합쳐집니다(Key, Ref, List는 불가).",
                        "필드명을 다르게 하거나, 합칠 열들의 타입을 '" + existing.First.TypeText + "'(으)로 똑같이 맞추세요.");
                    draft.IsValid = false;
                    continue;
                }

                existing.SourceIndexes.Add(index);
            }

            return draft;
        }

        private static void ResolveFirstKey(TableDraft draft, TypeResolver resolver, DiagnosticBag diagnostics)
        {
            for (int index = 0; index < draft.Columns.Count; index++)
            {
                ColumnDraft column = draft.Columns[index];
                if (!column.Expression.IsKey)
                {
                    continue;
                }

                if (resolver.TryResolve(column.Expression, false, NoTableKey, out ColumnType? typeOrNull, out string _, out string _) && typeOrNull!.CanBeKey)
                {
                    draft.FirstKeyTypeOrNull = typeOrNull;
                }

                return;
            }
        }

        private static ColumnType? NoTableKey(string tableName)
        {
            return null;
        }

        private static TableSchema? CreateSchema(TableDraft draft, TypeResolver resolver, Func<string, ColumnType?> findKeyTypeOrNull, DiagnosticBag diagnostics)
        {
            TableSource source = draft.Source;
            bool bValid = draft.IsValid;
            List<ColumnSchema> columns = new List<ColumnSchema>();
            HashSet<string> memberNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            int memberCount = 0;
            for (int index = 0; index < draft.Columns.Count; index++)
            {
                ColumnDraft column = draft.Columns[index];
                bool bMerged = column.SourceIndexes.Count > 1;
                if (!resolver.TryResolve(column.Expression, bMerged, findKeyTypeOrNull, out ColumnType? typeOrNull, out string reason, out string fix))
                {
                    diagnostics.AddError(column.First.TypeCell, reason, fix);
                    bValid = false;
                    continue;
                }

                ColumnType type = typeOrNull!;
                if (column.Expression.IsKey && !type.CanBeKey)
                {
                    diagnostics.AddError(column.First.TypeCell, "'" + type.CanonicalName + "'은(는) Key로 쓸 수 없는 타입입니다.",
                        "Key에는 정수, string, Guid, enum만 쓸 수 있습니다. 예: Key<int>, Key<string>");
                    bValid = false;
                    continue;
                }

                bool bUnique = memberNames.Add(column.Field);
                if (type is RefType)
                {
                    bUnique &= memberNames.Add(RowMemberNames.GetRefHasName(column.Field));
                    bUnique &= memberNames.Add(RowMemberNames.GetRefKeyName(column.Field));
                }

                if (!bUnique || column.Field == source.Name + "Row")
                {
                    diagnostics.AddError(column.First.FieldCell, "이 필드명은 생성되는 코드의 다른 이름과 겹칩니다.",
                        "필드명을 바꾸세요. Ref<...> 필드 'X'는 'HasX'와 'XKey'라는 이름도 함께 씁니다.");
                    bValid = false;
                    continue;
                }

                memberCount += type is RefType ? 2 : 1;
                columns.Add(new ColumnSchema(column.Field, type, column.Expression.IsKey, column.First.CommentCell.Trimmed, column.First.FieldCell, column.SourceIndexes));
            }

            if (memberCount > MAX_MEMBER_COUNT)
            {
                diagnostics.AddError(source.Location, "열이 너무 많습니다(" + memberCount + "개). 표 하나에 " + MAX_MEMBER_COUNT + "개까지 둘 수 있습니다.",
                    "표를 나누거나, 값이 여러 개인 열은 배열로 합치세요.");
                bValid = false;
            }

            return bValid ? new TableSchema(source.Name, source.Location, columns) : null;
        }

        private sealed class TableDraft
        {
            public TableSource Source { get; }
            public List<ColumnDraft> Columns { get; } = new List<ColumnDraft>();
            public bool IsValid { get; set; } = true;
            public ColumnType? FirstKeyTypeOrNull { get; set; }

            public TableDraft(TableSource source)
            {
                Source = source;
            }
        }

        private sealed class ColumnDraft
        {
            public SourceColumn First { get; }
            public TypeExpression Expression { get; }
            public List<int> SourceIndexes { get; } = new List<int>();
            public string Field => First.Field;

            public ColumnDraft(SourceColumn first, TypeExpression expression, int sourceIndex)
            {
                First = first;
                Expression = expression;
                SourceIndexes.Add(sourceIndex);
            }
        }
    }

    public static class RowMemberNames
    {
        public static string GetRefHasName(string field)
        {
            return "Has" + field;
        }

        public static string GetRefKeyName(string field)
        {
            return field + "Key";
        }
    }
}
