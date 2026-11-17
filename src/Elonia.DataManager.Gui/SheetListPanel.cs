using System.Collections.Generic;
using System.Numerics;
using ImGuiNET;

namespace Elonia.DataManager.Gui;

internal sealed class SheetListPanel
{
    public Sheet? Draw(TableWorkspace workspace, Sheet? selectedOrNull)
    {
        TableBuildResult? resultOrNull = workspace.ResultOrNull;
        if (resultOrNull == null)
        {
            return null;
        }

        Sheet? clickedOrNull = null;
        IReadOnlyList<Workbook> workbooks = resultOrNull.Workbooks;
        for (int workbookIndex = 0; workbookIndex < workbooks.Count; workbookIndex++)
        {
            Workbook workbook = workbooks[workbookIndex];
            ImGui.SeparatorText(workbook.Name);
            if (!ImGui.BeginTable(workbook.Name, 3, ImGuiTableFlags.SizingStretchProp))
            {
                continue;
            }

            ImGui.TableSetupColumn("시트", ImGuiTableColumnFlags.WidthStretch, 3.0f);
            ImGui.TableSetupColumn("역할", ImGuiTableColumnFlags.WidthStretch, 2.0f);
            ImGui.TableSetupColumn("문제", ImGuiTableColumnFlags.WidthFixed, 44.0f);
            for (int sheetIndex = 0; sheetIndex < workbook.Sheets.Count; sheetIndex++)
            {
                Sheet sheet = workbook.Sheets[sheetIndex];
                ESheetKind kind = SheetRole.FromName(sheet.Name).Kind;
                workspace.CountProblems(sheet, out int errorCount, out int warningCount);

                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                bool bMuted = kind == ESheetKind.Ignored;
                if (bMuted)
                {
                    ImGui.PushStyleColor(ImGuiCol.Text, Palette.MUTED_TEXT);
                }

                ImGui.PushID(sheetIndex);
                if (ImGui.Selectable(sheet.Name, sheet == selectedOrNull, ImGuiSelectableFlags.SpanAllColumns))
                {
                    clickedOrNull = sheet;
                }

                ImGui.PopID();
                if (bMuted)
                {
                    ImGui.PopStyleColor();
                }

                ImGui.TableNextColumn();
                ImGui.TextColored(Palette.MUTED_TEXT, GetKindName(kind));

                ImGui.TableNextColumn();
                if (errorCount > 0)
                {
                    ImGui.TextColored(Palette.ERROR_TEXT, errorCount.ToString());
                }
                else if (warningCount > 0)
                {
                    ImGui.TextColored(Palette.WARNING_TEXT, warningCount.ToString());
                }
            }

            ImGui.EndTable();
        }

        return clickedOrNull;
    }

    private static string GetKindName(ESheetKind kind)
    {
        switch (kind)
        {
            case ESheetKind.Table:
                return "표";
            case ESheetKind.Config:
                return "Config";
            case ESheetKind.ChildRows:
                return "자식 행";
            case ESheetKind.ColumnExtension:
                return "열 확장";
            case ESheetKind.Enums:
                return "enum";
            case ESheetKind.Ignored:
                return "무시";
            default:
                return "이름 오류";
        }
    }
}
