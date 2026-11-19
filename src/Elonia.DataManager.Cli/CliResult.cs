using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Elonia.DataManager.Cli;

internal static class CliResult
{
    private const string SHEET_SEPARATOR = " / ";
    private static readonly JsonSerializerOptions JSON_OPTIONS = new JsonSerializerOptions
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    internal static void WriteJson(IReadOnlyList<WorkbookInfo> workbooks, DiagnosticBag diagnostics,
        CommitReport? reportOrNull, string inputDirectory)
    {
        List<object> spreadsheets = new List<object>(workbooks.Count);
        Dictionary<string, HashSet<string>> sheetNames = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        foreach (WorkbookInfo workbook in workbooks)
        {
            List<object> sheets = new List<object>(workbook.SheetNames.Count);
            HashSet<string> names = new HashSet<string>(StringComparer.Ordinal);
            foreach (string name in workbook.SheetNames)
            {
                ESheetKind kind = SheetRole.FromName(name).Kind;
                sheets.Add(new { name, kind = JsonNamingPolicy.CamelCase.ConvertName(kind.ToString()) });
                names.Add(name);
            }

            sheetNames.Add(workbook.Name, names);
            spreadsheets.Add(new { name = workbook.Name, sheets });
        }

        List<object> issues = new List<object>(diagnostics.Items.Count);
        foreach (Diagnostic diagnostic in diagnostics.Items)
        {
            string? spreadsheetOrNull = diagnostic.OriginOrNull?.Workbook;
            string? sheetOrNull = diagnostic.OriginOrNull?.Sheet;
            string location = diagnostic.Location;
            if (diagnostic.OriginOrNull == null)
            {
                int separator = location.IndexOf(SHEET_SEPARATOR, StringComparison.Ordinal);
                if (separator >= 0)
                {
                    string bookName = location.Substring(0, separator);
                    string sheetName = location.Substring(separator + SHEET_SEPARATOR.Length);
                    if (sheetNames.TryGetValue(bookName, out HashSet<string>? names) && names.Contains(sheetName))
                    {
                        spreadsheetOrNull = bookName;
                        sheetOrNull = sheetName;
                    }
                }
                else if (sheetNames.ContainsKey(location))
                {
                    spreadsheetOrNull = location;
                }
                else if (location.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase)
                    && Path.GetFileName(location) == location && File.Exists(Path.Combine(inputDirectory, location)))
                {
                    spreadsheetOrNull = location;
                    sheetNames.Add(location, new HashSet<string>(StringComparer.Ordinal));
                    spreadsheets.Add(new { name = location, sheets = Array.Empty<object>() });
                }
            }

            issues.Add(new
            {
                severity = diagnostic.Severity == ESeverity.Error ? "error" : "warning",
                location,
                spreadsheet = spreadsheetOrNull,
                sheet = sheetOrNull,
                address = diagnostic.OriginOrNull?.Address,
                value = diagnostic.ValueOrNull,
                reason = diagnostic.Reason,
                fix = diagnostic.FixOrNull
            });
        }

        object? reportOrNullForJson = reportOrNull == null ? null : new
        {
            written = reportOrNull.Written,
            deleted = reportOrNull.Deleted,
            unchanged = reportOrNull.UnchangedCount
        };
        Console.WriteLine(JsonSerializer.Serialize(new { spreadsheets, diagnostics = issues, report = reportOrNullForJson }, JSON_OPTIONS));
    }
}
