using System;
using System.Collections.Generic;

namespace Elonia.DataManager
{
    public enum ESeverity
    {
        Warning,

        Error
    }

    public sealed class Diagnostic
    {
        public ESeverity Severity { get; }

        public string Location { get; }

        public CellOrigin? OriginOrNull { get; }

        public string? ValueOrNull { get; }

        public string Reason { get; }

        public string? FixOrNull { get; }

        public Diagnostic(ESeverity severity, string location, CellOrigin? originOrNull, string? valueOrNull, string reason, string? fixOrNull)
        {
            Severity = severity;
            Location = location;
            OriginOrNull = originOrNull;
            ValueOrNull = valueOrNull;
            Reason = reason;
            FixOrNull = fixOrNull;
        }
    }

    public sealed class DiagnosticBag
    {
        private readonly List<Diagnostic> _items = new List<Diagnostic>();

        public IReadOnlyList<Diagnostic> Items => _items;
        public int ErrorCount { get; private set; }
        public int WarningCount { get; private set; }
        public bool HasErrors => ErrorCount > 0;

        public void AddError(Cell cell, string reason, string? fixOrNull = null)
        {
            Add(new Diagnostic(ESeverity.Error, cell.Origin.ToString(), cell.Origin, cell.Text, reason, fixOrNull));
        }

        public void AddError(string location, string reason, string? fixOrNull = null)
        {
            Add(new Diagnostic(ESeverity.Error, location, null, null, reason, fixOrNull));
        }

        public void AddWarning(Cell cell, string reason, string? fixOrNull = null)
        {
            Add(new Diagnostic(ESeverity.Warning, cell.Origin.ToString(), cell.Origin, cell.Text, reason, fixOrNull));
        }

        public void AddWarning(string location, string reason, string? fixOrNull = null)
        {
            Add(new Diagnostic(ESeverity.Warning, location, null, null, reason, fixOrNull));
        }

        private void Add(Diagnostic diagnostic)
        {
            if (diagnostic == null)
            {
                throw new ArgumentNullException(nameof(diagnostic));
            }

            _items.Add(diagnostic);
            if (diagnostic.Severity == ESeverity.Error)
            {
                ErrorCount++;
            }
            else
            {
                WarningCount++;
            }
        }
    }
}
