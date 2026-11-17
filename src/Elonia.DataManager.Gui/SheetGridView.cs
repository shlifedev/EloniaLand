using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Media;

namespace Elonia.DataManager.Gui;

internal sealed class SheetGridView : UserControl
{
    private const double ROW_NUMBER_WIDTH = 52.0;
    private const double COLUMN_WIDTH = 150.0;
    private const double ROW_HEIGHT = 26.0;

    private const int MAX_CELL_TEXT_LENGTH = 120;

    private readonly DataGrid _grid;
    private readonly TextBlock _emptyMessage;

    private List<SheetGridRow> _rows = new List<SheetGridRow>();

    private ESheetKind _kind;
    private bool _isHeaderInColumnHeader;
    private bool _isHeaderOnLeft;

    public SheetGridView()
    {
        _grid = new DataGrid
        {
            AutoGenerateColumns = false,
            IsReadOnly = true,
            CanUserSortColumns = false,
            CanUserReorderColumns = false,
            CanUserResizeColumns = true,
            GridLinesVisibility = DataGridGridLinesVisibility.All,
            HeadersVisibility = DataGridHeadersVisibility.Column,
            SelectionMode = DataGridSelectionMode.Single,
            RowHeight = ROW_HEIGHT,
        };
        _emptyMessage = new TextBlock
        {
            Text = "보여 줄 시트가 없습니다.",
            Foreground = Palette.MUTED_TEXT,
            Margin = new Thickness(12),
            IsVisible = false,
        };

        Panel panel = new Panel();
        panel.Children.Add(_grid);
        panel.Children.Add(_emptyMessage);
        Content = panel;
    }

    public void ShowSheet(TableWorkspace workspace, Sheet? sheetOrNull)
    {
        _grid.ItemsSource = null;
        _grid.Columns.Clear();
        _rows = new List<SheetGridRow>();
        if (sheetOrNull == null || sheetOrNull.RowCount == 0 || sheetOrNull.ColumnCount == 0)
        {
            _emptyMessage.Text = sheetOrNull == null ? "보여 줄 시트가 없습니다." : "빈 시트입니다.";
            _emptyMessage.IsVisible = true;
            _grid.IsVisible = false;
            return;
        }

        Sheet sheet = sheetOrNull;
        _emptyMessage.IsVisible = false;
        _grid.IsVisible = true;
        _kind = SheetRole.FromName(sheet.Name).Kind;
        bool bReadable = _kind != ESheetKind.Ignored && _kind != ESheetKind.Invalid;
        _isHeaderOnLeft = _kind == ESheetKind.Config;
        _isHeaderInColumnHeader = bReadable && !_isHeaderOnLeft;
        int headerCount = bReadable ? TableSourceReader.FIRST_DATA_ROW : 0;

        _grid.Columns.Add(CreateRowNumberColumn());
        for (int columnIndex = 0; columnIndex < sheet.ColumnCount; columnIndex++)
        {
            _grid.Columns.Add(CreateColumn(workspace, sheet, columnIndex, headerCount));
        }

        _grid.FrozenColumnCount = 1 + (_isHeaderOnLeft ? Math.Min(headerCount, sheet.ColumnCount) : 0);

        int firstRowIndex = _isHeaderInColumnHeader ? Math.Min(headerCount, sheet.RowCount) : 0;
        for (int rowIndex = firstRowIndex; rowIndex < sheet.RowCount; rowIndex++)
        {
            SheetGridCell[] cells = new SheetGridCell[sheet.ColumnCount];
            for (int columnIndex = 0; columnIndex < cells.Length; columnIndex++)
            {
                cells[columnIndex] = CreateCell(workspace, sheet, rowIndex, columnIndex, headerCount);
            }

            _rows.Add(new SheetGridRow(rowIndex, cells));
        }

        _grid.ItemsSource = _rows;
    }

    public void FocusCell(CellOrigin origin)
    {
        int columnIndex = origin.Column;
        if (columnIndex >= _grid.Columns.Count)
        {
            return;
        }

        DataGridColumn column = _grid.Columns[columnIndex];
        SheetGridRow? rowOrNull = FindRowOrNull(origin.Row - 1);
        if (rowOrNull == null)
        {
            _grid.SelectedItem = null;
            if (_rows.Count > 0)
            {
                _grid.ScrollIntoView(_rows[0], column);
            }

            return;
        }

        _grid.SelectedItem = rowOrNull;
        _grid.ScrollIntoView(rowOrNull, column);
    }

    private SheetGridRow? FindRowOrNull(int rowIndex)
    {
        for (int index = 0; index < _rows.Count; index++)
        {
            if (_rows[index].RowIndex == rowIndex)
            {
                return _rows[index];
            }
        }

        return null;
    }

    private static DataGridColumn CreateRowNumberColumn()
    {
        return new DataGridTemplateColumn
        {
            Header = "",
            Width = new DataGridLength(ROW_NUMBER_WIDTH),
            CanUserResize = false,
            CellTemplate = new FuncDataTemplate<SheetGridRow>((SheetGridRow row, INameScope scope) =>
            {
                TextBlock text = new TextBlock
                {
                    Foreground = Palette.MUTED_TEXT,
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Margin = new Thickness(0, 0, 8, 0),
                };
                text.Bind(TextBlock.TextProperty, new Binding(nameof(SheetGridRow.RowNumber)));
                return text;
            }),
        };
    }

