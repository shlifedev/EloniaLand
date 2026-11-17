using Avalonia.Media;

namespace Elonia.DataManager.Gui;

internal sealed class SheetGridCell
{
    public string Text { get; }
    public IBrush Background { get; }

    public double Opacity { get; }
    public FontWeight Weight { get; }

    public string? ToolTipOrNull { get; }

    public SheetGridCell(string text, IBrush background, double opacity, FontWeight weight, string? toolTipOrNull)
    {
        Text = text;
        Background = background;
        Opacity = opacity;
        Weight = weight;
        ToolTipOrNull = toolTipOrNull;
    }
}

internal sealed class SheetGridRow
{
    public int RowIndex { get; }

    public string RowNumber { get; }
    public SheetGridCell[] Cells { get; }

    public SheetGridRow(int rowIndex, SheetGridCell[] cells)
    {
        RowIndex = rowIndex;
        RowNumber = (rowIndex + 1).ToString();
        Cells = cells;
    }
}
