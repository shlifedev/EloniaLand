using System;
using System.Numerics;
using ImGuiNET;

namespace Elonia.DataManager.Gui;

internal sealed class SheetGridPanel
{
    private const int MAX_TABLE_COLUMNS = 512;

    private const int MAX_CELL_TEXT_LENGTH = 80;

    private const float ROW_NUMBER_WIDTH = 44.0f;
    private const float COLUMN_WIDTH = 120.0f;

    private Sheet? _sheetOrNull;
    private string[][] _texts = Array.Empty<string[]>();
    private string[] _columnLabels = Array.Empty<string>();
    private string[] _rowLabels = Array.Empty<string>();
    private ESheetKind _kind;

    private CellOrigin? _focusOrNull;
    private bool _shouldScrollToFocus;

    public void Focus(CellOrigin origin)
    {
        _focusOrNull = origin;
        _shouldScrollToFocus = true;
    }

    public void ClearFocus()
    {
        _focusOrNull = null;
        _shouldScrollToFocus = false;
    }

    public unsafe void Draw(TableWorkspace workspace, Sheet sheet)
    {
        if (sheet != _sheetOrNull)
        {
            Prepare(sheet);
        }

        if (sheet.RowCount == 0 || sheet.ColumnCount == 0)
        {
            ImGui.TextColored(Palette.MUTED_TEXT, "빈 시트입니다.");
            return;
        }

        int columnCount = Math.Min(sheet.ColumnCount, MAX_TABLE_COLUMNS - 1);
        if (columnCount < sheet.ColumnCount)
        {
            ImGui.TextColored(Palette.WARNING_TEXT, $"열이 너무 많아 앞의 {columnCount}개만 보여 줍니다.");
        }

        bool bHeaderOnLeft = _kind == ESheetKind.Config;
        int headerCount = _kind == ESheetKind.Ignored || _kind == ESheetKind.Invalid ? 0 : TableSourceReader.FIRST_DATA_ROW;
        int frozenRowCount = bHeaderOnLeft ? 0 : Math.Min(headerCount, sheet.RowCount);
        int frozenColumnCount = bHeaderOnLeft ? Math.Min(headerCount, columnCount) : 0;

        ImGuiTableFlags flags = ImGuiTableFlags.ScrollX | ImGuiTableFlags.ScrollY | ImGuiTableFlags.Borders | ImGuiTableFlags.Resizable | ImGuiTableFlags.SizingFixedFit;
        if (!ImGui.BeginTable("grid", columnCount + 1, flags, new Vector2(0.0f, -1.0f)))
        {
            return;
        }

        ImGui.TableSetupScrollFreeze(1 + frozenColumnCount, 1 + frozenRowCount);
        ImGui.TableSetupColumn("", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoResize, ROW_NUMBER_WIDTH);
        for (int columnIndex = 0; columnIndex < columnCount; columnIndex++)
        {
            ImGui.TableSetupColumn(_columnLabels[columnIndex], ImGuiTableColumnFlags.WidthFixed, COLUMN_WIDTH);
        }

        ImGui.TableHeadersRow();

        for (int rowIndex = 0; rowIndex < frozenRowCount; rowIndex++)
        {
            DrawRow(workspace, sheet, rowIndex, columnCount, headerCount, bHeaderOnLeft);
        }

        ImGuiListClipperPtr clipper = new ImGuiListClipperPtr(ImGuiNative.ImGuiListClipper_ImGuiListClipper());
        clipper.Begin(sheet.RowCount - frozenRowCount);
        while (clipper.Step())
        {
            for (int index = clipper.DisplayStart; index < clipper.DisplayEnd; index++)
            {
                DrawRow(workspace, sheet, frozenRowCount + index, columnCount, headerCount, bHeaderOnLeft);
            }
        }

        if (_shouldScrollToFocus && _focusOrNull != null && clipper.ItemsHeight > 0.0f)
        {
            int focusRowIndex = _focusOrNull.Value.Row - 1 - frozenRowCount;
            if (focusRowIndex >= 0 && (focusRowIndex < clipper.DisplayStart || focusRowIndex >= clipper.DisplayEnd - 1))
            {
                ImGui.SetScrollY(Math.Max(0.0f, (focusRowIndex - 3) * clipper.ItemsHeight));
            }
        }

        clipper.End();
        clipper.Destroy();
        ImGui.EndTable();
    }

    private void DrawRow(TableWorkspace workspace, Sheet sheet, int rowIndex, int columnCount, int headerCount, bool bHeaderOnLeft)
    {
        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.TextColored(Palette.MUTED_TEXT, _rowLabels[rowIndex]);
        for (int columnIndex = 0; columnIndex < columnCount; columnIndex++)
        {
            ImGui.TableNextColumn();
            DrawCell(workspace, sheet, rowIndex, columnIndex, headerCount, bHeaderOnLeft);
        }
    }