    private DataGridColumn CreateColumn(TableWorkspace workspace, Sheet sheet, int columnIndex, int headerCount)
    {
        string path = "Cells[" + columnIndex + "]";
        DataGridTemplateColumn column = new DataGridTemplateColumn
        {
            Header = CreateColumnHeader(workspace, sheet, columnIndex, headerCount),
            Width = new DataGridLength(COLUMN_WIDTH),
            CellTemplate = new FuncDataTemplate<SheetGridRow>((SheetGridRow row, INameScope scope) =>
            {
                TextBlock text = new TextBlock
                {
                    VerticalAlignment = VerticalAlignment.Center,
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    Margin = new Thickness(6, 2),
                };
                text.Bind(TextBlock.TextProperty, new Binding(path + "." + nameof(SheetGridCell.Text)));
                text.Bind(OpacityProperty, new Binding(path + "." + nameof(SheetGridCell.Opacity)));
                text.Bind(TextBlock.FontWeightProperty, new Binding(path + "." + nameof(SheetGridCell.Weight)));

                Border border = new Border { Child = text };
                border.Bind(Border.BackgroundProperty, new Binding(path + "." + nameof(SheetGridCell.Background)));
                border.Bind(ToolTip.TipProperty, new Binding(path + "." + nameof(SheetGridCell.ToolTipOrNull)));
                return border;
            }),
        };
        return column;
    }

    private Control CreateColumnHeader(TableWorkspace workspace, Sheet sheet, int columnIndex, int headerCount)
    {
        StackPanel panel = new StackPanel { Spacing = 1, MinWidth = COLUMN_WIDTH - 16, Margin = new Thickness(0, 4, 0, 4) };
        panel.Children.Add(new TextBlock
        {
            Text = CellOrigin.GetColumnLetters(columnIndex + 1),
            Foreground = Palette.MUTED_TEXT,
            FontSize = 12,
            Margin = new Thickness(4, 0, 0, 2),
        });
        if (!_isHeaderInColumnHeader)
        {
            return panel;
        }

        for (int rowIndex = 0; rowIndex < headerCount; rowIndex++)
        {
            SheetGridCell cell = CreateCell(workspace, sheet, rowIndex, columnIndex, headerCount);
            TextBlock text = new TextBlock
            {
                Text = cell.Text.Length == 0 ? " " : cell.Text,
                Opacity = cell.Opacity,
                FontWeight = cell.Weight,
                TextTrimming = TextTrimming.CharacterEllipsis,
                Margin = new Thickness(4, 1),
            };
            Border border = new Border { Background = cell.Background, Child = text };
            if (cell.ToolTipOrNull != null)
            {
                ToolTip.SetTip(border, cell.ToolTipOrNull);
            }

            panel.Children.Add(border);
        }

        return panel;
    }

    private SheetGridCell CreateCell(TableWorkspace workspace, Sheet sheet, int rowIndex, int columnIndex, int headerCount)
    {
        Cell cell = sheet.GetCell(rowIndex, columnIndex);
        Diagnostic? diagnosticOrNull = workspace.FindDiagnosticOrNull(cell.Origin);

        int headerIndex = _isHeaderOnLeft ? columnIndex : rowIndex;
        bool bHeader = headerIndex < headerCount;
        bool bIgnored = IsIgnored(sheet, rowIndex, columnIndex, headerCount);

        IBrush background = Palette.NO_CELL;
        string? toolTipOrNull = null;
        if (diagnosticOrNull != null)
        {
            background = diagnosticOrNull.Severity == ESeverity.Error ? Palette.ERROR_CELL : Palette.WARNING_CELL;
            toolTipOrNull = ProblemItem.Describe(diagnosticOrNull);
        }
        else if (bHeader)
        {
            background = Palette.HEADER_CELL;
        }

        return new SheetGridCell(
            ToSingleLine(cell.Text),
            background,
            bIgnored ? Palette.MUTED_OPACITY : 1.0,
            bHeader && !bIgnored ? FontWeight.SemiBold : FontWeight.Normal,
            toolTipOrNull);
    }

    private bool IsIgnored(Sheet sheet, int rowIndex, int columnIndex, int headerCount)
    {
        if (_kind == ESheetKind.Ignored || _kind == ESheetKind.Invalid)
        {
            return true;
        }

        int itemIndex = _isHeaderOnLeft ? rowIndex : columnIndex;
        int lineIndex = _isHeaderOnLeft ? columnIndex : rowIndex;
        Cell typeCell = GetCell(sheet, TableSourceReader.TYPE_ROW, itemIndex);
        Cell fieldCell = GetCell(sheet, TableSourceReader.FIELD_ROW, itemIndex);
        Cell commentCell = GetCell(sheet, TableSourceReader.COMMENT_ROW, itemIndex);
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

        Cell mark = _kind == ESheetKind.Enums ? sheet.GetCell(rowIndex, columnIndex) : GetCell(sheet, lineIndex, 0);
        return SheetRole.IsIgnoreMark(mark.Text);
    }

    private Cell GetCell(Sheet sheet, int lineIndex, int itemIndex)
    {
        return _isHeaderOnLeft ? sheet.GetCell(itemIndex, lineIndex) : sheet.GetCell(lineIndex, itemIndex);
    }

    private static string ToSingleLine(string text)
    {
        string singleLine = text.Replace('\r', ' ').Replace('\n', ' ');
        return singleLine.Length <= MAX_CELL_TEXT_LENGTH ? singleLine : singleLine.Substring(0, MAX_CELL_TEXT_LENGTH) + "...";
    }
}
