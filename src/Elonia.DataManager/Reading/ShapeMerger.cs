using System;
using System.Collections.Generic;

namespace Elonia.DataManager
{
    public static class ShapeMerger
    {
        public static void AppendChildRows(TableSource parent, TableSource child, DiagnosticBag diagnostics)
        {
            if (!TryMapColumns(parent, child, out int[] childIndexByParentIndex, out string difference))
            {
                diagnostics.AddError(child.Location, "자식 시트의 열이 표 '" + parent.Name + "'과(와) 다릅니다. " + difference,
                    "자식 시트의 1행(타입)과 2행(필드명)을 '" + parent.Location + "' 시트와 똑같이 맞추세요.");
                return;
            }

            for (int rowIndex = 0; rowIndex < child.Rows.Count; rowIndex++)
            {
                Cell[] childCells = child.Rows[rowIndex];
                Cell[] cells = new Cell[parent.Columns.Count];
                for (int index = 0; index < cells.Length; index++)
                {
                    cells[index] = childCells[childIndexByParentIndex[index]];
                }

                parent.Rows.Add(cells);
            }
        }

        private static bool TryMapColumns(TableSource parent, TableSource child, out int[] childIndexByParentIndex, out string difference)
        {
            childIndexByParentIndex = new int[parent.Columns.Count];
            difference = "";
            bool[] bUsed = new bool[child.Columns.Count];
            for (int parentIndex = 0; parentIndex < parent.Columns.Count; parentIndex++)
            {
                SourceColumn parentColumn = parent.Columns[parentIndex];
                int found = -1;
                for (int childIndex = 0; childIndex < child.Columns.Count; childIndex++)
                {
                    SourceColumn childColumn = child.Columns[childIndex];
                    if (!bUsed[childIndex] && childColumn.Field == parentColumn.Field && childColumn.TypeText == parentColumn.TypeText)
                    {
                        found = childIndex;
                        break;
                    }
                }

                if (found < 0)
                {
                    difference = "자식 시트에 '" + parentColumn.TypeText + " " + parentColumn.Field + "' 열이 없습니다.";
                    return false;
                }

                bUsed[found] = true;
                childIndexByParentIndex[parentIndex] = found;
            }

            for (int childIndex = 0; childIndex < child.Columns.Count; childIndex++)
            {
                if (!bUsed[childIndex])
                {
                    difference = "자식 시트에만 '" + child.Columns[childIndex].TypeText + " " + child.Columns[childIndex].Field + "' 열이 있습니다.";
                    return false;
                }
            }

            return true;
        }

        public static void JoinColumns(TableSource parent, TableSource extension, Func<SourceColumn, ColumnType?> resolveKeyTypeOrNull, DiagnosticBag diagnostics)
        {
            int parentKeyIndex = FindFirstKeyIndex(parent);
            int extensionKeyIndex = FindFirstKeyIndex(extension);
            if (parentKeyIndex < 0 || extensionKeyIndex < 0)
            {
                string missing = parentKeyIndex < 0 ? parent.Location : extension.Location;
                diagnostics.AddError(missing, "열 확장 시트는 Key로 행을 맞추는데, 이 시트에 Key<...> 열이 없습니다.",
                    "표와 열 확장 시트 양쪽에 같은 Key<...> 열을 두세요.");
                return;
            }

            SourceColumn parentKey = parent.Columns[parentKeyIndex];
            SourceColumn extensionKey = extension.Columns[extensionKeyIndex];
            if (parentKey.Field != extensionKey.Field || parentKey.TypeText != extensionKey.TypeText)
            {
                diagnostics.AddError(extensionKey.FieldCell, "열 확장 시트의 Key 열이 표 '" + parent.Name + "'의 Key 열(" + parentKey.TypeText + " " + parentKey.Field + ")과 다릅니다.",
                    "Key 열의 타입과 필드명을 표와 똑같이 맞추세요.");
                return;
            }

            ColumnType? keyTypeOrNull = resolveKeyTypeOrNull(parentKey);
            if (keyTypeOrNull == null)
            {
                return;
            }

            Dictionary<object, Cell[]> extensionRowsByKey = new Dictionary<object, Cell[]>();
            for (int rowIndex = 0; rowIndex < extension.Rows.Count; rowIndex++)
            {
                Cell keyCell = extension.Rows[rowIndex][extensionKeyIndex];
                if (keyCell.IsBlank)
                {
                    continue;
                }

                if (!keyTypeOrNull.TryParse(keyCell.Text, out object key, out string reason))
                {
                    diagnostics.AddError(keyCell, reason, "표 '" + parent.Name + "'에 있는 Key 값을 적으세요.");
                    continue;
                }

                if (extensionRowsByKey.ContainsKey(key))
                {
                    diagnostics.AddError(keyCell, "열 확장 시트에 같은 Key가 두 번 나옵니다.", "한 Key는 한 행에만 적으세요.");
                    continue;
                }

                extensionRowsByKey.Add(key, extension.Rows[rowIndex]);
            }

            List<int> addedIndexes = new List<int>();
            for (int index = 0; index < extension.Columns.Count; index++)
            {
                if (index != extensionKeyIndex)
                {
                    addedIndexes.Add(index);
                    parent.Columns.Add(extension.Columns[index]);
                }
            }

            HashSet<object> usedKeys = new HashSet<object>();
            for (int rowIndex = 0; rowIndex < parent.Rows.Count; rowIndex++)
            {
                Cell[] parentCells = parent.Rows[rowIndex];
                Cell keyCell = parentCells[parentKeyIndex];
                Cell[]? extensionCellsOrNull = null;
                if (!keyCell.IsBlank && keyTypeOrNull.TryParse(keyCell.Text, out object key, out string _) && extensionRowsByKey.TryGetValue(key, out extensionCellsOrNull))
                {
                    usedKeys.Add(key);
                }

                Cell[] cells = new Cell[parentCells.Length + addedIndexes.Count];
                Array.Copy(parentCells, cells, parentCells.Length);
                for (int index = 0; index < addedIndexes.Count; index++)
                {
                    cells[parentCells.Length + index] = extensionCellsOrNull != null
                        ? extensionCellsOrNull[addedIndexes[index]]
                        : new Cell("", keyCell.Origin);
                }

                parent.Rows[rowIndex] = cells;
            }

            foreach (KeyValuePair<object, Cell[]> pair in extensionRowsByKey)
            {
                if (!usedKeys.Contains(pair.Key))
                {
                    diagnostics.AddError(pair.Value[extensionKeyIndex], "표 '" + parent.Name + "'에 이 Key를 가진 행이 없습니다.",
                        "표에 있는 Key 값으로 고치거나, 표에 그 행을 먼저 추가하세요.");
                }
            }
        }

        private static int FindFirstKeyIndex(TableSource source)
        {
            for (int index = 0; index < source.Columns.Count; index++)
            {
                if (TypeExpression.TryParse(source.Columns[index].TypeText, out TypeExpression? expressionOrNull, out string _) && expressionOrNull!.IsKey)
                {
                    return index;
                }
            }

            return -1;
        }
    }
}
