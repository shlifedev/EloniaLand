using System.Text;
using Avalonia.Media;

namespace Elonia.DataManager.Gui;

internal sealed class SheetItem
{
    public Sheet Sheet { get; }

    public string Title { get; }
    public string Kind { get; }

    public string Count { get; }
    public IBrush CountBrush { get; }
    public double Opacity { get; }

    public SheetItem(Sheet sheet, bool bShowWorkbook, int errorCount, int warningCount)
    {
        ESheetKind kind = SheetRole.FromName(sheet.Name).Kind;
        Sheet = sheet;
        Title = bShowWorkbook ? sheet.WorkbookName + " · " + sheet.Name : sheet.Name;
        Kind = GetKindName(kind);
        Count = errorCount > 0 ? errorCount.ToString() : warningCount > 0 ? warningCount.ToString() : "";
        CountBrush = errorCount > 0 ? Palette.ERROR_TEXT : Palette.WARNING_TEXT;
        Opacity = kind == ESheetKind.Ignored ? Palette.MUTED_OPACITY : 1.0;
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

internal sealed class ProblemItem
{
    public Diagnostic Diagnostic { get; }
    public string Kind { get; }
    public IBrush KindBrush { get; }
    public string Location => Diagnostic.Location;
    public string Value => Diagnostic.ValueOrNull ?? "";
    public string Reason => Diagnostic.Reason;
    public string? Fix => Diagnostic.FixOrNull;

    public string Details { get; }

    public ProblemItem(Diagnostic diagnostic)
    {
        Diagnostic = diagnostic;
        bool bError = diagnostic.Severity == ESeverity.Error;
        Kind = bError ? "오류" : "경고";
        KindBrush = bError ? Palette.ERROR_TEXT : Palette.WARNING_TEXT;
        Details = Describe(diagnostic);
    }

    public static string Describe(Diagnostic diagnostic)
    {
        StringBuilder builder = new StringBuilder();
        builder.Append(diagnostic.Severity == ESeverity.Error ? "[오류] " : "[경고] ").Append(diagnostic.Location);
        if (diagnostic.ValueOrNull != null)
        {
            builder.Append("\n값: \"").Append(diagnostic.ValueOrNull).Append('"');
        }

        builder.Append("\n이유: ").Append(diagnostic.Reason);
        if (diagnostic.FixOrNull != null)
        {
            builder.Append("\n고치는 방법: ").Append(diagnostic.FixOrNull);
        }

        return builder.ToString();
    }
}
