using System;
using System.Collections.Generic;

namespace Elonia.DataManager
{
    public sealed class TypeResolver
    {
        private readonly TypeRegistry _registry;
        private readonly Dictionary<string, EnumDefinition> _enums;

        public TypeResolver(TypeRegistry registry, Dictionary<string, EnumDefinition> enums)
        {
            if (registry == null)
            {
                throw new ArgumentNullException(nameof(registry));
            }

            if (enums == null)
            {
                throw new ArgumentNullException(nameof(enums));
            }

            _registry = registry;
            _enums = enums;
        }

        public bool TryResolve(TypeExpression expression, bool bMergedColumns, Func<string, ColumnType?> findKeyTypeOrNull,
            out ColumnType? typeOrNull, out string reason, out string fix)
        {
            typeOrNull = null;
            reason = "";
            fix = "";
            switch (expression.Kind)
            {
                case ETypeKind.Value:
                    return TryResolveValue(expression, bMergedColumns, out typeOrNull, out reason, out fix);
                case ETypeKind.Enum:
                    return TryResolveEnum(expression, bMergedColumns, out typeOrNull, out reason, out fix);
                case ETypeKind.Reference:
                    ColumnType? keyTypeOrNull = findKeyTypeOrNull(expression.Name);
                    if (keyTypeOrNull == null)
                    {
                        reason = "Ref<" + expression.Name + ">가 가리키는 표 '" + expression.Name + "'이(가) 없거나, 그 표에 Key<...> 열이 없습니다.";
                        fix = "표 이름의 철자와 대소문자를 확인하고, 그 표의 한 열을 Key<...>로 표시하세요.";
                        return false;
                    }

                    typeOrNull = new RefType(expression.Name, keyTypeOrNull);
                    return true;
                default:
                    throw new InvalidOperationException("A mapping column has no column type.");
            }
        }

        private bool TryResolveValue(TypeExpression expression, bool bMergedColumns, out ColumnType? typeOrNull, out string reason, out string fix)
        {
            typeOrNull = null;
            reason = "";
            fix = "";
            ScalarColumnType? scalarOrNull = _registry.FindOrNull(expression.Name);
            if (scalarOrNull == null)
            {
                string? similarOrNull = _registry.FindSimilarNameOrNull(expression.Name);
                reason = "'" + expression.Name + "'은(는) 모르는 타입입니다.";
                fix = similarOrNull != null
                    ? "'" + similarOrNull + "'을(를) 뜻한 것이라면 대소문자를 맞춰 적으세요."
                    : "쓸 수 있는 타입: " + _registry.GetNameList() + ". enum은 Enum<이름>, 다른 표의 행은 Ref<표>로 적습니다.";
                return false;
            }

            if (expression.Collection == ECollectionKind.List)
            {
                typeOrNull = scalarOrNull.CreateListType();
            }
            else if (expression.Collection == ECollectionKind.Array || bMergedColumns)
            {
                typeOrNull = scalarOrNull.CreateArrayType();
            }
            else
            {
                typeOrNull = scalarOrNull;
            }

            return true;
        }

        private bool TryResolveEnum(TypeExpression expression, bool bMergedColumns, out ColumnType? typeOrNull, out string reason, out string fix)
        {
            typeOrNull = null;
            reason = "";
            fix = "";
            if (!_enums.TryGetValue(expression.Name, out EnumDefinition? definition))
            {
                reason = "'" + expression.Name + "'이라는 enum이 없습니다.";
                fix = "Enums 시트에 이 이름의 enum을 정의했는지, 철자와 대소문자가 같은지 확인하세요.";
                return false;
            }

            EnumType enumType = new EnumType(definition);
            typeOrNull = expression.Collection == ECollectionKind.Array || bMergedColumns ? new EnumArrayType(enumType) : (ColumnType)enumType;
            return true;
        }
    }
}