    private void DrawCell(TableWorkspace workspace, Sheet sheet, int rowIndex, int columnIndex, int headerCount, bool bHeaderOnLeft)
    {
        Cell cell = sheet.GetCell(rowIndex, columnIndex);
        Diagnostic? diagnosticOrNull = workspace.FindDiagnosticOrNull(cell.Origin);
        bool bFocused = _focusOrNull != null && _focusOrNull.Value.Equals(cell.Origin);

        int headerIndex = bHeaderOnLeft ? columnIndex : rowIndex;
        bool bHeader = headerIndex < headerCount;
        bool bIgnored = IsIgnored(sheet, rowIndex, columnIndex, headerCount, bHeaderOnLeft);

        if (diagnosticOrNull != null)
        {
            bool bError = diagnosticOrNull.Severity == ESeverity.Error;
            Vector4 color = bFocused
                ? (bError ? Palette.FOCUSED_ERROR_CELL : Palette.FOCUSED_WARNING_CELL)
                : (bError ? Palette.ERROR_CELL : Palette.WARNING_CELL);
            ImGui.TableSetBgColor(ImGuiTableBgTarget.CellBg, ImGui.GetColorU32(color));
        }
        else if (bFocused)
        {
            ImGui.TableSetBgColor(ImGuiTableBgTarget.CellBg, ImGui.GetColorU32(Palette.FOCUSED_CELL));
        }
        else if (bHeader)
        {
            ImGui.TableSetBgColor(ImGuiTableBgTarget.CellBg, ImGui.GetColorU32(Palette.HEADER_CELL));
        }

        string text = _texts[rowIndex][columnIndex];
        if (bIgnored)
        {
            ImGui.TextColored(Palette.MUTED_TEXT, text);
        }
        else if (bHeader && diagnosticOrNull == null)
        {
            ImGui.TextColored(Palette.HEADER_TEXT, text);
        }
        else
        {
            ImGui.TextUnformatted(text);
        }

        if (diagnosticOrNull != null && ImGui.TableGetColumnFlags().HasFlag(ImGuiTableColumnFlags.IsHovered) && IsMouseOnCurrentRow())
        {
            ProblemPanel.DrawTooltip(diagnosticOrNull);
        }

        if (bFocused && _shouldScrollToFocus)
        {
            ImGui.SetScrollHereX(0.5f);
            ImGui.SetScrollHereY(0.5f);
            _shouldScrollToFocus = false;
        }
    }

    private static bool IsMouseOnCurrentRow()
    {
        float mouseY = ImGui.GetMousePos().Y;
        float padding = ImGui.GetStyle().CellPadding.Y;
        return mouseY >= ImGui.GetItemRectMin().Y - padding && mouseY < ImGui.GetItemRectMax().Y + padding;
    }

    private bool IsIgnored(Sheet sheet, int rowIndex, int columnIndex, int headerCount, bool bHeaderOnLeft)
    {
        if (_kind == ESheetKind.Ignored || _kind == ESheetKind.Invalid)
        {
            return true;
        }

        int itemIndex = bHeaderOnLeft ? rowIndex : columnIndex;
        int lineIndex = bHeaderOnLeft ? columnIndex : rowIndex;
        Cell typeCell = GetCell(sheet, TableSourceReader.TYPE_ROW, itemIndex, bHeaderOnLeft);
        Cell fieldCell = GetCell(sheet, TableSourceReader.FIELD_ROW, itemIndex, bHeaderOnLeft);
        Cell commentCell = GetCell(sheet, TableSourceReader.COMMENT_ROW, itemIndex, bHeaderOnLeft);
        if (SheetRole.IsIgnoreMark(typeCell.Text) || SheetRole.IsIgnoreMark(fieldCell.Text) || SheetRole.IsIgnoreMark(commentCell.Text))
        {
            return true;
        }

        if (_kind != ESheetKind.Enums && typeCell.IsBlank && fieldCell.IsBlank)
        {
            return true;
        }

        if (lineIndex < headerCount)
        {
            return false;
        }

        Cell mark = _kind == ESheetKind.Enums ? sheet.GetCell(rowIndex, columnIndex) : GetCell(sheet, lineIndex, 0, bHeaderOnLeft);
        return SheetRole.IsIgnoreMark(mark.Text);
    }

    private static Cell GetCell(Sheet sheet, int lineIndex, int itemIndex, bool bHeaderOnLeft)
    {
        return bHeaderOnLeft ? sheet.GetCell(itemIndex, lineIndex) : sheet.GetCell(lineIndex, itemIndex);
    }

    private void Prepare(Sheet sheet)
    {
        _sheetOrNull = sheet;
        _kind = SheetRole.FromName(sheet.Name).Kind;
        _texts = new string[sheet.RowCount][];
        _rowLabels = new string[sheet.RowCount];
        for (int rowIndex = 0; rowIndex < sheet.RowCount; rowIndex++)
        {
            _rowLabels[rowIndex] = (rowIndex + 1).ToString();
            _texts[rowIndex] = new string[sheet.ColumnCount];
            for (int columnIndex = 0; columnIndex < sheet.ColumnCount; columnIndex++)
            {
                _texts[rowIndex][columnIndex] = ToSingleLine(sheet.GetCell(rowIndex, columnIndex).Text);
            }
        }

        _columnLabels = new string[sheet.ColumnCount];
        for (int columnIndex = 0; columnIndex < sheet.ColumnCount; columnIndex++)
        {
            _columnLabels[columnIndex] = CellOrigin.GetColumnLetters(columnIndex + 1);
        }
    }

    private static string ToSingleLine(string text)
    {
        string singleLine = text.Replace('\r', ' ').Replace('\n', ' ');
        return singleLine.Length <= MAX_CELL_TEXT_LENGTH ? singleLine : singleLine.Substring(0, MAX_CELL_TEXT_LENGTH) + "...";
    }
}
